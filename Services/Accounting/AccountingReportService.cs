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

    private static decimal PositionAmount(List<(decimal Debit, decimal Credit)> rows, AccountNormalBalance normal)
    {
        var net = rows.Sum(r => r.Debit - r.Credit);
        return normal == AccountNormalBalance.Debit ? net : -net;
    }

    public async Task<TrialBalanceViewModel> GetTrialBalanceAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var rows = await GetDetailsAsync(companyId, fromDate, toDate);
        var lines = new List<TrialBalanceLineViewModel>();

        foreach (var group in rows.GroupBy(r => r.Account.Id))
        {
            var account = group.First().Account;
            lines.Add(new TrialBalanceLineViewModel
            {
                AccountId = account.Id,
                AccountCode = account.AccountCode,
                AccountName = account.AccountName,
                AccountType = account.AccountType,
                Debit = Math.Round(group.Sum(g => g.D.Debit), 2),
                Credit = Math.Round(group.Sum(g => g.D.Credit), 2)
            });
        }

        var vm = new TrialBalanceViewModel
        {
            Lines = lines
                .OrderBy(l => l.AccountCode)
                .ToList(),
            TotalDebit = Math.Round(lines.Sum(l => l.Debit), 2),
            TotalCredit = Math.Round(lines.Sum(l => l.Credit), 2)
        };
        return vm;
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
        var vm = new ProfitAndLossViewModel { FromDate = fromDate, ToDate = toDate };

        foreach (var group in rows.GroupBy(r => r.Account.Id))
        {
            var account = group.First().Account;
            var rowsForAccount = group.Select(g => (g.D.Debit, g.D.Credit)).ToList();
            var amount = PositionAmount(rowsForAccount, account.NormalBalance);

            switch (account.AccountType)
            {
                case AccountType.Revenue:
                    vm.Revenues.Add(new ProfitAndLossLineViewModel
                    {
                        AccountCode = account.AccountCode,
                        AccountName = account.AccountName,
                        Amount = Math.Round(amount, 2)
                    });
                    break;

                case AccountType.Expense:
                    vm.Expenses.Add(new ProfitAndLossLineViewModel
                    {
                        AccountCode = account.AccountCode,
                        AccountName = account.AccountName,
                        Amount = Math.Round(amount, 2)
                    });
                    break;
            }
        }

        vm.Revenues = vm.Revenues.OrderBy(r => r.AccountCode).ToList();
        vm.Expenses = vm.Expenses.OrderBy(e => e.AccountCode).ToList();
        vm.TotalRevenue = Math.Round(vm.Revenues.Sum(r => r.Amount), 2);
        vm.TotalExpense = Math.Round(vm.Expenses.Sum(e => e.Amount), 2);
        return vm;
    }

    public async Task<BalanceSheetViewModel> GetBalanceSheetAsync(int companyId, DateTime? asOfDate = null)
    {
        var rows = await GetDetailsAsync(companyId, null, asOfDate);
        var accounts = await _db.ChartOfAccounts
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId)
            .ToListAsync();

        // P&L totals across the whole period up to the as-of date.
        var pnl = await GetProfitAndLossAsync(companyId, null, asOfDate);

        var vm = new BalanceSheetViewModel { AsOfDate = asOfDate };
        var groupSets = rows.GroupBy(r => r.Account.Id);

        foreach (var account in accounts.OrderBy(a => a.AccountCode))
        {
            var group = groupSets.FirstOrDefault(g => g.Key == account.Id);
            var rowsForAccount = group?.Select(g => (g.D.Debit, g.D.Credit)).ToList() ?? [];

            // Opening balance is stored signed (negative for credit-normal accounts).
            var net = rowsForAccount.Sum(r => r.Debit - r.Credit);
            var amount = account.NormalBalance == AccountNormalBalance.Debit
                ? Math.Round(account.OpeningBalance + net, 2)
                : Math.Round(-(account.OpeningBalance + net), 2);

            switch (account.AccountType)
            {
                case AccountType.Asset:
                    vm.Assets.Add(new BalanceSheetLineViewModel { AccountCode = account.AccountCode, AccountName = account.AccountName, Amount = amount });
                    vm.TotalAssets += amount;
                    break;

                case AccountType.Liability:
                    vm.Liabilities.Add(new BalanceSheetLineViewModel { AccountCode = account.AccountCode, AccountName = account.AccountName, Amount = amount });
                    vm.TotalLiabilities += amount;
                    break;

                case AccountType.Equity:
                    vm.Equity.Add(new BalanceSheetLineViewModel { AccountCode = account.AccountCode, AccountName = account.AccountName, Amount = amount });
                    vm.TotalEquity += amount;
                    break;
            }
        }

        vm.TotalAssets = Math.Round(vm.TotalAssets, 2);
        vm.TotalLiabilities = Math.Round(vm.TotalLiabilities, 2);
        vm.TotalEquity = Math.Round(vm.TotalEquity, 2);
        vm.NetProfit = pnl.NetProfit;
        return vm;
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
}