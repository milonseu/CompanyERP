using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.Employee;

public enum SalaryPaymentStatus
{
    [Display(Name = "Pending")]
    Pending = 1,

    [Display(Name = "Paid")]
    Paid = 2
}

public enum PaymentMode
{
    [Display(Name = "Cash")]
    Cash = 1,

    [Display(Name = "Bank")]
    Bank = 2
}

public class SalaryPayment : BaseEntity
{
    [Required]
    public int EmployeeId { get; set; }

    [Required(ErrorMessage = "Salary month is required.")]
    [Display(Name = "Salary Month")]
    [DataType(DataType.Date)]
    public DateTime ForMonth { get; set; }

    [Required(ErrorMessage = "Payment date is required.")]
    [Display(Name = "Payment Date")]
    [DataType(DataType.Date)]
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Range(0, double.MaxValue, ErrorMessage = "Amount must be positive.")]
    [Display(Name = "Amount")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Payment status is required.")]
    [Display(Name = "Payment Status")]
    public SalaryPaymentStatus Status { get; set; } = SalaryPaymentStatus.Pending;

    [Display(Name = "Payment Mode")]
    public PaymentMode? PaymentMode { get; set; }

    [StringLength(50)]
    [Display(Name = "Reference No")]
    public string? ReferenceNo { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public Employee? Employee { get; set; }
}