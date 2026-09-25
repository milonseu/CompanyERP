using CompanyERP.Data;
using CompanyERP.Entities.Accounting;
using CompanyERP.Interfaces.Services;
using CompanyERP.ViewModels.Accounting;
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
}