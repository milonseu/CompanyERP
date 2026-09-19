using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.Supplier;

public class SupplierContact : BaseEntity
{
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

    public Supplier? Supplier { get; set; }
}