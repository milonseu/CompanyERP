using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.Asset;

public class AssetDepreciation : BaseEntity
{
    [Required]
    public int AssetRegisterId { get; set; }

    [Required(ErrorMessage = "Depreciation period is required.")]
    [StringLength(7)]
    [Display(Name = "Period (yyyy-MM)")]
    public string PeriodKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Depreciation date is required.")]
    [Display(Name = "Period End Date")]
    [DataType(DataType.Date)]
    public DateTime PeriodDate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Amount must be zero or more.")]
    public decimal Amount { get; set; }

    public decimal AccumulatedAfter { get; set; }

    [StringLength(300)]
    public string? Note { get; set; }

    public AssetRegister? AssetRegister { get; set; }
}