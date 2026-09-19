using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Inventory;

namespace CompanyERP.ViewModels.Inventory;

public class StockInViewModel
{
    [Required(ErrorMessage = "Product is required.")]
    [Display(Name = "Product")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Warehouse is required.")]
    [Display(Name = "Warehouse")]
    public int WarehouseId { get; set; }

    [Required(ErrorMessage = "Transaction type is required.")]
    [Display(Name = "Type")]
    public StockTransactionType Type { get; set; } = StockTransactionType.PurchaseReceiving;

    [Range(0.0001, double.MaxValue, ErrorMessage = "Quantity must be positive.")]
    [Display(Name = "Quantity")]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Unit cost cannot be negative.")]
    [Display(Name = "Unit Cost")]
    public decimal UnitCost { get; set; }

    [Required(ErrorMessage = "Reference number is required.")]
    [StringLength(30)]
    [Display(Name = "Reference No")]
    public string ReferenceNo { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Required(ErrorMessage = "Transaction date is required.")]
    [Display(Name = "Transaction Date")]
    public DateTime TransactionDate { get; set; } = DateTime.Today;

    [StringLength(500)]
    [Display(Name = "Note")]
    public string? Note { get; set; }
}

public class StockOutViewModel
{
    [Required(ErrorMessage = "Product is required.")]
    [Display(Name = "Product")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Warehouse is required.")]
    [Display(Name = "Warehouse")]
    public int WarehouseId { get; set; }

    [Required(ErrorMessage = "Transaction type is required.")]
    [Display(Name = "Type")]
    public StockTransactionType Type { get; set; } = StockTransactionType.SalesStockOut;

    [Display(Name = "Available Quantity")]
    public decimal AvailableQty { get; set; }

    [Range(0.0001, double.MaxValue, ErrorMessage = "Quantity must be positive.")]
    [Display(Name = "Quantity")]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Unit cost cannot be negative.")]
    [Display(Name = "Unit Cost")]
    public decimal UnitCost { get; set; }

    [Required(ErrorMessage = "Reference number is required.")]
    [StringLength(30)]
    [Display(Name = "Reference No")]
    public string ReferenceNo { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Required(ErrorMessage = "Transaction date is required.")]
    [Display(Name = "Transaction Date")]
    public DateTime TransactionDate { get; set; } = DateTime.Today;

    [StringLength(500)]
    [Display(Name = "Note")]
    public string? Note { get; set; }
}

public class StockTransferViewModel
{
    [Required(ErrorMessage = "Product is required.")]
    [Display(Name = "Product")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "From warehouse is required.")]
    [Display(Name = "From Warehouse")]
    public int FromWarehouseId { get; set; }

    [Required(ErrorMessage = "To warehouse is required.")]
    [Display(Name = "To Warehouse")]
    public int ToWarehouseId { get; set; }

    [Range(0.0001, double.MaxValue, ErrorMessage = "Quantity must be positive.")]
    [Display(Name = "Quantity")]
    public decimal Quantity { get; set; }

    [DataType(DataType.Date)]
    [Required(ErrorMessage = "Transfer date is required.")]
    [Display(Name = "Transfer Date")]
    public DateTime TransferDate { get; set; } = DateTime.Today;

    [StringLength(500)]
    [Display(Name = "Note")]
    public string? Note { get; set; }
}

public class StockAdjustmentViewModel
{
    [Required(ErrorMessage = "Product is required.")]
    [Display(Name = "Product")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Warehouse is required.")]
    [Display(Name = "Warehouse")]
    public int WarehouseId { get; set; }

    [Display(Name = "Current Quantity")]
    public decimal CurrentQty { get; set; }

    [Display(Name = "Quantity (+/-)")]
    public decimal Quantity { get; set; }

    [Required(ErrorMessage = "Reference number is required.")]
    [StringLength(30)]
    [Display(Name = "Reference No")]
    public string ReferenceNo { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Required(ErrorMessage = "Transaction date is required.")]
    [Display(Name = "Transaction Date")]
    public DateTime TransactionDate { get; set; } = DateTime.Today;

    [StringLength(500)]
    [Display(Name = "Note")]
    public string? Note { get; set; }
}