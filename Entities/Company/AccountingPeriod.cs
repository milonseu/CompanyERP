using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.Company;

public enum AccountingPeriodStatus
{
    [Display(Name = "Open")]
    Open = 1,

    [Display(Name = "Closed")]
    Closed = 2
}

public class AccountingPeriod : BaseEntity
{
    [Required(ErrorMessage = "Financial year is required.")]
    [Display(Name = "Financial Year")]
    public int FinancialYearId { get; set; }

    [Required(ErrorMessage = "Period code is required.")]
    [StringLength(20)]
    [Display(Name = "Period Code")]
    public string PeriodCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Period name is required.")]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; }

    [Display(Name = "Status")]
    public AccountingPeriodStatus Status { get; set; } = AccountingPeriodStatus.Open;

    public FinancialYear? FinancialYear { get; set; }
}