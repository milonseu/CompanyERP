using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;

namespace CompanyERP.Entities.Accounting;

public class ChartOfAccount : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Account code is required.")]
    [StringLength(20)]
    [Display(Name = "Account Code")]
    public string AccountCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Account name is required.")]
    [StringLength(100)]
    [Display(Name = "Account Name")]
    public string AccountName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Account type is required.")]
    [Display(Name = "Account Type")]
    public AccountType AccountType { get; set; } = AccountType.Asset;

    [Required(ErrorMessage = "Normal balance is required.")]
    [Display(Name = "Normal Balance")]
    public AccountNormalBalance NormalBalance { get; set; } = AccountNormalBalance.Debit;

    [Display(Name = "Opening Balance")]
    [DataType(DataType.Currency)]
    public decimal OpeningBalance { get; set; }

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public new bool IsActive { get; set; } = true;

    public CompanyProfile? Company { get; set; }

    public string AccountDisplayLabel => $"{AccountCode} - {AccountName}";
}