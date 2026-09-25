using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;

namespace CompanyERP.Entities.Accounting;

public class JournalEntry : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Display(Name = "Branch")]
    public int? BranchId { get; set; }

    [Required(ErrorMessage = "Journal entry number is required.")]
    [StringLength(30)]
    [Display(Name = "Entry No")]
    public string EntryNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Entry date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Entry Date")]
    public DateTime EntryDate { get; set; } = DateTime.Today;

    [Display(Name = "Accounting Period")]
    public int? AccountingPeriodId { get; set; }

    [StringLength(20)]
    [Display(Name = "Period")]
    public string? PeriodKey { get; set; }

    [StringLength(50)]
    [Display(Name = "Source Module")]
    public string? SourceModule { get; set; }

    [StringLength(50)]
    [Display(Name = "Source Reference")]
    public string? SourceReference { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(500)]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Total Debit")]
    [DataType(DataType.Currency)]
    public decimal TotalDebit { get; set; }

    [Display(Name = "Total Credit")]
    [DataType(DataType.Currency)]
    public decimal TotalCredit { get; set; }

    public CompanyProfile? Company { get; set; }
    public CompanyBranch.Branch? Branch { get; set; }
    public AccountingPeriod? AccountingPeriod { get; set; }
    public List<JournalEntryDetail> Details { get; set; } = [];
}