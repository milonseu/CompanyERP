using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.Accounting;

public class JournalEntryDetail : BaseEntity
{
    [Required(ErrorMessage = "Journal entry is required.")]
    [Display(Name = "Journal Entry")]
    public int JournalEntryId { get; set; }

    [Required(ErrorMessage = "Account is required.")]
    [Display(Name = "Account")]
    public int AccountId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Debit must be zero or greater.")]
    [DataType(DataType.Currency)]
    [Display(Name = "Debit")]
    public decimal Debit { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Credit must be zero or greater.")]
    [DataType(DataType.Currency)]
    [Display(Name = "Credit")]
    public decimal Credit { get; set; }

    [StringLength(500)]
    [Display(Name = "Note")]
    public string? Note { get; set; }

    public JournalEntry? JournalEntry { get; set; }
    public ChartOfAccount? Account { get; set; }
}