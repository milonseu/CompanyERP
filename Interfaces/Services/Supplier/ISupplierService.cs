using CompanyERP.Entities.Supplier;

namespace CompanyERP.Interfaces.Services;

public interface ISupplierService
{
    Task<List<Supplier>> GetAllAsync();
    Task<Supplier?> GetByIdAsync(int id);
    Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId = null);
    Task<(bool Success, string Error)> CreateAsync(Supplier supplier);
    Task<(bool Success, string Error)> UpdateAsync(Supplier supplier);
    Task<(bool Success, string Error)> DeleteAsync(int id);

    Task<List<SupplierContact>> GetContactsAsync(int supplierId);
    Task<(bool Success, string Error)> AddContactAsync(SupplierContact contact);
    Task<(bool Success, string Error)> DeleteContactAsync(int id);

    Task<List<SupplierAddress>> GetAddressesAsync(int supplierId);
    Task<(bool Success, string Error)> AddAddressAsync(SupplierAddress address);
    Task<(bool Success, string Error)> DeleteAddressAsync(int id);
}