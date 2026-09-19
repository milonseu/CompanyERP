using CompanyERP.Entities.Asset;

namespace CompanyERP.Interfaces.Services;

public interface IAssetRegisterService
{
    Task<List<AssetRegister>> GetAllAsync(int companyId, int? branchId = null, AssetStatus? status = null, string? search = null);
    Task<AssetRegister?> GetByIdAsync(int id);
    Task<string> GenerateAssetNoAsync(int companyId, DateTime purchaseDate);
    Task<(bool Success, string Error)> CreateAsync(AssetRegister asset, AssetAcquisition acquisition);
    Task<(bool Success, string Error)> UpdateAsync(AssetRegister asset);
    Task<(bool Success, string Error)> DeleteAsync(int id);
    Task<List<AssetRegister>> GetAvailableAsync(int companyId, bool includeDisposed = false);
    Task<(bool Success, string Error)> AssignAsync(AssetAssignment assignment);
    Task<(bool Success, string Error)> ReturnAssetAsync(int assetId, DateTime returnedDate);
    Task<(bool Success, string Error)> TransferAsync(AssetTransfer transfer);
    Task<(bool Success, string Error)> AddDocumentAsync(AssetDocument document);
}