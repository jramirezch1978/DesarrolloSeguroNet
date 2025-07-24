namespace SecureBank.Domain.Entities;

/// <summary>
/// Entidad para rastrear intentos de login en SecureBank Digital
/// Implementa detección de patrones sospechosos y rate limiting
/// </summary>
public class LoginAttempt
{
    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; }
    public string? Email { get; private set; }
    public string? DocumentNumber { get; private set; }
    public bool IsSuccessful { get; private set; }
    public string FailureReason { get; private set; } = string.Empty;
    public DateTime AttemptedAt { get; private set; }
    public string IpAddress { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;
    public string? DeviceFingerprint { get; private set; }
    public string? SessionId { get; private set; }
    public string Country { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public bool IsTrustedDevice { get; private set; }
    public bool RequiredTwoFactor { get; private set; }
    public bool TwoFactorPassed { get; private set; }
    public string? TwoFactorMethod { get; private set; }
    public TimeSpan Duration { get; private set; }
    public bool IsBlocked { get; private set; }
    public string? BlockReason { get; private set; }
    public RiskScore RiskScore { get; private set; }
    public string? RiskFactors { get; private set; }

    // Navegación
    public virtual User? User { get; private set; }

    // Constructor privado para EF Core
    private LoginAttempt() { }

    /// <summary>
    /// Constructor para crear un nuevo intento de login
    /// </summary>
    public LoginAttempt(
        string ipAddress,
        string userAgent,
        string? email = null,
        string? documentNumber = null,
        string? deviceFingerprint = null,
        string? sessionId = null,
        string country = "Unknown",
        string city = "Unknown")
    {
        Id = Guid.NewGuid();
        Email = email?.ToLowerInvariant().Trim();
        DocumentNumber = documentNumber?.Trim();
        AttemptedAt = DateTime.UtcNow;
        IpAddress = ValidateIpAddress(ipAddress);
        UserAgent = ValidateUserAgent(userAgent);
        DeviceFingerprint = deviceFingerprint;
        SessionId = sessionId;
        Country = ValidateLocation(country);
        City = ValidateLocation(city);
        
        // Estados iniciales
        IsSuccessful = false;
        FailureReason = string.Empty;
        IsTrustedDevice = false;
        RequiredTwoFactor = false;
        TwoFactorPassed = false;
        IsBlocked = false;
        Duration = TimeSpan.Zero;
        RiskScore = RiskScore.Low; // Se calculará después
    }

    /// <summary>
    /// Marca el intento como exitoso
    /// </summary>
    public void MarkAsSuccessful(
        Guid userId, 
        bool isTrustedDevice, 
        bool requiredTwoFactor = false, 
        bool twoFactorPassed = false,
        string? twoFactorMethod = null)
    {
        if (IsSuccessful)
            throw new InvalidOperationException("El intento ya está marcado como exitoso");

        UserId = userId;
        IsSuccessful = true;
        FailureReason = string.Empty;
        IsTrustedDevice = isTrustedDevice;
        RequiredTwoFactor = requiredTwoFactor;
        TwoFactorPassed = twoFactorPassed;
        TwoFactorMethod = twoFactorMethod;
        Duration = DateTime.UtcNow - AttemptedAt;
        
        // Recalcular score de riesgo
        RiskScore = CalculateRiskScore();
    }

    /// <summary>
    /// Marca el intento como fallido
    /// </summary>
    public void MarkAsFailed(string reason, bool wasBlocked = false, string? blockReason = null)
    {
        if (IsSuccessful)
            throw new InvalidOperationException("No se puede marcar como fallido un intento exitoso");

        IsSuccessful = false;
        FailureReason = ValidateFailureReason(reason);
        IsBlocked = wasBlocked;
        BlockReason = blockReason;
        Duration = DateTime.UtcNow - AttemptedAt;
        
        // Recalcular score de riesgo
        RiskScore = CalculateRiskScore();
    }

    /// <summary>
    /// Actualiza el score de riesgo con factores adicionales
    /// </summary>
    public void UpdateRiskScore(RiskScore newScore, string? riskFactors = null)
    {
        RiskScore = newScore;
        if (!string.IsNullOrWhiteSpace(riskFactors))
        {
            RiskFactors = RiskFactors != null ? $"{RiskFactors}; {riskFactors}" : riskFactors;
        }
    }

    /// <summary>
    /// Verifica si el intento es desde una ubicación nueva
    /// </summary>
    public bool IsFromNewLocation(IEnumerable<LoginAttempt> previousAttempts)
    {
        if (!previousAttempts.Any())
            return true;

        return !previousAttempts.Any(attempt => 
            attempt.Country == Country && 
            attempt.City == City && 
            attempt.IsSuccessful);
    }

    /// <summary>
    /// Verifica si el intento es desde un dispositivo nuevo
    /// </summary>
    public bool IsFromNewDevice(IEnumerable<LoginAttempt> previousAttempts)
    {
        if (string.IsNullOrWhiteSpace(DeviceFingerprint))
            return true;

        return !previousAttempts.Any(attempt => 
            attempt.DeviceFingerprint == DeviceFingerprint && 
            attempt.IsSuccessful);
    }

    /// <summary>
    /// Verifica si es un intento fuera del horario normal
    /// </summary>
    public bool IsOutsideNormalHours()
    {
        var hour = AttemptedAt.Hour;
        return hour < 6 || hour > 23; // Fuera de 6 AM - 11 PM
    }

    /// <summary>
    /// Calcula la velocidad de intentos recientes
    /// </summary>
    public static int CalculateRecentAttemptsRate(
        IEnumerable<LoginAttempt> recentAttempts, 
        TimeSpan timeWindow)
    {
        var cutoffTime = DateTime.UtcNow - timeWindow;
        return recentAttempts.Count(attempt => attempt.AttemptedAt >= cutoffTime);
    }

    /// <summary>
    /// Obtiene un resumen del intento para logs
    /// </summary>
    public string GetSummary()
    {
        var result = IsSuccessful ? "SUCCESS" : "FAILED";
        var identity = Email ?? DocumentNumber ?? "Unknown";
        var mfa = RequiredTwoFactor ? (TwoFactorPassed ? " (2FA: PASS)" : " (2FA: FAIL)") : "";
        
        return $"[{AttemptedAt:yyyy-MM-dd HH:mm:ss}] {result} - {identity} from {IpAddress} ({Country}/{City}){mfa} - Risk: {RiskScore}";
    }

    // Métodos privados
    private static string ValidateIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("La dirección IP es obligatoria", nameof(ipAddress));

        return ipAddress.Trim();
    }

    private static string ValidateUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "Unknown";

        return userAgent.Length > 500 ? userAgent.Substring(0, 500) : userAgent.Trim();
    }

    private static string ValidateLocation(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return "Unknown";

        return location.Length > 100 ? location.Substring(0, 100) : location.Trim();
    }

    private static string ValidateFailureReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("La razón del fallo es obligatoria", nameof(reason));

        if (reason.Length > 500)
            throw new ArgumentException("La razón del fallo no puede tener más de 500 caracteres", nameof(reason));

        return reason.Trim();
    }

    private RiskScore CalculateRiskScore()
    {
        var score = 0;

        // Factores que aumentan el riesgo
        if (!IsSuccessful) score += 2;
        if (IsBlocked) score += 3;
        if (!IsTrustedDevice) score += 1;
        if (RequiredTwoFactor && !TwoFactorPassed) score += 3;
        if (IsOutsideNormalHours()) score += 1;
        if (Country == "Unknown") score += 2;

        return score switch
        {
            >= 7 => RiskScore.Critical,
            >= 5 => RiskScore.High,
            >= 3 => RiskScore.Medium,
            _ => RiskScore.Low
        };
    }
}

/// <summary>
/// Score de riesgo para intentos de login
/// </summary>
public enum RiskScore
{
    /// <summary>
    /// Riesgo bajo - permitir sin restricciones
    /// </summary>
    Low = 1,

    /// <summary>
    /// Riesgo medio - monitorear de cerca
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Riesgo alto - requerir validaciones adicionales
    /// </summary>
    High = 3,

    /// <summary>
    /// Riesgo crítico - bloquear automáticamente
    /// </summary>
    Critical = 4
} 