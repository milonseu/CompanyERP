using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;
using CompanyERP.Entities.CompanyBranch;
using SupplierEntity = CompanyERP.Entities.Supplier.Supplier;

namespace CompanyERP.Entities.Expense;

public class ExpenseEntry : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Branch is required.")]
    public int BranchId { get; set; }

    [Required(ErrorMessage = "Expense type is required.")]
    public int ExpenseTypeId { get; set; }

    [Display(Name = "Supplier")]
    public int? SupplierId { get; set; }

    [Required(ErrorMessage = "Expense number is required.")]
    [StringLength(30)]
    [Display(Name = "Expense No")]
    public string ExpenseNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Expense date is required.")]
    [Display(Name = "Expense Date")]
    [DataType(DataType.Date)]
    public DateTime ExpenseDate { get; set; } = DateTime.Today;

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Display(Name = "Payment Type")]
    public ExpensePaymentType PaymentType { get; set; } = ExpensePaymentType.Cash;

    [Range(0, double.MaxValue, ErrorMessage = "Amount paid must be zero or more.")]
    [Display(Name = "Amount Paid")]
    public decimal AmountPaid { get; set; }

    [StringLength(100)]
    [Display(Name = "Payment Reference")]
    public string? PaymentReference { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public CompanyProfile? Company { get; set; }
    public Branch? Branch { get; set; }
    public ExpenseType? ExpenseType { get; set; }
    public SupplierEntity? Supplier { get; set; }
}