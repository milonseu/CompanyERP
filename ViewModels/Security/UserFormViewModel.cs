using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Security;

namespace CompanyERP.ViewModels.Security;

public class UserFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "User name is required.")]
    [StringLength(50)]
    [Display(Name = "User Name")]
    public string UserName { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Full Name")]
    public string? FullName { get; set; }

    [StringLength(100)]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(20)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare(nameof(Password), ErrorMessage = "Password and confirm password do not match.")]
    public string? ConfirmPassword { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Last Login")]
    public DateTime? LastLoginAt { get; set; }

    public List<int> SelectedRoleIds { get; set; } = [];
    public List<Role> AllRoles { get; set; } = [];
    public List<int> SelectedPermissionIds { get; set; } = [];

    public User ToEntity()
    {
        return new User
        {
            Id = Id ?? 0,
            UserName = UserName,
            FullName = FullName,
            Email = Email,
            Phone = Phone,
            IsActive = IsActive,
            LastLoginAt = LastLoginAt
        };
    }

    public static UserFormViewModel FromEntity(User user)
    {
        return new UserFormViewModel
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt
        };
    }
}