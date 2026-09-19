using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.CompanyBranch;

public class BranchType : BaseEntity
{
    [Required(ErrorMessage = "Branch type code is required.")]
    [StringLength(20)]
    [Display(Name = "Branch Type Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Branch type name is required.")]
    [StringLength(100)]
    [Display(Name = "Branch Type")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public ICollection<Branch> Branches { get; set; } = [];
}