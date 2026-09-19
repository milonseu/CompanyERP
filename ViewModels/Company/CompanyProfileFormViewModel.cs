using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Company;

namespace CompanyERP.ViewModels.Company;

public class CompanyProfileFormViewModel
{
    public int? Id { get; set; }

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

    [Required(ErrorMessage = "Currency is required.")]
    [StringLength(10)]
    public string CurrencyCode { get; set; } = "BDT";

    [DataType(DataType.Date)]
    [Display(Name = "Incorporated On")]
    public DateTime? IncorporationDate { get; set; }

    [Range(1, 12)]
    [Display(Name = "Fiscal Year Start Month")]
    public int? FiscalYearStartMonth { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public CompanyProfile ToEntity()
    {
        return new CompanyProfile
        {
            Id = Id ?? 0,
            Code = Code,
            Name = Name,
            LegalName = LegalName,
            RegistrationNo = RegistrationNo,
            TaxId = TaxId,
            VatRegistrationNo = VatRegistrationNo,
            TradeLicenseNo = TradeLicenseNo,
            Phone = Phone,
            Email = Email,
            Website = Website,
            Address = Address,
            City = City,
            State = State,
            PostalCode = PostalCode,
            Country = Country,
            CurrencyCode = CurrencyCode,
            IncorporationDate = IncorporationDate,
            FiscalYearStartMonth = FiscalYearStartMonth,
            IsActive = IsActive
        };
    }

    public static CompanyProfileFormViewModel FromEntity(CompanyProfile profile)
    {
        return new CompanyProfileFormViewModel
        {
            Id = profile.Id,
            Code = profile.Code,
            Name = profile.Name,
            LegalName = profile.LegalName,
            RegistrationNo = profile.RegistrationNo,
            TaxId = profile.TaxId,
            VatRegistrationNo = profile.VatRegistrationNo,
            TradeLicenseNo = profile.TradeLicenseNo,
            Phone = profile.Phone,
            Email = profile.Email,
            Website = profile.Website,
            Address = profile.Address,
            City = profile.City,
            State = profile.State,
            PostalCode = profile.PostalCode,
            Country = profile.Country,
            CurrencyCode = profile.CurrencyCode,
            IncorporationDate = profile.IncorporationDate,
            FiscalYearStartMonth = profile.FiscalYearStartMonth,
            IsActive = profile.IsActive
        };
    }
}