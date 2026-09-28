using CompanyERP.Interfaces.Services;

namespace CompanyERP.Services.Security;

public static class SecurityDefs
{
    public const string SuperAdminRoleCode = "SUPERADMIN";

    private static readonly Dictionary<string, string> _moduleMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Company"] = "Company",
        ["FinancialYear"] = "Company",
        ["AccountingPeriod"] = "Accounting",
        ["BranchType"] = "Branch",
        ["Branch"] = "Branch",
        ["Department"] = "Employee",
        ["Designation"] = "Employee",
        ["Employee"] = "Employee",
        ["HrReport"] = "Report",
        ["Supplier"] = "Supplier",
        ["Customer"] = "Customer",
        ["CategoryType"] = "MasterData",
        ["Category"] = "MasterData",
        ["ProductCategory"] = "Inventory",
        ["Product"] = "Inventory",
        ["Warehouse"] = "Inventory",
        ["Inventory"] = "Inventory",
        ["InventoryReport"] = "Report",
        ["PurchaseRequest"] = "Purchase",
        ["PurchaseQuotation"] = "Purchase",
        ["PurchaseOrder"] = "Purchase",
        ["PurchaseInvoice"] = "Purchase",
        ["PurchaseReceiving"] = "Purchase",
        ["PurchaseReturn"] = "Purchase",
        ["PurchaseReport"] = "Report",
        ["AssetCategory"] = "Asset",
        ["AssetType"] = "Asset",
        ["Asset"] = "Asset",
        ["AssetTransfer"] = "Asset",
        ["AssetMaintenance"] = "Asset",
        ["AssetDepreciation"] = "Asset",
        ["AssetDisposal"] = "Asset",
        ["AssetReport"] = "Report",
        ["ExpenseCategory"] = "Expense",
        ["ExpenseType"] = "Expense",
        ["Expense"] = "Expense",
        ["ExpenseReport"] = "Report",
        ["Service"] = "Sales",
        ["SalesOrder"] = "Sales",
        ["SalesInvoice"] = "Sales",
        ["ServiceOrder"] = "Sales",
        ["ServiceDelivery"] = "Sales",
        ["SalesReturn"] = "Sales",
        ["SalesReport"] = "Report",
        ["PaymentMethod"] = "Payment",
        ["CashAccount"] = "Payment",
        ["BankAccount"] = "Payment",
        ["Payment"] = "Payment",
        ["PaymentReport"] = "Report",
        ["ChartOfAccount"] = "Accounting",
        ["JournalEntry"] = "Accounting",
        ["AccountingReport"] = "Report",
        ["AgingReport"] = "Report",
        ["StatementReport"] = "Report",
    };

    public static string GetModule(string controllerName)
        => _moduleMap.TryGetValue(controllerName, out var module) ? module : string.Empty;

    public sealed record CatalogItem(string Module, string Code, string Name);

    public static IReadOnlyList<CatalogItem> Catalog { get; } = BuildCatalog();

    public static bool IsCatalogKnown(string? code)
        => !string.IsNullOrWhiteSpace(code) && Catalog.Any(c =>
            string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));

    private static List<CatalogItem> BuildCatalog()
    {
        var items = new List<CatalogItem>();

        foreach (var module in ModulesWithFullCrud)
        {
            foreach (var op in CrudOperations)
            {
                items.Add(new CatalogItem(module, $"{module}.{op}", $"{module} - {op}"));
            }
        }

        foreach (var module in ViewOnlyModules)
        {
            items.Add(new CatalogItem(module, $"{module}.View", $"{module} - View"));
        }

        items.Add(new CatalogItem("Inventory", "Inventory.StockIn", "Inventory - Stock In"));
        items.Add(new CatalogItem("Inventory", "Inventory.StockOut", "Inventory - Stock Out"));
        items.Add(new CatalogItem("Inventory", "Inventory.Adjust", "Inventory - Adjust Stock"));
        items.Add(new CatalogItem("Inventory", "Inventory.StockTransfer", "Inventory - Stock Transfer"));
        items.Add(new CatalogItem("Purchase", "Purchase.Approve", "Purchase - Approve"));
        items.Add(new CatalogItem("Accounting", "Accounting.Post", "Accounting - Post Journal"));
        items.Add(new CatalogItem("Accounting", "Accounting.Close", "Accounting - Close Period"));

        return items;
    }

    private static readonly string[] CrudOperations = ["View", "Create", "Edit", "Delete"];

    private static readonly string[] ModulesWithFullCrud =
    [
        "Company", "Branch", "Employee", "Supplier", "Customer", "MasterData",
        "Inventory", "Purchase", "Asset", "Expense", "Sales", "Payment",
        "Accounting", "Security"
    ];

    private static readonly string[] ViewOnlyModules = ["Dashboard", "Report"];
}