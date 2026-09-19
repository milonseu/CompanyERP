using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Customer;

namespace CompanyERP.ViewModels.Customer;

public class CustomerAddressFormViewModel
{
    public int? Id { get; set; }

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

    public CustomerAddress ToEntity()
    {
        return new CustomerAddress
        {
            Id = Id ?? 0,
            CustomerId = CustomerId,
            AddressType = AddressType,
            AddressLine = AddressLine,
            City = City,
            State = State,
            PostalCode = PostalCode,
            Country = Country,
            IsPrimary = IsPrimary
        };
    }

    public static CustomerAddressFormViewModel FromEntity(CustomerAddress address)
    {
        return new CustomerAddressFormViewModel
        {
            Id = address.Id,
            CustomerId = address.CustomerId,
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