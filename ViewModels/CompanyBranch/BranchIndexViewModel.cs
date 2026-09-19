using CompanyERP.Entities.CompanyBranch;

namespace CompanyERP.ViewModels.CompanyBranch;

public class BranchIndexViewModel
{
    public Branch Branch { get; set; } = new();
    public int EmployeeCount { get; set; }
    public int WarehouseCount { get; set; }
}