using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.CompanyBranch;

namespace CompanyERP.Entities.Employee;

public class EmployeeBranchAssignment : BaseEntity
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    [Display(Name = "Branch")]
    public int BranchId { get; set; }

    [Display(Name = "Assigned Date")]
    [DataType(DataType.Date)]
    public DateTime? AssignedDate { get; set; }

    [Display(Name = "Default")]
    public bool IsDefault { get; set; }

    public Employee? Employee { get; set; }
    public Branch? Branch { get; set; }
}