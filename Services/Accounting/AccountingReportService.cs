using CompanyERP.Data;
using CompanyERP.Entities.Accounting;
using CompanyERP.Entities.Asset;
using CompanyERP.Entities.Payment;
using CompanyERP.Interfaces.Services;
using CompanyERP.ViewModels.Accounting;
using CompanyERP.ViewModels.Reports;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Accounting;

public class AccountingReportService : IAccountingReportService
{
    private readonly ApplicationDbContext _db;

    public AccountingReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    private async Task<List<(JournalEntryDetail D, ChartOfAccount Account, JournalEntry Entry)>> GetDetailsAsync(
        int companyId, DateTime? fromDate = null, DateTime? toDate = null, int? branchId = null)
    {
        var query = _db.JournalEntryDetails
            .AsNoTracking()
            .Where(d => d.Account!.CompanyId == companyId);

        if (branchId.HasValue)
        {
            query = query.Where(d => d.JournalEntry!.BranchId == branchId.Value);
        }

        if (fromDate.HasValue || toDate.HasValue)
        {
            query = query.Where(d => d.JournalEntry != null);
            if (fromDate.HasValue)
            {
                query = query.Where(d => d.JournalEntry!.EntryDate.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(d => d.JournalEntry!.EntryDate.Date <= toDate.Value.Date);
            }
        }

        var rows = await query
            .Include(d => d.Account)
            .Include(d => d.JournalEntry)
            .ToListAsync();

        return rows.Select(d => (d, d.Account!, d.JournalEntry!)).ToList();
    }

    public async Task<TrialBalanceViewModel> GetTrialBalanceAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var rows = await GetDetailsAsync(companyId, fromDate, toDate);
        var postings = rows.Select(r => (r.D.Debit, r.D.Credit, r.D.AccountId)).ToList();
        var roots = await BuildReportTreeAsync(companyId, postings);

        var lines = new List<TrialBalanceLineViewModel>();
        var totalDebit = 0m;
        var totalCredit = 0m;

        foreach (var root in roots)
        {
            AppendTbNode(root, 0);
        }

        var vm = new TrialBalanceViewModel
        {
            Lines = lines,
            TotalDebit = Math.Round(totalDebit, 2),
            TotalCredit = Math.Round(totalCredit, 2)
        };
        return vm;

        void AppendTbNode(ReportAccountNode node, int depth)
        {
            var (debit, credit) = NodeTotals(node);
            var isSubtotal = node.Children.Count > 0;
            lines.Add(new TrialBalanceLineViewModel
            {
                AccountId = node.Account.Id,
                AccountCode = node.Account.AccountCode,
                AccountName = node.Account.AccountName,
                AccountType = node.Account.AccountType,
                Debit = Math.Round(debit, 2),
                Credit = Math.Round(credit, 2),
                Depth = depth,
                IsSubtotal = isSubtotal
            });

            if (depth == 0)
            {
                totalDebit += debit;
                totalCredit += credit;
            }

            foreach (var child in node.Children.OrderBy(c => c.Account.AccountCode))
            {
                AppendTbNode(child, depth + 1);
            }
        }
    }

