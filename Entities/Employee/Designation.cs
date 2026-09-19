using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;

namespace CompanyERP.Entities.Employee;

public class Designation : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Designation code is required.")]
    [StringLength(20)]
    [Display(Name = "Designation Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Designation name is required.")]
    [StringLength(100)]
    [Display(Name = "Designation")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public CompanyProfile? Company { get; set; }

    public ICollection<Employee> Employees { get; set; } = [];
}