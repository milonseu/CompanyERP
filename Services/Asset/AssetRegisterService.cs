using CompanyERP.Data;
using CompanyERP.Entities.Asset;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Asset;

public class AssetRegisterService : IAssetRegisterService
{
    private readonly ApplicationDbContext _db;
    private readonly ITransactionPostingService _postingService;

    public AssetRegisterService(ApplicationDbContext db, ITransactionPostingService postingService)
    {
        _db = db;
        _postingService = postingService;
    }

    public async Task<List<AssetRegister>> GetAllAsync(int companyId, int? branchId = null, AssetStatus? status = null, string? search = null)
    {
        var query = _db.AssetRegisters
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId)
            .Include(a => a.Branch)
            .Include(a => a.AssetType).ThenInclude(t => t!.AssetCategory)
            .Include(a => a.Acquisition)
            .AsQueryable();

        if (branchId.HasValue)
        {
            query = query.Where(a => a.BranchId == branchId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLowerInvariant();
            query = query.Where(a => a.AssetNo.ToLower().Contains(keyword)
                || a.Name.ToLower().Contains(keyword)
                || (a.SerialNo != null && a.SerialNo.ToLower().Contains(keyword)));
        }

        return await query
            .OrderByDescending(a => a.PurchaseDate)
            .ThenByDescending(a => a.Id)
            .ToListAsync();
    }

    public async Task<AssetRegister?> GetByIdAsync(int id)
    {
        return await _db.AssetRegisters
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Include(a => a.Company)
            .Include(a => a.Branch)
            .Include(a => a.AssetType).ThenInclude(t => t!.AssetCategory)
            .Include(a => a.Acquisition).ThenInclude(ac => ac!.Supplier)
            .Include(a => a.Assignments).ThenInclude(x => x.Employee)
            .Include(a => a.Transfers).ThenInclude(x => x.FromBranch)
            .Include(a => a.Transfers).ThenInclude(x => x.ToBranch)
            .Include(a => a.Maintenances)
            .Include(a => a.Depreciations.OrderByDescending(d => d.PeriodDate))
            .Include(a => a.Disposal)
            .Include(a => a.Documents)
            .FirstOrDefaultAsync();
    }

    public async Task<string> GenerateAssetNoAsync(int companyId, DateTime purchaseDate)
    {
        var count = await _db.AssetRegisters
            .CountAsync(a => a.CompanyId == companyId && a.PurchaseDate.Date == purchaseDate.Date);
        return $"AST-{purchaseDate:yyyyMMdd}-{(count + 1):D3}";
    }

