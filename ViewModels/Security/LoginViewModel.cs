using System.ComponentModel.DataAnnotations;

namespace CompanyERP.ViewModels.Security;

public class LoginViewModel
{
    [Required(ErrorMessage = "User name is required.")]
    [Display(Name = "User Name")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember Me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}