using System.ComponentModel.DataAnnotations;

namespace CompanyERP.ViewModels.Security;

public class RegisterViewModel
{
    [Required(ErrorMessage = "User name is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "User name must be 3-100 characters.")]
    [Display(Name = "User Name")]
    public string UserName { get; set; } = string.Empty;

    [Display(Name = "Full Name")]
    public string? FullName { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare(nameof(Password), ErrorMessage = "Password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}