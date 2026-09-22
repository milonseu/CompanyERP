using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;

namespace CompanyERP.Entities.Sales;

public class Service : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Service code is required.")]
    [StringLength(30)]
    [Display(Name = "Service Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Service name is required.")]
    [StringLength(150)]
    [Display(Name = "Service Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Service Type")]
    public ServiceKind ServiceKind { get; set; } = ServiceKind.Service;

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Unit price cannot be negative.")]
    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Cost price cannot be negative.")]
    [Display(Name = "Cost Price")]
    public decimal CostPrice { get; set; }

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public new bool IsActive { get; set; } = true;

    public CompanyProfile? Company { get; set; }
}