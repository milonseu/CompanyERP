using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Security;

namespace CompanyERP.ViewModels.Security;

public class MenuFormViewModel
{
    public int? Id { get; set; }

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

    [Range(0, 999)]
    [Display(Name = "Display Order")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "System Menu")]
    public bool IsSystem { get; set; }

    public List<Menu> AllMenus { get; set; } = [];

    public Menu ToEntity()
    {
        return new Menu
        {
            Id = Id ?? 0,
            Code = Code,
            Name = Name,
            Icon = Icon,
            Controller = Controller,
            Action = Action,
            Area = Area,
            ParentId = ParentId,
            DisplayOrder = DisplayOrder,
            IsActive = IsActive,
            IsSystem = IsSystem
        };
    }

    public static MenuFormViewModel FromEntity(Menu menu)
    {
        return new MenuFormViewModel
        {
            Id = menu.Id,
            Code = menu.Code,
            Name = menu.Name,
            Icon = menu.Icon,
            Controller = menu.Controller,
            Action = menu.Action,
            Area = menu.Area,
            ParentId = menu.ParentId,
            DisplayOrder = menu.DisplayOrder,
            IsActive = menu.IsActive,
            IsSystem = menu.IsSystem
        };
    }
}