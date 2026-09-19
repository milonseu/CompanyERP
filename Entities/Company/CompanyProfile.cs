using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.Company;

public class CompanyProfile : BaseEntity
{
    [Required(ErrorMessage = "Company code is required.")]
    [StringLength(20)]
    [Display(Name = "Company Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Company name is required.")]
    [StringLength(150)]
    [Display(Name = "Company Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Legal name is required.")]
    [StringLength(150)]
    [Display(Name = "Legal Name")]
    public string LegalName { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Registration No")]
    public string? RegistrationNo { get; set; }

    [StringLength(50)]
    [Display(Name = "Tax ID / BIN")]
    public string? TaxId { get; set; }

    [StringLength(50)]
    [Display(Name = "VAT Registration No")]
    public string? VatRegistrationNo { get; set; }

    [StringLength(50)]
    [Display(Name = "Trade License No")]
    public string? TradeLicenseNo { get; set; }

    [StringLength(30)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(150)]
    public string? Website { get; set; }

    [StringLength(250)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(20)]
    [Display(Name = "Postal Code")]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    [Required]
    [StringLength(10)]
    [Display(Name = "Currency")]
    public string CurrencyCode { get; set; } = "BDT";

    [DataType(DataType.Date)]
    [Display(Name = "Incorporated On")]
    public DateTime? IncorporationDate { get; set; }

    [Range(1, 12)]
    [Display(Name = "Fiscal Year Start Month")]
    public int? FiscalYearStartMonth { get; set; }
}