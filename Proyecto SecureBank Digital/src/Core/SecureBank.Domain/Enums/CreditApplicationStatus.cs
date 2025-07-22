namespace SecureBank.Domain.Enums;

/// <summary>
/// Estados de una solicitud de crédito
/// </summary>
public enum CreditApplicationStatus
{
    /// <summary>
    /// Solicitud recibida, pendiente de revisión
    /// </summary>
    Pending = 1,

    /// <summary>
    /// En proceso de evaluación
    /// </summary>
    UnderReview = 2,

    /// <summary>
    /// Requiere documentación adicional
    /// </summary>
    RequiresDocumentation = 3,

    /// <summary>
    /// En evaluación crediticia
    /// </summary>
    CreditCheck = 4,

    /// <summary>
    /// Aprobada, pendiente de desembolso
    /// </summary>
    Approved = 5,

    /// <summary>
    /// Rechazada
    /// </summary>
    Rejected = 6,

    /// <summary>
    /// Cancelada por el cliente
    /// </summary>
    Cancelled = 7,

    /// <summary>
    /// Desembolsada
    /// </summary>
    Disbursed = 8,

    /// <summary>
    /// Expirada por falta de documentación
    /// </summary>
    Expired = 9
} 