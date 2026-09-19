using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;
using SupplierEntity = CompanyERP.Entities.Supplier.Supplier;

namespace CompanyERP.Entities.Asset;

public class AssetAcquisition : BaseEntity
{
    public int AssetRegisterId { get; set; }

    [Required(ErrorMessage = "Acquisition date is required.")]
    [Display(Name = "Acquisition Date")]
    [DataType(DataType.Date)]
    public DateTime AcquisitionDate { get; set; } = DateTime.Today;

    [Display(Name = "Supplier")]
    public int? SupplierId { get; set; }

    [Display(Name = "Payment Type")]
    public AcquisitionPaymentType PaymentType { get; set; } = AcquisitionPaymentType.Bank;

    [Range(0, double.MaxValue, ErrorMessage = "Amount paid must be zero or more.")]
    [Display(Name = "Amount Paid")]
    public decimal AmountPaid { get; set; }

    [StringLength(100)]
    [Display(Name = "Payment Reference")]
    public string? PaymentReference { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public AssetRegister? AssetRegister { get; set; }
    public SupplierEntity? Supplier { get; set; }
}