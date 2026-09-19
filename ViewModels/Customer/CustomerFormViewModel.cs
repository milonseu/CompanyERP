using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Customer;

namespace CompanyERP.ViewModels.Customer;

public class CustomerFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Customer code is required.")]
    [StringLength(20)]
    [Display(Name = "Customer Code")]
    public string CustomerCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer name is required.")]
    [StringLength(150)]
    [Display(Name = "Customer Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Legal Name")]
    public string? LegalName { get; set; }

    [StringLength(100)]
    [Display(Name = "Contact Person")]
    public string? ContactPerson { get; set; }

    [StringLength(30)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [StringLength(100)]
    [EmailAddress]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(200)]
    [Display(Name = "Website")]
    public string? Website { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Opening receivable cannot be negative.")]
    [Display(Name = "Opening Receivable")]
    public decimal OpeningReceivable { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Opening Balance Date")]
    public DateTime? OpeningBalanceDate { get; set; }

    [StringLength(500)]
    [Display(Name = "Note")]
    public string? Note { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public Entities.Customer.Customer ToEntity()
    {
        return new Entities.Customer.Customer
        {
            Id = Id ?? 0,
            CompanyId = CompanyId,
            CustomerCode = CustomerCode,
            Name = Name,
            LegalName = LegalName,
            ContactPerson = ContactPerson,
            Phone = Phone,
            Email = Email,
            Website = Website,
            OpeningReceivable = OpeningReceivable,
            OpeningBalanceDate = OpeningBalanceDate,
            Note = Note,
            IsActive = IsActive
        };
    }

    public static CustomerFormViewModel FromEntity(Entities.Customer.Customer customer)
    {
        return new CustomerFormViewModel
        {
            Id = customer.Id,
            CompanyId = customer.CompanyId,
            CustomerCode = customer.CustomerCode,
            Name = customer.Name,
            LegalName = customer.LegalName,
            ContactPerson = customer.ContactPerson,
            Phone = customer.Phone,
            Email = customer.Email,
            Website = customer.Website,
            OpeningReceivable = customer.OpeningReceivable,
            OpeningBalanceDate = customer.OpeningBalanceDate,
            Note = customer.Note,
            IsActive = customer.IsActive
        };
    }
}