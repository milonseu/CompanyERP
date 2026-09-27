namespace CompanyERP.Interfaces.Services;
public interface ISecuritySeederService
{
    Task SeedAsync(string? adminUserName = null, string? adminPassword = null);
}
