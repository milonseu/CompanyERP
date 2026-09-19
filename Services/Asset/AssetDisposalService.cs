using CompanyERP.Data;
using CompanyERP.Entities.Asset;
using CompanyERP.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace CompanyERP.Services.Asset;

public class AssetDisposalService : IAssetDisposalService
{
    private readonly ApplicationDbContext _db;

    public AssetDisposalService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<AssetDisposal>> GetAllAsync(int companyId)
    {
        return await _db.AssetDisposals
            .AsNoTracking()
            .Where(d => d.AssetRegister!.CompanyId == companyId)
            .Include(d => d.AssetRegister).ThenInclude(a => a!.Branch)
            .OrderByDescending(d => d.DisposalDate)
            .ThenByDescending(d => d.Id)
            .ToListAsync();
    }

    public async Task<AssetDisposal?> GetByIdAsync(int id)
    {
        return await _db.AssetDisposals
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Include(d => d.AssetRegister).ThenInclude(a => a!.Branch)
            .Include(d => d.AssetRegister).ThenInclude(a => a!.AssetType)
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Error)> DisposeAsync(AssetDisposal disposal)
    {
        var asset = await _db.AssetRegisters.FindAsync(disposal.AssetRegisterId);
        if (asset is null)
        {
            return (false, "Asset not found.");
        }

        if (asset.Status == AssetStatus.Disposed)
        {
            return (false, "Asset has already been disposed.");
        }

        if (await _db.AssetDisposals.AnyAsync(x => x.AssetRegisterId == asset.Id))
        {
            return (false, "A disposal already exists for this asset.");
        }

        if (disposal.SaleValue < 0)
        {
            return (false, "Sale value cannot be negative.");
        }

        disposal.DisposalDate = disposal.DisposalDate == default ? DateTime.Today : disposal.DisposalDate;
        disposal.Note = string.IsNullOrWhiteSpace(disposal.Note) ? null : disposal.Note.Trim();
        disposal.BookValueAtDisposal = asset.BookValue;
        disposal.GainLossAmount = Math.Round(disposal.SaleValue - asset.BookValue, 2);

        if (disposal.GainLossAmount > 0)
        {
            disposal.Result = DisposalResult.Gain;
        }
        else if (disposal.GainLossAmount < 0)
        {
            disposal.Result = DisposalResult.Loss;
        }
        else
        {
            disposal.Result = DisposalResult.NoGainLoss;
        }

        _db.AssetDisposals.Add(disposal);
        asset.Status = AssetStatus.Disposed;
        _db.AssetRegisters.Update(asset);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }
}
