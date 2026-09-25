using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.Security;

public class LoginHistory : BaseEntity
{
    [StringLength(50)]
    [Display(Name = "User")]
    public string UserName { get; set; } = string.Empty;

    [Display(Name = "Login Time")]
    public DateTime LoginTime { get; set; } = DateTime.Now;

    [Display(Name = "Logout Time")]
    public DateTime? LogoutTime { get; set; }

    [Display(Name = "Successful")]
    public bool IsSuccess { get; set; }

    [StringLength(50)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? FailReason { get; set; }
}