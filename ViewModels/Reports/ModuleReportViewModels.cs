namespace CompanyERP.ViewModels.Reports;

// ---------- Payment module ----------
public class PaymentSummaryViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? BranchId { get; set; }
    public List<PaymentMethodRow> Methods { get; set; } = new();
    public List<PaymentCategoryRow> Categories { get; set; } = new();
    public List<PaymentAccountRow> Accounts { get; set; } = new();
    public decimal TotalAmount { get; set; }
}

public class PaymentMethodRow
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

public class PaymentCategoryRow
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

public class PaymentAccountRow
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class SalesVsPaymentViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<SalesVsPaymentRow> Rows { get; set; } = new();
    public decimal TotalSales { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalBalance { get; set; }
}

public class SalesVsPaymentRow
{
    public int CustomerId { get; set; }
    public string Customer { get; set; } = string.Empty;
    public decimal Sales { get; set; }
    public decimal Paid { get; set; }
    public decimal Balance { get; set; }
}

public class PurchasesVsPaymentViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<PurchasesVsPaymentRow> Rows { get; set; } = new();
    public decimal TotalPurchases { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalBalance { get; set; }
}

public class PurchasesVsPaymentRow
{
    public int SupplierId { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public decimal Purchases { get; set; }
    public decimal Paid { get; set; }
    public decimal Balance { get; set; }
}

// ---------- Inventory module ----------
public class StockReportViewModel
{
    public int? CategoryId { get; set; }
    public int? WarehouseId { get; set; }
    public List<StockRow> Rows { get; set; } = new();
    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
}

public class StockRow
{
    public string Code { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AverageCost { get; set; }
    public decimal Value { get; set; }
    public decimal MinimumStockLevel { get; set; }
    public string LowStock => Quantity <= MinimumStockLevel ? "Low" : "OK";
}

public class StockMovementViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<StockMovementRow> Rows { get; set; } = new();
    public List<StockMovementProductRow> Products { get; set; } = new();
    public decimal TotalIn { get; set; }
    public decimal TotalOut { get; set; }
}

public class StockMovementRow
{
    public string Code { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Value { get; set; }
}

// ---------- Asset module ----------
public class AssetReportViewModel
{
    public DateTime? AsOfDate { get; set; }
    public int? AssetTypeId { get; set; }
    public List<AssetRegisterRow> Rows { get; set; } = new();
    public decimal TotalCost { get; set; }
    public decimal TotalAccumulatedDepreciation { get; set; }
    public decimal TotalBookValue { get; set; }
}

public class AssetRegisterRow
{
    public string AssetNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public decimal Cost { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal BookValue { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class DepreciationScheduleViewModel
{
    public string? FromPeriod { get; set; }
    public string? ToPeriod { get; set; }
    public List<DepreciationRow> Rows { get; set; } = new();
    public decimal TotalDepreciation { get; set; }
}

public class DepreciationRow
{
    public string AssetNo { get; set; } = string.Empty;
    public string Asset { get; set; } = string.Empty;
    public string PeriodKey { get; set; } = string.Empty;
    public DateTime PeriodDate { get; set; }
    public decimal Amount { get; set; }
    public decimal AccumulatedAfter { get; set; }
    public decimal MonthlyProjected { get; set; }
}

public class DisposalSummaryViewModel
{
    public List<DisposalRow> Rows { get; set; } = new();
    public decimal TotalSaleValue { get; set; }
    public decimal TotalBookValue { get; set; }
    public decimal TotalGainLoss { get; set; }
}

public class DisposalRow
{
    public string AssetNo { get; set; } = string.Empty;
    public string Asset { get; set; } = string.Empty;
    public DateTime DisposalDate { get; set; }
    public decimal SaleValue { get; set; }
    public decimal BookValue { get; set; }
    public decimal GainLoss { get; set; }
}

// ---------- Expense module ----------
public class ExpenseSummaryViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? ExpenseTypeId { get; set; }
    public List<ExpenseRow> Rows { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding { get; set; }
}

public class ExpenseRow
{
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
    public decimal Paid { get; set; }
    public decimal Outstanding { get; set; }
}

// ---------- HR / Employee module ----------
public class SalarySummaryViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? DepartmentId { get; set; }
    public List<SalaryRow> Rows { get; set; } = new();
    public decimal TotalPaid { get; set; }
    public decimal TotalPending { get; set; }
}

public class SalaryRow
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int Payments { get; set; }
    public decimal Paid { get; set; }
    public decimal Pending { get; set; }
}

// ---------- Sales module ----------
public class SalesSummaryViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<SalesSummaryRow> Rows { get; set; } = new();
    public int TotalInvoices { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalPaid { get; set; }
}

public class SalesSummaryRow
{
    public int CustomerId { get; set; }
    public string Customer { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal Quantity { get; set; }
    public decimal Sales { get; set; }
    public decimal Paid { get; set; }
}

// ---------- Purchase module ----------
public class PurchaseSummaryViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<PurchaseSummaryRow> Rows { get; set; } = new();
    public int TotalInvoices { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalPurchases { get; set; }
}

public class PurchaseSummaryRow
{
    public int SupplierId { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal Quantity { get; set; }
    public decimal Purchases { get; set; }
}

// ---------- HR / Employee: salary register ----------
public class SalaryRegisterViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? DepartmentId { get; set; }
    public List<SalaryRegisterRow> Rows { get; set; } = new();
    public decimal TotalGross { get; set; }
    public decimal TotalPf { get; set; }
    public decimal TotalNet { get; set; }
}

public class SalaryRegisterRow
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateTime ForMonth { get; set; }
    public decimal Basic { get; set; }
    public decimal HouseRent { get; set; }
    public decimal Medical { get; set; }
    public decimal Conveyance { get; set; }
    public decimal Other { get; set; }
    public decimal Pf { get; set; }
    public decimal Gross => Basic + HouseRent + Medical + Conveyance + Other;
    public decimal Net => Math.Max(0, Gross - Pf);
    public string Status { get; set; } = string.Empty;
}

// ---------- HR / Employee: asset custody ----------
public class AssetCustodyViewModel
{
    public List<AssetCustodyRow> Rows { get; set; } = new();
    public int TotalAssignments { get; set; }
}

public class AssetCustodyRow
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string Employee { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public DateTime AssignedOn { get; set; }
}

// ---------- HR / Employee: department-wise employee list ----------
public class EmployeeListViewModel
{
    public int? DepartmentId { get; set; }
    public List<EmployeeListRow> Rows { get; set; } = new();
}

public class EmployeeListRow
{
    public string Department { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public DateTime JoiningDate { get; set; }
}

// ---------- Expense: monthly trend + register ----------
public class ExpenseTrendViewModel
{
    public List<ExpenseTrendRow> Rows { get; set; } = new();
    public decimal TotalAmount { get; set; }
}

public class ExpenseTrendRow
{
    public string Month { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
    public decimal Paid { get; set; }
    public decimal Outstanding { get; set; }
}

public class ExpenseRegisterViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? BranchId { get; set; }
    public int? SupplierId { get; set; }
    public List<ExpenseRegisterRow> Rows { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding { get; set; }
}

public class ExpenseRegisterRow
{
    public DateTime Date { get; set; }
    public string ExpenseNo { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Paid { get; set; }
    public decimal Outstanding { get; set; }
}

// ---------- Inventory: movement product summary, low stock, warehouse summary ----------
public class StockMovementProductRow
{
    public string Code { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public decimal OpeningQty { get; set; }
    public decimal In { get; set; }
    public decimal Out { get; set; }
    public decimal ClosingQty { get; set; }
}

public class LowStockViewModel
{
    public int? CategoryId { get; set; }
    public List<LowStockRow> Rows { get; set; } = new();
    public int Count { get; set; }
}

public class LowStockRow
{
    public string Code { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public decimal OnHand { get; set; }
    public decimal MinimumStockLevel { get; set; }
    public decimal Shortfall { get; set; }
}

public class WarehouseStockViewModel
{
    public List<WarehouseStockRow> Rows { get; set; } = new();
    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
}

public class WarehouseStockRow
{
    public string Warehouse { get; set; } = string.Empty;
    public int Products { get; set; }
    public decimal Quantity { get; set; }
    public decimal Value { get; set; }
}

// ---------- Asset: group summary ----------
public class AssetGroupSummaryViewModel
{
    public List<AssetGroupRow> ByStatus { get; set; } = new();
    public List<AssetGroupRow> ByType { get; set; } = new();
    public List<AssetGroupRow> ByBranch { get; set; } = new();
    public decimal TotalCost { get; set; }
    public decimal TotalBookValue { get; set; }
}

public class AssetGroupRow
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Cost { get; set; }
    public decimal BookValue { get; set; }
}

// ---------- Purchase: PR/PO aging + top suppliers ----------
public class ProcurementAgingViewModel
{
    public List<ProcurementAgingRow> Requests { get; set; } = new();
    public List<ProcurementAgingRow> Orders { get; set; } = new();
    public int PendingRequestCount { get; set; }
    public int OpenOrderCount { get; set; }
}

public class ProcurementAgingRow
{
    public string DocNo { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int DaysOld { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public decimal Quantity { get; set; }
}

public class TopSuppliersViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Top { get; set; }
    public List<TopSupplierRow> Rows { get; set; } = new();
    public decimal TotalPurchases { get; set; }
}

public class TopSupplierRow
{
    public int Rank { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal Percent { get; set; }
}

// ---------- Sales: by product/category, outstanding, top, service completion ----------
public class SalesByProductViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<SalesByProductRow> Products { get; set; } = new();
    public List<SalesByCategoryRow> Categories { get; set; } = new();
    public decimal TotalSales { get; set; }
}

public class SalesByProductRow
{
    public string Code { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
}

public class SalesByCategoryRow
{
    public string Category { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
}

public class OutstandingReceivableViewModel
{
    public DateTime? AsOfDate { get; set; }
    public List<OutstandingReceivableRow> Rows { get; set; } = new();
    public decimal TotalOutstanding { get; set; }
}

public class OutstandingReceivableRow
{
    public int CustomerId { get; set; }
    public string Customer { get; set; } = string.Empty;
    public int Invoices { get; set; }
    public decimal Sales { get; set; }
    public decimal Paid { get; set; }
    public decimal Outstanding { get; set; }
}

public class TopSalesViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Top { get; set; }
    public List<TopCustomerRow> Customers { get; set; } = new();
    public List<TopProductRow> Products { get; set; } = new();
}

public class TopCustomerRow
{
    public int Rank { get; set; }
    public string Customer { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percent { get; set; }
}

public class TopProductRow
{
    public int Rank { get; set; }
    public string Product { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal Percent { get; set; }
}

public class ServiceCompletionViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Pending { get; set; }
    public int InProgress { get; set; }
    public int Delivered { get; set; }
    public int Cancelled { get; set; }
    public List<ServiceCompletionRow> Rows { get; set; } = new();
}

public class ServiceCompletionRow
{
    public string Service { get; set; } = string.Empty;
    public int Orders { get; set; }
    public decimal OrderedQty { get; set; }
    public decimal DeliveredQty { get; set; }
    public decimal Amount { get; set; }
    public decimal DeliveredPercent { get; set; }
}

// ---------- Accounting additions ----------
public class GeneralJournalViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? BranchId { get; set; }
    public List<GeneralJournalLineRow> Rows { get; set; } = new();
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
}

public class GeneralJournalLineRow
{
    public DateTime Date { get; set; }
    public string EntryNo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? SourceModule { get; set; }
    public string? SourceReference { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

public class ComparativePandLViewModel
{
    public DateTime? CurrentMonth { get; set; }
    public DateTime? PreviousMonth { get; set; }
    public List<ComparativePandLRow> Revenues { get; set; } = new();
    public List<ComparativePandLRow> Expenses { get; set; } = new();
    public decimal CurrentRevenue { get; set; }
    public decimal PreviousRevenue { get; set; }
    public decimal CurrentExpense { get; set; }
    public decimal PreviousExpense { get; set; }
    public decimal CurrentNet => CurrentRevenue - CurrentExpense;
    public decimal PreviousNet => PreviousRevenue - PreviousExpense;
}

public class ComparativePandLRow
{
    public string Label { get; set; } = string.Empty;
    public decimal Current { get; set; }
    public decimal Previous { get; set; }
}

public class CashFlowViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<CashFlowRow> Operating { get; set; } = new();
    public List<CashFlowRow> Investing { get; set; } = new();
    public List<CashFlowRow> Financing { get; set; } = new();
    public decimal OperatingNet { get; set; }
    public decimal InvestingNet { get; set; }
    public decimal FinancingNet { get; set; }
    public decimal NetChange => OperatingNet + InvestingNet + FinancingNet;
    public decimal OpeningCash { get; set; }
    public decimal ClosingCash { get; set; }
}

public class CashFlowRow
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class BankCashAccountSummaryViewModel
{
    public List<BankCashAccountRow> Rows { get; set; } = new();
    public decimal TotalOpening { get; set; }
    public decimal TotalReceipts { get; set; }
    public decimal TotalPayments { get; set; }
    public decimal TotalClosing { get; set; }
}

public class BankCashAccountRow
{
    public string Account { get; set; } = string.Empty;
    public decimal Opening { get; set; }
    public decimal Receipts { get; set; }
    public decimal Payments { get; set; }
    public decimal Closing { get; set; }
}

public class CoaReportViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<CoaReportRow> Rows { get; set; } = new();
    public decimal TotalOpening { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal TotalClosing { get; set; }
}

public class CoaReportRow
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal Opening { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Closing { get; set; }
}

public class VoucherViewModel
{
    public string Title { get; set; } = "Journal Voucher";
    public string EntryNo { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string? Branch { get; set; }
    public string? SourceModule { get; set; }
    public string? SourceReference { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<VoucherLineViewModel> Lines { get; set; } = new();
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
}

public class PaymentVoucherViewModel
{
    public string PaymentNo { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public string? Branch { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Party { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Account { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Note { get; set; }
}

public class VoucherLineViewModel
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? Note { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}