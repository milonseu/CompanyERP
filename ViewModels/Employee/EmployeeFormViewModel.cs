using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Employee;

namespace CompanyERP.ViewModels.Employee;

public class EmployeeFormViewModel
{
    public int? Id { get; set; }

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

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public Entities.Employee.Employee ToEntity()
    {
        return new Entities.Employee.Employee
        {
            Id = Id ?? 0,
            EmployeeCode = EmployeeCode,
            Name = Name,
            Gender = Gender,
            DateOfBirth = DateOfBirth,
            Mobile = Mobile,
            Email = Email,
            NationalId = NationalId,
            FatherName = FatherName,
            MotherName = MotherName,
            PresentAddress = PresentAddress,
            PermanentAddress = PermanentAddress,
            JoiningDate = JoiningDate,
            CompanyId = CompanyId,
            DepartmentId = DepartmentId,
            DesignationId = DesignationId,
            DefaultBranchId = DefaultBranchId,
            IsActive = IsActive
        };
    }

    public static EmployeeFormViewModel FromEntity(Entities.Employee.Employee employee)
    {
        return new EmployeeFormViewModel
        {
            Id = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            Name = employee.Name,
            Gender = employee.Gender,
            DateOfBirth = employee.DateOfBirth,
            Mobile = employee.Mobile,
            Email = employee.Email,
            NationalId = employee.NationalId,
            FatherName = employee.FatherName,
            MotherName = employee.MotherName,
            PresentAddress = employee.PresentAddress,
            PermanentAddress = employee.PermanentAddress,
            JoiningDate = employee.JoiningDate,
            CompanyId = employee.CompanyId,
            DepartmentId = employee.DepartmentId,
            DesignationId = employee.DesignationId,
            DefaultBranchId = employee.DefaultBranchId,
            IsActive = employee.IsActive
        };
    }
}