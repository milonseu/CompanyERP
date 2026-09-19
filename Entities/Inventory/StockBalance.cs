using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Inventory;

namespace CompanyERP.Entities.Inventory;

public class StockBalance : BaseEntity
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    public int WarehouseId { get; set; }

    [Required]
    [Display(Name = "Quantity")]
    public decimal Quantity { get; set; }

    [Display(Name = "Average Cost")]
    public decimal AverageCost { get; set; }

    public Product? Product { get; set; }
    public Warehouse? Warehouse { get; set; }
}