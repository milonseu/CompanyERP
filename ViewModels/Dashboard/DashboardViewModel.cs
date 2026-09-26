namespace CompanyERP.ViewModels.Dashboard;

public class DashboardViewModel
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = "CompanyERP";

    public decimal TotalSales { get; set; }
    public decimal TotalPurchases { get; set; }
    public decimal ReceivablesOutstanding { get; set; }
    public decimal PayablesOutstanding { get; set; }
    public decimal StockValue { get; set; }
    public decimal CashBankBalance { get; set; }
    public decimal AssetBookValue { get; set; }
    public decimal TotalExpenses { get; set; }

    public int CustomerCount { get; set; }
    public int SupplierCount { get; set; }
    public int EmployeeCount { get; set; }
    public int ProductCount { get; set; }
    public int AssetCount { get; set; }
    public int PendingServiceCount { get; set; }

    public List<MonthlyPoint> Monthly { get; set; } = new();
    public List<CategorySlice> CategorySales { get; set; } = new();
    public List<RecentInvoiceRow> RecentSales { get; set; } = new();
    public List<RecentPaymentRow> RecentPayments { get; set; } = new();
    public List<LowStockDashboardRow> LowStock { get; set; } = new();
    public List<ReceivableDashboardRow> TopReceivables { get; set; } = new();
    public List<PayableDashboardRow> TopPayables { get; set; } = new();
}

public class MonthlyPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Sales { get; set; }
    public decimal Purchases { get; set; }
}

public class CategorySlice
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class RecentInvoiceRow
{
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Party { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RecentPaymentRow
{
    public string PaymentNo { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Party { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class LowStockDashboardRow
{
    public string Code { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public decimal OnHand { get; set; }
    public decimal Minimum { get; set; }
    public decimal Shortfall => Math.Max(0, Minimum - OnHand);
}

public class ReceivableDashboardRow
{
    public string Customer { get; set; } = string.Empty;
    public decimal Outstanding { get; set; }
}

public class PayableDashboardRow
{
    public string Supplier { get; set; } = string.Empty;
    public decimal Outstanding { get; set; }
}