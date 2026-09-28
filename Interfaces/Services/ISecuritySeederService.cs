namespace CompanyERP.Interfaces.Services;
public interface ISecuritySeederService
{
    /// <summary>
    /// Seeds the security reference data a fresh database needs before anyone can register: the role
    /// list, the permission catalog, the Super Admin / Admin role-permission grants and the standard
    /// menu tree. No user account is created, so the first registration always becomes the Super Admin.
    /// </summary>
    Task SeedAsync();
}
