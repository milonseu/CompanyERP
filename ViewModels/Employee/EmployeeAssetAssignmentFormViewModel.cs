using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Employee;

namespace CompanyERP.ViewModels.Employee;

public class EmployeeAssetAssignmentFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Employee is required.")]
    public int EmployeeId { get; set; }

    [StringLength(50)]
    [Display(Name = "Asset Code")]
    public string? AssetCode { get; set; }

    [Required(ErrorMessage = "Asset name is required.")]
    [StringLength(150)]
    [Display(Name = "Asset Name")]
    public string AssetName { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Serial Number")]
    public string? SerialNumber { get; set; }

    [Required(ErrorMessage = "Assignment date is required.")]
    [Display(Name = "Assigned On")]
    [DataType(DataType.Date)]
    public DateTime AssignedOn { get; set; } = DateTime.Today;

    [Display(Name = "Returned On")]
    [DataType(DataType.Date)]
    public DateTime? ReturnedOn { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public EmployeeAssetAssignment ToEntity()
    {
        return new EmployeeAssetAssignment
        {
            Id = Id ?? 0,
            EmployeeId = EmployeeId,
            AssetCode = AssetCode,
            AssetName = AssetName,
            SerialNumber = SerialNumber,
            AssignedOn = AssignedOn,
            ReturnedOn = ReturnedOn,
            Note = Note
        };
    }

    public static EmployeeAssetAssignmentFormViewModel FromEntity(EmployeeAssetAssignment assignment)
    {
        return new EmployeeAssetAssignmentFormViewModel
        {
            Id = assignment.Id,
            EmployeeId = assignment.EmployeeId,
            AssetCode = assignment.AssetCode,
            AssetName = assignment.AssetName,
            SerialNumber = assignment.SerialNumber,
            AssignedOn = assignment.AssignedOn,
            ReturnedOn = assignment.ReturnedOn,
            Note = assignment.Note
        };
    }
}