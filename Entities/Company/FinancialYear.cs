using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.Company;

public class FinancialYear : BaseEntity
{
    [Required(ErrorMessage = "Year code is required.")]
    [StringLength(20)]
    [Display(Name = "Year Code")]
    public string YearCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Financial year name is required.")]
    [StringLength(50)]
    [Display(Name = "Financial Year")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; }

    [Display(Name = "Closed")]
    public bool IsClosed { get; set; }

    public ICollection<AccountingPeriod> AccountingPeriods { get; set; } = [];
}