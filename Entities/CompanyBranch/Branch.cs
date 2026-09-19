using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;

namespace CompanyERP.Entities.CompanyBranch;

public class Branch : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Branch type is required.")]
    [Display(Name = "Branch Type")]
    public int BranchTypeId { get; set; }

    [Required(ErrorMessage = "Branch code is required.")]
    [StringLength(20)]
    [Display(Name = "Branch Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Branch name is required.")]
    [StringLength(150)]
    [Display(Name = "Branch Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Short Name")]
    public string? ShortName { get; set; }

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

    [StringLength(30)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Opening Date")]
    public DateTime? OpeningDate { get; set; }

    [Display(Name = "Head Office")]
    public bool IsHeadOffice { get; set; }

    public CompanyProfile? Company { get; set; }
    public BranchType? BranchType { get; set; }
    public BranchSettings? Settings { get; set; }
}