using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;
using CompanyERP.Entities.CompanyBranch;
using SupplierEntity = CompanyERP.Entities.Supplier.Supplier;

namespace CompanyERP.Entities.Purchase;

public class PurchaseReturn : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Supplier is required.")]
    [Display(Name = "Supplier")]
    public int SupplierId { get; set; }

    [Required(ErrorMessage = "Invoice is required.")]
    [Display(Name = "Purchase Invoice")]
    public int PurchaseInvoiceId { get; set; }

    [Required(ErrorMessage = "Branch is required.")]
    [Display(Name = "Branch")]
    public int BranchId { get; set; }

    [Required(ErrorMessage = "Warehouse is required.")]
    [Display(Name = "Warehouse")]
    public int WarehouseId { get; set; }

    [Required(ErrorMessage = "Return number is required.")]
    [StringLength(30)]
    [Display(Name = "Return No")]
    public string ReturnNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Return date is required.")]
    [Display(Name = "Return Date")]
    [DataType(DataType.Date)]
    public DateTime ReturnDate { get; set; } = DateTime.Today;

    [StringLength(100)]
    [Display(Name = "Reference No")]
    public string? ReferenceNo { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public CompanyProfile? Company { get; set; }
    public Branch? Branch { get; set; }
    public SupplierEntity? Supplier { get; set; }
    public PurchaseInvoice? PurchaseInvoice { get; set; }
    public Inventory.Warehouse? Warehouse { get; set; }
    public List<PurchaseReturnLine> Lines { get; set; } = new();

    [NotMapped]
    public decimal Total => Lines.Sum(l => l.Quantity * l.UnitCost);
}

public class PurchaseReturnLine : BaseEntity
{
    public int PurchaseReturnId { get; set; }

    [Required(ErrorMessage = "Product is required.")]
    public int ProductId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Unit cost must be zero or more.")]
    [Display(Name = "Unit Cost")]
    public decimal UnitCost { get; set; }

    public PurchaseReturn? PurchaseReturn { get; set; }
    public Inventory.Product? Product { get; set; }
}