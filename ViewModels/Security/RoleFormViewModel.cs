using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Security;

namespace CompanyERP.ViewModels.Security;

public class RoleFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Role code is required.")]
    [StringLength(30)]
    [Display(Name = "Role Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role name is required.")]
    [StringLength(100)]
    [Display(Name = "Role")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "System Role")]
    public bool IsSystem { get; set; }

    public List<int> SelectedPermissionIds { get; set; } = [];
    public List<Permission> AllPermissions { get; set; } = [];
    public List<string> Modules { get; set; } = [];

    public Role ToEntity()
    {
        return new Role
        {
            Id = Id ?? 0,
            Code = Code,
            Name = Name,
            Description = Description,
            IsActive = IsActive,
            IsSystem = IsSystem
        };
    }

    public static RoleFormViewModel FromEntity(Role role)
    {
        return new RoleFormViewModel
        {
            Id = role.Id,
            Code = role.Code,
            Name = role.Name,
            Description = role.Description,
            IsActive = role.IsActive,
            IsSystem = role.IsSystem
        };
    }
}