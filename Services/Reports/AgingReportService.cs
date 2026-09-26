using CompanyERP.Data;
using CompanyERP.Entities.Payment;
using CompanyERP.Interfaces.Services;
using CompanyERP.ViewModels.Accounting;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Reports;

public class AgingReportService : IAgingReportService
{
    private readonly ApplicationDbContext _db;

    public AgingReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AgingReportViewModel> GetReceivablesAsync(int companyId, DateTime asOfDate, int? branchId = null, int? partyId = null)
    {
        var invoices = await _db.SalesInvoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Where(i => i.CompanyId == companyId)
            .ToListAsync();

        var parties = await _db.Customers
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var payments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId && p.Category == PaymentCategory.Customer && p.CustomerId != null)
            .ToListAsync();

        var rows = new List<AgingInvoiceRowViewModel>();

        var partyRows = new Dictionary<int, List<AgingInvoiceRowViewModel>>();
        foreach (var party in parties)
        {
            if (partyId.HasValue && party.Id != partyId.Value)
            {
                continue;
            }

            var partyInvoices = invoices
                .Where(i => i.CustomerId == party.Id)
                .Where(i => !branchId.HasValue || i.BranchId == branchId.Value)
                .ToList();

            var debt = new List<AgingInvoiceRowViewModel>();
            foreach (var invoice in partyInvoices)
            {
                var total = invoice.Lines.Sum(l => l.Quantity * l.UnitPrice);
                var paid = Math.Min(invoice.AmountPaid, total);
                if (total <= 0.004m)
                {
                    continue;
                }

                debt.Add(new AgingInvoiceRowViewModel
                {
                    Reference = invoice.InvoiceNo,
                    Description = invoice.Status.ToString(),
                    SourceType = "Invoice",
                    EffectiveDate = invoice.DueDate ?? invoice.InvoiceDate,
                    DueDate = invoice.DueDate,
                    Total = total,
                    Paid = paid,
                    Outstanding = Math.Max(0, total - paid)
                });
            }

            var earliest = partyInvoices.Count == 0
                ? asOfDate.Date
                : partyInvoices.Min(i => i.DueDate ?? i.InvoiceDate);
            var openingDate = party.OpeningBalanceDate ?? earliest;
            if (party.OpeningReceivable > 0.004m)
            {
                debt.Add(new AgingInvoiceRowViewModel
                {
                    Reference = "OPENING",
                    Description = "Opening Receivable",
                    SourceType = "Opening",
                    EffectiveDate = openingDate,
                    Total = party.OpeningReceivable,
                    Paid = 0,
                    Outstanding = party.OpeningReceivable
                });
            }

            var paidPool = payments
                .Where(p => p.CustomerId == party.Id)
                .Where(p => !branchId.HasValue || p.BranchId == branchId.Value)
                .Sum(p => p.Amount);

            AllocatePaid(debt, paidPool, asOfDate);

            var partyLine = debt.Where(d => d.Outstanding > 0.004m).OrderBy(d => d.EffectiveDate)
                .Select(d =>
                {
                    d.PartyId = party.Id;
                    d.PartyCode = party.CustomerCode;
                    d.PartyName = party.Name;
                    return d;
                })
                .ToList();
            if (partyLine.Count > 0 || (partyId.HasValue && party.Id == partyId.Value))
            {
                partyRows[party.Id] = partyLine;
            }

            rows.AddRange(partyLine);
        }

