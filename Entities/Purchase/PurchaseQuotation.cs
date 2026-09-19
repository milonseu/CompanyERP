using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;
using SupplierEntity = CompanyERP.Entities.Supplier.Supplier;

namespace CompanyERP.Entities.Purchase;

public class PurchaseQuotation : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Supplier is required.")]
    [Display(Name = "Supplier")]
    public int SupplierId { get; set; }

    [Display(Name = "From Purchase Request")]
    public int? PurchaseRequestId { get; set; }

    [Required(ErrorMessage = "Quotation number is required.")]
    [StringLength(30)]
    [Display(Name = "Quotation No")]
    public string QuotationNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Quotation date is required.")]
    [Display(Name = "Quotation Date")]
    [DataType(DataType.Date)]
    public DateTime QuotationDate { get; set; } = DateTime.Today;

    [Display(Name = "Valid Until")]
    [DataType(DataType.Date)]
    public DateTime? ValidUntil { get; set; }

    public PurchaseQuotationStatus Status { get; set; } = PurchaseQuotationStatus.Draft;

    [StringLength(500)]
    public string? Note { get; set; }

    public CompanyProfile? Company { get; set; }
    public SupplierEntity? Supplier { get; set; }
    public PurchaseRequest? PurchaseRequest { get; set; }
    public List<PurchaseQuotationLine> Lines { get; set; } = new();
}

public class PurchaseQuotationLine : BaseEntity
{
    public int PurchaseQuotationId { get; set; }

    [Required(ErrorMessage = "Product is required.")]
    public int ProductId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Unit price must be zero or more.")]
    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    public PurchaseQuotation? PurchaseQuotation { get; set; }
    public Inventory.Product? Product { get; set; }

    [NotMapped]
    public decimal Total => Quantity * UnitPrice;
}