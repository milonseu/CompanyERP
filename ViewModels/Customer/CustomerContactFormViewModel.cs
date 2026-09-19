using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Customer;

namespace CompanyERP.ViewModels.Customer;

public class CustomerContactFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Customer is required.")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Contact person is required.")]
    [StringLength(100)]
    [Display(Name = "Contact Person")]
    public string ContactPerson { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Designation")]
    public string? Designation { get; set; }

    [StringLength(30)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [StringLength(100)]
    [EmailAddress]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Display(Name = "Primary")]
    public bool IsPrimary { get; set; }

    public CustomerContact ToEntity()
    {
        return new CustomerContact
        {
            Id = Id ?? 0,
            CustomerId = CustomerId,
            ContactPerson = ContactPerson,
            Designation = Designation,
            Phone = Phone,
            Email = Email,
            IsPrimary = IsPrimary
        };
    }

    public static CustomerContactFormViewModel FromEntity(CustomerContact contact)
    {
        return new CustomerContactFormViewModel
        {
            Id = contact.Id,
            CustomerId = contact.CustomerId,
            ContactPerson = contact.ContactPerson,
            Designation = contact.Designation,
            Phone = contact.Phone,
            Email = contact.Email,
            IsPrimary = contact.IsPrimary
        };
    }
}