        return BuildReport(rows, partyRows, true, asOfDate, branchId, partyId);
    }

    public async Task<AgingReportViewModel> GetPayablesAsync(int companyId, DateTime asOfDate, int? branchId = null, int? partyId = null)
    {
        var invoices = await _db.PurchaseInvoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Where(i => i.CompanyId == companyId)
            .ToListAsync();

        var parties = await _db.Suppliers
            .AsNoTracking()
            .Where(s => s.CompanyId == companyId)
            .OrderBy(s => s.Name)
            .ToListAsync();

        var payments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId && p.Category == PaymentCategory.Supplier && p.SupplierId != null)
            .ToListAsync();

        var rows = new List<AgingInvoiceRowViewModel>();
        var partyRows = new Dictionary<int, List<AgingInvoiceRowViewModel>>();
        foreach (var party in parties)
        {
            if (partyId.HasValue && party.Id != partyId.Value)
            {
                continue;
            }

            var partyInvoices = invoices
                .Where(i => i.SupplierId == party.Id)
                .Where(i => !branchId.HasValue || i.BranchId == branchId.Value)
                .ToList();

            var debt = new List<AgingInvoiceRowViewModel>();
            foreach (var invoice in partyInvoices)
            {
                var total = invoice.Lines.Sum(l => l.Quantity * l.UnitPrice);
                if (total <= 0.004m)
                {
                    continue;
                }

                debt.Add(new AgingInvoiceRowViewModel
                {
                    Reference = invoice.InvoiceNo,
                    Description = invoice.Status.ToString(),
                    SourceType = "Invoice",
                    EffectiveDate = invoice.DueDate ?? invoice.InvoiceDate,
                    DueDate = invoice.DueDate,
                    Total = total,
                    Paid = 0,
                    Outstanding = total
                });
            }

            var earliest = partyInvoices.Count == 0
                ? asOfDate.Date
                : partyInvoices.Min(i => i.DueDate ?? i.InvoiceDate);
            var openingDate = party.OpeningBalanceDate ?? earliest;
            if (party.OpeningPayable > 0.004m)
            {
                debt.Add(new AgingInvoiceRowViewModel
                {
                    Reference = "OPENING",
                    Description = "Opening Payable",
                    SourceType = "Opening",
                    EffectiveDate = openingDate,
                    Total = party.OpeningPayable,
                    Paid = 0,
                    Outstanding = party.OpeningPayable
                });
            }

            var paidPool = payments
                .Where(p => p.SupplierId == party.Id)
                .Where(p => !branchId.HasValue || p.BranchId == branchId.Value)
                .Sum(p => p.Amount);

            AllocatePaid(debt, paidPool, asOfDate);

            var partyLine = debt.Where(d => d.Outstanding > 0.004m).OrderBy(d => d.EffectiveDate)
                .Select(d =>
                {
                    d.PartyId = party.Id;
                    d.PartyCode = party.SupplierCode;
                    d.PartyName = party.Name;
                    return d;
                })
                .ToList();
            if (partyLine.Count > 0 || (partyId.HasValue && party.Id == partyId.Value))
            {
                partyRows[party.Id] = partyLine;
            }

            rows.AddRange(partyLine);
        }

        return BuildReport(rows, partyRows, false, asOfDate, branchId, partyId);
    }

    private void AllocatePaid(List<AgingInvoiceRowViewModel> debt, decimal paidPool, DateTime asOfDate)
    {
        foreach (var row in debt.OrderBy(d => d.EffectiveDate))
        {
            row.Outstanding = Math.Max(0, Math.Round(row.Outstanding, 2));
        }

        var ordered = debt.OrderBy(d => d.EffectiveDate).ToList();
        var remaining = paidPool;
        foreach (var row in ordered)
        {
            if (remaining <= 0.004m)
            {
                break;
            }

            var reduce = Math.Min(row.Outstanding, remaining);
            row.Paid = Math.Round(row.Paid + reduce, 2);
            row.Outstanding = Math.Max(0, Math.Round(row.Outstanding - reduce, 2));
            remaining = Math.Max(0, Math.Round(remaining - reduce, 2));
        }

        foreach (var row in debt)
        {
            SetBucket(row, asOfDate);
        }
    }

    private static void SetBucket(AgingInvoiceRowViewModel row, DateTime asOfDate)
    {
        var effective = row.EffectiveDate ?? asOfDate.Date;
        var days = (asOfDate.Date - effective.Date).Days;
        row.DaysOverdue = days;
        row.Bucket = days <= 0
            ? AgingBucket.Current
            : days <= 30
                ? AgingBucket.Days1To30
                : days <= 60
                    ? AgingBucket.Days31To60
                    : days <= 90
                        ? AgingBucket.Days61To90
                        : AgingBucket.Days90Plus;
    }

    private static AgingReportViewModel BuildReport(
        List<AgingInvoiceRowViewModel> rows,
        Dictionary<int, List<AgingInvoiceRowViewModel>> partyRows,
        bool isReceivables,
        DateTime asOfDate,
        int? branchId,
        int? partyId)
    {
        var vm = new AgingReportViewModel
        {
            IsReceivables = isReceivables,
            Title = isReceivables ? "Receivable Aging" : "Payable Aging",
            PartyCaption = isReceivables ? "Customer" : "Supplier",
            AsOfDate = asOfDate.Date,
            BranchId = branchId,
            PartyId = partyId
        };

        var summary = new Dictionary<int, AgingSummaryRowViewModel>();
        foreach (var row in rows)
        {
            if (!summary.TryGetValue(row.PartyId, out var line))
            {
                line = new AgingSummaryRowViewModel
                {
                    PartyId = row.PartyId,
                    PartyCode = row.PartyCode,
                    PartyName = row.PartyName
                };
                summary[row.PartyId] = line;
            }

            switch (row.Bucket)
            {
                case AgingBucket.Current:
                    line.Current = Math.Round(line.Current + row.Outstanding, 2);
                    break;
                case AgingBucket.Days1To30:
                    line.Days1To30 = Math.Round(line.Days1To30 + row.Outstanding, 2);
                    break;
                case AgingBucket.Days31To60:
                    line.Days31To60 = Math.Round(line.Days31To60 + row.Outstanding, 2);
                    break;
                case AgingBucket.Days61To90:
                    line.Days61To90 = Math.Round(line.Days61To90 + row.Outstanding, 2);
                    break;
                default:
                    line.Days90Plus = Math.Round(line.Days90Plus + row.Outstanding, 2);
                    break;
            }
        }

        foreach (var line in summary.Values.OrderBy(s => s.PartyName))
        {
            vm.TotalCurrent = Math.Round(vm.TotalCurrent + line.Current, 2);
            vm.TotalDays1To30 = Math.Round(vm.TotalDays1To30 + line.Days1To30, 2);
            vm.TotalDays31To60 = Math.Round(vm.TotalDays31To60 + line.Days31To60, 2);
            vm.TotalDays61To90 = Math.Round(vm.TotalDays61To90 + line.Days61To90, 2);
            vm.TotalDays90Plus = Math.Round(vm.TotalDays90Plus + line.Days90Plus, 2);
        }

        vm.Rows = summary.Values.OrderBy(s => s.PartyName).ToList();

        if (partyId.HasValue && partyRows.TryGetValue(partyId.Value, out var detail))
        {
            vm.Invoices = detail;
        }

        return vm;
    }
}