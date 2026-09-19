using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;

namespace CompanyERP.Entities.Asset;

public class AssetType : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Asset category is required.")]
    public int AssetCategoryId { get; set; }

    [Required(ErrorMessage = "Asset type code is required.")]
    [StringLength(30)]
    [Display(Name = "Type Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Asset type name is required.")]
    [StringLength(100)]
    [Display(Name = "Type Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Depreciation Method")]
    public AssetDepreciationMethod DepreciationMethod { get; set; } = AssetDepreciationMethod.StraightLine;

    [Range(1, 600, ErrorMessage = "Useful life must be between 1 and 600 months.")]
    [Display(Name = "Useful Life (Months)")]
    public int UsefulLifeMonths { get; set; } = 36;

    [Range(0, double.MaxValue, ErrorMessage = "Salvage value must be zero or more.")]
    [Display(Name = "Salvage Value")]
    public decimal SalvageValue { get; set; }

    [StringLength(300)]
    public string? Description { get; set; }

    public CompanyProfile? Company { get; set; }
    public AssetCategory? AssetCategory { get; set; }
    public ICollection<AssetRegister> Assets { get; set; } = [];
}