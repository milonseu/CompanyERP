using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Employee;

namespace CompanyERP.ViewModels.Employee;

public class DepartmentFormViewModel
{
    public int? Id { get; set; }

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

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public Department ToEntity()
    {
        return new Department
        {
            Id = Id ?? 0,
            CompanyId = CompanyId,
            Code = Code,
            Name = Name,
            Description = Description,
            IsActive = IsActive
        };
    }

    public static DepartmentFormViewModel FromEntity(Department department)
    {
        return new DepartmentFormViewModel
        {
            Id = department.Id,
            CompanyId = department.CompanyId,
            Code = department.Code,
            Name = department.Name,
            Description = department.Description,
            IsActive = department.IsActive
        };
    }
}