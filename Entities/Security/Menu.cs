using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.Security;

public class Menu : BaseEntity
{
    [Required(ErrorMessage = "Menu code is required.")]
    [StringLength(40)]
    [Display(Name = "Menu Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Menu name is required.")]
    [StringLength(100)]
    [Display(Name = "Menu")]
    public string Name { get; set; } = string.Empty;

    [StringLength(60)]
    public string? Icon { get; set; }

    [StringLength(60)]
    [Display(Name = "Controller")]
    public string? Controller { get; set; }

    [StringLength(60)]
    [Display(Name = "Action")]
    public string? Action { get; set; }

    [StringLength(60)]
    public string? Area { get; set; }

    [Display(Name = "Parent Menu")]
    public int? ParentId { get; set; }

    [Display(Name = "Display Order")]
    public int DisplayOrder { get; set; }

    [Display(Name = "System Menu")]
    public bool IsSystem { get; set; }

    public Menu? Parent { get; set; }
    public ICollection<Menu> Children { get; set; } = [];
}