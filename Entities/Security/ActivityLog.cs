using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.Security;

public class ActivityLog : BaseEntity
{
    [StringLength(50)]
    [Display(Name = "User")]
    public string? UserName { get; set; }

    [StringLength(30)]
    [Display(Name = "Action")]
    public string Action { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Module")]
    public string? Module { get; set; }

    [StringLength(100)]
    [Display(Name = "Entity")]
    public string? EntityName { get; set; }

    [Display(Name = "Entity Id")]
    public int? EntityId { get; set; }

    [StringLength(500)]
    [Display(Name = "Details")]
    public string Details { get; set; } = string.Empty;

    [StringLength(50)]
    public string? IpAddress { get; set; }
}