using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.Employee;

public class SalaryStructure : BaseEntity
{
    [Required]
    public int EmployeeId { get; set; }

    [Required(ErrorMessage = "Effective date is required.")]
    [Display(Name = "Effective From")]
    [DataType(DataType.Date)]
    public DateTime EffectiveFrom { get; set; } = DateTime.Today;

    [Range(0, double.MaxValue)]
    [Display(Name = "Basic Salary")]
    public decimal BasicSalary { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "House Rent")]
    public decimal HouseRent { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Medical Allowance")]
    public decimal MedicalAllowance { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Conveyance Allowance")]
    public decimal ConveyanceAllowance { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Other Allowance")]
    public decimal OtherAllowance { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Provident Fund %")]
    public decimal ProvidentFundPercent { get; set; }

    [Display(Name = "Gross Salary")]
    public decimal GrossSalary
    {
        get => BasicSalary + HouseRent + MedicalAllowance + ConveyanceAllowance + OtherAllowance;
    }

    public Employee? Employee { get; set; }
}