using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Security;

namespace CompanyERP.ViewModels.Security;

public class PermissionFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Permission code is required.")]
    [StringLength(80)]
    [Display(Name = "Permission Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Permission name is required.")]
    [StringLength(100)]
    [Display(Name = "Permission")]
    public string Name { get; set; } = string.Empty;

    [StringLength(80)]
    [Display(Name = "Module")]
    public string? Module { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "System Permission")]
    public bool IsSystem { get; set; }

    public Permission ToEntity()
    {
        return new Permission
        {
            Id = Id ?? 0,
            Code = Code,
            Name = Name,
            Module = Module,
            Description = Description,
            IsActive = IsActive,
            IsSystem = IsSystem
        };
    }

    public static PermissionFormViewModel FromEntity(Permission permission)
    {
        return new PermissionFormViewModel
        {
            Id = permission.Id,
            Code = permission.Code,
            Name = permission.Name,
            Module = permission.Module,
            Description = permission.Description,
            IsActive = permission.IsActive,
            IsSystem = permission.IsSystem
        };
    }
}