using System.ComponentModel.DataAnnotations;

namespace CompanyERP.ViewModels.Accounting;

public class JournalEntryLineFormViewModel
{
    public int AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Note { get; set; }
}

public class JournalEntryFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Company is required.")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Entry date is required.")]
    [DataType(DataType.Date)]
    public DateTime EntryDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Entry number is required.")]
    [StringLength(30)]
    public string EntryNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public List<JournalEntryLineFormViewModel> Lines { get; set; } = [new(), new()];
}