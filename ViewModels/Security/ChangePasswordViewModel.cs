using System.ComponentModel.DataAnnotations;

namespace CompanyERP.ViewModels.Security;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare(nameof(NewPassword), ErrorMessage = "Password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
