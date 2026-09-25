using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.Security;

public class Permission : BaseEntity
{
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

    [Display(Name = "System Permission")]
    public bool IsSystem { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
    public ICollection<UserPermission> UserPermissions { get; set; } = [];
}