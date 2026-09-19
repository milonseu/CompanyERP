using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;
using CompanyERP.Entities.CompanyBranch;

namespace CompanyERP.Entities.Inventory;

public class Warehouse : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Branch is required.")]
    [Display(Name = "Branch")]
    public int BranchId { get; set; }

    [Required(ErrorMessage = "Warehouse code is required.")]
    [StringLength(20)]
    [Display(Name = "Warehouse Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Warehouse name is required.")]
    [StringLength(150)]
    [Display(Name = "Warehouse Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(250)]
    [Display(Name = "Address")]
    public string? Address { get; set; }

    [Display(Name = "Active")]
    public new bool IsActive { get; set; } = true;

    public CompanyProfile? Company { get; set; }
    public Branch? Branch { get; set; }
}