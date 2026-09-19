using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Employee;

namespace CompanyERP.ViewModels.Employee;

public class DesignationFormViewModel
{
    public int? Id { get; set; }

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

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public Designation ToEntity()
    {
        return new Designation
        {
            Id = Id ?? 0,
            CompanyId = CompanyId,
            Code = Code,
            Name = Name,
            Description = Description,
            IsActive = IsActive
        };
    }

    public static DesignationFormViewModel FromEntity(Designation designation)
    {
        return new DesignationFormViewModel
        {
            Id = designation.Id,
            CompanyId = designation.CompanyId,
            Code = designation.Code,
            Name = designation.Name,
            Description = designation.Description,
            IsActive = designation.IsActive
        };
    }
}