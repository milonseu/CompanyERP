using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.CompanyBranch;

namespace CompanyERP.ViewModels.CompanyBranch;

public class BranchSettingsFormViewModel
{
    public int? Id { get; set; }

    public int? BranchId { get; set; }

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

    public BranchSettings ToEntity()
    {
        return new BranchSettings
        {
            Id = Id ?? 0,
            BranchId = BranchId ?? 0,
            TransactionPrefix = TransactionPrefix,
            DefaultCurrencyCode = DefaultCurrencyCode,
            DefaultPaymentDays = DefaultPaymentDays,
            AllowInventory = AllowInventory,
            AllowSales = AllowSales,
            AllowPurchase = AllowPurchase,
            AllowExpense = AllowExpense
        };
    }

    public static BranchSettingsFormViewModel FromEntity(BranchSettings settings)
    {
        return new BranchSettingsFormViewModel
        {
            Id = settings.Id,
            BranchId = settings.BranchId,
            TransactionPrefix = settings.TransactionPrefix,
            DefaultCurrencyCode = settings.DefaultCurrencyCode,
            DefaultPaymentDays = settings.DefaultPaymentDays,
            AllowInventory = settings.AllowInventory,
            AllowSales = settings.AllowSales,
            AllowPurchase = settings.AllowPurchase,
            AllowExpense = settings.AllowExpense
        };
    }
}