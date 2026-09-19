using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Company;

namespace CompanyERP.ViewModels.Company;

public class FinancialYearFormViewModel
{
    public FinancialYearFormViewModel()
    {
        StartDate = new DateTime(DateTime.Today.Year, 1, 1);
        EndDate = new DateTime(DateTime.Today.Year, 12, 31);
    }

    public int? Id { get; set; }

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

    public FinancialYear ToEntity()
    {
        return new FinancialYear
        {
            Id = Id ?? 0,
            YearCode = YearCode,
            Name = Name,
            StartDate = StartDate,
            EndDate = EndDate,
            IsClosed = IsClosed
        };
    }

    public static FinancialYearFormViewModel FromEntity(FinancialYear year)
    {
        return new FinancialYearFormViewModel
        {
            Id = year.Id,
            YearCode = year.YearCode,
            Name = year.Name,
            StartDate = year.StartDate,
            EndDate = year.EndDate,
            IsClosed = year.IsClosed
        };
    }
}