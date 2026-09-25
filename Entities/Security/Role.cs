using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.Security;

public class Role : BaseEntity
{
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

    [Display(Name = "System Role")]
    public bool IsSystem { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}