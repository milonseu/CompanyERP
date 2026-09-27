namespace CompanyERP.Services.Security;

public static class MenuCatalog
{
    public sealed record CatalogMenu(
        string Code,
        string Name,
        string Icon,
        string? Controller,
        string? Action,
        string? PermissionCode,
        string? ParentCode,
        int DisplayOrder);

    public static IReadOnlyList<CatalogMenu> Items { get; } =
    [
        // Dashboard (direct link, shown to any authenticated user)
        new("DASHBOARD", "Dashboard", "bi-speedometer2", "Home", "Index", null, null, 1),

        // Company
        new("COMPANY", "Company Management", "bi-building", null, null, null, null, 2),
        new("COMPANY_PROFILE", "Company Profile", "bi-building-check", "Company", "Index", "Company.View", "COMPANY", 1),
        new("FINANCIAL_YEAR", "Financial Year", "bi-calendar-range", "FinancialYear", "Index", "Company.View", "COMPANY", 2),
        new("ACCOUNTING_PERIOD", "Accounting Period", "bi-calendar2-week", "AccountingPeriod", "Index", "Accounting.View", "COMPANY", 3),

        // Branch
        new("BRANCH", "Branch Management", "bi-columns-gap", null, null, null, null, 3),
        new("BRANCH_TYPE", "Branch Type", "bi-tags", "BranchType", "Index", "Branch.View", "BRANCH", 1),
        new("BRANCH_LIST", "Branch", "bi-diagram-3", "Branch", "Index", "Branch.View", "BRANCH", 2),

        // Employee
        new("EMPLOYEE", "Employee Management", "bi-people", null, null, null, null, 4),
        new("DEPARTMENT", "Department", "bi-diagram-2", "Department", "Index", "Employee.View", "EMPLOYEE", 1),
        new("DESIGNATION", "Designation", "bi-person-badge", "Designation", "Index", "Employee.View", "EMPLOYEE", 2),
        new("EMPLOYEE_LIST", "Employee", "bi-person-vcard", "Employee", "Index", "Employee.View", "EMPLOYEE", 3),
        new("HR_REPORTS", "Reports", "bi-bar-chart", "HrReport", "Index", "Report.View", "EMPLOYEE", 4),

        // Supplier
        new("SUPPLIER", "Supplier Management", "bi-truck", null, null, null, null, 5),
        new("SUPPLIER_LIST", "Supplier", "bi-truck", "Supplier", "Index", "Supplier.View", "SUPPLIER", 1),

        // Customer
        new("CUSTOMER", "Customer Management", "bi-person-lines-fill", null, null, null, null, 6),
        new("CUSTOMER_LIST", "Customer", "bi-person-lines-fill", "Customer", "Index", "Customer.View", "CUSTOMER", 1),

        // Master data
        new("MASTER_DATA", "Master Data", "bi-tags", null, null, null, null, 7),
        new("CATEGORY_TYPE", "Category Type", "bi-tag", "CategoryType", "Index", "MasterData.View", "MASTER_DATA", 1),
        new("CATEGORY", "Category", "bi-tags", "Category", "Index", "MasterData.View", "MASTER_DATA", 2),

        // Inventory
        new("INVENTORY", "Product & Inventory", "bi-box-seam", null, null, null, null, 8),
        new("PRODUCT_CATEGORY", "Product Category", "bi-tags", "ProductCategory", "Index", "Inventory.View", "INVENTORY", 1),
        new("PRODUCT", "Product", "bi-box", "Product", "Index", "Inventory.View", "INVENTORY", 2),
        new("WAREHOUSE", "Warehouse", "bi-building", "Warehouse", "Index", "Inventory.View", "INVENTORY", 3),
        new("STOCK_BALANCE", "Stock Balance", "bi-clipboard-check", "Inventory", "Balance", "Inventory.View", "INVENTORY", 4),
        new("STOCK_TRANSACTIONS", "Stock Transactions", "bi-list-check", "Inventory", "Transactions", "Inventory.View", "INVENTORY", 5),
        new("STOCK_TRANSFERS", "Stock Transfers", "bi-arrow-left-right", "Inventory", "Transfers", "Inventory.View", "INVENTORY", 6),
        new("INVENTORY_REPORTS", "Inventory Reports", "bi-bar-chart", "InventoryReport", "Index", "Report.View", "INVENTORY", 7),

        // Purchase
        new("PURCHASE", "Purchase", "bi-cart4", null, null, null, null, 9),
        new("PURCHASE_REQUEST", "Purchase Request", "bi-file-earmark-text", "PurchaseRequest", "Index", "Purchase.View", "PURCHASE", 1),
        new("PURCHASE_QUOTATION", "Quotation", "bi-file-earmark-richtext", "PurchaseQuotation", "Index", "Purchase.View", "PURCHASE", 2),
        new("PURCHASE_ORDER", "Purchase Order", "bi-receipt", "PurchaseOrder", "Index", "Purchase.View", "PURCHASE", 3),
        new("PURCHASE_INVOICE", "Invoice", "bi-credit-card", "PurchaseInvoice", "Index", "Purchase.View", "PURCHASE", 4),
        new("PURCHASE_RECEIVING", "Receiving", "bi-box-arrow-in-down", "PurchaseReceiving", "Index", "Purchase.View", "PURCHASE", 5),
        new("PURCHASE_RETURN", "Return", "bi-arrow-return-left", "PurchaseReturn", "Index", "Purchase.View", "PURCHASE", 6),
        new("PURCHASE_REPORTS", "Purchase Reports", "bi-bar-chart", "PurchaseReport", "Index", "Report.View", "PURCHASE", 7),

        // Asset
        new("ASSET", "Asset", "bi-truck", null, null, null, null, 10),
        new("ASSET_CATEGORY", "Asset Category", "bi-tags", "AssetCategory", "Index", "Asset.View", "ASSET", 1),
        new("ASSET_TYPE", "Asset Type", "bi-diagram-3", "AssetType", "Index", "Asset.View", "ASSET", 2),
        new("ASSET_REGISTER", "Asset Register", "bi-box", "Asset", "Index", "Asset.View", "ASSET", 3),
        new("ASSET_TRANSFER", "Asset Transfer", "bi-arrow-left-right", "AssetTransfer", "Index", "Asset.View", "ASSET", 4),
        new("ASSET_MAINTENANCE", "Asset Maintenance", "bi-tools", "AssetMaintenance", "Index", "Asset.View", "ASSET", 5),
        new("ASSET_DEPRECIATION", "Asset Depreciation", "bi-percent", "AssetDepreciation", "Index", "Asset.View", "ASSET", 6),
        new("ASSET_DISPOSAL", "Asset Disposal", "bi-trash3", "AssetDisposal", "Index", "Asset.View", "ASSET", 7),
        new("ASSET_REPORTS", "Asset Reports", "bi-bar-chart", "AssetReport", "Index", "Report.View", "ASSET", 8),

        // Expense
        new("EXPENSE", "Expense", "bi-cash-stack", null, null, null, null, 11),
        new("EXPENSE_CATEGORY", "Expense Category", "bi-tags", "ExpenseCategory", "Index", "Expense.View", "EXPENSE", 1),
        new("EXPENSE_TYPE", "Expense Type", "bi-diagram-3", "ExpenseType", "Index", "Expense.View", "EXPENSE", 2),
        new("EXPENSE_ENTRY", "Expense Entry", "bi-receipt-cutoff", "Expense", "Index", "Expense.View", "EXPENSE", 3),
        new("EXPENSE_REPORTS", "Expense Reports", "bi-bar-chart", "ExpenseReport", "Index", "Report.View", "EXPENSE", 4),

        // Sales
        new("SALES", "Sales", "bi-cart", null, null, null, null, 12),
        new("SERVICE_CATALOG", "Service Catalog", "bi-boxes", "Service", "Index", "Sales.View", "SALES", 1),
        new("SALES_ORDER", "Sales Order", "bi-bag", "SalesOrder", "Index", "Sales.View", "SALES", 2),
        new("SALES_INVOICE", "Sales Invoice", "bi-receipt", "SalesInvoice", "Index", "Sales.View", "SALES", 3),
        new("SERVICE_ORDER", "Service Order", "bi-tools", "ServiceOrder", "Index", "Sales.View", "SALES", 4),
        new("SERVICE_DELIVERY", "Service Delivery", "bi-truck", "ServiceDelivery", "Index", "Sales.View", "SALES", 5),
        new("SALES_RETURN", "Sales Return", "bi-arrow-return-left", "SalesReturn", "Index", "Sales.View", "SALES", 6),

        // Payment
        new("PAYMENT", "Payment Management", "bi-cash-coin", null, null, null, null, 13),
        new("PAYMENT_METHOD", "Payment Method", "bi-icons", "PaymentMethod", "Index", "Payment.View", "PAYMENT", 1),
        new("CASH_ACCOUNT", "Cash Account", "bi-wallet2", "CashAccount", "Index", "Payment.View", "PAYMENT", 2),
        new("BANK_ACCOUNT", "Bank Account", "bi-bank", "BankAccount", "Index", "Payment.View", "PAYMENT", 3),
        new("PAYMENTS", "Payments", "bi-cash-stack", "Payment", "Index", "Payment.View", "PAYMENT", 4),

        // Accounting
        new("ACCOUNTING", "Accounting", "bi-journal-text", null, null, null, null, 14),
        new("CHART_OF_ACCOUNTS", "Chart of Accounts", "bi-list-columns", "ChartOfAccount", "Index", "Accounting.View", "ACCOUNTING", 1),
        new("JOURNAL_ENTRIES", "Journal Entries", "bi-journal-text", "JournalEntry", "Index", "Accounting.View", "ACCOUNTING", 2),
        new("ACCOUNTING_REPORTS", "Accounting Reports", "bi-graph-up", "AccountingReport", "Index", "Report.View", "ACCOUNTING", 3),
        new("AGING_REPORTS", "Receivable / Payable Aging", "bi-hourglass-split", "AgingReport", "Index", "Report.View", "ACCOUNTING", 4),
        new("STATEMENT_REPORTS", "Customer / Supplier Statement", "bi-file-earmark-text", "StatementReport", "Index", "Report.View", "ACCOUNTING", 5),
        new("PAYMENT_REPORTS", "Payment Reports", "bi-cash-coin", "PaymentReport", "Index", "Report.View", "ACCOUNTING", 6),
        new("SALES_REPORTS", "Sales Reports", "bi-cart", "SalesReport", "Index", "Report.View", "ACCOUNTING", 7),

        // Security
        new("SECURITY", "Security", "bi-shield-lock", null, null, null, null, 99),
        new("USERS", "Users", "bi-person", "User", "Index", "Security.View", "SECURITY", 1),
        new("ROLES", "Roles", "bi-person-badge", "Role", "Index", "Security.View", "SECURITY", 2),
        new("PERMISSIONS", "Permissions", "bi-key", "Permission", "Index", "Security.View", "SECURITY", 3),
        new("MENUS", "Menus", "bi-menu-button-wide", "Menu", "Index", "Security.View", "SECURITY", 4),
        new("ACTIVITY_LOG", "Activity Log", "bi-clock-history", "ActivityLog", "Index", "Security.View", "SECURITY", 5),
        new("LOGIN_HISTORY", "Login History", "bi-box-arrow-in-right", "LoginHistory", "Index", "Security.View", "SECURITY", 6),
    ];
}