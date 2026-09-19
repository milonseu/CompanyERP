using CompanyERP.Entities.Asset;

namespace CompanyERP.Interfaces.Services;

public interface IAssetDisposalService
{
    Task<List<AssetDisposal>> GetAllAsync(int companyId);
    Task<AssetDisposal?> GetByIdAsync(int id);
    Task<(bool Success, string Error)> DisposeAsync(AssetDisposal disposal);
}