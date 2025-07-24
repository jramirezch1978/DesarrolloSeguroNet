namespace SecureBank.Domain.Enums;

/// <summary>
/// Estados de las transacciones
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Transacción creada, pendiente de procesamiento
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Transacción en proceso de validación
    /// </summary>
    Processing = 2,

    /// <summary>
    /// Transacción completada exitosamente
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Transacción falló
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Transacción cancelada por el usuario o sistema
    /// </summary>
    Cancelled = 5,

    /// <summary>
    /// Transacción en espera de aprobación
    /// </summary>
    PendingApproval = 6,

    /// <summary>
    /// Transacción rechazada
    /// </summary>
    Rejected = 7,

    /// <summary>
    /// Transacción programada para ejecutarse más tarde
    /// </summary>
    Scheduled = 8,

    /// <summary>
    /// Transacción revertida
    /// </summary>
    Reversed = 9,

    /// <summary>
    /// Transacción en proceso de reversión
    /// </summary>
    Reversing = 10
} 