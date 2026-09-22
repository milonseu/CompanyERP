using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;

namespace CompanyERP.Entities.Expense;

public class ExpenseType : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Expense category is required.")]
    public int ExpenseCategoryId { get; set; }

    [Required(ErrorMessage = "Type code is required.")]
    [StringLength(30)]
    [Display(Name = "Type Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Type name is required.")]
    [StringLength(100)]
    [Display(Name = "Type Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    public CompanyProfile? Company { get; set; }
    public ExpenseCategory? ExpenseCategory { get; set; }
    public ICollection<ExpenseEntry> Entries { get; set; } = [];
}