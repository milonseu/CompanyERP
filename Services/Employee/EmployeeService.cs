using CompanyERP.Data;
using CompanyERP.Entities.Employee;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Employee;

public class EmployeeService : IEmployeeService
{
private readonly ApplicationDbContext _db;
    private readonly ITransactionPostingService _postingService;

    public EmployeeService(ApplicationDbContext db, ITransactionPostingService postingService)
    {
        _db = db;
        _postingService = postingService;
    }

    public async Task<List<CompanyERP.Entities.Employee.Employee>> GetAllAsync()
    {
        return await _db.Employees
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Include(e => e.DefaultBranch)
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<CompanyERP.Entities.Employee.Employee?> GetByIdAsync(int id)
    {
        return await _db.Employees
            .Include(e => e.Company)
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Include(e => e.DefaultBranch)
            .Include(e => e.BranchAssignments)
                .ThenInclude(a => a.Branch)
            .Include(e => e.SalaryStructure)
            .Include(e => e.SalaryPayments)
            .Include(e => e.AssetAssignments)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
    {
        var query = _db.Employees.Where(e => e.EmployeeCode == code);
        if (excludeId.HasValue)
        {
            query = query.Where(e => e.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<(bool Success, string Error)> CreateAsync(CompanyERP.Entities.Employee.Employee employee)
    {
        employee.EmployeeCode = string.IsNullOrWhiteSpace(employee.EmployeeCode) ? string.Empty : employee.EmployeeCode.Trim().ToUpperInvariant();
        employee.Name = string.IsNullOrWhiteSpace(employee.Name) ? string.Empty : employee.Name.Trim();

        if (string.IsNullOrWhiteSpace(employee.EmployeeCode) || string.IsNullOrWhiteSpace(employee.Name))
        {
            return (false, "Employee code and name are required.");
        }

        if (await CodeExistsAsync(employee.EmployeeCode))
        {
            return (false, "Employee code already exists.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == employee.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (!await IsDepartmentOfCompanyAsync(employee.DepartmentId, employee.CompanyId))
        {
            return (false, "Selected department does not belong to the selected company.");
        }

        if (!await IsDesignationOfCompanyAsync(employee.DesignationId, employee.CompanyId))
        {
            return (false, "Selected designation does not belong to the selected company.");
        }

        if (employee.DefaultBranchId.HasValue && !await _db.Branches.AnyAsync(b => b.Id == employee.DefaultBranchId.Value))
        {
            return (false, "Selected branch does not exist.");
        }

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(CompanyERP.Entities.Employee.Employee employee)
    {
        employee.EmployeeCode = string.IsNullOrWhiteSpace(employee.EmployeeCode) ? string.Empty : employee.EmployeeCode.Trim().ToUpperInvariant();
        employee.Name = string.IsNullOrWhiteSpace(employee.Name) ? string.Empty : employee.Name.Trim();

        var existing = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employee.Id);
        if (existing is null)
        {
            return (false, "Employee not found.");
        }

        if (await CodeExistsAsync(employee.EmployeeCode, employee.Id))
        {
            return (false, "Employee code already exists.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == employee.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (!await IsDepartmentOfCompanyAsync(employee.DepartmentId, employee.CompanyId))
        {
            return (false, "Selected department does not belong to the selected company.");
        }

        if (!await IsDesignationOfCompanyAsync(employee.DesignationId, employee.CompanyId))
        {
            return (false, "Selected designation does not belong to the selected company.");
        }

        if (employee.DefaultBranchId.HasValue && !await _db.Branches.AnyAsync(b => b.Id == employee.DefaultBranchId.Value))
        {
            return (false, "Selected branch does not exist.");
        }

        employee.CreatedAt = existing.CreatedAt;
        employee.CreatedBy = existing.CreatedBy;

        _db.Employees.Update(employee);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee is null)
        {
            return (false, "Employee not found.");
        }

        if (await _db.SalaryPayments.AnyAsync(p => p.EmployeeId == id))
        {
            return (false, "Employee cannot be deleted because salary payments already exist.");
        }

        _db.Employees.Remove(employee);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<List<EmployeeBranchAssignment>> GetBranchAssignmentsAsync(int employeeId)
    {
        return await _db.EmployeeBranchAssignments
            .Include(a => a.Branch)
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.AssignedDate)
            .ToListAsync();
    }

    public async Task<(bool Success, string Error)> AddBranchAssignmentAsync(EmployeeBranchAssignment assignment)
    {
        if (!await _db.Branches.AnyAsync(b => b.Id == assignment.BranchId))
        {
            return (false, "Selected branch does not exist.");
        }

        if (await _db.EmployeeBranchAssignments.AnyAsync(a => a.EmployeeId == assignment.EmployeeId && a.BranchId == assignment.BranchId))
        {
            return (false, "The employee is already assigned to this branch.");
        }

        if (assignment.IsDefault)
        {
            var defaults = await _db.EmployeeBranchAssignments
                .Where(a => a.EmployeeId == assignment.EmployeeId && a.IsDefault)
                .ToListAsync();
            foreach (var d in defaults)
            {
                d.IsDefault = false;
            }
        }

        _db.EmployeeBranchAssignments.Add(assignment);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> RemoveBranchAssignmentAsync(int id)
    {
        var assignment = await _db.EmployeeBranchAssignments.FindAsync(id);
        if (assignment is null)
        {
            return (false, "Branch assignment not found.");
        }

        _db.EmployeeBranchAssignments.Remove(assignment);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<SalaryStructure?> GetSalaryStructureAsync(int employeeId)
    {
        return await _db.SalaryStructures.FirstOrDefaultAsync(s => s.EmployeeId == employeeId);
    }

    public async Task<(bool Success, string Error)> SaveSalaryStructureAsync(SalaryStructure structure)
    {
        var existing = await _db.SalaryStructures.AsNoTracking().FirstOrDefaultAsync(s => s.EmployeeId == structure.EmployeeId);
        if (existing is null)
        {
            _db.SalaryStructures.Add(structure);
        }
        else
        {
            structure.Id = existing.Id;
            structure.CreatedAt = existing.CreatedAt;
            structure.CreatedBy = existing.CreatedBy;
            _db.SalaryStructures.Update(structure);
        }

        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<List<SalaryPayment>> GetSalaryPaymentsAsync(int employeeId)
    {
        return await _db.SalaryPayments
            .Where(p => p.EmployeeId == employeeId)
            .OrderByDescending(p => p.ForMonth)
            .ToListAsync();
    }

    public async Task<(bool Success, string Error)> CreateSalaryPaymentAsync(SalaryPayment payment)
    {
        if (!await _db.Employees.AnyAsync(e => e.Id == payment.EmployeeId))
        {
            return (false, "Employee does not exist.");
        }

        if (payment.Amount <= 0)
        {
            return (false, "Payment amount must be positive.");
        }

        var monthStart = new DateTime(payment.ForMonth.Year, payment.ForMonth.Month, 1);
        payment.ForMonth = monthStart;

        if (await _db.SalaryPayments.AnyAsync(p => p.EmployeeId == payment.EmployeeId && p.ForMonth == monthStart))
        {
            return (false, "A salary payment for this month already exists for the employee.");
        }

if (payment.Status == SalaryPaymentStatus.Paid && payment.PaymentMode is null)
        {
            return (false, "Select a payment mode (Cash or Bank) when paying salary.");
        }

        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == payment.EmployeeId);

        // NOTE: Accounting effect is created during Accounting module integration:
        // Paid   -> Debit Salary Expense, Credit Cash/Bank
        // Pending-> Debit Salary Expense, Credit Salary Payable

        var post = payment.Status == SalaryPaymentStatus.Paid
            ? await _postingService.PostSalaryDirectAsync(employee?.CompanyId ?? 0, payment, employee?.DefaultBranchId)
            : await _postingService.PostSalaryAccrualAsync(employee?.CompanyId ?? 0, payment, employee?.DefaultBranchId);
        if (!post.Success)
        {
            return (false, post.Error);
        }

        _db.SalaryPayments.Add(payment);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteSalaryPaymentAsync(int id)
    {
        var payment = await _db.SalaryPayments.FindAsync(id);
        if (payment is null)
        {
            return (false, "Salary payment not found.");
        }

        _db.SalaryPayments.Remove(payment);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<List<EmployeeAssetAssignment>> GetAssetAssignmentsAsync(int employeeId)
    {
        return await _db.EmployeeAssetAssignments
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.AssignedOn)
            .ToListAsync();
    }

    public async Task<(bool Success, string Error)> AddAssetAssignmentAsync(EmployeeAssetAssignment assignment)
    {
        assignment.AssetName = string.IsNullOrWhiteSpace(assignment.AssetName) ? string.Empty : assignment.AssetName.Trim();
        if (string.IsNullOrWhiteSpace(assignment.AssetName))
        {
            return (false, "Asset name is required.");
        }

        _db.EmployeeAssetAssignments.Add(assignment);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> MarkAssetReturnedAsync(int id)
    {
        var assignment = await _db.EmployeeAssetAssignments.FindAsync(id);
        if (assignment is null)
        {
            return (false, "Asset assignment not found.");
        }

        if (assignment.ReturnedOn.HasValue)
        {
            return (false, "Asset is already returned.");
        }

        assignment.ReturnedOn = DateTime.Today;
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> RemoveAssetAssignmentAsync(int id)
    {
        var assignment = await _db.EmployeeAssetAssignments.FindAsync(id);
        if (assignment is null)
        {
            return (false, "Asset assignment not found.");
        }

        _db.EmployeeAssetAssignments.Remove(assignment);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    private async Task<bool> IsDepartmentOfCompanyAsync(int departmentId, int companyId)
    {
        return await _db.Departments.AnyAsync(d => d.Id == departmentId && d.CompanyId == companyId);
    }

    private async Task<bool> IsDesignationOfCompanyAsync(int designationId, int companyId)
    {
        return await _db.Designations.AnyAsync(d => d.Id == designationId && d.CompanyId == companyId);
    }
}