using MediatR;
using SecureBank.Domain.Enums;
using SecureBank.Shared.DTOs;
using System.ComponentModel.DataAnnotations;

namespace SecureBank.Application.Features.Products.Commands.ApplyForCredit;

/// <summary>
/// Comando para solicitar un crédito
/// </summary>
public class ApplyForCreditCommand : IRequest<ApplyForCreditResponse>
{
    /// <summary>
    /// ID del usuario solicitante
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Tipo de crédito solicitado
    /// </summary>
    [Required]
    public CreditType CreditType { get; set; }

    /// <summary>
    /// Monto solicitado
    /// </summary>
    [Required]
    [Range(1000, 500000)]
    public decimal RequestedAmount { get; set; }

    /// <summary>
    /// Plazo en meses
    /// </summary>
    [Required]
    [Range(6, 360)]
    public int TermInMonths { get; set; }

    /// <summary>
    /// Propósito del crédito
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// Información de ingresos
    /// </summary>
    [Required]
    public IncomeInfo IncomeInfo { get; set; } = new();

    /// <summary>
    /// Información de gastos
    /// </summary>
    [Required]
    public ExpenseInfo ExpenseInfo { get; set; } = new();
}

/// <summary>
/// Response de la solicitud de crédito
/// </summary>
public class ApplyForCreditResponse
{
    public bool Success { get; set; }
    public Guid ApplicationId { get; set; }
    public string ApplicationNumber { get; set; } = string.Empty;
    public CreditApplicationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public CreditOffer? PreApprovedOffer { get; set; }
    public CreditDecision Decision { get; set; } = new();
    public List<string> RequiredDocuments { get; set; } = new();
    public List<string> NextSteps { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Información laboral del solicitante
/// </summary>
public class EmploymentInfo
{
    [Required]
    [StringLength(20)]
    public string EmploymentType { get; set; } = string.Empty; // Dependiente, Independiente, Empresario

    [Required]
    [StringLength(100)]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(100)]
    public string Position { get; set; } = string.Empty;

    [Range(0, 50, ErrorMessage = "La antigüedad debe estar entre 0 y 50 años")]
    public int YearsInCompany { get; set; }

    [Range(0, 50, ErrorMessage = "La experiencia total debe estar entre 0 y 50 años")]
    public int TotalWorkExperience { get; set; }

    [StringLength(100)]
    public string Industry { get; set; } = string.Empty;

    [Phone]
    public string? CompanyPhone { get; set; }

    [StringLength(200)]
    public string? CompanyAddress { get; set; }
}

/// <summary>
/// Información de ingresos
/// </summary>
public class IncomeInfo
{
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Los ingresos deben ser mayores a 0")]
    public decimal MonthlyNetIncome { get; set; }

    [Range(0, double.MaxValue)]
    public decimal MonthlyGrossIncome { get; set; }

    [Range(0, double.MaxValue)]
    public decimal AdditionalIncome { get; set; }

    [StringLength(200)]
    public string? AdditionalIncomeSource { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SpouseIncome { get; set; }

    public bool HasVariableIncome { get; set; }

    [StringLength(500)]
    public string? IncomeNotes { get; set; }
}

/// <summary>
/// Información de gastos mensuales
/// </summary>
public class ExpenseInfo
{
    [Range(0, double.MaxValue)]
    public decimal HousingExpenses { get; set; } // Alquiler/hipoteca

    [Range(0, double.MaxValue)]
    public decimal FoodExpenses { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TransportationExpenses { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UtilitiesExpenses { get; set; }

    [Range(0, double.MaxValue)]
    public decimal EducationExpenses { get; set; }

    [Range(0, double.MaxValue)]
    public decimal HealthExpenses { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DebtPayments { get; set; } // Pagos de otras deudas

    [Range(0, double.MaxValue)]
    public decimal OtherExpenses { get; set; }

    [StringLength(500)]
    public string? ExpenseNotes { get; set; }
}

/// <summary>
/// Información de bienes y patrimonio
/// </summary>
public class AssetInfo
{
    [Range(0, double.MaxValue)]
    public decimal RealEstateValue { get; set; }

    [Range(0, double.MaxValue)]
    public decimal VehicleValue { get; set; }

    [Range(0, double.MaxValue)]
    public decimal BankDeposits { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Investments { get; set; }

    [Range(0, double.MaxValue)]
    public decimal OtherAssets { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalDebts { get; set; }

    [StringLength(500)]
    public string? AssetNotes { get; set; }
}

/// <summary>
/// Referencia personal
/// </summary>
public class PersonalReference
{
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Relationship { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(100)]
    public string? Occupation { get; set; }

    [Range(0, 50)]
    public int YearsKnown { get; set; }
}

/// <summary>
/// Referencia comercial
/// </summary>
public class CommercialReference
{
    [Required]
    [StringLength(100)]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string ContactPerson { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(50)]
    public string? RelationshipType { get; set; } // Cliente, Proveedor, etc.

    [Range(0, 50)]
    public int YearsOfRelationship { get; set; }
}

/// <summary>
/// Documento adjunto
/// </summary>
public class DocumentAttachment
{
    [Required]
    [StringLength(50)]
    public string DocumentType { get; set; } = string.Empty; // DNI, ReciboPago, EstadoCuenta, etc.

    [Required]
    [StringLength(500)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string FileUrl { get; set; } = string.Empty; // URL temporal segura

    [StringLength(200)]
    public string? Description { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public bool IsRequired { get; set; } = true;

    public bool IsVerified { get; set; } = false;
}

/// <summary>
/// Oferta de crédito pre-aprobada
/// </summary>
public class CreditOffer
{
    public decimal ApprovedAmount { get; set; }
    public int TermMonths { get; set; }
    public decimal InterestRate { get; set; }
    public decimal MonthlyPayment { get; set; }
    public decimal TotalToPay { get; set; }
    public DateTime ExpiresAt { get; set; }
    public List<string> Conditions { get; set; } = new();
    public List<string> Benefits { get; set; } = new();
}

/// <summary>
/// Decisión crediticia
/// </summary>
public class CreditDecision
{
    public string DecisionType { get; set; } = string.Empty; // Aprobado, Pre-aprobado, En evaluación, Rechazado
    public int CreditScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public List<string> PositiveFactors { get; set; } = new();
    public List<string> NegativeFactors { get; set; } = new();
    public List<string> RecommendationsToImprove { get; set; } = new();
    public DateTime? EstimatedDecisionDate { get; set; }
    public bool RequiresAdditionalVerification { get; set; }
}

/// <summary>
/// Tipos de crédito disponibles
/// </summary>
public enum CreditType
{
    Personal = 1,           // Crédito personal
    Vehicle = 2,           // Crédito vehicular
    Mortgage = 3,          // Crédito hipotecario
    Business = 4,          // Crédito empresarial
    Education = 5,         // Crédito educativo
    Medical = 6,           // Crédito médico
    Travel = 7,            // Crédito de viaje
    HomeImprovement = 8,   // Crédito para mejoras del hogar
    Consolidation = 9,     // Crédito de consolidación de deudas
    Investment = 10        // Crédito de inversión
}

/// <summary>
/// Estados de la solicitud de crédito
/// </summary>
public enum CreditApplicationStatus
{
    Submitted = 1,         // Enviada
    UnderReview = 2,       // En revisión
    DocumentsRequired = 3, // Documentos requeridos
    InVerification = 4,    // En verificación
    PreApproved = 5,       // Pre-aprobada
    Approved = 6,          // Aprobada
    Rejected = 7,          // Rechazada
    Cancelled = 8,         // Cancelada
    Expired = 9           // Expirada
} 