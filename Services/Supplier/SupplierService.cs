using CompanyERP.Data;
using CompanyERP.Entities.Supplier;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Supplier;

public class SupplierService : ISupplierService
{
    private readonly ApplicationDbContext _db;
    private readonly ITransactionPostingService _postingService;

    public SupplierService(ApplicationDbContext db, ITransactionPostingService postingService)
    {
        _db = db;
        _postingService = postingService;
    }

    public async Task<List<Entities.Supplier.Supplier>> GetAllAsync()
    {
        return await _db.Suppliers
            .Include(s => s.Company)
            .OrderBy(s => s.Company != null ? s.Company.Name : string.Empty)
            .ThenBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<Entities.Supplier.Supplier?> GetByIdAsync(int id)
    {
        return await _db.Suppliers
            .Include(s => s.Company)
            .Include(s => s.Contacts)
            .Include(s => s.Addresses)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<bool> CodeExistsAsync(string code, int companyId, int? excludeId = null)
    {
        var query = _db.Suppliers.Where(s => s.SupplierCode == code && s.CompanyId == companyId);
        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<(bool Success, string Error)> CreateAsync(Entities.Supplier.Supplier supplier)
    {
        supplier.SupplierCode = string.IsNullOrWhiteSpace(supplier.SupplierCode) ? string.Empty : supplier.SupplierCode.Trim().ToUpperInvariant();
        supplier.Name = string.IsNullOrWhiteSpace(supplier.Name) ? string.Empty : supplier.Name.Trim();

        if (string.IsNullOrWhiteSpace(supplier.SupplierCode) || string.IsNullOrWhiteSpace(supplier.Name))
        {
            return (false, "Supplier code and name are required.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == supplier.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (await CodeExistsAsync(supplier.SupplierCode, supplier.CompanyId))
        {
            return (false, $"Supplier code already exists for company {supplier.CompanyId}.");
        }

if (supplier.OpeningPayable < 0)
        {
            return (false, "Opening payable cannot be negative.");
        }

        // NOTE: Accounting effect is created during Accounting module integration:
        // Debit Opening Balance Equity, Credit Accounts Payable.
        var post = await _postingService.PostSupplierOpeningAsync(supplier, supplier.OpeningPayable);
        if (!post.Success)
        {
            return (false, post.Error);
        }

        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(Entities.Supplier.Supplier supplier)
    {
        supplier.SupplierCode = string.IsNullOrWhiteSpace(supplier.SupplierCode) ? string.Empty : supplier.SupplierCode.Trim().ToUpperInvariant();
        supplier.Name = string.IsNullOrWhiteSpace(supplier.Name) ? string.Empty : supplier.Name.Trim();

        var existing = await _db.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == supplier.Id);
        if (existing is null)
        {
            return (false, "Supplier not found.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == supplier.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (await CodeExistsAsync(supplier.SupplierCode, supplier.CompanyId, supplier.Id))
        {
            return (false, $"Supplier code already exists for company {supplier.CompanyId}.");
        }

        if (supplier.OpeningPayable < 0)
        {
            return (false, "Opening payable cannot be negative.");
        }

        supplier.CreatedAt = existing.CreatedAt;
        supplier.CreatedBy = existing.CreatedBy;

        _db.Suppliers.Update(supplier);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier is null)
        {
            return (false, "Supplier not found.");
        }

        _db.Suppliers.Remove(supplier);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<List<SupplierContact>> GetContactsAsync(int supplierId)
    {
        return await _db.SupplierContacts
            .Where(c => c.SupplierId == supplierId)
            .OrderByDescending(c => c.IsPrimary)
            .ThenBy(c => c.ContactPerson)
            .ToListAsync();
    }

    public async Task<(bool Success, string Error)> AddContactAsync(SupplierContact contact)
    {
        contact.ContactPerson = string.IsNullOrWhiteSpace(contact.ContactPerson) ? string.Empty : contact.ContactPerson.Trim();
        if (string.IsNullOrWhiteSpace(contact.ContactPerson))
        {
            return (false, "Contact person is required.");
        }

        if (!await _db.Suppliers.AnyAsync(s => s.Id == contact.SupplierId))
        {
            return (false, "Supplier does not exist.");
        }

        if (contact.IsPrimary)
        {
            await ClearPrimaryContactsAsync(contact.SupplierId);
        }

        _db.SupplierContacts.Add(contact);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteContactAsync(int id)
    {
        var contact = await _db.SupplierContacts.FindAsync(id);
        if (contact is null)
        {
            return (false, "Contact not found.");
        }

        _db.SupplierContacts.Remove(contact);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<List<SupplierAddress>> GetAddressesAsync(int supplierId)
    {
        return await _db.SupplierAddresses
            .Where(a => a.SupplierId == supplierId)
            .OrderByDescending(a => a.IsPrimary)
            .ThenBy(a => a.AddressType)
            .ToListAsync();
    }

    public async Task<(bool Success, string Error)> AddAddressAsync(SupplierAddress address)
    {
        address.AddressLine = string.IsNullOrWhiteSpace(address.AddressLine) ? string.Empty : address.AddressLine.Trim();
        if (string.IsNullOrWhiteSpace(address.AddressLine))
        {
            return (false, "Address is required.");
        }

        if (!await _db.Suppliers.AnyAsync(s => s.Id == address.SupplierId))
        {
            return (false, "Supplier does not exist.");
        }

        if (address.IsPrimary)
        {
            await ClearPrimaryAddressesAsync(address.SupplierId);
        }

        _db.SupplierAddresses.Add(address);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAddressAsync(int id)
    {
        var address = await _db.SupplierAddresses.FindAsync(id);
        if (address is null)
        {
            return (false, "Address not found.");
        }

        _db.SupplierAddresses.Remove(address);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    private async Task ClearPrimaryContactsAsync(int supplierId)
    {
        var primaries = await _db.SupplierContacts
            .Where(c => c.SupplierId == supplierId && c.IsPrimary)
            .ToListAsync();
        foreach (var c in primaries)
        {
            c.IsPrimary = false;
        }
    }

    private async Task ClearPrimaryAddressesAsync(int supplierId)
    {
        var primaries = await _db.SupplierAddresses
            .Where(a => a.SupplierId == supplierId && a.IsPrimary)
            .ToListAsync();
        foreach (var a in primaries)
        {
            a.IsPrimary = false;
        }
    }
}