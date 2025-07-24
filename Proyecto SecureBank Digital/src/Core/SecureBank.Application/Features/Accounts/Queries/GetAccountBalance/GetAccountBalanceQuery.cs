using MediatR;
using SecureBank.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SecureBank.Application.Features.Accounts.Queries.GetAccountBalance;

/// <summary>
/// Query para obtener el saldo y detalles de una cuenta bancaria
/// </summary>
public class GetAccountBalanceQuery : IRequest<GetAccountBalanceResponse>
{
    /// <summary>
    /// Número de cuenta o ID de la cuenta
    /// </summary>
    [Required]
    public string AccountIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// ID único de la cuenta (Guid)
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// ID del usuario que solicita la información
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Incluir historial de transacciones recientes
    /// </summary>
    public bool IncludeRecentTransactions { get; set; } = false;

    /// <summary>
    /// Número de transacciones recientes a incluir
    /// </summary>
    [Range(0, 50)]
    public int TransactionCount { get; set; } = 10;

    /// <summary>
    /// Incluir información de límites y configuración
    /// </summary>
    public bool IncludeLimits { get; set; } = true;

    /// <summary>
    /// Incluir proyecciones de intereses
    /// </summary>
    public bool IncludeProjections { get; set; } = false;

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
}

/// <summary>
/// Response con información completa de la cuenta
/// </summary>
public class GetAccountBalanceResponse
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
    
    public AccountBalanceInfo? Account { get; set; }
    public List<RecentTransactionDto> RecentTransactions { get; set; } = new();
    public AccountLimitsInfo? Limits { get; set; }
    public InterestProjection? InterestProjection { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Información detallada del saldo y estado de la cuenta
/// </summary>
public class AccountBalanceInfo
{
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal HeldBalance { get; set; }
    public decimal PendingDeposits { get; set; }
    public decimal PendingWithdrawals { get; set; }
    public bool IsActive { get; set; }
    public DateTime LastTransactionDate { get; set; }
    public DateTime LastBalanceUpdate { get; set; }
    public string AccountStatus { get; set; } = string.Empty;
    public AccountMetrics Metrics { get; set; } = new();
}

/// <summary>
/// Información de límites y configuración de la cuenta
/// </summary>
public class AccountLimitsInfo
{
    public decimal DailyTransferLimit { get; set; }
    public decimal RemainingDailyLimit { get; set; }
    public decimal MonthlyTransferLimit { get; set; }
    public decimal RemainingMonthlyLimit { get; set; }
    public decimal OverdraftLimit { get; set; }
    public decimal UsedOverdraft { get; set; }
    public DateTime LimitsResetDate { get; set; }
    public List<LimitUsage> RecentLimitUsage { get; set; } = new();
}

/// <summary>
/// Proyección de intereses y rendimientos
/// </summary>
public class InterestProjection
{
    public decimal CurrentInterestRate { get; set; }
    public decimal AccruedInterest { get; set; }
    public decimal ProjectedMonthlyInterest { get; set; }
    public decimal ProjectedYearlyInterest { get; set; }
    public DateTime NextInterestPayment { get; set; }
    public List<InterestPayment> RecentInterestPayments { get; set; } = new();
}

/// <summary>
/// DTO para transacciones recientes
/// </summary>
public class RecentTransactionDto
{
    public Guid TransactionId { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal BalanceAfter { get; set; }
    public string? DestinationAccount { get; set; }
    public string? Reference { get; set; }
}

/// <summary>
/// Métricas de actividad de la cuenta
/// </summary>
public class AccountMetrics
{
    public int TransactionsThisMonth { get; set; }
    public decimal TotalInflowThisMonth { get; set; }
    public decimal TotalOutflowThisMonth { get; set; }
    public decimal AverageMonthlyBalance { get; set; }
    public int DaysSinceLastTransaction { get; set; }
    public decimal HighestBalance30Days { get; set; }
    public decimal LowestBalance30Days { get; set; }
}

/// <summary>
/// Uso de límites de transferencia
/// </summary>
public class LimitUsage
{
    public DateTime Date { get; set; }
    public decimal AmountUsed { get; set; }
    public decimal LimitAtTime { get; set; }
    public decimal PercentageUsed { get; set; }
}

/// <summary>
/// Pago de intereses histórico
/// </summary>
public class InterestPayment
{
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public decimal InterestRate { get; set; }
    public decimal BaseAmount { get; set; }
} 