using Microsoft.AspNetCore.Http;
using SecureBank.Application.Common.Interfaces;
using SecureBank.Domain.Enums;
using System.Security.Claims;

namespace SecureBank.Infrastructure.Services;

/// <summary>
/// Implementación del servicio de usuario actual
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;

    public UserRole? Role
    {
        get
        {
            var roleClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<UserRole>(roleClaim, out var role) ? role : null;
        }
    }

    public string? IpAddress
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            return context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim() ??
                   context.Request.Headers["X-Real-IP"].FirstOrDefault() ??
                   context.Connection.RemoteIpAddress?.ToString();
        }
    }

    public string? UserAgent => _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public string? DeviceFingerprint => _httpContextAccessor.HttpContext?.Request.Headers["X-Device-Fingerprint"].ToString();

    public string? SessionId => _httpContextAccessor.HttpContext?.Request.Headers["X-Session-Id"].ToString();

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public bool IsTrustedDevice
    {
        get
        {
            var trustedClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("is_trusted_device")?.Value;
            return bool.TryParse(trustedClaim, out var isTrusted) && isTrusted;
        }
    }

    public bool IsInRole(string role)
    {
        return _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
    }

    public bool HasRole(UserRole role)
    {
        var currentRole = Role;
        return currentRole.HasValue && currentRole.Value == role;
    }

    public bool HasAnyRole(params UserRole[] roles)
    {
        var currentRole = Role;
        return currentRole.HasValue && roles.Contains(currentRole.Value);
    }

    public bool HasAccessToAccount(Guid accountId)
    {
        // TODO: Implementar lógica de verificación de acceso a cuenta
        // Esto debería verificar que el usuario actual tiene acceso a la cuenta específica
        // Por ahora retorna true si el usuario está autenticado
        return IsAuthenticated;
    }

    public bool CanAccessAccount(Guid accountId)
    {
        return HasAccessToAccount(accountId);
    }

    public Application.Common.Interfaces.SecurityContext GetSecurityContext()
    {
        return new Application.Common.Interfaces.SecurityContext
        {
            UserId = UserId,
            Email = Email,
            Role = Role,
            IpAddress = IpAddress,
            UserAgent = UserAgent,
            DeviceFingerprint = DeviceFingerprint,
            SessionId = SessionId,
            IsAuthenticated = IsAuthenticated,
            IsTrustedDevice = IsTrustedDevice,
            RequestTimestamp = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Contexto de seguridad del usuario actual
/// </summary>
public class SecurityContext
{
    /// <summary>
    /// ID del usuario actual
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Email del usuario actual
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Rol del usuario actual
    /// </summary>
    public UserRole? Role { get; set; }

    /// <summary>
    /// Dirección IP de la solicitud
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User Agent del navegador
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Huella digital del dispositivo
    /// </summary>
    public string? DeviceFingerprint { get; set; }

    /// <summary>
    /// ID de sesión
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Indica si el usuario está autenticado
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// Indica si es un dispositivo de confianza
    /// </summary>
    public bool IsTrustedDevice { get; set; }

    /// <summary>
    /// Timestamp de la solicitud
    /// </summary>
    public DateTime RequestTimestamp { get; set; }

    /// <summary>
    /// Información geográfica derivada de la IP
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Ciudad derivada de la IP
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Score de riesgo de la sesión
    /// </summary>
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;

    /// <summary>
    /// Indica si se requiere autenticación de segundo factor
    /// </summary>
    public bool RequiresTwoFactor { get; set; }

    /// <summary>
    /// Duración de la sesión actual
    /// </summary>
    public TimeSpan? SessionDuration { get; set; }

    /// <summary>
    /// Último acceso registrado
    /// </summary>
    public DateTime? LastAccess { get; set; }
} 