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

    /// <summary>
    /// Moving-average inventory value held in this warehouse. Quantity x AverageCost is only an
    /// estimate, so the balance keeps its own value ledger: receipts add qty x cost, issues relieve
    /// qty x the current average. The accounting module relieves Inventory and debits COGS with the
    /// same figure, which keeps account 1300 tied to this subledger.
    /// </summary>
    [Display(Name = "Total Value")]
    public decimal TotalValue { get; set; }

    public Product? Product { get; set; }
    public Warehouse? Warehouse { get; set; }
}