    public async Task<LedgerViewModel> GetLedgerAsync(int companyId, int? accountId = null, DateTime? fromDate = null, DateTime? toDate = null, int? branchId = null)
    {
        var rows = await GetDetailsAsync(companyId, fromDate, toDate, branchId);
        var groups = rows.GroupBy(r => r.Account.Id);

        var query = _db.ChartOfAccounts.AsNoTracking().Where(a => a.CompanyId == companyId);
        if (accountId.HasValue)
        {
            query = query.Where(a => a.Id == accountId.Value);
        }

        var accounts = await query
            .OrderBy(a => a.AccountCode)
            .ToListAsync();

        var vm = new LedgerViewModel();
        var lineRows = new List<LedgerLineViewModel>();

        foreach (var account in accounts)
        {
            decimal opening = account.NormalBalance == AccountNormalBalance.Debit
                ? account.OpeningBalance
                : -account.OpeningBalance;

            var detailRows = groups.FirstOrDefault(g => g.Key == account.Id)?.ToList() ?? [];
            decimal running = opening;
            var accountLines = new List<LedgerLineViewModel>();

            foreach (var detail in detailRows.OrderBy(r => r.Entry.EntryDate).ThenBy(r => r.Entry.Id))
            {
                if (account.NormalBalance == AccountNormalBalance.Debit)
                {
                    running += detail.D.Debit - detail.D.Credit;
                }
                else
                {
                    running += detail.D.Credit - detail.D.Debit;
                }

                accountLines.Add(new LedgerLineViewModel
                {
                    Date = detail.Entry.EntryDate,
                    EntryNo = detail.Entry.EntryNo,
                    SourceModule = detail.Entry.SourceModule,
                    SourceReference = detail.Entry.SourceReference,
                    Description = detail.Entry.Description,
                    Debit = detail.D.Debit,
                    Credit = detail.D.Credit,
                    Balance = Math.Round(running, 2)
                });
            }

            vm.OpeningBalance = Math.Round(vm.OpeningBalance + opening, 2);
            vm.TotalDebit += Math.Round(detailRows.Sum(r => r.D.Debit), 2);
            vm.TotalCredit += Math.Round(detailRows.Sum(r => r.D.Credit), 2);
            lineRows.AddRange(accountLines);
        }

        vm.AccountId = accountId;
        vm.AccountDisplay = accounts.Count == 1
            ? $"{accounts[0].AccountCode} - {accounts[0].AccountName}"
            : null;
        vm.BranchId = branchId;
        vm.BranchName = branchId.HasValue
            ? await _db.Branches.Where(b => b.Id == branchId.Value).Select(b => b.Name).FirstOrDefaultAsync()
            : null;
        vm.Lines = lineRows
            .OrderBy(l => l.Date)
            .ThenBy(l => l.EntryNo)
            .ToList();
        vm.ClosingBalance = Math.Round(vm.OpeningBalance + vm.TotalDebit - vm.TotalCredit, 2);
        vm.TotalDebit = Math.Round(vm.TotalDebit, 2);
        vm.TotalCredit = Math.Round(vm.TotalCredit, 2);
        return vm;
    }

