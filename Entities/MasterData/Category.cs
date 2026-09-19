using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;

namespace CompanyERP.Entities.MasterData;

public class Category : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Category type is required.")]
    [Display(Name = "Category Type")]
    public int CategoryTypeId { get; set; }

    [Required(ErrorMessage = "Category code is required.")]
    [StringLength(20)]
    [Display(Name = "Category Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(100)]
    [Display(Name = "Category Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public new bool IsActive { get; set; } = true;

    public CompanyProfile? Company { get; set; }
    public CategoryType? CategoryType { get; set; }
}