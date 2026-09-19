using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;
using CompanyERP.Entities.CompanyBranch;
using CompanyERP.Entities.Inventory;
using SupplierEntity = CompanyERP.Entities.Supplier.Supplier;

namespace CompanyERP.Entities.Purchase;

public class PurchaseReceiving : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Purchase order is required.")]
    [Display(Name = "Purchase Order")]
    public int PurchaseOrderId { get; set; }

    [Required(ErrorMessage = "Supplier is required.")]
    [Display(Name = "Supplier")]
    public int SupplierId { get; set; }

    [Required(ErrorMessage = "Warehouse is required.")]
    [Display(Name = "Warehouse")]
    public int WarehouseId { get; set; }

    [Required(ErrorMessage = "Receiving number is required.")]
    [StringLength(30)]
    [Display(Name = "Receiving No")]
    public string ReceivingNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Received date is required.")]
    [Display(Name = "Received Date")]
    [DataType(DataType.Date)]
    public DateTime ReceivedDate { get; set; } = DateTime.Today;

    [StringLength(100)]
    [Display(Name = "Reference No")]
    public string? ReferenceNo { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public CompanyProfile? Company { get; set; }
    public Branch? Branch { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public SupplierEntity? Supplier { get; set; }
    public Warehouse? Warehouse { get; set; }
    public List<PurchaseReceivingLine> Lines { get; set; } = new();
}

public class PurchaseReceivingLine : BaseEntity
{
    public int PurchaseReceivingId { get; set; }

    [Required(ErrorMessage = "Product is required.")]
    public int ProductId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Unit cost must be zero or more.")]
    [Display(Name = "Unit Cost")]
    public decimal UnitCost { get; set; }

    public PurchaseReceiving? PurchaseReceiving { get; set; }
    public Product? Product { get; set; }
}