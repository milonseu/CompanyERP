using System.ComponentModel.DataAnnotations;

namespace CompanyERP.Entities.Accounting;

public enum AccountType
{
    [Display(Name = "Asset")]
    Asset = 1,

    [Display(Name = "Liability")]
    Liability = 2,

    [Display(Name = "Equity")]
    Equity = 3,

    [Display(Name = "Revenue")]
    Revenue = 4,

    [Display(Name = "Expense")]
    Expense = 5
}

public enum AccountNormalBalance
{
    [Display(Name = "Debit")]
    Debit = 1,

    [Display(Name = "Credit")]
    Credit = 2
}