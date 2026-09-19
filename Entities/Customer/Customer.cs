using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;

namespace CompanyERP.Entities.Customer;

public class Customer : BaseEntity
{
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
    public new bool IsActive { get; set; } = true;

    public CompanyProfile? Company { get; set; }
    public List<CustomerContact> Contacts { get; set; } = new();
    public List<CustomerAddress> Addresses { get; set; } = new();
}