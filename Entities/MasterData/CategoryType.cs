using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;

namespace CompanyERP.Entities.MasterData;

public class CategoryType : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Category type code is required.")]
    [StringLength(20)]
    [Display(Name = "Type Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category type name is required.")]
    [StringLength(100)]
    [Display(Name = "Type Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public new bool IsActive { get; set; } = true;

    public CompanyProfile? Company { get; set; }
    public List<Category> Categories { get; set; } = new();
}