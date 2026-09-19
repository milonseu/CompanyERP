using CompanyERP.Entities.Employee;

namespace CompanyERP.Interfaces.Services;

public interface IEmployeeService
{
    Task<List<Employee>> GetAllAsync();
    Task<Employee?> GetByIdAsync(int id);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    Task<(bool Success, string Error)> CreateAsync(Employee employee);
    Task<(bool Success, string Error)> UpdateAsync(Employee employee);
    Task<(bool Success, string Error)> DeleteAsync(int id);

    Task<List<EmployeeBranchAssignment>> GetBranchAssignmentsAsync(int employeeId);
    Task<(bool Success, string Error)> AddBranchAssignmentAsync(EmployeeBranchAssignment assignment);
    Task<(bool Success, string Error)> RemoveBranchAssignmentAsync(int id);

    Task<SalaryStructure?> GetSalaryStructureAsync(int employeeId);
    Task<(bool Success, string Error)> SaveSalaryStructureAsync(SalaryStructure structure);

    Task<List<SalaryPayment>> GetSalaryPaymentsAsync(int employeeId);
    Task<(bool Success, string Error)> CreateSalaryPaymentAsync(SalaryPayment payment);
    Task<(bool Success, string Error)> DeleteSalaryPaymentAsync(int id);

    Task<List<EmployeeAssetAssignment>> GetAssetAssignmentsAsync(int employeeId);
    Task<(bool Success, string Error)> AddAssetAssignmentAsync(EmployeeAssetAssignment assignment);
    Task<(bool Success, string Error)> MarkAssetReturnedAsync(int id);
    Task<(bool Success, string Error)> RemoveAssetAssignmentAsync(int id);
}