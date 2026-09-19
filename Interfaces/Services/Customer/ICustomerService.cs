using CompanyERP.Entities.Customer;

namespace CompanyERP.Interfaces.Services;

public interface ICustomerService
{
    Task<List<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(int id);
    Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId = null);
    Task<(bool Success, string Error)> CreateAsync(Customer customer);
    Task<(bool Success, string Error)> UpdateAsync(Customer customer);
    Task<(bool Success, string Error)> DeleteAsync(int id);

    Task<List<CustomerContact>> GetContactsAsync(int customerId);
    Task<(bool Success, string Error)> AddContactAsync(CustomerContact contact);
    Task<(bool Success, string Error)> DeleteContactAsync(int id);

    Task<List<CustomerAddress>> GetAddressesAsync(int customerId);
    Task<(bool Success, string Error)> AddAddressAsync(CustomerAddress address);
    Task<(bool Success, string Error)> DeleteAddressAsync(int id);
}