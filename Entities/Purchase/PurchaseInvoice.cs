using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;
using SupplierEntity = CompanyERP.Entities.Supplier.Supplier;

namespace CompanyERP.Entities.Purchase;

public class PurchaseInvoice : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Branch is required.")]
    [Display(Name = "Branch")]
    public int BranchId { get; set; }

    [Required(ErrorMessage = "Supplier is required.")]
    [Display(Name = "Supplier")]
    public int SupplierId { get; set; }

    [Required(ErrorMessage = "Purchase order is required.")]
    [Display(Name = "Purchase Order")]
    public int PurchaseOrderId { get; set; }

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

    public PurchaseInvoiceStatus Status { get; set; } = PurchaseInvoiceStatus.Unpaid;

    [StringLength(500)]
    public string? Note { get; set; }

    public CompanyProfile? Company { get; set; }
    public CompanyBranch.Branch? Branch { get; set; }
    public SupplierEntity? Supplier { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public List<PurchaseInvoiceLine> Lines { get; set; } = new();

    [NotMapped]
    public decimal Total => Lines.Sum(l => l.Amount);
}

public class PurchaseInvoiceLine : BaseEntity
{
    public int PurchaseInvoiceId { get; set; }

    [Required(ErrorMessage = "Product is required.")]
    public int ProductId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Unit price must be zero or more.")]
    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    public PurchaseInvoice? PurchaseInvoice { get; set; }
    public Inventory.Product? Product { get; set; }

    [NotMapped]
    public decimal Amount => Quantity * UnitPrice;
}