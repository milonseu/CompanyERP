using CompanyERP.Data;
using CompanyERP.Entities.Customer;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Customer;

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _db;

    public CustomerService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Entities.Customer.Customer>> GetAllAsync()
    {
        return await _db.Customers
            .Include(c => c.Company)
            .OrderBy(c => c.Company != null ? c.Company.Name : string.Empty)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Entities.Customer.Customer?> GetByIdAsync(int id)
    {
        return await _db.Customers
            .Include(c => c.Company)
            .Include(c => c.Contacts)
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId = null)
    {
        var query = _db.Customers.Where(c => c.CustomerCode == code && c.CompanyId == companyId);
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<(bool Success, string Error)> CreateAsync(Entities.Customer.Customer customer)
    {
        customer.CustomerCode = string.IsNullOrWhiteSpace(customer.CustomerCode) ? string.Empty : customer.CustomerCode.Trim().ToUpperInvariant();
        customer.Name = string.IsNullOrWhiteSpace(customer.Name) ? string.Empty : customer.Name.Trim();

        if (string.IsNullOrWhiteSpace(customer.CustomerCode) || string.IsNullOrWhiteSpace(customer.Name))
        {
            return (false, "Customer code and name are required.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == customer.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (await CodeExistsAsync(customer.CustomerCode, customer.CompanyId))
        {
            return (false, $"Customer code already exists for company {customer.CompanyId}.");
        }

        if (customer.OpeningReceivable < 0)
        {
            return (false, "Opening receivable cannot be negative.");
        }

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(Entities.Customer.Customer customer)
    {
        customer.CustomerCode = string.IsNullOrWhiteSpace(customer.CustomerCode) ? string.Empty : customer.CustomerCode.Trim().ToUpperInvariant();
        customer.Name = string.IsNullOrWhiteSpace(customer.Name) ? string.Empty : customer.Name.Trim();

        var existing = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customer.Id);
        if (existing is null)
        {
            return (false, "Customer not found.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == customer.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (await CodeExistsAsync(customer.CustomerCode, customer.CompanyId, customer.Id))
        {
            return (false, $"Customer code already exists for company {customer.CompanyId}.");
        }

        if (customer.OpeningReceivable < 0)
        {
            return (false, "Opening receivable cannot be negative.");
        }

        customer.CreatedAt = existing.CreatedAt;
        customer.CreatedBy = existing.CreatedBy;

        _db.Customers.Update(customer);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null)
        {
            return (false, "Customer not found.");
        }

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<List<CustomerContact>> GetContactsAsync(int customerId)
    {
        return await _db.CustomerContacts
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.IsPrimary)
            .ThenBy(c => c.ContactPerson)
            .ToListAsync();
    }

    public async Task<(bool Success, string Error)> AddContactAsync(CustomerContact contact)
    {
        contact.ContactPerson = string.IsNullOrWhiteSpace(contact.ContactPerson) ? string.Empty : contact.ContactPerson.Trim();
        if (string.IsNullOrWhiteSpace(contact.ContactPerson))
        {
            return (false, "Contact person is required.");
        }

        if (!await _db.Customers.AnyAsync(c => c.Id == contact.CustomerId))
        {
            return (false, "Customer does not exist.");
        }

        if (contact.IsPrimary)
        {
            await ClearPrimaryContactsAsync(contact.CustomerId);
        }

        _db.CustomerContacts.Add(contact);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteContactAsync(int id)
    {
        var contact = await _db.CustomerContacts.FindAsync(id);
        if (contact is null)
        {
            return (false, "Contact not found.");
        }

        _db.CustomerContacts.Remove(contact);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<List<CustomerAddress>> GetAddressesAsync(int customerId)
    {
        return await _db.CustomerAddresses
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.IsPrimary)
            .ThenBy(a => a.AddressType)
            .ToListAsync();
    }

    public async Task<(bool Success, string Error)> AddAddressAsync(CustomerAddress address)
    {
        address.AddressLine = string.IsNullOrWhiteSpace(address.AddressLine) ? string.Empty : address.AddressLine.Trim();
        if (string.IsNullOrWhiteSpace(address.AddressLine))
        {
            return (false, "Address is required.");
        }

        if (!await _db.Customers.AnyAsync(c => c.Id == address.CustomerId))
        {
            return (false, "Customer does not exist.");
        }

        if (address.IsPrimary)
        {
            await ClearPrimaryAddressesAsync(address.CustomerId);
        }

        _db.CustomerAddresses.Add(address);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAddressAsync(int id)
    {
        var address = await _db.CustomerAddresses.FindAsync(id);
        if (address is null)
        {
            return (false, "Address not found.");
        }

        _db.CustomerAddresses.Remove(address);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    private async Task ClearPrimaryContactsAsync(int customerId)
    {
        var primaries = await _db.CustomerContacts
            .Where(c => c.CustomerId == customerId && c.IsPrimary)
            .ToListAsync();
        foreach (var c in primaries)
        {
            c.IsPrimary = false;
        }
    }

    private async Task ClearPrimaryAddressesAsync(int customerId)
    {
        var primaries = await _db.CustomerAddresses
            .Where(a => a.CustomerId == customerId && a.IsPrimary)
            .ToListAsync();
        foreach (var a in primaries)
        {
            a.IsPrimary = false;
        }
    }
}