using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;

namespace CompanyERP.Entities.Employee;

public class Department : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Department code is required.")]
    [StringLength(20)]
    [Display(Name = "Department Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department name is required.")]
    [StringLength(100)]
    [Display(Name = "Department")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public CompanyProfile? Company { get; set; }

    public ICollection<Employee> Employees { get; set; } = [];
}