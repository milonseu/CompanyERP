using CompanyERP.Data;
using CompanyERP.Interfaces.Services;
using CompanyERP.Services.Asset;
using CompanyERP.Services.Company;
using CompanyERP.Services.CompanyBranch;
using CompanyERP.Services.Customer;
using CompanyERP.Services.Employee;
using CompanyERP.Services.Expense;
using CompanyERP.Services.Inventory;
using CompanyERP.Services.MasterData;
using CompanyERP.Services.Purchase;
using CompanyERP.Services.Sales;
using CompanyERP.Services.Supplier;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

var app = builder.Build();

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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();