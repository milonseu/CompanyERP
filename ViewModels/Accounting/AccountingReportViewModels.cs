using CompanyERP.Entities.Accounting;

namespace CompanyERP.ViewModels.Accounting;

public class TrialBalanceLineViewModel
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public int Depth { get; set; }
    public bool IsSubtotal { get; set; }
}

public class TrialBalanceViewModel
{
    public List<TrialBalanceLineViewModel> Lines { get; set; } = [];
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public bool IsBalanced => Math.Round(TotalDebit, 2) == Math.Round(TotalCredit, 2);
}

public class LedgerLineViewModel
{
    public DateTime Date { get; set; }
    public string EntryNo { get; set; } = string.Empty;
    public string? SourceModule { get; set; }
    public string? SourceReference { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}

public class LedgerViewModel
{
    public int? AccountId { get; set; }
    public string? AccountDisplay { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public decimal OpeningBalance { get; set; }
    public List<LedgerLineViewModel> Lines { get; set; } = [];
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal ClosingBalance { get; set; }
}

public class CashBookLineViewModel
{
    public DateTime Date { get; set; }
    public string EntryNo { get; set; } = string.Empty;
    public string? SourceModule { get; set; }
    public string? SourceReference { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}

public class CashBookViewModel
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public List<CashBookLineViewModel> Lines { get; set; } = [];
    public decimal TotalIn { get; set; }
    public decimal TotalOut { get; set; }
    public decimal ClosingBalance { get; set; }
}

public class ProfitAndLossLineViewModel
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Depth { get; set; }
    public bool IsSubtotal { get; set; }
}

public class ProfitAndLossViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<ProfitAndLossLineViewModel> Revenues { get; set; } = [];
    public List<ProfitAndLossLineViewModel> Expenses { get; set; } = [];
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetProfit => TotalRevenue - TotalExpense;
}

public class BalanceSheetLineViewModel
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Depth { get; set; }
    public bool IsSubtotal { get; set; }
}

public class BalanceSheetViewModel
{
    public DateTime? AsOfDate { get; set; }
    public List<BalanceSheetLineViewModel> Assets { get; set; } = [];
    public List<BalanceSheetLineViewModel> Liabilities { get; set; } = [];
    public List<BalanceSheetLineViewModel> Equity { get; set; } = [];
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
    public decimal NetProfit { get; set; }
    public decimal TotalLiabilitiesEquity => TotalLiabilities + TotalEquity + NetProfit;
    public decimal Difference => TotalAssets - TotalLiabilitiesEquity;
}

public class ReceivablePayableLineViewModel
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Opening { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Closing { get; set; }
}

public class ReceivablesPayablesViewModel
{
    public List<ReceivablePayableLineViewModel> Receivables { get; set; } = [];
    public List<ReceivablePayableLineViewModel> Payables { get; set; } = [];
    public decimal TotalReceivables { get; set; }
    public decimal TotalPayables { get; set; }
}