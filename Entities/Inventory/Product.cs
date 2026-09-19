using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;

namespace CompanyERP.Entities.Inventory;

public class Product : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Product category is required.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Product code is required.")]
    [StringLength(30)]
    [Display(Name = "Product Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(150)]
    [Display(Name = "Product Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [StringLength(30)]
    [Display(Name = "Unit")]
    public string? Unit { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Cost price cannot be negative.")]
    [Display(Name = "Cost Price")]
    public decimal CostPrice { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Sale price cannot be negative.")]
    [Display(Name = "Sale Price")]
    public decimal SalePrice { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Minimum stock level cannot be negative.")]
    [Display(Name = "Minimum Stock Level")]
    public decimal MinimumStockLevel { get; set; }

    [Display(Name = "Active")]
    public new bool IsActive { get; set; } = true;

    public CompanyProfile? Company { get; set; }
    public ProductCategory? Category { get; set; }
}