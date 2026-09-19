using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Employee;

namespace CompanyERP.ViewModels.Employee;

public class SalaryStructureFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Employee is required.")]
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

    [Range(0, 100)]
    [Display(Name = "Provident Fund %")]
    public decimal ProvidentFundPercent { get; set; }

    [Display(Name = "Gross Salary")]
    public decimal GrossSalary => BasicSalary + HouseRent + MedicalAllowance + ConveyanceAllowance + OtherAllowance;

    public SalaryStructure ToEntity()
    {
        return new SalaryStructure
        {
            Id = Id ?? 0,
            EmployeeId = EmployeeId,
            EffectiveFrom = EffectiveFrom,
            BasicSalary = BasicSalary,
            HouseRent = HouseRent,
            MedicalAllowance = MedicalAllowance,
            ConveyanceAllowance = ConveyanceAllowance,
            OtherAllowance = OtherAllowance,
            ProvidentFundPercent = ProvidentFundPercent
        };
    }

    public static SalaryStructureFormViewModel FromEntity(SalaryStructure structure)
    {
        return new SalaryStructureFormViewModel
        {
            Id = structure.Id,
            EmployeeId = structure.EmployeeId,
            EffectiveFrom = structure.EffectiveFrom,
            BasicSalary = structure.BasicSalary,
            HouseRent = structure.HouseRent,
            MedicalAllowance = structure.MedicalAllowance,
            ConveyanceAllowance = structure.ConveyanceAllowance,
            OtherAllowance = structure.OtherAllowance,
            ProvidentFundPercent = structure.ProvidentFundPercent
        };
    }
}