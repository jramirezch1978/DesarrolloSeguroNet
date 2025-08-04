using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace SecureShop.Security;

public class SecureClaimsTransformation : IClaimsTransformation
{
    private readonly ILogger<SecureClaimsTransformation> _logger;
    private readonly IUserService _userService;

    public SecureClaimsTransformation(
        ILogger<SecureClaimsTransformation> logger,
        IUserService userService)
    {
        _logger = logger;
        _userService = userService;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = (ClaimsIdentity)principal.Identity!;
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = principal.FindFirst(ClaimTypes.Email)?.Value ?? 
                   principal.FindFirst("preferred_username")?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("Usuario sin ID o email válido");
            return principal;
        }

        try
        {
            // Agregar roles específicos de la aplicación
            var userRoles = await _userService.GetUserRolesAsync(userId);
            foreach (var role in userRoles)
            {
                if (!principal.IsInRole(role))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }
            }

            // Agregar claims de departamento
            var department = await _userService.GetUserDepartmentAsync(userId);
            if (!string.IsNullOrEmpty(department))
            {
                identity.AddClaim(new Claim("department", department));
            }

            // Agregar permisos granulares
            var permissions = await _userService.GetUserPermissionsAsync(userId);
            foreach (var permission in permissions)
            {
                identity.AddClaim(new Claim("permission", permission));
            }

            // Agregar información de tienda/ubicación si aplica
            var storeId = await _userService.GetUserStoreIdAsync(userId);
            if (!string.IsNullOrEmpty(storeId))
            {
                identity.AddClaim(new Claim("store_id", storeId));
            }

            _logger.LogInformation("Claims enriquecidos para usuario {UserId}: {Roles} roles, {Permissions} permisos",
                userId, userRoles.Count, permissions.Count);

            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enriqueciendo claims para usuario {UserId}", userId);
            return principal; // Devolver principal original en caso de error
        }
    }
}