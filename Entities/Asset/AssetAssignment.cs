using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;
using EmployeeEntity = CompanyERP.Entities.Employee.Employee;

namespace CompanyERP.Entities.Asset;

public class AssetAssignment : BaseEntity
{
    [Required]
    public int AssetRegisterId { get; set; }

    [Required(ErrorMessage = "Employee is required.")]
    public int EmployeeId { get; set; }

    [Required(ErrorMessage = "Assignment date is required.")]
    [Display(Name = "Assigned On")]
    [DataType(DataType.Date)]
    public DateTime AssignedDate { get; set; } = DateTime.Today;

    [Display(Name = "Returned On")]
    [DataType(DataType.Date)]
    public DateTime? ReturnedDate { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public AssetRegister? AssetRegister { get; set; }
    public EmployeeEntity? Employee { get; set; }
}