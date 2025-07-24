using MediatR;
using SecureBank.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SecureBank.Application.Features.Transactions.Commands.CreatePayment;

/// <summary>
/// Comando para crear un pago de servicios
/// </summary>
public class CreatePaymentCommand : IRequest<CreatePaymentResponse>
{
    /// <summary>
    /// ID del usuario que realiza el pago
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// ID de la cuenta desde la cual se realiza el pago
    /// </summary>
    [Required]
    public Guid FromAccountId { get; set; }

    /// <summary>
    /// Tipo de servicio a pagar
    /// </summary>
    [Required]
    public ServiceType ServiceType { get; set; }

    /// <summary>
    /// Código del proveedor del servicio
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ServiceProviderCode { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del proveedor del servicio
    /// </summary>
    [Required]
    [StringLength(200)]
    public string ServiceProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Número de servicio (número de suministro, cliente, etc.)
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ServiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Monto del pago
    /// </summary>
    [Required]
    [Range(0.01, 1000000)]
    public decimal Amount { get; set; }

    /// <summary>
    /// Moneda del pago
    /// </summary>
    [Required]
    [StringLength(3)]
    public string Currency { get; set; } = "PEN";

    /// <summary>
    /// Descripción del pago
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Período de facturación (ejemplo: "2024-01", "2024-Q1")
    /// </summary>
    [StringLength(20)]
    public string? BillingPeriod { get; set; }

    /// <summary>
    /// Número de factura o referencia
    /// </summary>
    [StringLength(100)]
    public string? InvoiceNumber { get; set; }

    /// <summary>
    /// Fecha de vencimiento del servicio
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Indica si el pago está programado para una fecha futura
    /// </summary>
    public bool IsScheduled { get; set; } = false;

    /// <summary>
    /// Fecha programada para el pago (requerida si IsScheduled = true)
    /// </summary>
    public DateTime? ScheduledDate { get; set; }

    /// <summary>
    /// Indica si es un pago recurrente
    /// </summary>
    public bool IsRecurring { get; set; } = false;

    /// <summary>
    /// Configuración de recurrencia (requerida si IsRecurring = true)
    /// </summary>
    public PaymentRecurrence? RecurrenceConfig { get; set; }

    /// <summary>
    /// Notificar por email
    /// </summary>
    public bool NotifyByEmail { get; set; } = true;

    /// <summary>
    /// Notificar por SMS
    /// </summary>
    public bool NotifyBySms { get; set; } = false;

    /// <summary>
    /// PIN de confirmación para la transacción
    /// </summary>
    [Required]
    [StringLength(6, MinimumLength = 4)]
    public string ConfirmationPin { get; set; } = string.Empty;

    /// <summary>
    /// Código de segundo factor (si está habilitado)
    /// </summary>
    [StringLength(10)]
    public string? TwoFactorCode { get; set; }

    /// <summary>
    /// Dirección IP desde la cual se origina la transacción
    /// </summary>
    [Required]
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// User Agent del cliente
    /// </summary>
    [Required]
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Huella digital del dispositivo
    /// </summary>
    [Required]
    public string DeviceFingerprint { get; set; } = string.Empty;

    /// <summary>
    /// ID de sesión
    /// </summary>
    [Required]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Datos adicionales específicos del servicio (JSON)
    /// </summary>
    public Dictionary<string, object>? AdditionalServiceData { get; set; }
}

/// <summary>
/// Respuesta del comando de creación de pago
/// </summary>
public class CreatePaymentResponse
{
    /// <summary>
    /// Indica si el pago fue exitoso
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ID de la transacción generada
    /// </summary>
    public Guid? TransactionId { get; set; }

    /// <summary>
    /// Número de transacción para seguimiento
    /// </summary>
    public string? TransactionNumber { get; set; }

    /// <summary>
    /// Estado actual de la transacción
    /// </summary>
    public TransactionStatus Status { get; set; }

    /// <summary>
    /// Información de procesamiento del pago
    /// </summary>
    public PaymentProcessingInfo? ProcessingInfo { get; set; }

    /// <summary>
    /// Mensajes de respuesta
    /// </summary>
    public List<string> Messages { get; set; } = new();

    /// <summary>
    /// Errores de validación si los hay
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Alertas de seguridad generadas
    /// </summary>
    public List<SecurityAlert> SecurityAlerts { get; set; } = new();

    /// <summary>
    /// Nivel de riesgo asignado
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    /// Timestamp de la respuesta
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Configuración de recurrencia para pagos
/// </summary>
public class PaymentRecurrence
{
    /// <summary>
    /// Frecuencia de recurrencia
    /// </summary>
    public RecurrenceFrequency Frequency { get; set; }

    /// <summary>
    /// Intervalo entre pagos (para frecuencias personalizadas)
    /// </summary>
    public int Interval { get; set; } = 1;

    /// <summary>
    /// Día del mes para pagos mensuales (1-31)
    /// </summary>
    public int? DayOfMonth { get; set; }

    /// <summary>
    /// Fecha de fin de recurrencia
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Número máximo de pagos (alternativa a EndDate)
    /// </summary>
    public int? MaxPayments { get; set; }

    /// <summary>
    /// Indica si está activa la recurrencia
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Información de procesamiento del pago
/// </summary>
public class PaymentProcessingInfo
{
    /// <summary>
    /// Tiempo estimado de procesamiento
    /// </summary>
    public TimeSpan EstimatedProcessingTime { get; set; }

    /// <summary>
    /// Comisión aplicada
    /// </summary>
    public decimal Fee { get; set; }

    /// <summary>
    /// Saldo disponible después del pago
    /// </summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>
    /// Número de confirmación del proveedor (si aplica)
    /// </summary>
    public string? ProviderConfirmationNumber { get; set; }

    /// <summary>
    /// Requiere aprobación manual
    /// </summary>
    public bool RequiresApproval { get; set; }

    /// <summary>
    /// Razón de aprobación manual
    /// </summary>
    public string? ApprovalReason { get; set; }
}

/// <summary>
/// Tipos de servicios disponibles para pago
/// </summary>
public enum ServiceType
{
    // Servicios básicos
    Electricity = 1,
    Water = 2,
    Gas = 3,
    Internet = 4,
    Phone = 5,
    CableTV = 6,
    
    // Educación
    School = 10,
    University = 11,
    OnlineCourse = 12,
    
    // Seguros
    HealthInsurance = 20,
    CarInsurance = 21,
    HomeInsurance = 22,
    LifeInsurance = 23,
    
    // Gobierno
    Taxes = 30,
    Fines = 31,
    Permits = 32,
    
    // Entretenimiento
    Streaming = 40,
    Gaming = 41,
    Gym = 42,
    
    // Transporte
    PublicTransport = 50,
    Toll = 51,
    Parking = 52,
    
    // Otros
    Credit = 60,
    Loan = 61,
    Other = 99
}

/// <summary>
/// Alerta de seguridad
/// </summary>
public class SecurityAlert
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public RiskLevel Level { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
} 