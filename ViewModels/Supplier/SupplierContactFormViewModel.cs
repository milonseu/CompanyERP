using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Supplier;

namespace CompanyERP.ViewModels.Supplier;

public class SupplierContactFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Supplier is required.")]
    public int SupplierId { get; set; }

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

    public SupplierContact ToEntity()
    {
        return new SupplierContact
        {
            Id = Id ?? 0,
            SupplierId = SupplierId,
            ContactPerson = ContactPerson,
            Designation = Designation,
            Phone = Phone,
            Email = Email,
            IsPrimary = IsPrimary
        };
    }

    public static SupplierContactFormViewModel FromEntity(SupplierContact contact)
    {
        return new SupplierContactFormViewModel
        {
            Id = contact.Id,
            SupplierId = contact.SupplierId,
            ContactPerson = contact.ContactPerson,
            Designation = contact.Designation,
            Phone = contact.Phone,
            Email = contact.Email,
            IsPrimary = contact.IsPrimary
        };
    }
}