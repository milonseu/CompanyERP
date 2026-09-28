using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.Security;

public class User : BaseEntity
{
    [Required(ErrorMessage = "User name is required.")]
    [StringLength(50)]
    [Display(Name = "User Name")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Full Name")]
    public string? FullName { get; set; }

    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(20)]
    [Phone]
    public string? Phone { get; set; }

    [Display(Name = "System Account")]
    public bool IsSystem { get; set; }
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Set when the account is still carrying a password the owner never chose (the seeded default
    /// administrator). While this is set the user is confined to the change-password screen.
    /// </summary>
    public bool MustChangePassword { get; set; }


    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<UserPermission> UserPermissions { get; set; } = [];
}