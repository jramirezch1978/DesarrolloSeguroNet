namespace SecureBank.Shared.DTOs;

/// <summary>
/// Respuesta base para todas las operaciones de API
/// </summary>
public class BaseResponse
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public string? Code { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Respuesta de error estándar
/// </summary>
public class ErrorResponse : BaseResponse
{
    public ErrorResponse()
    {
        IsSuccess = false;
    }

    public Dictionary<string, string>? ValidationErrors { get; set; }
    public string? TraceId { get; set; }
}

/// <summary>
/// Respuesta de balance de cuenta
/// </summary>
public class AccountBalanceResponse : BaseResponse
{
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal AvailableBalance { get; set; }
    public string Currency { get; set; } = "PEN";
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Respuesta de detalles de cuenta
/// </summary>
public class AccountDetailsResponse : BaseResponse
{
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal AvailableBalance { get; set; }
    public string Currency { get; set; } = "PEN";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastTransactionDate { get; set; }
    public decimal DailyTransferLimit { get; set; }
    public decimal MonthlyTransferLimit { get; set; }
}

/// <summary>
/// Respuesta de creación de cuenta
/// </summary>
public class CreateAccountResponse : BaseResponse
{
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public string Currency { get; set; } = "PEN";
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Respuesta de actualización de límites
/// </summary>
public class UpdateLimitsResponse : BaseResponse
{
    public Guid AccountId { get; set; }
    public decimal DailyLimit { get; set; }
    public decimal MonthlyLimit { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Respuesta de cierre de cuenta
/// </summary>
public class CloseAccountResponse : BaseResponse
{
    public Guid AccountId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime ClosedAt { get; set; }
}

/// <summary>
/// Respuesta de aplicación de crédito
/// </summary>
public class ApplyForCreditResponse : BaseResponse
{
    public Guid ApplicationId { get; set; }
    public string ApplicationNumber { get; set; } = string.Empty;
    public string CreditType { get; set; } = string.Empty;
    public decimal RequestedAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ApplicationDate { get; set; }
    public string? NextSteps { get; set; }
}

/// <summary>
/// Respuesta de login
/// </summary>
public class LoginResponse : BaseResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public string DeviceFingerprint { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
}

/// <summary>
/// Respuesta de refresh token
/// </summary>
public class RefreshTokenResponse : BaseResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
}

/// <summary>
/// Información de ingresos
/// </summary>
public class IncomeInfo
{
    public decimal MonthlyIncome { get; set; }
    public string IncomeType { get; set; } = string.Empty;
    public string EmployerName { get; set; } = string.Empty;
    public int YearsEmployed { get; set; }
    public bool HasOtherIncome { get; set; }
    public decimal? OtherIncomeAmount { get; set; }
}

/// <summary>
/// Información de gastos
/// </summary>
public class ExpenseInfo
{
    public decimal MonthlyExpenses { get; set; }
    public decimal RentOrMortgage { get; set; }
    public decimal Utilities { get; set; }
    public decimal Transportation { get; set; }
    public decimal Food { get; set; }
    public decimal OtherExpenses { get; set; }
    public decimal ExistingLoans { get; set; }
} 