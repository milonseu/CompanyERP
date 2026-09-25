using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;
using CompanyERP.Entities.CompanyBranch;

namespace CompanyERP.Entities.Payment;

public class CashAccount : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Branch is required.")]
    [Display(Name = "Branch")]
    public int BranchId { get; set; }

    [Required(ErrorMessage = "Account name is required.")]
    [StringLength(100)]
    [Display(Name = "Account Name")]
    public string AccountName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Account code is required.")]
    [StringLength(20)]
    [Display(Name = "Account Code")]
    public string AccountCode { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Opening balance must be zero or more.")]
    [Display(Name = "Opening Balance")]
    public decimal OpeningBalance { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public CompanyProfile? Company { get; set; }
    public Branch? Branch { get; set; }
}