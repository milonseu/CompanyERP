using System.Security.Claims;
using CompanyERP.Interfaces.Services;
using Microsoft.AspNetCore.Authentication;

namespace CompanyERP.Services.Security;

public sealed class PermissionClaimsTransformer : IClaimsTransformation
{
    private readonly IAuthService _auth;

    public PermissionClaimsTransformer(IAuthService auth)
    {
        _auth = auth;
    }

    public const string PermissionClaimType = "perm";
    public const string SuperAdminClaimType = "superadmin";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return principal;
        }

        var userIdValue = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdValue, out var userId))
        {
            return principal;
        }

        var clone = principal.Clone();
        var identity = (ClaimsIdentity)clone.Identity!;

        var stale = identity.FindAll(c =>
            c.Type == PermissionClaimType || c.Type == SuperAdminClaimType).ToList();
        foreach (var claim in stale)
        {
            identity.RemoveClaim(claim);
        }

        if (await _auth.IsSuperAdminAsync(userId))
        {
            identity.AddClaim(new Claim(SuperAdminClaimType, "1"));
        }

        foreach (var code in await _auth.GetPermissionCodesAsync(userId))
        {
            identity.AddClaim(new Claim(PermissionClaimType, code));
        }

        return clone;
    }
}