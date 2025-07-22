using MediatR;
using SecureBank.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SecureBank.Application.Features.Transactions.Commands.CreatePayment;

/// <summary>
/// Command para crear un pago de servicios
/// </summary>
public class CreatePaymentCommand : IRequest<CreatePaymentResponse>
{
    /// <summary>
    /// ID del usuario que realiza el pago
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// ID de la cuenta origen del pago
    /// </summary>
    [Required]
    public Guid FromAccountId { get; set; }

    /// <summary>
    /// Tipo de servicio a pagar
    /// </summary>
    [Required]
    public ServiceType ServiceType { get; set; }

    /// <summary>
    /// Código de la empresa proveedora del servicio
    /// </summary>
    [Required]
    [StringLength(20)]
    public string ServiceProviderCode { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de la empresa proveedora
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ServiceProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Número de suministro, cliente o código identificador
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ServiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Monto del pago
    /// </summary>
    [Required]
    [Range(0.01, 20000, ErrorMessage = "El monto debe estar entre 0.01 y 20,000")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Moneda del pago
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "PEN";

    /// <summary>
    /// Descripción del pago
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 5)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Período de facturación (mes/año)
    /// </summary>
    [StringLength(20)]
    public string? BillingPeriod { get; set; }

    /// <summary>
    /// Número de factura o recibo
    /// </summary>
    [StringLength(50)]
    public string? InvoiceNumber { get; set; }

    /// <summary>
    /// Fecha de vencimiento de la factura
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Es un pago programado
    /// </summary>
    public bool IsScheduled { get; set; } = false;

    /// <summary>
    /// Fecha programada para el pago
    /// </summary>
    public DateTime? ScheduledDate { get; set; }

    /// <summary>
    /// Es un pago recurrente (débito automático)
    /// </summary>
    public bool IsRecurring { get; set; } = false;

    /// <summary>
    /// Configuración de recurrencia
    /// </summary>
    public PaymentRecurrence? RecurrenceConfig { get; set; }

    /// <summary>
    /// Solicitar notificación por email
    /// </summary>
    public bool NotifyByEmail { get; set; } = true;

    /// <summary>
    /// Solicitar notificación por SMS
    /// </summary>
    public bool NotifyBySms { get; set; } = false;

    /// <summary>
    /// PIN de confirmación
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
    /// Datos adicionales del servicio
    /// </summary>
    public Dictionary<string, string> AdditionalServiceData { get; set; } = new();
}

/// <summary>
/// Response de la creación de pago
/// </summary>
public class CreatePaymentResponse
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
    public string ServiceProviderName { get; set; } = string.Empty;
    public string ServiceNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ConfirmationCode { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public bool RequiresApproval { get; set; }
    public PaymentProcessingInfo ProcessingInfo { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<SecurityAlert> SecurityAlerts { get; set; } = new();
}

/// <summary>
/// Configuración de recurrencia para pagos
/// </summary>
public class PaymentRecurrence
{
    public RecurrenceFrequency Frequency { get; set; }
    public int DayOfMonth { get; set; } // Para pagos mensuales
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? MaxOccurrences { get; set; }
    public decimal? MaxAmount { get; set; } // Límite máximo por pago
    public bool AutoAdjustForHolidays { get; set; } = true;
}

/// <summary>
/// Información de procesamiento del pago
/// </summary>
public class PaymentProcessingInfo
{
    public TimeSpan EstimatedProcessingTime { get; set; }
    public bool RequiresManualReview { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public string ProcessingRoute { get; set; } = string.Empty; // Inmediato, Lote, Manual
    public List<string> NextSteps { get; set; } = new();
    public string? BankReferenceNumber { get; set; }
}

/// <summary>
/// Tipos de servicios disponibles para pago
/// </summary>
public enum ServiceType
{
    // Servicios básicos
    Electricity = 1,        // Luz
    Water = 2,             // Agua
    Gas = 3,               // Gas
    Telephone = 4,         // Teléfono fijo
    Internet = 5,          // Internet
    Cable = 6,             // TV Cable
    Mobile = 7,            // Teléfono móvil

    // Servicios financieros
    CreditCard = 10,       // Tarjeta de crédito
    Loan = 11,            // Préstamo
    Insurance = 12,        // Seguro
    Investment = 13,       // Inversión

    // Servicios públicos
    Municipality = 20,     // Municipalidad
    Tax = 21,             // Impuestos
    Traffic = 22,         // Multas de tránsito
    Education = 23,       // Educación
    Health = 24,          // Salud

    // Entretenimiento
    Streaming = 30,       // Netflix, Spotify, etc.
    Gaming = 31,          // Juegos online
    Subscription = 32,    // Suscripciones

    // Transporte
    Transport = 40,       // Transporte público
    Parking = 41,         // Estacionamiento
    Toll = 42,           // Peajes

    // Otros
    Other = 99           // Otros servicios
} 