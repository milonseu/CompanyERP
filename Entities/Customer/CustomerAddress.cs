using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.Customer;

public enum CustomerAddressType
{
    [Display(Name = "Office")]
    Office,

    [Display(Name = "Billing")]
    Billing,

    [Display(Name = "Shipping")]
    Shipping,

    [Display(Name = "Delivery")]
    Delivery
}

public class CustomerAddress : BaseEntity
{
    [Required(ErrorMessage = "Customer is required.")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Address type is required.")]
    [Display(Name = "Address Type")]
    public CustomerAddressType AddressType { get; set; }

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

    public Customer? Customer { get; set; }
}