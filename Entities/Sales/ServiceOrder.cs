using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;
using CompanyERP.Entities.CompanyBranch;
using CustomerEntity = CompanyERP.Entities.Customer.Customer;
using ServiceEntity = CompanyERP.Entities.Sales.Service;

namespace CompanyERP.Entities.Sales;

public class ServiceOrder : BaseEntity
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

    [Required(ErrorMessage = "Service is required.")]
    [Display(Name = "Service")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "Order number is required.")]
    [StringLength(30)]
    [Display(Name = "Order No")]
    public string OrderNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Order date is required.")]
    [Display(Name = "Order Date")]
    [DataType(DataType.Date)]
    public DateTime OrderDate { get; set; } = DateTime.Today;

    [Display(Name = "Service Date")]
    [DataType(DataType.Date)]
    public DateTime? ServiceDate { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Unit price must be zero or more.")]
    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    public ServiceOrderStatus Status { get; set; } = ServiceOrderStatus.Pending;

    [StringLength(500)]
    public string? Note { get; set; }

    public CompanyProfile? Company { get; set; }
    public Branch? Branch { get; set; }
    public CustomerEntity? Customer { get; set; }
    public ServiceEntity? Service { get; set; }
    public List<ServiceDelivery> Deliveries { get; set; } = new();

    [NotMapped]
    public decimal Total => Quantity * UnitPrice;

    [NotMapped]
    public decimal DeliveredQuantity => Deliveries.Sum(d => d.Quantity);
}