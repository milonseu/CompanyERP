using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;
using CompanyERP.Entities.CompanyBranch;
using CustomerEntity = CompanyERP.Entities.Customer.Customer;
using ProductEntity = CompanyERP.Entities.Inventory.Product;
using ServiceEntity = CompanyERP.Entities.Sales.Service;

namespace CompanyERP.Entities.Sales;

public class SalesOrder : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Branch is required.")]
    [Display(Name = "Branch")]
    public int BranchId { get; set; }

    [Required(ErrorMessage = "Customer is required.")]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Order number is required.")]
    [StringLength(30)]
    [Display(Name = "Order No")]
    public string OrderNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Order date is required.")]
    [Display(Name = "Order Date")]
    [DataType(DataType.Date)]
    public DateTime OrderDate { get; set; } = DateTime.Today;

    [Display(Name = "Expected Delivery Date")]
    [DataType(DataType.Date)]
    public DateTime? ExpectedDeliveryDate { get; set; }

    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;

    [StringLength(500)]
    public string? Note { get; set; }

    public CompanyProfile? Company { get; set; }
    public Branch? Branch { get; set; }
    public CustomerEntity? Customer { get; set; }
    public List<SalesOrderLine> Lines { get; set; } = new();

    [NotMapped]
    public decimal Total => Lines.Sum(l => l.Amount);
}

public class SalesOrderLine : BaseEntity
{
    public int SalesOrderId { get; set; }

    [Display(Name = "Item Type")]
    public SalesItemType ItemType { get; set; } = SalesItemType.Product;

    [Display(Name = "Product")]
    public int? ProductId { get; set; }

    [Display(Name = "Service")]
    public int? ServiceId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Unit price must be zero or more.")]
    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    public SalesOrder? SalesOrder { get; set; }
    public ProductEntity? Product { get; set; }
    public ServiceEntity? Service { get; set; }

    [NotMapped]
    public decimal Amount => Quantity * UnitPrice;
}