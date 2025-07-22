using Microsoft.AspNetCore.Http;
using SecureBank.Application.Common.Interfaces;
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

    public string? Role => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;

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

    public bool HasAccessToAccount(Guid accountId)
    {
        // Implementar lógica de verificación de acceso a cuenta
        // Por ahora retorna true si el usuario está autenticado
        return IsAuthenticated;
    }

    public SecurityContext GetSecurityContext()
    {
        return new SecurityContext
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
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceFingerprint { get; set; }
    public string? SessionId { get; set; }
    public bool IsAuthenticated { get; set; }
    public bool IsTrustedDevice { get; set; }
    public DateTime RequestTimestamp { get; set; }
} 