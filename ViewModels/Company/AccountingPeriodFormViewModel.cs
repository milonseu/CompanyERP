using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Company;

namespace CompanyERP.ViewModels.Company;

public class AccountingPeriodFormViewModel
{
    public int? Id { get; set; }

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
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "End date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; } = DateTime.Today;

    [Display(Name = "Status")]
    public AccountingPeriodStatus Status { get; set; } = AccountingPeriodStatus.Open;

    public AccountingPeriod ToEntity()
    {
        return new AccountingPeriod
        {
            Id = Id ?? 0,
            FinancialYearId = FinancialYearId,
            PeriodCode = PeriodCode,
            Name = Name,
            StartDate = StartDate,
            EndDate = EndDate,
            Status = Status
        };
    }

    public static AccountingPeriodFormViewModel FromEntity(AccountingPeriod period)
    {
        return new AccountingPeriodFormViewModel
        {
            Id = period.Id,
            FinancialYearId = period.FinancialYearId,
            PeriodCode = period.PeriodCode,
            Name = period.Name,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            Status = period.Status
        };
    }
}