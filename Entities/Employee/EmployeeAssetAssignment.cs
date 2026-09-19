using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.Employee;

public class EmployeeAssetAssignment : BaseEntity
{
    [Required]
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

    public Employee? Employee { get; set; }
}