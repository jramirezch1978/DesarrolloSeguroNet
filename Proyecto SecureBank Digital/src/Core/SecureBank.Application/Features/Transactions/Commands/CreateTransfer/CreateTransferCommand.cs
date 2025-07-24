using MediatR;
using SecureBank.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SecureBank.Application.Features.Transactions.Commands.CreateTransfer;

/// <summary>
/// Command para crear una transferencia bancaria
/// </summary>
public class CreateTransferCommand : IRequest<CreateTransferResponse>
{
    /// <summary>
    /// ID del usuario que realiza la transferencia
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// ID de la cuenta origen
    /// </summary>
    [Required]
    public Guid FromAccountId { get; set; }

    /// <summary>
    /// Número de cuenta destino
    /// </summary>
    [Required]
    [StringLength(20, MinimumLength = 10)]
    public string ToAccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Banco destino (para transferencias interbancarias)
    /// </summary>
    [StringLength(10)]
    public string? ToBankCode { get; set; }

    /// <summary>
    /// Tipo de documento del beneficiario
    /// </summary>
    [StringLength(3)]
    public string? BeneficiaryDocumentType { get; set; }

    /// <summary>
    /// Número de documento del beneficiario
    /// </summary>
    [StringLength(20)]
    public string? BeneficiaryDocumentNumber { get; set; }

    /// <summary>
    /// Nombre del beneficiario
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string BeneficiaryName { get; set; } = string.Empty;

    /// <summary>
    /// Monto de la transferencia
    /// </summary>
    [Required]
    [Range(0.01, 50000, ErrorMessage = "El monto debe estar entre 0.01 y 50,000")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Moneda de la transferencia
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "PEN";

    /// <summary>
    /// Descripción o concepto de la transferencia
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 5)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Referencia adicional (opcional)
    /// </summary>
    [StringLength(50)]
    public string? Reference { get; set; }

    /// <summary>
    /// Es una transferencia programada
    /// </summary>
    public bool IsScheduled { get; set; } = false;

    /// <summary>
    /// Fecha programada (si IsScheduled = true)
    /// </summary>
    public DateTime? ScheduledDate { get; set; }

    /// <summary>
    /// Es una transferencia recurrente
    /// </summary>
    public bool IsRecurring { get; set; } = false;

    /// <summary>
    /// Frecuencia de recurrencia
    /// </summary>
    public RecurrenceFrequency? RecurrenceFrequency { get; set; }

    /// <summary>
    /// Fecha de fin de la recurrencia
    /// </summary>
    public DateTime? RecurrenceEndDate { get; set; }

    /// <summary>
    /// Solicitar notificación por email
    /// </summary>
    public bool NotifyByEmail { get; set; } = true;

    /// <summary>
    /// Solicitar notificación por SMS
    /// </summary>
    public bool NotifyBySms { get; set; } = false;

    /// <summary>
    /// PIN de confirmación para la transacción
    /// </summary>
    [Required]
    [StringLength(6, MinimumLength = 4)]
    public string ConfirmationPin { get; set; } = string.Empty;

    /// <summary>
    /// Código de doble factor (si está habilitado)
    /// </summary>
    [StringLength(10)]
    public string? TwoFactorCode { get; set; }

    /// <summary>
    /// Información de auditoría - dirección IP
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Información de auditoría - user agent
    /// </summary>
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Huella digital del dispositivo
    /// </summary>
    public string DeviceFingerprint { get; set; } = string.Empty;

    /// <summary>
    /// ID de sesión
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Datos adicionales para análisis de fraude
    /// </summary>
    public FraudAnalysisData? FraudAnalysis { get; set; }
}

/// <summary>
/// Response de la creación de transferencia
/// </summary>
public class CreateTransferResponse
{
    public bool Success { get; set; }
    public Guid TransactionId { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? EstimatedCompletionTime { get; set; }
    public string ToAccountNumber { get; set; } = string.Empty;
    public string BeneficiaryName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; }
    public bool RequiresApproval { get; set; }
    public TransferRequirements Requirements { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<SecurityAlert> SecurityAlerts { get; set; } = new();
}

/// <summary>
/// Datos para análisis de fraude
/// </summary>
public class FraudAnalysisData
{
    public bool IsNewBeneficiary { get; set; }
    public bool IsNewDevice { get; set; }
    public bool IsNewLocation { get; set; }
    public int TransactionsLast24Hours { get; set; }
    public decimal AmountLast24Hours { get; set; }
    public TimeSpan TimeSinceLastTransaction { get; set; }
    public bool IsOutsideBusinessHours { get; set; }
    public string? GeolocationData { get; set; }
    public double BehaviorScore { get; set; }
}

/// <summary>
/// Requisitos adicionales para la transferencia
/// </summary>
public class TransferRequirements
{
    public bool RequiresTwoFactor { get; set; }
    public bool RequiresManagerApproval { get; set; }
    public bool RequiresAdditionalVerification { get; set; }
    public List<string> VerificationSteps { get; set; } = new();
    public TimeSpan EstimatedProcessingTime { get; set; }
}

/// <summary>
/// Alerta de seguridad
/// </summary>
public class SecurityAlert
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Frecuencia de recurrencia
/// </summary>
public enum RecurrenceFrequency
{
    Daily = 1,
    Weekly = 2,
    BiWeekly = 3,
    Monthly = 4,
    Quarterly = 5,
    SemiAnnual = 6,
    Annual = 7
}

/// <summary>
/// Estado de la transacción
/// </summary>
public enum TransactionStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5,
    Scheduled = 6,
    RequiresApproval = 7,
    Approved = 8,
    Rejected = 9,
    Expired = 10
}

/// <summary>
/// Nivel de riesgo
/// </summary>
public enum RiskLevel
{
    VeryLow = 1,
    Low = 2,
    Medium = 3,
    High = 4,
    VeryHigh = 5,
    Critical = 6
} 