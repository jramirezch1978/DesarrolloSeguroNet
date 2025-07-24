namespace SecureBank.Domain.Enums;

/// <summary>
/// Estados del usuario en SecureBank Digital
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// Cuenta creada pero pendiente de verificación de email y teléfono
    /// </summary>
    PendingVerification = 1,

    /// <summary>
    /// Cuenta activa y operativa
    /// </summary>
    Active = 2,

    /// <summary>
    /// Cuenta bloqueada temporalmente por intentos fallidos o sospecha de fraude
    /// </summary>
    Locked = 3,

    /// <summary>
    /// Cuenta suspendida por incumplimiento de términos
    /// </summary>
    Suspended = 4,

    /// <summary>
    /// Cuenta cerrada por el usuario o por decisión del banco
    /// </summary>
    Closed = 5,

    /// <summary>
    /// Cuenta inactiva por falta de uso prolongado
    /// </summary>
    Inactive = 6
} 