using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;
using CompanyERP.Entities.CompanyBranch;

namespace CompanyERP.Entities.Employee;

public enum Gender
{
    [Display(Name = "Male")]
    Male = 1,

    [Display(Name = "Female")]
    Female = 2,

    [Display(Name = "Other")]
    Other = 3
}

public class Employee : BaseEntity
{
    [Required(ErrorMessage = "Employee code is required.")]
    [StringLength(20)]
    [Display(Name = "Employee Code")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Employee name is required.")]
    [StringLength(150)]
    [Display(Name = "Employee Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Gender")]
    public Gender Gender { get; set; } = Gender.Male;

    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(30)]
    [Display(Name = "Mobile")]
    public string? Mobile { get; set; }

    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(30)]
    [Display(Name = "National ID")]
    public string? NationalId { get; set; }

    [StringLength(150)]
    [Display(Name = "Father's Name")]
    public string? FatherName { get; set; }

    [StringLength(150)]
    [Display(Name = "Mother's Name")]
    public string? MotherName { get; set; }

    [StringLength(250)]
    [Display(Name = "Present Address")]
    public string? PresentAddress { get; set; }

    [StringLength(250)]
    [Display(Name = "Permanent Address")]
    public string? PermanentAddress { get; set; }

    [Required(ErrorMessage = "Joining date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Joining Date")]
    public DateTime JoiningDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Department is required.")]
    [Display(Name = "Department")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "Designation is required.")]
    [Display(Name = "Designation")]
    public int DesignationId { get; set; }

    [Display(Name = "Default Branch")]
    public int? DefaultBranchId { get; set; }

    public CompanyProfile? Company { get; set; }
    public Department? Department { get; set; }
    public Designation? Designation { get; set; }
    public Branch? DefaultBranch { get; set; }

    public SalaryStructure? SalaryStructure { get; set; }
    public ICollection<EmployeeBranchAssignment> BranchAssignments { get; set; } = [];
    public ICollection<SalaryPayment> SalaryPayments { get; set; } = [];
    public ICollection<EmployeeAssetAssignment> AssetAssignments { get; set; } = [];
}