using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;
using CompanyERP.Entities.CompanyBranch;
using CustomerEntity = CompanyERP.Entities.Customer.Customer;
using SupplierEntity = CompanyERP.Entities.Supplier.Supplier;
using ExpenseEntity = CompanyERP.Entities.Expense.ExpenseEntry;
using SalaryEntity = CompanyERP.Entities.Employee.SalaryPayment;
using AssetEntity = CompanyERP.Entities.Asset.AssetRegister;

namespace CompanyERP.Entities.Payment;

public class Payment : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Branch is required.")]
    [Display(Name = "Branch")]
    public int BranchId { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    [Display(Name = "Category")]
    public PaymentCategory Category { get; set; }

    [Required(ErrorMessage = "Payment number is required.")]
    [StringLength(30)]
    [Display(Name = "Payment No")]
    public string PaymentNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Payment date is required.")]
    [Display(Name = "Payment Date")]
    [DataType(DataType.Date)]
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Supplier")]
    public int? SupplierId { get; set; }

    [Display(Name = "Expense Entry")]
    public int? ExpenseEntryId { get; set; }

    [Display(Name = "Salary Payment")]
    public int? SalaryPaymentId { get; set; }

    [Display(Name = "Asset")]
    public int? AssetRegisterId { get; set; }

    [StringLength(50)]
    [Display(Name = "Source Module")]
    public string? SourceModule { get; set; }

    [StringLength(50)]
    [Display(Name = "Source Reference No")]
    public string? SourceReferenceNo { get; set; }

    [Required(ErrorMessage = "Payment method is required.")]
    [Display(Name = "Payment Method")]
    public int PaymentMethodId { get; set; }

    [Display(Name = "Account Type")]
    public PaymentAccountType AccountType { get; set; } = PaymentAccountType.Cash;

    [Display(Name = "Cash Account")]
    public int? CashAccountId { get; set; }

    [Display(Name = "Bank Account")]
    public int? BankAccountId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [StringLength(50)]
    [Display(Name = "Reference No")]
    public string? ReferenceNo { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Posted;

    [StringLength(500)]
    public string? Note { get; set; }

    public CompanyProfile? Company { get; set; }
    public Branch? Branch { get; set; }
    public CustomerEntity? Customer { get; set; }
    public SupplierEntity? Supplier { get; set; }
    public ExpenseEntity? ExpenseEntry { get; set; }
    public SalaryEntity? SalaryPayment { get; set; }
    public AssetEntity? AssetRegister { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public CashAccount? CashAccount { get; set; }
    public BankAccount? BankAccount { get; set; }
}