using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.CompanyBranch;

namespace CompanyERP.ViewModels.CompanyBranch;

public class BranchTypeFormViewModel
{
    public int? Id { get; set; }

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

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public BranchType ToEntity()
    {
        return new BranchType
        {
            Id = Id ?? 0,
            Code = Code,
            Name = Name,
            Description = Description,
            IsActive = IsActive
        };
    }

    public static BranchTypeFormViewModel FromEntity(BranchType branchType)
    {
        return new BranchTypeFormViewModel
        {
            Id = branchType.Id,
            Code = branchType.Code,
            Name = branchType.Name,
            Description = branchType.Description,
            IsActive = branchType.IsActive
        };
    }
}