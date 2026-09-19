using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Supplier;

namespace CompanyERP.ViewModels.Supplier;

public class SupplierFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Supplier code is required.")]
    [StringLength(20)]
    [Display(Name = "Supplier Code")]
    public string SupplierCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Supplier name is required.")]
    [StringLength(150)]
    [Display(Name = "Supplier Name")]
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

    [StringLength(30)]
    [Display(Name = "Fax")]
    public string? Fax { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Opening payable cannot be negative.")]
    [Display(Name = "Opening Payable")]
    public decimal OpeningPayable { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Opening Balance Date")]
    public DateTime? OpeningBalanceDate { get; set; }

    [StringLength(500)]
    [Display(Name = "Note")]
    public string? Note { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public Entities.Supplier.Supplier ToEntity()
    {
        return new Entities.Supplier.Supplier
        {
            Id = Id ?? 0,
            CompanyId = CompanyId,
            SupplierCode = SupplierCode,
            Name = Name,
            LegalName = LegalName,
            ContactPerson = ContactPerson,
            Phone = Phone,
            Email = Email,
            Website = Website,
            Fax = Fax,
            OpeningPayable = OpeningPayable,
            OpeningBalanceDate = OpeningBalanceDate,
            Note = Note,
            IsActive = IsActive
        };
    }

    public static SupplierFormViewModel FromEntity(Entities.Supplier.Supplier supplier)
    {
        return new SupplierFormViewModel
        {
            Id = supplier.Id,
            CompanyId = supplier.CompanyId,
            SupplierCode = supplier.SupplierCode,
            Name = supplier.Name,
            LegalName = supplier.LegalName,
            ContactPerson = supplier.ContactPerson,
            Phone = supplier.Phone,
            Email = supplier.Email,
            Website = supplier.Website,
            Fax = supplier.Fax,
            OpeningPayable = supplier.OpeningPayable,
            OpeningBalanceDate = supplier.OpeningBalanceDate,
            Note = supplier.Note,
            IsActive = supplier.IsActive
        };
    }
}