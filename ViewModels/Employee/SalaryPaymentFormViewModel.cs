using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Employee;

namespace CompanyERP.ViewModels.Employee;

public class SalaryPaymentFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Employee is required.")]
    public int EmployeeId { get; set; }

    [Required(ErrorMessage = "Salary month is required.")]
    [Display(Name = "Salary Month")]
    [DataType(DataType.Date)]
    public DateTime ForMonth { get; set; } = DateTime.Today;

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

    public SalaryPayment ToEntity()
    {
        return new SalaryPayment
        {
            Id = Id ?? 0,
            EmployeeId = EmployeeId,
            ForMonth = ForMonth,
            PaymentDate = PaymentDate,
            Amount = Amount,
            Status = Status,
            PaymentMode = PaymentMode,
            ReferenceNo = ReferenceNo,
            Note = Note
        };
    }

    public static SalaryPaymentFormViewModel FromEntity(SalaryPayment payment)
    {
        return new SalaryPaymentFormViewModel
        {
            Id = payment.Id,
            EmployeeId = payment.EmployeeId,
            ForMonth = payment.ForMonth,
            PaymentDate = payment.PaymentDate,
            Amount = payment.Amount,
            Status = payment.Status,
            PaymentMode = payment.PaymentMode,
            ReferenceNo = payment.ReferenceNo,
            Note = payment.Note
        };
    }
}