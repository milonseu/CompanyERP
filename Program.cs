using CompanyERP.Data;
using CompanyERP.Interfaces.Services;
using CompanyERP.Services.Accounting;
using CompanyERP.Services.Asset;
using CompanyERP.Services.Company;
using CompanyERP.Services.CompanyBranch;
using CompanyERP.Services.Customer;
using CompanyERP.Services.Dashboard;
using CompanyERP.Services.Employee;
using CompanyERP.Services.Expense;
using CompanyERP.Services.Inventory;
using CompanyERP.Services.MasterData;
using CompanyERP.Services.Payments;
using CompanyERP.Services.Purchase;
using CompanyERP.Services.Reports;
using CompanyERP.Services.Sales;
using CompanyERP.Services.Security;
using CompanyERP.Services.Supplier;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ActivityLogFilter>();
    options.Filters.Add<MustChangePasswordFilter>();
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Module: Security & Permission Management
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<ILoginHistoryService, LoginHistoryService>();
builder.Services.AddScoped<ISecuritySeederService, SecuritySeederService>();
builder.Services.AddScoped<ActivityLogFilter>();
builder.Services.AddScoped<MustChangePasswordFilter>();
builder.Services.AddScoped<IClaimsTransformation, PermissionClaimsTransformer>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Module: Company Management
builder.Services.AddScoped<ICompanyProfileService, CompanyProfileService>();
builder.Services.AddScoped<IFinancialYearService, FinancialYearService>();
builder.Services.AddScoped<IAccountingPeriodService, AccountingPeriodService>();

// Module: Branch Management
builder.Services.AddScoped<IBranchTypeService, BranchTypeService>();
builder.Services.AddScoped<IBranchService, BranchService>();

// Module: Employee Management
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IDesignationService, DesignationService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// Module: Supplier Management
builder.Services.AddScoped<ISupplierService, SupplierService>();

// Module: Customer Management
builder.Services.AddScoped<ICustomerService, CustomerService>();

// Module: Product & Inventory Management
builder.Services.AddScoped<IProductCategoryService, ProductCategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();

// Module: Master Data Management
builder.Services.AddScoped<ICategoryTypeService, CategoryTypeService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// Module: Purchase Management
builder.Services.AddScoped<IPurchaseRequestService, PurchaseRequestService>();
builder.Services.AddScoped<IPurchaseQuotationService, PurchaseQuotationService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IPurchaseInvoiceService, PurchaseInvoiceService>();
builder.Services.AddScoped<IPurchaseReceivingService, PurchaseReceivingService>();
builder.Services.AddScoped<IPurchaseReturnService, PurchaseReturnService>();

// Module: Asset Management
builder.Services.AddScoped<IAssetCategoryService, AssetCategoryService>();
builder.Services.AddScoped<IAssetTypeService, AssetTypeService>();
builder.Services.AddScoped<IAssetRegisterService, AssetRegisterService>();
builder.Services.AddScoped<IAssetMaintenanceService, AssetMaintenanceService>();
builder.Services.AddScoped<IAssetDepreciationService, AssetDepreciationService>();
builder.Services.AddScoped<IAssetDisposalService, AssetDisposalService>();

// Module: Expense Management
builder.Services.AddScoped<IExpenseCategoryService, ExpenseCategoryService>();
builder.Services.AddScoped<IExpenseTypeService, ExpenseTypeService>();
builder.Services.AddScoped<IExpenseEntryService, ExpenseEntryService>();

// Module: Sales & Service Management
builder.Services.AddScoped<IServiceService, ServiceService>();
builder.Services.AddScoped<ISalesOrderService, SalesOrderService>();
builder.Services.AddScoped<ISalesInvoiceService, SalesInvoiceService>();
builder.Services.AddScoped<IServiceOrderService, ServiceOrderService>();
builder.Services.AddScoped<IServiceDeliveryService, ServiceDeliveryService>();
builder.Services.AddScoped<ISalesReturnService, SalesReturnService>();

