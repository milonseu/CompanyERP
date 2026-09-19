using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;
using CompanyERP.Entities.Inventory;

namespace CompanyERP.Entities.Inventory;

public enum StockTransactionType
{
    [Display(Name = "Opening Stock")]
    Opening,

    [Display(Name = "Purchase Receiving")]
    PurchaseReceiving,

    [Display(Name = "Sales Stock Out")]
    SalesStockOut,

    [Display(Name = "Sales Return")]
    SalesReturn,

    [Display(Name = "Purchase Return")]
    PurchaseReturn,

    [Display(Name = "Stock Transfer")]
    StockTransfer,

    [Display(Name = "Stock Adjustment")]
    Adjustment
}

public class StockTransaction : BaseEntity
{
    [Required]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Product is required.")]
    [Display(Name = "Product")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Warehouse is required.")]
    [Display(Name = "Warehouse")]
    public int WarehouseId { get; set; }

    [Required(ErrorMessage = "Transaction type is required.")]
    [Display(Name = "Type")]
    public StockTransactionType Type { get; set; }

    [Required(ErrorMessage = "Quantity is required.")]
    [Range(typeof(decimal), "-999999999999", "999999999999", ErrorMessage = "Quantity is outside the valid range.")]
    [Display(Name = "Quantity")]
    public decimal Quantity { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Unit cost cannot be negative.")]
    [Display(Name = "Unit Cost")]
    public decimal UnitCost { get; set; }

    [Required(ErrorMessage = "Reference number is required.")]
    [StringLength(30)]
    [Display(Name = "Reference No")]
    public string ReferenceNo { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Required(ErrorMessage = "Transaction date is required.")]
    [Display(Name = "Transaction Date")]
    public DateTime TransactionDate { get; set; }

    [Display(Name = "To Warehouse")]
    public int? ToWarehouseId { get; set; }

    [StringLength(500)]
    [Display(Name = "Note")]
    public string? Note { get; set; }

    public CompanyProfile? Company { get; set; }
    public Product? Product { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Warehouse? ToWarehouse { get; set; }
}