using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;

namespace CompanyERP.Entities.Inventory;

public class StockTransfer : BaseEntity
{
    [Required]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Product is required.")]
    [Display(Name = "Product")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "From warehouse is required.")]
    [Display(Name = "From Warehouse")]
    public int FromWarehouseId { get; set; }

    [Required(ErrorMessage = "To warehouse is required.")]
    [Display(Name = "To Warehouse")]
    public int ToWarehouseId { get; set; }

    [Range(typeof(decimal), "0.001", "999999999999", ErrorMessage = "Transfer quantity must be positive.")]
    [Display(Name = "Quantity")]
    public decimal Quantity { get; set; }

    [DataType(DataType.Date)]
    [Required(ErrorMessage = "Transfer date is required.")]
    [Display(Name = "Transfer Date")]
    public DateTime TransferDate { get; set; }

    [StringLength(500)]
    [Display(Name = "Note")]
    public string? Note { get; set; }

    public CompanyProfile? Company { get; set; }
    public Product? Product { get; set; }
    public Warehouse? FromWarehouse { get; set; }
    public Warehouse? ToWarehouse { get; set; }
}