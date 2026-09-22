using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;
using CompanyERP.Entities.CompanyBranch;
using CustomerEntity = CompanyERP.Entities.Customer.Customer;
using ProductEntity = CompanyERP.Entities.Inventory.Product;
using ServiceEntity = CompanyERP.Entities.Sales.Service;
using WarehouseEntity = CompanyERP.Entities.Inventory.Warehouse;

namespace CompanyERP.Entities.Sales;

public class SalesInvoice : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Branch is required.")]
    [Display(Name = "Branch")]
    public int BranchId { get; set; }

    [Required(ErrorMessage = "Customer is required.")]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Sales order is required.")]
    [Display(Name = "Sales Order")]
    public int SalesOrderId { get; set; }

    [Required(ErrorMessage = "Warehouse is required for product stock-out.")]
    [Display(Name = "Warehouse")]
    public int WarehouseId { get; set; }

    [Required(ErrorMessage = "Invoice number is required.")]
    [StringLength(30)]
    [Display(Name = "Invoice No")]
    public string InvoiceNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Invoice date is required.")]
    [Display(Name = "Invoice Date")]
    [DataType(DataType.Date)]
    public DateTime InvoiceDate { get; set; } = DateTime.Today;

    [Display(Name = "Due Date")]
    [DataType(DataType.Date)]
    public DateTime? DueDate { get; set; }

    public SalesInvoiceStatus Status { get; set; } = SalesInvoiceStatus.Unpaid;

    [Display(Name = "Payment Type")]
    public SalesPaymentType PaymentType { get; set; } = SalesPaymentType.OnAccount;

    [Range(0, double.MaxValue, ErrorMessage = "Amount paid must be zero or more.")]
    [Display(Name = "Amount Paid")]
    public decimal AmountPaid { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public CompanyProfile? Company { get; set; }
    public Branch? Branch { get; set; }
    public CustomerEntity? Customer { get; set; }
    public SalesOrder? SalesOrder { get; set; }
    public WarehouseEntity? Warehouse { get; set; }
    public List<SalesInvoiceLine> Lines { get; set; } = new();

    [NotMapped]
    public decimal Total => Lines.Sum(l => l.Amount);
}

public class SalesInvoiceLine : BaseEntity
{
    public int SalesInvoiceId { get; set; }

    [Display(Name = "Item Type")]
    public SalesItemType ItemType { get; set; } = SalesItemType.Product;

    [Display(Name = "Product")]
    public int? ProductId { get; set; }

    [Display(Name = "Service")]
    public int? ServiceId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Unit price must be zero or more.")]
    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    public SalesInvoice? SalesInvoice { get; set; }
    public ProductEntity? Product { get; set; }
    public ServiceEntity? Service { get; set; }

    [NotMapped]
    public decimal Amount => Quantity * UnitPrice;
}