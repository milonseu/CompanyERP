using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;
using CompanyERP.Entities.CompanyBranch;
using SupplierEntity = CompanyERP.Entities.Supplier.Supplier;

namespace CompanyERP.Entities.Purchase;

public class PurchaseOrder : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Supplier is required.")]
    [Display(Name = "Supplier")]
    public int SupplierId { get; set; }

    [Required(ErrorMessage = "Branch is required.")]
    [Display(Name = "Branch")]
    public int BranchId { get; set; }

    [Required(ErrorMessage = "Warehouse is required.")]
    [Display(Name = "Warehouse")]
    public int WarehouseId { get; set; }

    [Display(Name = "From Quotation")]
    public int? PurchaseQuotationId { get; set; }

    [Required(ErrorMessage = "Order number is required.")]
    [StringLength(30)]
    [Display(Name = "Order No")]
    public string OrderNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Order date is required.")]
    [Display(Name = "Order Date")]
    [DataType(DataType.Date)]
    public DateTime OrderDate { get; set; } = DateTime.Today;

    [Display(Name = "Expected Delivery")]
    [DataType(DataType.Date)]
    public DateTime? ExpectedDeliveryDate { get; set; }

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    [StringLength(500)]
    public string? Note { get; set; }

    public CompanyProfile? Company { get; set; }
    public SupplierEntity? Supplier { get; set; }
    public Branch? Branch { get; set; }
    public Inventory.Warehouse? Warehouse { get; set; }
    public PurchaseQuotation? PurchaseQuotation { get; set; }
    public List<PurchaseOrderLine> Lines { get; set; } = new();

    [NotMapped]
    public decimal Total => Lines.Sum(l => l.Total);
}

public class PurchaseOrderLine : BaseEntity
{
    public int PurchaseOrderId { get; set; }

    [Required(ErrorMessage = "Product is required.")]
    public int ProductId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Unit cost must be zero or more.")]
    [Display(Name = "Unit Cost")]
    public decimal UnitCost { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }
    public Inventory.Product? Product { get; set; }

    [NotMapped]
    public decimal Total => Quantity * UnitCost;
}