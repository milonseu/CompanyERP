using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Supplier;

namespace CompanyERP.ViewModels.Supplier;

public class SupplierAddressFormViewModel
{
    public int? Id { get; set; }

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

    public SupplierAddress ToEntity()
    {
        return new SupplierAddress
        {
            Id = Id ?? 0,
            SupplierId = SupplierId,
            AddressType = AddressType,
            AddressLine = AddressLine,
            City = City,
            State = State,
            PostalCode = PostalCode,
            Country = Country,
            IsPrimary = IsPrimary
        };
    }

    public static SupplierAddressFormViewModel FromEntity(SupplierAddress address)
    {
        return new SupplierAddressFormViewModel
        {
            Id = address.Id,
            SupplierId = address.SupplierId,
            AddressType = address.AddressType,
            AddressLine = address.AddressLine,
            City = address.City,
            State = address.State,
            PostalCode = address.PostalCode,
            Country = address.Country,
            IsPrimary = address.IsPrimary
        };
    }
}