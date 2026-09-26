using CompanyERP.Data;
using CompanyERP.Entities.Asset;
using CompanyERP.Entities.Employee;
using CompanyERP.Interfaces.Services;
using CompanyERP.ViewModels.Reports;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Reports;

public class HrReportService : IHrReportService
{
    private readonly ApplicationDbContext _db;

    public HrReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<SalarySummaryViewModel> GetSalarySummaryAsync(int companyId, DateTime? fromDate, DateTime? toDate, int? departmentId)
    {
        var from = fromDate?.Date ?? DateTime.MinValue.Date;
        var to = toDate?.Date ?? DateTime.MaxValue.Date;

        var payments = await _db.SalaryPayments
            .AsNoTracking()
            .Include(s => s.Employee)
                .ThenInclude(e => e!.Department)
            .Where(s => s.Employee != null && s.Employee.CompanyId == companyId &&
                        s.PaymentDate >= from && s.PaymentDate <= to)
            .ToListAsync();

        var rows = payments
            .Where(s => !departmentId.HasValue || s.Employee!.DepartmentId == departmentId.Value)
            .GroupBy(s => s.Employee!.Id)
            .Select(g =>
            {
                var employee = g.First().Employee!;
                return new SalaryRow
                {
                    EmployeeCode = employee.EmployeeCode,
                    Name = employee.Name,
                    Department = employee.Department?.Name ?? "",
                    Payments = g.Count(),
                    Paid = Math.Round(g.Where(s => s.Status == SalaryPaymentStatus.Paid).Sum(s => s.Amount), 2),
                    Pending = Math.Round(g.Where(s => s.Status == SalaryPaymentStatus.Pending).Sum(s => s.Amount), 2)
                };
            })
            .OrderBy(r => r.Name)
            .ToList();

        return new SalarySummaryViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            DepartmentId = departmentId,
            Rows = rows,
            TotalPaid = Math.Round(rows.Sum(r => r.Paid), 2),
            TotalPending = Math.Round(rows.Sum(r => r.Pending), 2)
        };
    }

    public async Task<SalaryRegisterViewModel> GetSalaryRegisterAsync(int companyId, DateTime? fromDate, DateTime? toDate, int? departmentId)
    {
        var from = fromDate?.Date ?? DateTime.MinValue.Date;
        var to = toDate?.Date ?? DateTime.MaxValue.Date;

        var payments = await _db.SalaryPayments
            .AsNoTracking()
            .Include(s => s.Employee)
                .ThenInclude(e => e!.Department)
            .Include(s => s.Employee)
                .ThenInclude(e => e!.SalaryStructure)
            .Where(s => s.Employee != null && s.Employee.CompanyId == companyId &&
                        s.PaymentDate >= from && s.PaymentDate <= to)
            .ToListAsync();

        var structures = await _db.SalaryStructures
            .AsNoTracking()
            .Where(st => st.Employee != null && st.Employee.CompanyId == companyId)
            .ToListAsync();

        var rows = payments
            .Where(s => !departmentId.HasValue || s.Employee!.DepartmentId == departmentId.Value)
            .Select(s =>
            {
                var structure = structures
                    .Where(st => st.EmployeeId == s.EmployeeId && st.EffectiveFrom.Date <= s.ForMonth.Date)
                    .OrderByDescending(st => st.EffectiveFrom)
                    .FirstOrDefault();
                var basic = structure?.BasicSalary ?? 0;
                var house = structure?.HouseRent ?? 0;
                var medical = structure?.MedicalAllowance ?? 0;
                var conveyance = structure?.ConveyanceAllowance ?? 0;
                var other = structure?.OtherAllowance ?? 0;
                var pfPercent = structure?.ProvidentFundPercent ?? 0;
                var gross = basic + house + medical + conveyance + other;
                var pf = gross * pfPercent / 100m;
                return new SalaryRegisterRow
                {
                    EmployeeCode = s.Employee!.EmployeeCode,
                    Name = s.Employee.Name,
                    Department = s.Employee.Department?.Name ?? "",
                    ForMonth = s.ForMonth,
                    Basic = Math.Round(basic, 2),
                    HouseRent = Math.Round(house, 2),
                    Medical = Math.Round(medical, 2),
                    Conveyance = Math.Round(conveyance, 2),
                    Other = Math.Round(other, 2),
                    Pf = Math.Round(pf, 2),
                    Status = s.Status.ToString()
                };
            })
            .OrderBy(r => r.Name)
            .ThenBy(r => r.ForMonth)
            .ToList();

        return new SalaryRegisterViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            DepartmentId = departmentId,
            Rows = rows,
            TotalGross = Math.Round(rows.Sum(r => r.Gross), 2),
            TotalPf = Math.Round(rows.Sum(r => r.Pf), 2),
            TotalNet = Math.Round(rows.Sum(r => r.Net), 2)
        };
    }

    public async Task<AssetCustodyViewModel> GetAssetCustodyAsync(int companyId)
    {
        var rows = (await _db.EmployeeAssetAssignments
            .AsNoTracking()
            .Include(a => a.Employee)
                .ThenInclude(e => e!.Department)
            .Where(a => a.Employee != null && a.Employee.CompanyId == companyId && a.ReturnedOn == null)
            .OrderBy(a => a.Employee!.Name)
            .ToListAsync())
            .Select(a => new AssetCustodyRow
            {
                EmployeeCode = a.Employee!.EmployeeCode,
                Employee = a.Employee.Name,
                Department = a.Employee.Department?.Name ?? "",
                AssetCode = a.AssetCode ?? "",
                AssetName = a.AssetName,
                SerialNumber = a.SerialNumber ?? "",
                AssignedOn = a.AssignedOn
            })
            .ToList();

        return new AssetCustodyViewModel
        {
            Rows = rows,
            TotalAssignments = rows.Count
        };
    }

    public async Task<EmployeeListViewModel> GetEmployeeListAsync(int companyId, int? departmentId)
    {
        var employees = await _db.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Where(e => e.CompanyId == companyId)
            .ToListAsync();

        var rows = employees
            .Where(e => !departmentId.HasValue || e.DepartmentId == departmentId.Value)
            .Select(e => new EmployeeListRow
            {
                Department = e.Department?.Name ?? "",
                EmployeeCode = e.EmployeeCode,
                Name = e.Name,
                Designation = e.Designation?.Name ?? "",
                JoiningDate = e.JoiningDate
            })
            .OrderBy(r => r.Department)
            .ThenBy(r => r.Name)
            .ToList();

        return new EmployeeListViewModel
        {
            DepartmentId = departmentId,
            Rows = rows
        };
    }
}