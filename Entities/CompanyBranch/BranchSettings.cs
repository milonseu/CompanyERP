using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.CompanyBranch;

public class BranchSettings : BaseEntity
{
    [Required]
    public int BranchId { get; set; }

    [StringLength(10)]
    [Display(Name = "Transaction Prefix")]
    public string? TransactionPrefix { get; set; }

    [Required(ErrorMessage = "Default currency is required.")]
    [StringLength(10)]
    [Display(Name = "Default Currency")]
    public string DefaultCurrencyCode { get; set; } = "BDT";

    [Range(0, 365)]
    [Display(Name = "Default Payment Days")]
    public int DefaultPaymentDays { get; set; }

    [Display(Name = "Allow Inventory")]
    public bool AllowInventory { get; set; } = true;

    [Display(Name = "Allow Sales")]
    public bool AllowSales { get; set; } = true;

    [Display(Name = "Allow Purchase")]
    public bool AllowPurchase { get; set; } = true;

    [Display(Name = "Allow Expense")]
    public bool AllowExpense { get; set; } = true;

    public Branch? Branch { get; set; }
}