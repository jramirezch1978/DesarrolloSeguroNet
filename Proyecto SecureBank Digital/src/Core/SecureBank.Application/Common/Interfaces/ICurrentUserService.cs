using SecureBank.Domain.Enums;

namespace SecureBank.Application.Common.Interfaces;

/// <summary>
/// Servicio para obtener información del usuario actual en SecureBank Digital
/// Proporciona acceso seguro al contexto del usuario autenticado
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// ID del usuario actual
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Email del usuario actual
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Rol del usuario actual
    /// </summary>
    UserRole? Role { get; }

    /// <summary>
    /// Dirección IP del usuario actual
    /// </summary>
    string? IpAddress { get; }

    /// <summary>
    /// User agent del navegador/dispositivo
    /// </summary>
    string? UserAgent { get; }

    /// <summary>
    /// Fingerprint del dispositivo
    /// </summary>
    string? DeviceFingerprint { get; }

    /// <summary>
    /// ID de la sesión actual
    /// </summary>
    string? SessionId { get; }

    /// <summary>
    /// Indica si el usuario está autenticado
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Indica si el dispositivo actual es confiable
    /// </summary>
    bool IsTrustedDevice { get; }

    /// <summary>
    /// Verifica si el usuario actual tiene un rol específico
    /// </summary>
    bool HasRole(UserRole role);

    /// <summary>
    /// Verifica si el usuario actual tiene al menos uno de los roles especificados
    /// </summary>
    bool HasAnyRole(params UserRole[] roles);

    /// <summary>
    /// Verifica si el usuario actual puede acceder a una cuenta específica
    /// </summary>
    /// <param name="accountId">ID de la cuenta a verificar</param>
    /// <returns>True si tiene acceso, false en caso contrario</returns>
    bool HasAccessToAccount(Guid accountId);

    /// <summary>
    /// Obtiene información completa del contexto de seguridad
    /// </summary>
    SecurityContext GetSecurityContext();
}

/// <summary>
/// Contexto de seguridad del usuario actual
/// </summary>
public class SecurityContext
{
    public Guid? UserId { get; init; }
    public string? Email { get; init; }
    public UserRole? Role { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? DeviceFingerprint { get; init; }
    public string? SessionId { get; init; }
    public bool IsAuthenticated { get; init; }
    public bool IsTrustedDevice { get; init; }
    public DateTime RequestTimestamp { get; init; }
    public string? Country { get; init; }
    public string? City { get; init; }
    public List<string> Permissions { get; init; } = new();
} 