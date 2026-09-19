using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Employee;

namespace CompanyERP.ViewModels.Employee;

public class EmployeeBranchAssignmentFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Employee is required.")]
    public int EmployeeId { get; set; }

    [Required(ErrorMessage = "Branch is required.")]
    [Display(Name = "Branch")]
    public int BranchId { get; set; }

    [Display(Name = "Assigned Date")]
    [DataType(DataType.Date)]
    public DateTime? AssignedDate { get; set; }

    [Display(Name = "Default")]
    public bool IsDefault { get; set; }

    public EmployeeBranchAssignment ToEntity()
    {
        return new EmployeeBranchAssignment
        {
            Id = Id ?? 0,
            EmployeeId = EmployeeId,
            BranchId = BranchId,
            AssignedDate = AssignedDate,
            IsDefault = IsDefault
        };
    }

    public static EmployeeBranchAssignmentFormViewModel FromEntity(EmployeeBranchAssignment assignment)
    {
        return new EmployeeBranchAssignmentFormViewModel
        {
            Id = assignment.Id,
            EmployeeId = assignment.EmployeeId,
            BranchId = assignment.BranchId,
            AssignedDate = assignment.AssignedDate,
            IsDefault = assignment.IsDefault
        };
    }
}