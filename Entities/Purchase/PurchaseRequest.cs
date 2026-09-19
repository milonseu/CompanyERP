using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;
using CompanyERP.Entities.CompanyBranch;

namespace CompanyERP.Entities.Purchase;

public class PurchaseRequest : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Display(Name = "Branch")]
    public int? BranchId { get; set; }

    [Required(ErrorMessage = "Request number is required.")]
    [StringLength(30)]
    [Display(Name = "Request No")]
    public string RequestNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Request date is required.")]
    [Display(Name = "Request Date")]
    [DataType(DataType.Date)]
    public DateTime RequestDate { get; set; } = DateTime.Today;

    [Display(Name = "Requested By")]
    [StringLength(100)]
    public string? RequestedBy { get; set; }

    public PurchaseRequestStatus Status { get; set; } = PurchaseRequestStatus.Draft;

    [StringLength(500)]
    public string? Note { get; set; }

    public CompanyProfile? Company { get; set; }
    public Branch? Branch { get; set; }
    public List<PurchaseRequestLine> Lines { get; set; } = new();
}

public class PurchaseRequestLine : BaseEntity
{
    public int PurchaseRequestId { get; set; }

    [Required(ErrorMessage = "Product is required.")]
    public int ProductId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public decimal Quantity { get; set; }

    public PurchaseRequest? PurchaseRequest { get; set; }
    public Inventory.Product? Product { get; set; }
}