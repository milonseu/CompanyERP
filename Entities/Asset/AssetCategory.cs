using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;

namespace CompanyERP.Entities.Asset;

public class AssetCategory : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Category code is required.")]
    [StringLength(30)]
    [Display(Name = "Category Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(100)]
    [Display(Name = "Category Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    public CompanyProfile? Company { get; set; }
    public ICollection<AssetType> AssetTypes { get; set; } = [];
}