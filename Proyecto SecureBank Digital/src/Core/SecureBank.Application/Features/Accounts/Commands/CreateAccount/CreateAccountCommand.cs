using MediatR;
using SecureBank.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SecureBank.Application.Features.Accounts.Commands.CreateAccount;

/// <summary>
/// Command para crear una nueva cuenta bancaria
/// </summary>
public class CreateAccountCommand : IRequest<CreateAccountResponse>
{
    /// <summary>
    /// ID del usuario propietario de la cuenta
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Tipo de cuenta a crear
    /// </summary>
    [Required]
    public AccountType AccountType { get; set; }

    /// <summary>
    /// Moneda de la cuenta (PEN, USD)
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "PEN";

    /// <summary>
    /// Depósito inicial (opcional para algunos tipos)
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "El depósito inicial debe ser mayor o igual a 0")]
    public decimal InitialDeposit { get; set; } = 0;

    /// <summary>
    /// Límite diario de transferencias personalizado
    /// </summary>
    [Range(0, 50000, ErrorMessage = "El límite diario debe estar entre 0 y 50,000")]
    public decimal? CustomDailyLimit { get; set; }

    /// <summary>
    /// Límite mensual de transferencias personalizado
    /// </summary>
    [Range(0, 500000, ErrorMessage = "El límite mensual debe estar entre 0 y 500,000")]
    public decimal? CustomMonthlyLimit { get; set; }

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
    /// Código de referido (opcional)
    /// </summary>
    [StringLength(20)]
    public string? ReferralCode { get; set; }

    /// <summary>
    /// Propósito de la cuenta (requerido para cuentas empresariales)
    /// </summary>
    [StringLength(500)]
    public string? AccountPurpose { get; set; }

    /// <summary>
    /// Información fiscal adicional
    /// </summary>
    public TaxInfo? TaxInformation { get; set; }
}

/// <summary>
/// Response de la creación de cuenta
/// </summary>
public class CreateAccountResponse
{
    public bool Success { get; set; }
    
    /// <summary>
    /// Compatibilidad: alias para Success
    /// </summary>
    public bool IsSuccess => Success;
    
    /// <summary>
    /// Mensaje descriptivo del resultado
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal DailyTransferLimit { get; set; }
    public decimal MonthlyTransferLimit { get; set; }
    public decimal InterestRate { get; set; }
    public decimal MaintenanceFee { get; set; }
    public DateTime CreatedAt { get; set; }
    public AccountRequirements Requirements { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Requisitos y limitaciones de la cuenta creada
/// </summary>
public class AccountRequirements
{
    public bool RequiresInitialDeposit { get; set; }
    public decimal MinimumInitialDeposit { get; set; }
    public bool RequiresMonthlyBalance { get; set; }
    public decimal MinimumMonthlyBalance { get; set; }
    public List<string> Features { get; set; } = new();
    public List<string> Restrictions { get; set; } = new();
}

/// <summary>
/// Información fiscal para cuentas empresariales
/// </summary>
public class TaxInfo
{
    [Required]
    [StringLength(20)]
    public string TaxId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(20)]
    public string TaxCategory { get; set; } = string.Empty;

    public bool IsExempt { get; set; }
} 