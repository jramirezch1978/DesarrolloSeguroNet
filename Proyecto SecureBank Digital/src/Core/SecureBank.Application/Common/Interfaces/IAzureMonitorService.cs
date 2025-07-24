using SecureBank.Domain.Enums;

namespace SecureBank.Application.Common.Interfaces;

/// <summary>
/// Servicio para integración con Azure Monitor y Application Insights
/// Maneja logging estructurado para Machine Learning y análisis de fraude
/// </summary>
public interface IAzureMonitorService
{
    /// <summary>
    /// Registra eventos de seguridad para análisis ML
    /// </summary>
    Task LogSecurityEventAsync(SecurityEvent securityEvent);

    /// <summary>
    /// Registra transacciones para análisis de fraude
    /// </summary>
    Task LogTransactionEventAsync(TransactionEvent transactionEvent);

    /// <summary>
    /// Registra comportamiento de usuario para análisis ML
    /// </summary>
    Task LogUserBehaviorAsync(UserBehaviorEvent behaviorEvent);

    /// <summary>
    /// Registra eventos de fraude detectados
    /// </summary>
    Task LogFraudEventAsync(FraudEvent fraudEvent);

    /// <summary>
    /// Registra métricas personalizadas para Machine Learning
    /// </summary>
    Task LogCustomMetricAsync(string metricName, double value, Dictionary<string, string>? properties = null);

    /// <summary>
    /// Registra excepciones con contexto adicional
    /// </summary>
    Task LogExceptionAsync(Exception exception, Dictionary<string, string>? properties = null);
}

/// <summary>
/// Evento de seguridad para Azure Monitor
/// </summary>
public class SecurityEvent
{
    public string EventType { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceFingerprint { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Evento de transacción para Azure Monitor
/// </summary>
public class TransactionEvent
{
    public Guid TransactionId { get; set; }
    public Guid UserId { get; set; }
    public TransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public int FraudScore { get; set; }
    public string? IpAddress { get; set; }
    public string? DeviceFingerprint { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Evento de comportamiento de usuario para Azure Monitor
/// </summary>
public class UserBehaviorEvent
{
    public Guid UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceFingerprint { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public bool IsTrustedDevice { get; set; }
    public TimeSpan SessionDuration { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Evento de fraude para Azure Monitor
/// </summary>
public class FraudEvent
{
    public Guid TransactionId { get; set; }
    public Guid UserId { get; set; }
    public string FraudType { get; set; } = string.Empty;
    public int FraudScore { get; set; }
    public List<string> FraudIndicators { get; set; } = new();
    public string? IpAddress { get; set; }
    public string? DeviceFingerprint { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
} 