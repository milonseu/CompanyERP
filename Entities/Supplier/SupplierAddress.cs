using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.Supplier;

public enum SupplierAddressType
{
    [Display(Name = "Office")]
    Office,

    [Display(Name = "Factory")]
    Factory,

    [Display(Name = "Billing")]
    Billing,

    [Display(Name = "Shipping")]
    Shipping
}

public class SupplierAddress : BaseEntity
{
    [Required(ErrorMessage = "Supplier is required.")]
    public int SupplierId { get; set; }

    [Required(ErrorMessage = "Address type is required.")]
    [Display(Name = "Address Type")]
    public SupplierAddressType AddressType { get; set; }

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(250)]
    [Display(Name = "Address")]
    public string AddressLine { get; set; } = string.Empty;

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(20)]
    [Display(Name = "Postal Code")]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    [Display(Name = "Primary")]
    public bool IsPrimary { get; set; }

    public Supplier? Supplier { get; set; }
}