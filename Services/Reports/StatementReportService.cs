using CompanyERP.Data;
using CompanyERP.Entities.Payment;
using CompanyERP.Interfaces.Services;
using CompanyERP.ViewModels.Accounting;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Reports;

public class StatementReportService : IStatementReportService
{
    private readonly ApplicationDbContext _db;

    public StatementReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<StatementViewModel> GetCustomerStatementAsync(int companyId, int customerId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var customer = await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId && c.CompanyId == companyId);
        if (customer is null)
        {
            return new StatementViewModel { IsCustomer = true, CompanyId = companyId, PartyId = customerId, FromDate = fromDate, ToDate = toDate };
        }

        var invoices = await _db.SalesInvoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Where(i => i.CompanyId == companyId && i.CustomerId == customerId)
            .ToListAsync();

        var payments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId && p.Category == PaymentCategory.Customer && p.CustomerId == customerId)
            .ToListAsync();

        var debits = invoices.Select(i => new StatementDebit
        {
            Date = i.InvoiceDate,
            Reference = i.InvoiceNo,
            Description = $"Sales Invoice ({i.Status})",
            Total = Math.Round(i.Lines.Sum(l => l.Quantity * l.UnitPrice), 2),
            Paid = Math.Round(Math.Min(i.AmountPaid, i.Lines.Sum(l => l.Quantity * l.UnitPrice)), 2)
        }).ToList();

        var credits = payments.Select(p => new StatementCredit
        {
            Date = p.PaymentDate,
            Reference = p.PaymentNo,
            Description = $"Payment ({p.PaymentMethod?.Name ?? "Payment"})",
            Amount = Math.Round(p.Amount, 2)
        }).ToList();

        return BuildStatement(
            isCustomer: true,
            party: (customer.Id, customer.CustomerCode, customer.Name),
            openingBalance: customer.OpeningReceivable,
            openingDate: customer.OpeningBalanceDate,
            debits: debits,
            credits: credits,
            companyId: companyId,
            fromDate: fromDate,
            toDate: toDate);
    }

    public async Task<StatementViewModel> GetSupplierStatementAsync(int companyId, int supplierId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var supplier = await _db.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == supplierId && s.CompanyId == companyId);
        if (supplier is null)
        {
            return new StatementViewModel { IsCustomer = false, CompanyId = companyId, PartyId = supplierId, FromDate = fromDate, ToDate = toDate };
        }

        var invoices = await _db.PurchaseInvoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Where(i => i.CompanyId == companyId && i.SupplierId == supplierId)
            .ToListAsync();

        var payments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId && p.Category == PaymentCategory.Supplier && p.SupplierId == supplierId)
            .ToListAsync();

        var debits = invoices.Select(i => new StatementDebit
        {
            Date = i.InvoiceDate,
            Reference = i.InvoiceNo,
            Description = $"Purchase Invoice ({i.Status})",
            Total = Math.Round(i.Lines.Sum(l => l.Quantity * l.UnitPrice), 2),
            Paid = 0
        }).ToList();

        var credits = payments.Select(p => new StatementCredit
        {
            Date = p.PaymentDate,
            Reference = p.PaymentNo,
            Description = $"Payment ({p.PaymentMethod?.Name ?? "Payment"})",
            Amount = Math.Round(p.Amount, 2)
        }).ToList();

        return BuildStatement(
            isCustomer: false,
            party: (supplier.Id, supplier.SupplierCode, supplier.Name),
            openingBalance: supplier.OpeningPayable,
            openingDate: supplier.OpeningBalanceDate,
            debits: debits,
            credits: credits,
            companyId: companyId,
            fromDate: fromDate,
            toDate: toDate);
    }

    private static StatementViewModel BuildStatement(
        bool isCustomer,
        (int Id, string Code, string Name) party,
        decimal openingBalance,
        DateTime? openingDate,
        List<StatementDebit> debits,
        List<StatementCredit> credits,
        int companyId,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var from = fromDate?.Date ?? DateTime.MinValue.Date;
        var to = toDate?.Date ?? DateTime.MaxValue.Date;

        var lines = new List<StatementLineViewModel>();
        decimal opening = 0;

        if (from > DateTime.MinValue.Date)
        {
            foreach (var d in debits.Where(d => d.Date < from))
            {
                opening += d.Total;
                opening -= d.Paid;
            }

            foreach (var c in credits.Where(c => c.Date < from))
            {
                opening -= c.Amount;
            }

            if (openingDate.HasValue && openingDate.Value.Date >= from)
            {
                lines.Add(new StatementLineViewModel
                {
                    Date = openingDate.Value.Date,
                    Reference = "OPENING",
                    Type = "Opening",
                    Description = isCustomer ? "Opening Receivable" : "Opening Payable",
                    Invoice = openingBalance
                });
            }
            else
            {
                opening += openingBalance;
            }
        }
        else
        {
            var first = debits.Count > 0 || credits.Count > 0
                ? debits.Select(d => d.Date).Concat(credits.Select(c => c.Date)).Min()
                : (DateTime?)null;
            var openLineDate = openingDate ?? first ?? DateTime.Today;
            opening = 0;
            lines.Add(new StatementLineViewModel
            {
                Date = openLineDate,
                Reference = "OPENING",
                Type = "Opening",
                Description = isCustomer ? "Opening Receivable" : "Opening Payable",
                Invoice = openingBalance
            });
        }

        foreach (var d in debits.Where(d => d.Date >= from && d.Date <= to))
        {
            lines.Add(new StatementLineViewModel
            {
                Date = d.Date,
                Reference = d.Reference,
                Type = "Invoice",
                Description = d.Description,
                Invoice = d.Total,
                Payment = 0
            });

            if (d.Paid > 0.004m)
            {
                lines.Add(new StatementLineViewModel
                {
                    Date = d.Date,
                    Reference = d.Reference,
                    Type = "Payment",
                    Description = "Invoice payment",
                    Payment = d.Paid
                });
            }
        }

        foreach (var c in credits.Where(c => c.Date >= from && c.Date <= to))
        {
            lines.Add(new StatementLineViewModel
            {
                Date = c.Date,
                Reference = c.Reference,
                Type = "Payment",
                Description = c.Description,
                Payment = c.Amount
            });
        }

        lines = lines
            .OrderBy(l => l.Date)
            .ThenBy(l => l.Type == "Opening" ? 0 : l.Type == "Invoice" ? 1 : 2)
            .ThenBy(l => l.Reference)
            .ToList();

        decimal balance = Math.Round(opening, 2);
        foreach (var line in lines)
        {
            balance = Math.Round(balance + line.Invoice - line.Payment, 2);
            line.Balance = balance;
        }

        return new StatementViewModel
        {
            IsCustomer = isCustomer,
            Title = isCustomer ? "Customer Statement" : "Supplier Statement",
            PartyCaption = isCustomer ? "Customer" : "Supplier",
            CompanyId = companyId,
            PartyId = party.Id,
            PartyCode = party.Code,
            PartyName = party.Name,
            FromDate = fromDate,
            ToDate = toDate,
            OpeningBalance = Math.Round(opening, 2),
            Lines = lines,
            TotalInvoice = Math.Round(lines.Where(l => l.Type != "Opening").Sum(l => l.Invoice), 2),
            TotalPayment = Math.Round(lines.Where(l => l.Type != "Opening").Sum(l => l.Payment), 2),
            ClosingBalance = Math.Max(0m, Math.Round(lines.Count > 0 ? lines[^1].Balance : opening, 2))
        };
    }

    private class StatementDebit
    {
        public DateTime Date { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
    }

    private class StatementCredit
    {
        public DateTime Date { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}