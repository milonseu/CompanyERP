using System.Security.Claims;

namespace CompanyERP.Services.Security;

public static class PermissionExtensions
{
    public static bool HasPermission(this ClaimsPrincipal user, string permissionCode)
    {
        if (user.HasClaim(PermissionClaimsTransformer.SuperAdminClaimType, "1"))
        {
            return true;
        }

        foreach (var claim in user.FindAll(c => c.Type == PermissionClaimsTransformer.PermissionClaimType))
        {
            if (string.Equals(claim.Value, permissionCode, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}