// Module: Payment Management
builder.Services.AddScoped<IPaymentMethodService, PaymentMethodService>();
builder.Services.AddScoped<ICashAccountService, CashAccountService>();
builder.Services.AddScoped<IBankAccountService, BankAccountService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Module: Accounting Management
builder.Services.AddScoped<IChartOfAccountService, ChartOfAccountService>();
builder.Services.AddScoped<IJournalEntryService, JournalEntryService>();
builder.Services.AddScoped<ITransactionPostingService, TransactionPostingService>();
builder.Services.AddScoped<IAccountingReportService, AccountingReportService>();
builder.Services.AddScoped<IAgingReportService, AgingReportService>();
builder.Services.AddScoped<IStatementReportService, StatementReportService>();

// Module: Report Modules
builder.Services.AddScoped<IPaymentReportService, PaymentReportService>();
builder.Services.AddScoped<IInventoryReportService, InventoryReportService>();
builder.Services.AddScoped<IAssetReportService, AssetReportService>();
builder.Services.AddScoped<IExpenseReportService, ExpenseReportService>();
builder.Services.AddScoped<IHrReportService, HrReportService>();
builder.Services.AddScoped<ISalesReportService, SalesReportService>();
builder.Services.AddScoped<IPurchaseReportService, PurchaseReportService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();

// First-run security seed: roles, permissions, role-permission grants and the standard menu tree. No
// user is created, so the first person to register at /Account/Register becomes the Super Admin and
// owns the only credential in the system. The seeder is idempotent, so an existing (or partially
// seeded) database is repaired rather than duplicated. Disable with SeedSecurity:OnStartup=false.
if (app.Configuration.GetValue("SeedSecurity:OnStartup", true))
{
    using var seedScope = app.Services.CreateScope();
    var seedLogger = seedScope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SecuritySeed");
    try
    {
        await seedScope.ServiceProvider.GetRequiredService<ISecuritySeederService>().SeedAsync();
        seedLogger.LogInformation("Security seed completed (roles, permissions, role grants, menus).");
    }
    catch (Exception ex)
    {
        // Log and continue: a seeding failure must not take down a working instance.
        seedLogger.LogError(ex, "Security seed failed. Registration may not be able to grant Super Admin.");
    }
}

// Chart of Accounts seed: installs the default four layer tree (Class > Group > Sub-Group > Leaf)
// for every company that does not have one yet. TransactionPostingService already runs the same
// routine lazily before a posting, which leaves a new company with an empty Chart of Accounts page
// until its first transaction. Seeding here means the user sees the structure immediately.
// Idempotent: existing accounts keep their codes, names and types. Disable with SeedChartOfAccounts:OnStartup=false.
if (app.Configuration.GetValue("SeedChartOfAccounts:OnStartup", true))
{
    using var coaScope = app.Services.CreateScope();
    var coaLogger = coaScope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ChartOfAccountsSeed");
    try
    {
        var companyService = coaScope.ServiceProvider.GetRequiredService<ICompanyProfileService>();
        var postingService = coaScope.ServiceProvider.GetRequiredService<ITransactionPostingService>();
        var companies = await companyService.GetAllAsync();

        foreach (var company in companies)
        {
            var result = await postingService.EnsureDefaultsAsync(company.Id);
            if (!result.Success)
            {
                coaLogger.LogWarning("Chart of Accounts seed skipped for company {CompanyId}: {Error}", company.Id, result.Error);
            }
        }

        coaLogger.LogInformation("Chart of Accounts seed completed for {CompanyCount} company(ies).", companies.Count);
    }
    catch (Exception ex)
    {
        // Log and continue: a missing default chart must not stop the app from starting.
        coaLogger.LogError(ex, "Chart of Accounts seed failed. Accounts will be created on first posting instead.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();