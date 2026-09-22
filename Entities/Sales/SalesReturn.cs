using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;
using CompanyERP.Entities.CompanyBranch;
using CustomerEntity = CompanyERP.Entities.Customer.Customer;
using ProductEntity = CompanyERP.Entities.Inventory.Product;
using ServiceEntity = CompanyERP.Entities.Sales.Service;

namespace CompanyERP.Entities.Sales;

public class SalesReturn : BaseEntity
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

    [Required(ErrorMessage = "Sales invoice is required.")]
    [Display(Name = "Sales Invoice")]
    public int SalesInvoiceId { get; set; }

    [Required(ErrorMessage = "Return number is required.")]
    [StringLength(30)]
    [Display(Name = "Return No")]
    public string ReturnNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Return date is required.")]
    [Display(Name = "Return Date")]
    [DataType(DataType.Date)]
    public DateTime ReturnDate { get; set; } = DateTime.Today;

    public SalesReturnStatus Status { get; set; } = SalesReturnStatus.Posted;

    [StringLength(500)]
    public string? Note { get; set; }

    public CompanyProfile? Company { get; set; }
    public Branch? Branch { get; set; }
    public CustomerEntity? Customer { get; set; }
    public SalesInvoice? SalesInvoice { get; set; }
    public List<SalesReturnLine> Lines { get; set; } = new();

    [NotMapped]
    public decimal RefundTotal => Lines.Sum(l => l.Amount);
}

public class SalesReturnLine : BaseEntity
{
    public int SalesReturnId { get; set; }

    [Display(Name = "Item Type")]
    public SalesItemType ItemType { get; set; } = SalesItemType.Product;

    [Display(Name = "Product")]
    public int? ProductId { get; set; }

    [Display(Name = "Service")]
    public int? ServiceId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Refund unit price must be zero or more.")]
    [Display(Name = "Refund Unit Price")]
    public decimal UnitPrice { get; set; }

    public SalesReturn? SalesReturn { get; set; }
    public ProductEntity? Product { get; set; }
    public ServiceEntity? Service { get; set; }

    [NotMapped]
    public decimal Amount => Quantity * UnitPrice;
}