    public async Task<CashBookViewModel> GetCashBookAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        return await GetMoneyBookAsync(companyId, "1000", fromDate, toDate);
    }

    public async Task<CashBookViewModel> GetBankBookAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        return await GetMoneyBookAsync(companyId, "1100", fromDate, toDate);
    }

    private async Task<CashBookViewModel> GetMoneyBookAsync(int companyId, string code, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var account = await _db.ChartOfAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == code);

        if (account is null)
        {
            return new CashBookViewModel { AccountCode = code, AccountName = "(empty)" };
        }

        var rows = (await GetDetailsAsync(companyId, fromDate, toDate))
            .Where(r => r.Account.Id == account.Id)
            .OrderBy(r => r.Entry.EntryDate)
            .ThenBy(r => r.Entry.Id)
            .ToList();

        decimal opening = account.NormalBalance == AccountNormalBalance.Debit
            ? account.OpeningBalance
            : -account.OpeningBalance;

        var lines = new List<CashBookLineViewModel>();
        decimal running = opening;
        foreach (var row in rows)
        {
            running += row.D.Debit - row.D.Credit;
            lines.Add(new CashBookLineViewModel
            {
                Date = row.Entry.EntryDate,
                EntryNo = row.Entry.EntryNo,
                SourceModule = row.Entry.SourceModule,
                SourceReference = row.Entry.SourceReference,
                Description = row.Entry.Description,
                Debit = row.D.Debit,
                Credit = row.D.Credit,
                Balance = Math.Round(running, 2)
            });
        }

        return new CashBookViewModel
        {
            AccountCode = account.AccountCode,
            AccountName = account.AccountName,
            OpeningBalance = Math.Round(opening, 2),
            Lines = lines,
            TotalIn = Math.Round(rows.Sum(r => r.D.Debit), 2),
            TotalOut = Math.Round(rows.Sum(r => r.D.Credit), 2),
            ClosingBalance = Math.Round(opening + rows.Sum(r => r.D.Debit - r.D.Credit), 2)
        };
    }

    public async Task<ProfitAndLossViewModel> GetProfitAndLossAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var rows = await GetDetailsAsync(companyId, fromDate, toDate);
        var postings = rows.Select(r => (r.D.Debit, r.D.Credit, r.D.AccountId)).ToList();
        var roots = await BuildReportTreeAsync(companyId, postings);

        var vm = new ProfitAndLossViewModel { FromDate = fromDate, ToDate = toDate };

        foreach (var root in roots)
        {
            AppendPnlNode(root, 0, vm.Revenues, AccountType.Revenue);
            AppendPnlNode(root, 0, vm.Expenses, AccountType.Expense);
        }

        vm.Revenues = vm.Revenues.OrderBy(r => r.AccountCode).ToList();
        vm.Expenses = vm.Expenses.OrderBy(e => e.AccountCode).ToList();

        var revenueRoot = roots.FirstOrDefault(r => r.Account.AccountType == AccountType.Revenue);
        var expenseRoot = roots.FirstOrDefault(r => r.Account.AccountType == AccountType.Expense);
        vm.TotalRevenue = Math.Round(revenueRoot is null ? 0 : NodeAmount(revenueRoot), 2);
        vm.TotalExpense = Math.Round(expenseRoot is null ? 0 : NodeAmount(expenseRoot), 2);
        return vm;

        void AppendPnlNode(ReportAccountNode node, int depth, List<ProfitAndLossLineViewModel> target, AccountType wantedType)
        {
            var include = node.Account.AccountType == wantedType;
            if (include)
            {
                target.Add(new ProfitAndLossLineViewModel
                {
                    AccountCode = node.Account.AccountCode,
                    AccountName = node.Account.AccountName,
                    Amount = Math.Round(NodeAmount(node), 2),
                    Depth = depth,
                    IsSubtotal = node.Children.Count > 0
                });
            }

            foreach (var child in node.Children.OrderBy(c => c.Account.AccountCode))
            {
                AppendPnlNode(child, include ? depth + 1 : depth, target, wantedType);
            }
        }
    }

    public async Task<BalanceSheetViewModel> GetBalanceSheetAsync(int companyId, DateTime? asOfDate = null)
    {
        var rows = await GetDetailsAsync(companyId, null, asOfDate);
        var postings = rows.Select(r => (r.D.Debit, r.D.Credit, r.D.AccountId)).ToList();
        var roots = await BuildReportTreeAsync(companyId, postings);

        var vm = new BalanceSheetViewModel { AsOfDate = asOfDate };

        foreach (var root in roots)
        {
            var amount = Math.Round(NodeAmount(root), 2);
            switch (root.Account.AccountType)
            {
                case AccountType.Asset:
                    AppendBsNode(root, 0, vm.Assets);
                    vm.TotalAssets += amount;
                    break;

                case AccountType.Liability:
                    AppendBsNode(root, 0, vm.Liabilities);
                    vm.TotalLiabilities += amount;
                    break;

                case AccountType.Equity:
                    AppendBsNode(root, 0, vm.Equity);
                    vm.TotalEquity += amount;
                    break;
            }
        }

        vm.TotalAssets = Math.Round(vm.TotalAssets, 2);
        vm.TotalLiabilities = Math.Round(vm.TotalLiabilities, 2);
        vm.TotalEquity = Math.Round(vm.TotalEquity, 2);
        var pnl = await GetProfitAndLossAsync(companyId, null, asOfDate);
        vm.NetProfit = pnl.NetProfit;
        return vm;

        void AppendBsNode(ReportAccountNode node, int depth, List<BalanceSheetLineViewModel> target)
        {
            target.Add(new BalanceSheetLineViewModel
            {
                AccountCode = node.Account.AccountCode,
                AccountName = node.Account.AccountName,
                Amount = Math.Round(NodeAmount(node), 2),
                Depth = depth,
                IsSubtotal = node.Children.Count > 0
            });

            foreach (var child in node.Children.OrderBy(c => c.Account.AccountCode))
            {
                AppendBsNode(child, depth + 1, target);
            }
        }
    }

    public async Task<ReceivablesPayablesViewModel> GetReceivablesPayablesAsync(int companyId)
    {
        var rows = await GetDetailsAsync(companyId);
        var vm = new ReceivablesPayablesViewModel();

        foreach (var account in await _db.ChartOfAccounts
                     .AsNoTracking()
                     .Where(a => a.CompanyId == companyId && (a.AccountCode == "1200" || a.AccountCode == "2000"))
                     .OrderBy(a => a.AccountCode)
                     .ToListAsync())
        {
            var group = rows.Where(r => r.Account.Id == account.Id).ToList();
            var opening = account.NormalBalance == AccountNormalBalance.Debit
                ? account.OpeningBalance
                : -account.OpeningBalance;
            var debit = group.Sum(r => r.D.Debit);
            var credit = group.Sum(r => r.D.Credit);

            var line = new ReceivablePayableLineViewModel
            {
                AccountCode = account.AccountCode,
                AccountName = account.AccountName,
                Opening = Math.Round(opening, 2),
                Debit = Math.Round(debit, 2),
                Credit = Math.Round(credit, 2),
                Closing = Math.Round(opening + debit - credit, 2)
            };

            if (account.AccountCode == "1200")
            {
                vm.Receivables.Add(line);
                vm.TotalReceivables += line.Closing;
            }
            else
            {
                vm.Payables.Add(line);
                vm.TotalPayables += line.Closing;
            }
        }

        vm.TotalReceivables = Math.Round(vm.TotalReceivables, 2);
        vm.TotalPayables = Math.Round(vm.TotalPayables, 2);
        return vm;
    }

    private sealed class ReportAccountNode
    {
        public ChartOfAccount Account { get; init; } = null!;
        public List<ReportAccountNode> Children { get; } = [];
        public List<(decimal Debit, decimal Credit)> Postings { get; } = [];
        public int Depth { get; set; }
    }

    private async Task<List<ReportAccountNode>> BuildReportTreeAsync(int companyId, List<(decimal Debit, decimal Credit, int AccountId)> postings)
    {
        var accounts = await _db.ChartOfAccounts
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId)
            .ToListAsync();

        var idSet = accounts.Select(a => a.Id).ToHashSet();
        var nodes = accounts.ToDictionary(a => a.Id, a => new ReportAccountNode { Account = a });

        foreach (var (debit, credit, accountId) in postings)
        {
            if (nodes.TryGetValue(accountId, out var node))
            {
                node.Postings.Add((debit, credit));
            }
        }

        foreach (var account in accounts)
        {
            if (account.ParentId.HasValue && idSet.Contains(account.ParentId.Value))
            {
                nodes[account.ParentId.Value].Children.Add(nodes[account.Id]);
            }
        }

        var roots = accounts
            .Where(a => !a.ParentId.HasValue || !idSet.Contains(a.ParentId!.Value))
            .OrderBy(a => a.AccountCode)
            .Select(a => nodes[a.Id])
            .ToList();

        foreach (var root in roots)
        {
            AssignDepth(root, 0);
        }

        return roots;

        void AssignDepth(ReportAccountNode node, int depth)
        {
            node.Depth = depth;
            foreach (var child in node.Children)
            {
                AssignDepth(child, depth + 1);
            }
        }
    }

    private static (decimal Debit, decimal Credit) NodeTotals(ReportAccountNode node)
    {
        var debit = node.Postings.Sum(p => p.Debit);
        var credit = node.Postings.Sum(p => p.Credit);
        foreach (var child in node.Children)
        {
            var (d, c) = NodeTotals(child);
            debit += d;
            credit += c;
        }

        return (debit, credit);
    }

    private static decimal NodeAmount(ReportAccountNode node)
    {
        var isDebitNormal = node.Account.NormalBalance == AccountNormalBalance.Debit;

        var net = node.Postings.Sum(p => p.Debit - p.Credit);
        decimal amount;
        if (node.Children.Count == 0)
        {
            amount = isDebitNormal
                ? node.Account.OpeningBalance + net
                : -(node.Account.OpeningBalance + net);
        }
        else
        {
            amount = 0m;
            if (isDebitNormal)
            {
                amount += node.Account.OpeningBalance;
            }
            else
            {
                amount -= node.Account.OpeningBalance;
            }

            foreach (var child in node.Children)
            {
                amount += NodeAmount(child);
            }
        }

        return amount;
    }

    public async Task<GeneralJournalViewModel> GetGeneralJournalAsync(int companyId, DateTime? fromDate, DateTime? toDate, int? branchId = null)
    {
        var rows = await GetDetailsAsync(companyId, fromDate, toDate, branchId);

        var lines = rows
            .OrderBy(r => r.Entry.EntryDate)
            .ThenBy(r => r.Entry.EntryNo)
            .Select(r => new GeneralJournalLineRow
            {
                Date = r.Entry.EntryDate,
                EntryNo = r.Entry.EntryNo,
                Description = r.Entry.Description,
                SourceModule = r.Entry.SourceModule,
                SourceReference = r.Entry.SourceReference,
                AccountCode = r.Account.AccountCode,
                AccountName = r.Account.AccountName,
                Debit = Math.Round(r.D.Debit, 2),
                Credit = Math.Round(r.D.Credit, 2)
            })
            .ToList();

        return new GeneralJournalViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            BranchId = branchId,
            Rows = lines,
            TotalDebit = Math.Round(lines.Sum(l => l.Debit), 2),
            TotalCredit = Math.Round(lines.Sum(l => l.Credit), 2)
        };
    }

    public async Task<ComparativePandLViewModel> GetComparativePandLAsync(int companyId, DateTime? monthDate)
    {
        var month = new DateTime(monthDate?.Year ?? DateTime.Today.Year, monthDate?.Month ?? DateTime.Today.Month, 1);
        var previous = month.AddMonths(-1);

        var current = await GetMonthPnlAsync(companyId, month);
        var prior = await GetMonthPnlAsync(companyId, previous);

        var revenueLabels = current.Revenues.Keys
            .Concat(prior.Revenues.Keys)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var expenseLabels = current.Expenses.Keys
            .Concat(prior.Expenses.Keys)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var revenues = revenueLabels
            .Select(l => new ComparativePandLRow
            {
                Label = l,
                Current = Math.Round(current.Revenues.GetValueOrDefault(l), 2),
                Previous = Math.Round(prior.Revenues.GetValueOrDefault(l), 2)
            })
            .ToList();

        var expenses = expenseLabels
            .Select(l => new ComparativePandLRow
            {
                Label = l,
                Current = Math.Round(current.Expenses.GetValueOrDefault(l), 2),
                Previous = Math.Round(prior.Expenses.GetValueOrDefault(l), 2)
            })
            .ToList();

        return new ComparativePandLViewModel
        {
            CurrentMonth = month,
            PreviousMonth = previous,
            Revenues = revenues,
            Expenses = expenses,
            CurrentRevenue = Math.Round(current.Revenues.GetValueOrDefault("__total__"), 2),
            PreviousRevenue = Math.Round(prior.Revenues.GetValueOrDefault("__total__"), 2),
            CurrentExpense = Math.Round(current.Expenses.GetValueOrDefault("__total__"), 2),
            PreviousExpense = Math.Round(prior.Expenses.GetValueOrDefault("__total__"), 2)
        };
    }

    public async Task<CashFlowViewModel> GetCashFlowAsync(int companyId, DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate?.Date ?? DateTime.MinValue.Date;
        var to = toDate?.Date ?? DateTime.MaxValue.Date;
        var openingCutoff = from == DateTime.MinValue.Date ? DateTime.MinValue : from.AddDays(-1);

        var pnl = await GetMonthPnlRangeAsync(companyId, from, to);
        var netIncome = pnl.TryGetValue("__netincome__", out var ni) ? ni : 0;

        var depreciation = await _db.AssetDepreciations
            .AsNoTracking()
            .Where(d => d.AssetRegister != null && d.AssetRegister.CompanyId == companyId &&
                        d.PeriodDate.Date >= from && d.PeriodDate.Date <= to)
            .SumAsync(d => (decimal?)d.Amount) ?? 0;

        decimal ArAt(DateTime cutoff)
        {
            var customers = _db.Customers.Where(c => c.CompanyId == companyId).ToList().ToDictionary(c => c.Id);
            var invs = _db.SalesInvoices.Include(i => i.Lines).Where(i => i.CompanyId == companyId && i.InvoiceDate.Date <= cutoff).ToList();
            var pays = _db.Payments.Where(p => p.CompanyId == companyId && p.Category == PaymentCategory.Customer && p.PaymentDate.Date <= cutoff).ToList();
            return customers.Values.Sum(c =>
            {
                var sales = invs.Where(i => i.CustomerId == c.Id).Sum(i => i.Lines.Sum(l => l.Quantity * l.UnitPrice));
                var paid = invs.Where(i => i.CustomerId == c.Id).Sum(i => Math.Min(i.AmountPaid, i.Lines.Sum(l => l.Quantity * l.UnitPrice))) +
                           pays.Where(p => p.CustomerId == c.Id).Sum(p => p.Amount);
                return Math.Max(0, c.OpeningReceivable + sales - paid);
            });
        }

        decimal ApAt(DateTime cutoff)
        {
            var suppliers = _db.Suppliers.Where(s => s.CompanyId == companyId).ToList().ToDictionary(s => s.Id);
            var invs = _db.PurchaseInvoices.Include(i => i.Lines).Where(i => i.CompanyId == companyId && i.InvoiceDate.Date <= cutoff).ToList();
            var pays = _db.Payments.Where(p => p.CompanyId == companyId && p.Category == PaymentCategory.Supplier && p.PaymentDate.Date <= cutoff).ToList();
            return suppliers.Values.Sum(s =>
            {
                var purchases = invs.Where(i => i.SupplierId == s.Id).Sum(i => i.Lines.Sum(l => l.Quantity * l.UnitPrice));
                var paid = pays.Where(p => p.SupplierId == s.Id).Sum(p => p.Amount);
                return Math.Max(0, s.OpeningPayable + purchases - paid);
            });
        }

        decimal InvAt(DateTime cutoff)
        {
            var productIds = _db.Products
                .Where(p => p.CompanyId == companyId)
                .Select(p => p.Id)
                .ToList();
            var balances = _db.StockBalances
                .Where(b => productIds.Contains(b.ProductId))
                .ToList();
            return balances.Sum(b => b.Quantity * b.AverageCost);
        }

        var arOpen = ArAt(openingCutoff);
        var arClose = ArAt(to);
        var apOpen = ApAt(openingCutoff);
        var apClose = ApAt(to);
        var invOpen = InvAt(openingCutoff);
        var invClose = InvAt(to);

        var changeAr = arClose - arOpen;
        var changeAp = apClose - apOpen;
        var changeInv = invClose - invOpen;

        var fixedPurchases = (await _db.AssetRegisters
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.PurchaseDate.Date >= from && a.PurchaseDate.Date <= to)
            .SumAsync(a => (decimal?)a.Cost) ?? 0);

        var disposalProceeds = (await _db.AssetDisposals
            .AsNoTracking()
            .Where(d => d.AssetRegister != null && d.AssetRegister.CompanyId == companyId &&
                        d.DisposalDate.Date >= from && d.DisposalDate.Date <= to)
            .SumAsync(d => (decimal?)d.SaleValue) ?? 0);

        var operating = new List<CashFlowRow>
        {
            new() { Label = "Net income (profit)", Amount = Math.Round(netIncome, 2) },
            new() { Label = "Add: Depreciation & amortisation", Amount = Math.Round(depreciation, 2) },
            new() { Label = "Change in receivables", Amount = Math.Round(-changeAr, 2) },
            new() { Label = "Change in inventory", Amount = Math.Round(-changeInv, 2) },
            new() { Label = "Change in payables", Amount = Math.Round(changeAp, 2) }
        };
        var operatingNet = operating.Sum(r => r.Amount);

        var investing = new List<CashFlowRow>
        {
            new() { Label = "Purchase of fixed assets", Amount = Math.Round(-fixedPurchases, 2) },
            new() { Label = "Proceeds from asset disposals", Amount = Math.Round(disposalProceeds, 2) }
        };
        var investingNet = investing.Sum(r => r.Amount);

        var financing = new List<CashFlowRow>();
        var financingNet = 0m;

        var openingCash = await _db.CashAccounts.AsNoTracking().Where(c => c.CompanyId == companyId).SumAsync(c => (decimal?)c.OpeningBalance) ?? 0;
        openingCash += await _db.BankAccounts.AsNoTracking().Where(b => b.CompanyId == companyId).SumAsync(b => (decimal?)b.OpeningBalance) ?? 0;

        var closingCash = Math.Max(0, openingCash + operatingNet + investingNet + financingNet);

        return new CashFlowViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            Operating = operating,
            Investing = investing,
            Financing = financing,
            OperatingNet = Math.Round(operatingNet, 2),
            InvestingNet = Math.Round(investingNet, 2),
            FinancingNet = Math.Round(financingNet, 2),
            OpeningCash = Math.Round(openingCash, 2),
            ClosingCash = Math.Round(closingCash, 2)
        };
    }

    public async Task<BankCashAccountSummaryViewModel> GetBankCashSummaryAsync(int companyId)
    {
        var cashAccounts = await _db.CashAccounts.AsNoTracking().Where(c => c.CompanyId == companyId).ToListAsync();
        var bankAccounts = await _db.BankAccounts.AsNoTracking().Where(b => b.CompanyId == companyId).ToListAsync();
        var payments = await _db.Payments.AsNoTracking()
            .Where(p => p.CompanyId == companyId)
            .ToListAsync();

        var rows = new List<BankCashAccountRow>();

        foreach (var a in cashAccounts)
        {
            var inbound = payments.Where(p => p.AccountType == PaymentAccountType.Cash && p.CashAccountId == a.Id && p.Category == PaymentCategory.Customer).Sum(p => p.Amount);
            var outbound = payments.Where(p => p.AccountType == PaymentAccountType.Cash && p.CashAccountId == a.Id && p.Category != PaymentCategory.Customer).Sum(p => p.Amount);
            rows.Add(new BankCashAccountRow
            {
                Account = $"Cash - {a.AccountName} ({a.AccountCode})",
                Opening = Math.Round(a.OpeningBalance, 2),
                Receipts = Math.Round(inbound, 2),
                Payments = Math.Round(outbound, 2),
                Closing = Math.Round(a.OpeningBalance + inbound - outbound, 2)
            });
        }

        foreach (var a in bankAccounts)
        {
            var inbound = payments.Where(p => p.AccountType == PaymentAccountType.Bank && p.BankAccountId == a.Id && p.Category == PaymentCategory.Customer).Sum(p => p.Amount);
            var outbound = payments.Where(p => p.AccountType == PaymentAccountType.Bank && p.BankAccountId == a.Id && p.Category != PaymentCategory.Customer).Sum(p => p.Amount);
            rows.Add(new BankCashAccountRow
            {
                Account = $"Bank - {a.AccountName} ({a.BankName})",
                Opening = Math.Round(a.OpeningBalance, 2),
                Receipts = Math.Round(inbound, 2),
                Payments = Math.Round(outbound, 2),
                Closing = Math.Round(a.OpeningBalance + inbound - outbound, 2)
            });
        }

        return new BankCashAccountSummaryViewModel
        {
            Rows = rows,
            TotalOpening = Math.Round(rows.Sum(r => r.Opening), 2),
            TotalReceipts = Math.Round(rows.Sum(r => r.Receipts), 2),
            TotalPayments = Math.Round(rows.Sum(r => r.Payments), 2),
            TotalClosing = Math.Round(rows.Sum(r => r.Closing), 2)
        };
    }

    public async Task<CoaReportViewModel> GetCoaReportAsync(int companyId, DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate?.Date ?? DateTime.MinValue.Date;
        var to = toDate?.Date ?? DateTime.MaxValue.Date;

        var accounts = await _db.ChartOfAccounts.AsNoTracking()
            .Where(a => a.CompanyId == companyId)
            .ToListAsync();

        var details = await _db.JournalEntryDetails.AsNoTracking()
            .Include(d => d.JournalEntry)
            .Include(d => d.Account)
            .Where(d => d.Account != null && d.Account.CompanyId == companyId)
            .ToListAsync();

        var rows = accounts
            .Select(a =>
            {
                var opening = a.OpeningBalance + details
                    .Where(d => d.AccountId == a.Id && d.JournalEntry!.EntryDate.Date < from)
                    .Sum(d => d.Debit - d.Credit);
                var debit = details
                    .Where(d => d.AccountId == a.Id && d.JournalEntry!.EntryDate.Date >= from && d.JournalEntry!.EntryDate.Date <= to)
                    .Sum(d => d.Debit);
                var credit = details
                    .Where(d => d.AccountId == a.Id && d.JournalEntry!.EntryDate.Date >= from && d.JournalEntry!.EntryDate.Date <= to)
                    .Sum(d => d.Credit);
                return new CoaReportRow
                {
                    AccountCode = a.AccountCode,
                    AccountName = a.AccountName,
                    AccountType = a.AccountType.ToString(),
                    Opening = Math.Round(opening, 2),
                    Debit = Math.Round(debit, 2),
                    Credit = Math.Round(credit, 2),
                    Closing = Math.Round(opening + debit - credit, 2)
                };
            })
            .OrderBy(r => r.AccountCode)
            .ToList();

        return new CoaReportViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            Rows = rows,
            TotalOpening = Math.Round(rows.Sum(r => r.Opening), 2),
            TotalDebit = Math.Round(rows.Sum(r => r.Debit), 2),
            TotalCredit = Math.Round(rows.Sum(r => r.Credit), 2),
            TotalClosing = Math.Round(rows.Sum(r => r.Closing), 2)
        };
    }

    public async Task<VoucherViewModel> GetJournalVoucherAsync(int companyId, int journalEntryId)
    {
        var entry = await _db.JournalEntries
            .AsNoTracking()
            .Include(e => e.Branch)
            .Include(e => e.Details)
                .ThenInclude(d => d.Account)
            .FirstOrDefaultAsync(e => e.Id == journalEntryId && e.CompanyId == companyId);

        if (entry is null)
        {
            return new VoucherViewModel() { EntryNo = $"Not found {journalEntryId}" };
        }

        var lines = entry.Details
            .Select(d => new VoucherLineViewModel
            {
                AccountCode = d.Account?.AccountCode ?? "",
                AccountName = d.Account?.AccountName ?? "",
                Note = d.Note,
                Debit = Math.Round(d.Debit, 2),
                Credit = Math.Round(d.Credit, 2)
            })
            .OrderByDescending(l => l.Debit)
            .ToList();

        return new VoucherViewModel
        {
            EntryNo = entry.EntryNo,
            EntryDate = entry.EntryDate,
            Branch = entry.Branch?.Name,
            SourceModule = entry.SourceModule,
            SourceReference = entry.SourceReference,
            Description = entry.Description,
            Lines = lines,
            TotalDebit = Math.Round(lines.Sum(l => l.Debit), 2),
            TotalCredit = Math.Round(lines.Sum(l => l.Credit), 2)
        };
    }

    public async Task<PaymentVoucherViewModel> GetPaymentVoucherAsync(int companyId, int paymentId)
    {
        var payment = await _db.Payments
            .AsNoTracking()
            .Include(p => p.Branch)
            .Include(p => p.Customer)
            .Include(p => p.Supplier)
            .Include(p => p.ExpenseEntry)
            .Include(p => p.SalaryPayment)
            .Include(p => p.PaymentMethod)
            .Include(p => p.CashAccount)
            .Include(p => p.BankAccount)
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.CompanyId == companyId);

        if (payment is null)
        {
            return new PaymentVoucherViewModel() { PaymentNo = $"Not found {paymentId}" };
        }

        string party = payment.Category switch
        {
            PaymentCategory.Customer => payment.Customer?.Name ?? "",
            PaymentCategory.Supplier => payment.Supplier?.Name ?? "",
            PaymentCategory.Expense => payment.ExpenseEntry?.Description ?? "",
            PaymentCategory.Salary => payment.SalaryPayment?.Employee?.Name ?? "",
            _ => ""
        };

        return new PaymentVoucherViewModel
        {
            PaymentNo = payment.PaymentNo,
            PaymentDate = payment.PaymentDate,
            Branch = payment.Branch?.Name,
            Category = payment.Category.ToString(),
            Party = party,
            Method = payment.PaymentMethod?.Name ?? "",
            Account = payment.AccountType == PaymentAccountType.Cash
                ? $"Cash - {payment.CashAccount?.AccountName ?? ""}"
                : $"Bank - {payment.BankAccount?.AccountName ?? ""}",
            Amount = Math.Round(payment.Amount, 2),
            ReferenceNo = payment.ReferenceNo,
            Note = payment.Note
        };
    }

    private async Task<Dictionary<string, decimal>> GetMonthPnlRangeAsync(int companyId, DateTime from, DateTime to)
    {
        var rows = await GetDetailsAsync(companyId, from, to);
        var postings = rows.Select(r => (r.D.Debit, r.D.Credit, r.D.AccountId)).ToList();
        var roots = await BuildReportTreeAsync(companyId, postings);

        var revenueRoot = roots.FirstOrDefault(r => r.Account.AccountType == AccountType.Revenue);
        var expenseRoot = roots.FirstOrDefault(r => r.Account.AccountType == AccountType.Expense);
        var dict = new Dictionary<string, decimal>();
        dict["__revenue__"] = Math.Round(revenueRoot is null ? 0 : NodeAmount(revenueRoot), 2);
        dict["__expense__"] = Math.Round(expenseRoot is null ? 0 : NodeAmount(expenseRoot), 2);
        dict["__netincome__"] = dict["__revenue__"] - dict["__expense__"];
        return dict;
    }

    private async Task<(Dictionary<string, decimal> Revenues, Dictionary<string, decimal> Expenses)> GetMonthPnlAsync(int companyId, DateTime month)
    {
        var next = month.AddMonths(1);
        var rows = await GetDetailsAsync(companyId, month, next.AddDays(-1));
        var postings = rows.Select(r => (r.D.Debit, r.D.Credit, r.D.AccountId)).ToList();
        var roots = await BuildReportTreeAsync(companyId, postings);

        var revenues = new Dictionary<string, decimal>();
        var expenses = new Dictionary<string, decimal>();
        foreach (var root in roots)
        {
            CollectLeaves(root, AccountType.Revenue, revenues);
            CollectLeaves(root, AccountType.Expense, expenses);
        }

        revenues["__total__"] = Math.Round(revenues.Values.Sum(), 2);
        expenses["__total__"] = Math.Round(expenses.Values.Sum(), 2);

        return (revenues, expenses);
    }

    private static void CollectLeaves(ReportAccountNode node, AccountType wantedType, Dictionary<string, decimal> dict)
    {
        if (node.Account.AccountType == wantedType && node.Children.Count == 0)
        {
            var amount = Math.Round(Math.Abs(NodeAmount(node)), 2);
            if (amount > 0.004m)
            {
                dict[$"{node.Account.AccountCode} - {node.Account.AccountName}"] = amount;
            }
        }

        foreach (var child in node.Children)
        {
            CollectLeaves(child, wantedType, dict);
        }
    }
}