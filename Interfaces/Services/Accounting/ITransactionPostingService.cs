using CompanyERP.Entities.Accounting;
using CompanyERP.Entities.Asset;
using CompanyERP.Entities.Expense;
using CompanyERP.Entities.Inventory;
using CompanyERP.Entities.Payment;
using CompanyERP.Entities.Sales;
using CompanyERP.Entities.Purchase;
using EmployeeSalary = CompanyERP.Entities.Employee.SalaryPayment;

namespace CompanyERP.Interfaces.Services;

/// <summary>
/// Real-time posting service. Methods stage balanced journal entries against the
/// shared DbContext without saving; the calling module service saves so source record
/// and journal entry are committed atomically.
/// </summary>
public interface ITransactionPostingService
{
    Task<(bool Success, string Error)> EnsureDefaultsAsync(int companyId);

    Task<(bool Success, string Error)> PostCustomerOpeningAsync(Entities.Customer.Customer customer, decimal openingReceivable);
    Task<(bool Success, string Error)> PostSupplierOpeningAsync(Entities.Supplier.Supplier supplier, decimal openingPayable);
    Task<(bool Success, string Error)> PostSalesInvoiceAsync(SalesInvoice invoice, IReadOnlyDictionary<int, Product> products);
    Task<(bool Success, string Error)> PostSalesReturnAsync(SalesReturn salesReturn, SalesInvoice invoice, IReadOnlyDictionary<int, Product> products);
    Task<(bool Success, string Error)> PostPurchaseInvoiceAsync(PurchaseInvoice invoice);
    Task<(bool Success, string Error)> PostPurchaseReturnAsync(PurchaseReturn purchaseReturn);
    Task<(bool Success, string Error)> PostCustomerPaymentAsync(Payment payment);
    Task<(bool Success, string Error)> PostSupplierPaymentAsync(Payment payment);
    Task<(bool Success, string Error)> PostExpenseEntryAsync(ExpenseEntry entry);
    Task<(bool Success, string Error)> PostExpensePaymentAsync(Payment payment, ExpenseEntry entry);
    Task<(bool Success, string Error)> PostSalaryAccrualAsync(int companyId, EmployeeSalary salary, int? branchId = null);
    Task<(bool Success, string Error)> PostSalaryDirectAsync(int companyId, EmployeeSalary salary, int? branchId = null);
    Task<(bool Success, string Error)> PostSalaryPaymentAsync(Payment payment);
    Task<(bool Success, string Error)> PostAssetAcquisitionAsync(AssetRegister asset, AssetAcquisition acquisition);
    Task<(bool Success, string Error)> PostAssetPaymentAsync(Payment payment);
    Task<(bool Success, string Error)> PostAssetDepreciationAsync(int companyId, string periodKey, decimal amount, string note);
    Task<(bool Success, string Error)> PostAssetDisposalAsync(AssetRegister asset, AssetDisposal disposal);

    Task<(bool Success, string Error, JournalEntry? Entry)> PostManualAsync(
        int companyId, DateTime entryDate, string description, List<(int AccountId, decimal Debit, decimal Credit, string? Note)> lines);
}