    public async Task<(bool Success, string Error)> CreateAsync(AssetRegister asset, AssetAcquisition acquisition)
    {
        asset.AssetNo = string.IsNullOrWhiteSpace(asset.AssetNo) ? string.Empty : asset.AssetNo.Trim();
        asset.Name = string.IsNullOrWhiteSpace(asset.Name) ? string.Empty : asset.Name.Trim();
        asset.SerialNo = string.IsNullOrWhiteSpace(asset.SerialNo) ? null : asset.SerialNo.Trim();
        asset.Model = string.IsNullOrWhiteSpace(asset.Model) ? null : asset.Model.Trim();
        asset.Note = string.IsNullOrWhiteSpace(asset.Note) ? null : asset.Note.Trim();
        acquisition.PaymentReference = string.IsNullOrWhiteSpace(acquisition.PaymentReference) ? null : acquisition.PaymentReference.Trim();
        acquisition.Note = string.IsNullOrWhiteSpace(acquisition.Note) ? null : acquisition.Note.Trim();

        if (string.IsNullOrWhiteSpace(asset.AssetNo))
        {
            return (false, "Asset number is required.");
        }

        if (string.IsNullOrWhiteSpace(asset.Name))
        {
            return (false, "Asset name is required.");
        }

        if (asset.Cost < 0)
        {
            return (false, "Cost cannot be negative.");
        }

        if (asset.SalvageValue > asset.Cost)
        {
            return (false, "Salvage value cannot exceed the cost.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == asset.CompanyId))
        {
            return (false, "Selected company does not exist.");
        }

        if (!await _db.Branches.AnyAsync(b => b.Id == asset.BranchId && b.CompanyId == asset.CompanyId))
        {
            return (false, "Selected branch does not belong to the company.");
        }

        if (!await _db.AssetTypes.AnyAsync(t => t.Id == asset.AssetTypeId && t.CompanyId == asset.CompanyId))
        {
            return (false, "Selected asset type does not belong to the company.");
        }

        if (asset.AssetTypeId > 0)
        {
            var type = await _db.AssetTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == asset.AssetTypeId);
            if (type != null)
            {
                asset.UsefulLifeMonths = type.UsefulLifeMonths;
                asset.SalvageValue = type.SalvageValue;
                asset.DepreciationMethod = type.DepreciationMethod;
            }
        }

        var exists = await _db.AssetRegisters.AnyAsync(a => a.CompanyId == asset.CompanyId && a.AssetNo == asset.AssetNo);
        if (exists)
        {
            return (false, $"Asset number '{asset.AssetNo}' already exists for the company.");
        }

        if (acquisition.AmountPaid > asset.Cost)
        {
            return (false, "Amount paid cannot exceed the asset cost.");
        }

        if (acquisition.SupplierId.HasValue &&
            !await _db.Suppliers.AnyAsync(s => s.Id == acquisition.SupplierId.Value && s.CompanyId == asset.CompanyId))
        {
            return (false, "Selected supplier does not belong to the company.");
        }

        acquisition.AcquisitionDate = acquisition.AcquisitionDate == default ? asset.PurchaseDate : acquisition.AcquisitionDate;

        asset.Acquisition = acquisition;
        asset.Status = AssetStatus.Registered;
        asset.AccumulatedDepreciation = 0;

        // NOTE: Accounting effect is created during Accounting module integration:
        // Debit Fixed Assets, Credit Cash/Bank or Asset Payable.
        var post = await _postingService.PostAssetAcquisitionAsync(asset, acquisition);
        if (!post.Success)
        {
            return (false, post.Error);
        }

        _db.AssetRegisters.Add(asset);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> UpdateAsync(AssetRegister asset)
    {
        var existing = await _db.AssetRegisters.AsNoTracking().FirstOrDefaultAsync(a => a.Id == asset.Id);
        if (existing is null)
        {
            return (false, "Asset not found.");
        }

        if (existing.Status == AssetStatus.Disposed)
        {
            return (false, "A disposed asset cannot be edited.");
        }

        asset.AssetNo = string.IsNullOrWhiteSpace(asset.AssetNo) ? string.Empty : asset.AssetNo.Trim();
        asset.Name = string.IsNullOrWhiteSpace(asset.Name) ? string.Empty : asset.Name.Trim();
        asset.SerialNo = string.IsNullOrWhiteSpace(asset.SerialNo) ? null : asset.SerialNo.Trim();
        asset.Model = string.IsNullOrWhiteSpace(asset.Model) ? null : asset.Model.Trim();
        asset.Note = string.IsNullOrWhiteSpace(asset.Note) ? null : asset.Note.Trim();

        if (string.IsNullOrWhiteSpace(asset.Name) || string.IsNullOrWhiteSpace(asset.AssetNo))
        {
            return (false, "Asset name and number are required.");
        }

        if (await _db.AssetRegisters.AnyAsync(a => a.CompanyId == existing.CompanyId && a.AssetNo == asset.AssetNo && a.Id != asset.Id))
        {
            return (false, $"Asset number '{asset.AssetNo}' already exists for the company.");
        }

        if (!await _db.AssetTypes.AnyAsync(t => t.Id == asset.AssetTypeId && t.CompanyId == existing.CompanyId))
        {
            return (false, "Selected asset type does not belong to the company.");
        }

        bool hasDepreciation = await _db.AssetDepreciations.AnyAsync(d => d.AssetRegisterId == asset.Id);
        if (hasDepreciation &&
            (asset.Cost != existing.Cost || asset.UsefulLifeMonths != existing.UsefulLifeMonths || asset.SalvageValue != existing.SalvageValue))
        {
            return (false, "Cost, useful life and salvage value cannot be changed after depreciation has been posted.");
        }

        asset.CompanyId = existing.CompanyId;
        asset.Cost = existing.Cost;
        asset.PurchaseDate = existing.PurchaseDate;
        asset.UsefulLifeMonths = existing.UsefulLifeMonths;
        asset.SalvageValue = existing.SalvageValue;
        asset.DepreciationMethod = existing.DepreciationMethod;
        asset.AccumulatedDepreciation = existing.AccumulatedDepreciation;
        asset.CreatedAt = existing.CreatedAt;
        asset.CreatedBy = existing.CreatedBy;
        _db.AssetRegisters.Update(asset);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(int id)
    {
        var asset = await _db.AssetRegisters.FindAsync(id);
        if (asset is null)
        {
            return (false, "Asset not found.");
        }

        if (await _db.AssetAssignments.AnyAsync(x => x.AssetRegisterId == id) ||
            await _db.AssetTransfers.AnyAsync(x => x.AssetRegisterId == id) ||
            await _db.AssetMaintenances.AnyAsync(x => x.AssetRegisterId == id) ||
            await _db.AssetDepreciations.AnyAsync(x => x.AssetRegisterId == id) ||
            await _db.AssetDisposals.AnyAsync(x => x.AssetRegisterId == id))
        {
            return (false, "Cannot delete an asset that has assignments, transfers, maintenance, depreciation or disposal history.");
        }

        _db.AssetRegisters.Remove(asset);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<List<AssetRegister>> GetAvailableAsync(int companyId, bool includeDisposed = false)
    {
        var query = _db.AssetRegisters
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.Status != AssetStatus.Disposed)
            .Include(a => a.AssetType)
            .Include(a => a.Branch)
            .AsQueryable();

        if (!includeDisposed)
        {
            query = query.Where(a => a.Assignments.All(x => x.ReturnedDate.HasValue));
        }

        return await query.OrderBy(a => a.AssetNo).ToListAsync();
    }

    public async Task<(bool Success, string Error)> AssignAsync(AssetAssignment assignment)
    {
        var asset = await _db.AssetRegisters.FindAsync(assignment.AssetRegisterId);
        if (asset is null)
        {
            return (false, "Asset not found.");
        }

        if (asset.Status == AssetStatus.Disposed)
        {
            return (false, "A disposed asset cannot be assigned.");
        }

        var open = await _db.AssetAssignments.AnyAsync(x => x.AssetRegisterId == asset.Id && !x.ReturnedDate.HasValue);
        if (open)
        {
            return (false, "Asset is already assigned and not yet returned.");
        }

        if (!await _db.Employees.AnyAsync(e => e.Id == assignment.EmployeeId &&
                (e.CompanyId == asset.CompanyId || e.DefaultBranchId == asset.BranchId || e.BranchAssignments.Any(ba => ba.BranchId == asset.BranchId))))
        {
            return (false, "Selected employee does not belong to the asset company or branch.");
        }

        assignment.AssignedDate = assignment.AssignedDate == default ? DateTime.Today : assignment.AssignedDate;
        assignment.Note = string.IsNullOrWhiteSpace(assignment.Note) ? null : assignment.Note.Trim();
        _db.AssetAssignments.Add(assignment);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> ReturnAssetAsync(int assetId, DateTime returnedDate)
    {
        var open = await _db.AssetAssignments
            .Where(x => x.AssetRegisterId == assetId && !x.ReturnedDate.HasValue)
            .OrderByDescending(x => x.AssignedDate)
            .FirstOrDefaultAsync();

        if (open is null)
        {
            return (false, "No open assignment found for the asset.");
        }

        open.ReturnedDate = returnedDate == default ? DateTime.Today : returnedDate;
        _db.AssetAssignments.Update(open);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> TransferAsync(AssetTransfer transfer)
    {
        var asset = await _db.AssetRegisters.FindAsync(transfer.AssetRegisterId);
        if (asset is null)
        {
            return (false, "Asset not found.");
        }

        if (asset.Status == AssetStatus.Disposed)
        {
            return (false, "A disposed asset cannot be transferred.");
        }

        if (!await _db.Branches.AnyAsync(b => b.Id == transfer.ToBranchId && b.CompanyId == asset.CompanyId))
        {
            return (false, "To-branch does not belong to the asset company.");
        }

        if (transfer.FromBranchId.HasValue &&
            !await _db.Branches.AnyAsync(b => b.Id == transfer.FromBranchId.Value && b.CompanyId == asset.CompanyId))
        {
            return (false, "From-branch does not belong to the asset company.");
        }

        if (transfer.FromBranchId.HasValue && transfer.FromBranchId.Value == transfer.ToBranchId)
        {
            return (false, "From-branch and to-branch cannot be the same.");
        }

        transfer.TransferDate = transfer.TransferDate == default ? DateTime.Today : transfer.TransferDate;
        transfer.Note = string.IsNullOrWhiteSpace(transfer.Note) ? null : transfer.Note.Trim();
        _db.AssetTransfers.Add(transfer);

        asset.BranchId = transfer.ToBranchId;
        if (!string.IsNullOrWhiteSpace(transfer.ToLocation))
        {
            asset.Location = transfer.ToLocation.Trim();
        }
        _db.AssetRegisters.Update(asset);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> AddDocumentAsync(AssetDocument document)
    {
        var asset = await _db.AssetRegisters.FindAsync(document.AssetRegisterId);
        if (asset is null)
        {
            return (false, "Asset not found.");
        }

        document.DocumentName = string.IsNullOrWhiteSpace(document.DocumentName) ? string.Empty : document.DocumentName.Trim();
        if (string.IsNullOrWhiteSpace(document.DocumentName))
        {
            return (false, "Document name is required.");
        }

        _db.AssetDocuments.Add(document);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}