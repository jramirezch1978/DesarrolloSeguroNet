using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System.Diagnostics;
using System.Text.Json;

namespace SecureBank.AuthAPI.Services;

/// <summary>
/// Servicio de Azure Monitor para SecureBank Digital
/// Envía logs estructurados y telemetría para análisis con Machine Learning
/// </summary>
public interface IAzureMonitorService
{
    Task LogSecurityEventAsync(string eventName, object eventData, string? userId = null);
    Task LogTransactionEventAsync(string eventName, object eventData, string userId);
    Task LogUserBehaviorAsync(string action, object behaviorData, string userId);
    Task LogFraudDetectionEventAsync(string eventType, object fraudData, string? userId = null);
    Task LogPerformanceMetricAsync(string metricName, double value, Dictionary<string, string>? properties = null);
    Task LogBusinessMetricAsync(string metricName, double value, Dictionary<string, string>? properties = null);
    void TrackException(Exception exception, Dictionary<string, string>? properties = null);
    void TrackPageView(string pageName, Dictionary<string, string>? properties = null);
    void TrackDependency(string dependencyType, string dependencyName, TimeSpan duration, bool success);
}

public class AzureMonitorService : IAzureMonitorService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<AzureMonitorService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _applicationVersion;
    private readonly string _environment;

    public AzureMonitorService(
        TelemetryClient telemetryClient,
        ILogger<AzureMonitorService> logger,
        IConfiguration configuration)
    {
        _telemetryClient = telemetryClient;
        _logger = logger;
        _configuration = configuration;
        _applicationVersion = GetApplicationVersion();
        _environment = configuration["Environment"] ?? "Unknown";
    }

    /// <summary>
    /// Registra eventos de seguridad para análisis de fraude con ML
    /// </summary>
    public async Task LogSecurityEventAsync(string eventName, object eventData, string? userId = null)
    {
        try
        {
            var telemetry = new EventTelemetry("SecurityEvent");
            
            // Propiedades principales para ML
            telemetry.Properties["EventName"] = eventName;
            telemetry.Properties["UserId"] = userId ?? "anonymous";
            telemetry.Properties["Timestamp"] = DateTime.UtcNow.ToString("O");
            telemetry.Properties["Environment"] = _environment;
            telemetry.Properties["ApplicationVersion"] = _applicationVersion;
            telemetry.Properties["EventType"] = "Security";
            
            // Serializar datos del evento
            var eventJson = JsonSerializer.Serialize(eventData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            telemetry.Properties["EventData"] = eventJson;
            
            // Extraer propiedades específicas para facilitar consultas ML
            if (eventData is not null)
            {
                var eventDict = JsonSerializer.Deserialize<Dictionary<string, object>>(eventJson);
                ExtractMlRelevantProperties(telemetry, eventDict);
            }
            
            // Métricas específicas por tipo de evento
            switch (eventName.ToLowerInvariant())
            {
                case "loginfailed":
                    telemetry.Metrics["LoginFailureCount"] = 1;
                    break;
                case "ratelimitexceeded":
                    telemetry.Metrics["RateLimitViolationCount"] = 1;
                    break;
                case "suspiciousactivity":
                    telemetry.Metrics["SuspiciousActivityCount"] = 1;
                    break;
            }
            
            _telemetryClient.TrackEvent(telemetry);
            await _telemetryClient.FlushAsync(CancellationToken.None);
            
            _logger.LogInformation("Security event logged to Azure Monitor: {EventName}", eventName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging security event {EventName} to Azure Monitor", eventName);
        }
    }

    /// <summary>
    /// Registra eventos de transacciones para análisis de patrones con ML
    /// </summary>
    public async Task LogTransactionEventAsync(string eventName, object eventData, string userId)
    {
        try
        {
            var telemetry = new EventTelemetry("TransactionEvent");
            
            telemetry.Properties["EventName"] = eventName;
            telemetry.Properties["UserId"] = userId;
            telemetry.Properties["Timestamp"] = DateTime.UtcNow.ToString("O");
            telemetry.Properties["Environment"] = _environment;
            telemetry.Properties["EventType"] = "Transaction";
            
            var eventJson = JsonSerializer.Serialize(eventData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            telemetry.Properties["EventData"] = eventJson;
            
            // Extraer propiedades para ML (montos, tipos, frecuencia, etc.)
            if (eventData is not null)
            {
                var eventDict = JsonSerializer.Deserialize<Dictionary<string, object>>(eventJson);
                ExtractTransactionMlProperties(telemetry, eventDict);
            }
            
            _telemetryClient.TrackEvent(telemetry);
            await _telemetryClient.FlushAsync(CancellationToken.None);
            
            _logger.LogInformation("Transaction event logged: {EventName} for user {UserId}", eventName, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging transaction event {EventName}", eventName);
        }
    }

    /// <summary>
    /// Registra comportamiento del usuario para análisis de UX y patrones con ML
    /// </summary>
    public async Task LogUserBehaviorAsync(string action, object behaviorData, string userId)
    {
        try
        {
            var telemetry = new EventTelemetry("UserBehavior");
            
            telemetry.Properties["Action"] = action;
            telemetry.Properties["UserId"] = userId;
            telemetry.Properties["Timestamp"] = DateTime.UtcNow.ToString("O");
            telemetry.Properties["Environment"] = _environment;
            telemetry.Properties["EventType"] = "Behavior";
            
            var behaviorJson = JsonSerializer.Serialize(behaviorData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            telemetry.Properties["BehaviorData"] = behaviorJson;
            
            // Extraer propiedades relevantes para análisis de comportamiento
            if (behaviorData is not null)
            {
                var behaviorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(behaviorJson);
                ExtractBehaviorMlProperties(telemetry, behaviorDict);
            }
            
            _telemetryClient.TrackEvent(telemetry);
            await _telemetryClient.FlushAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging user behavior {Action}", action);
        }
    }

    /// <summary>
    /// Registra eventos específicos de detección de fraude para entrenamiento ML
    /// </summary>
    public async Task LogFraudDetectionEventAsync(string eventType, object fraudData, string? userId = null)
    {
        try
        {
            var telemetry = new EventTelemetry("FraudDetection");
            
            telemetry.Properties["EventType"] = eventType;
            telemetry.Properties["UserId"] = userId ?? "anonymous";
            telemetry.Properties["Timestamp"] = DateTime.UtcNow.ToString("O");
            telemetry.Properties["Environment"] = _environment;
            telemetry.Properties["Category"] = "Fraud";
            
            var fraudJson = JsonSerializer.Serialize(fraudData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            telemetry.Properties["FraudData"] = fraudJson;
            
            // Propiedades específicas para algoritmos de detección de fraude
            if (fraudData is not null)
            {
                var fraudDict = JsonSerializer.Deserialize<Dictionary<string, object>>(fraudJson);
                ExtractFraudMlProperties(telemetry, fraudDict);
            }
            
            // Métricas de fraude
            telemetry.Metrics["FraudEventCount"] = 1;
            
            _telemetryClient.TrackEvent(telemetry);
            await _telemetryClient.FlushAsync(CancellationToken.None);
            
            _logger.LogWarning("Fraud detection event logged: {EventType}", eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging fraud detection event {EventType}", eventType);
        }
    }

    /// <summary>
    /// Registra métricas de rendimiento para monitoreo y análisis
    /// </summary>
    public async Task LogPerformanceMetricAsync(string metricName, double value, Dictionary<string, string>? properties = null)
    {
        try
        {
            var telemetry = new MetricTelemetry(metricName, value);
            
            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    telemetry.Properties[prop.Key] = prop.Value;
                }
            }
            
            telemetry.Properties["Environment"] = _environment;
            telemetry.Properties["Timestamp"] = DateTime.UtcNow.ToString("O");
            telemetry.Properties["MetricType"] = "Performance";
            
            _telemetryClient.TrackMetric(telemetry);
            await _telemetryClient.FlushAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging performance metric {MetricName}", metricName);
        }
    }

    /// <summary>
    /// Registra métricas de negocio para análisis y BI
    /// </summary>
    public async Task LogBusinessMetricAsync(string metricName, double value, Dictionary<string, string>? properties = null)
    {
        try
        {
            var telemetry = new MetricTelemetry(metricName, value);
            
            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    telemetry.Properties[prop.Key] = prop.Value;
                }
            }
            
            telemetry.Properties["Environment"] = _environment;
            telemetry.Properties["Timestamp"] = DateTime.UtcNow.ToString("O");
            telemetry.Properties["MetricType"] = "Business";
            
            _telemetryClient.TrackMetric(telemetry);
            await _telemetryClient.FlushAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging business metric {MetricName}", metricName);
        }
    }

    /// <summary>
    /// Rastrea excepciones para análisis de errores
    /// </summary>
    public void TrackException(Exception exception, Dictionary<string, string>? properties = null)
    {
        try
        {
            var telemetry = new ExceptionTelemetry(exception);
            
            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    telemetry.Properties[prop.Key] = prop.Value;
                }
            }
            
            telemetry.Properties["Environment"] = _environment;
            telemetry.Properties["Timestamp"] = DateTime.UtcNow.ToString("O");
            
            _telemetryClient.TrackException(telemetry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking exception to Azure Monitor");
        }
    }

    /// <summary>
    /// Rastrea vistas de página para análisis de UX
    /// </summary>
    public void TrackPageView(string pageName, Dictionary<string, string>? properties = null)
    {
        try
        {
            var telemetry = new PageViewTelemetry(pageName);
            
            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    telemetry.Properties[prop.Key] = prop.Value;
                }
            }
            
            telemetry.Properties["Environment"] = _environment;
            telemetry.Properties["Timestamp"] = DateTime.UtcNow.ToString("O");
            
            _telemetryClient.TrackPageView(telemetry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking page view {PageName}", pageName);
        }
    }

    /// <summary>
    /// Rastrea dependencias externas para monitoreo de performance
    /// </summary>
    public void TrackDependency(string dependencyType, string dependencyName, TimeSpan duration, bool success)
    {
        try
        {
            var telemetry = new DependencyTelemetry(dependencyType, dependencyName, dependencyName, null)
            {
                Duration = duration,
                Success = success,
                Timestamp = DateTime.UtcNow
            };
            
            telemetry.Properties["Environment"] = _environment;
            
            _telemetryClient.TrackDependency(telemetry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking dependency {DependencyName}", dependencyName);
        }
    }

    // Métodos privados para extracción de propiedades ML
    private void ExtractMlRelevantProperties(EventTelemetry telemetry, Dictionary<string, object>? eventDict)
    {
        if (eventDict == null) return;

        // Extraer propiedades comunes para ML
        ExtractIfExists(telemetry, eventDict, "ipAddress", "ML_IpAddress");
        ExtractIfExists(telemetry, eventDict, "userAgent", "ML_UserAgent");
        ExtractIfExists(telemetry, eventDict, "deviceFingerprint", "ML_DeviceFingerprint");
        ExtractIfExists(telemetry, eventDict, "location", "ML_Location");
        ExtractIfExists(telemetry, eventDict, "country", "ML_Country");
        ExtractIfExists(telemetry, eventDict, "city", "ML_City");
        ExtractIfExists(telemetry, eventDict, "riskScore", "ML_RiskScore");
        ExtractIfExists(telemetry, eventDict, "sessionDuration", "ML_SessionDuration");
        ExtractIfExists(telemetry, eventDict, "timeOfDay", "ML_TimeOfDay");
        ExtractIfExists(telemetry, eventDict, "dayOfWeek", "ML_DayOfWeek");
    }

    private void ExtractTransactionMlProperties(EventTelemetry telemetry, Dictionary<string, object>? eventDict)
    {
        if (eventDict == null) return;

        ExtractIfExists(telemetry, eventDict, "amount", "ML_TransactionAmount");
        ExtractIfExists(telemetry, eventDict, "transactionType", "ML_TransactionType");
        ExtractIfExists(telemetry, eventDict, "accountType", "ML_AccountType");
        ExtractIfExists(telemetry, eventDict, "frequency", "ML_TransactionFrequency");
        ExtractIfExists(telemetry, eventDict, "balanceAfter", "ML_BalanceAfter");
        ExtractIfExists(telemetry, eventDict, "merchantCategory", "ML_MerchantCategory");
        ExtractIfExists(telemetry, eventDict, "isRecurring", "ML_IsRecurring");
        ExtractIfExists(telemetry, eventDict, "riskLevel", "ML_TransactionRiskLevel");
    }

    private void ExtractBehaviorMlProperties(EventTelemetry telemetry, Dictionary<string, object>? behaviorDict)
    {
        if (behaviorDict == null) return;

        ExtractIfExists(telemetry, behaviorDict, "timeSpent", "ML_TimeSpent");
        ExtractIfExists(telemetry, behaviorDict, "clickCount", "ML_ClickCount");
        ExtractIfExists(telemetry, behaviorDict, "scrollDepth", "ML_ScrollDepth");
        ExtractIfExists(telemetry, behaviorDict, "pageViews", "ML_PageViews");
        ExtractIfExists(telemetry, behaviorDict, "feature", "ML_FeatureUsed");
        ExtractIfExists(telemetry, behaviorDict, "navigationPattern", "ML_NavigationPattern");
        ExtractIfExists(telemetry, behaviorDict, "sessionId", "ML_SessionId");
    }

    private void ExtractFraudMlProperties(EventTelemetry telemetry, Dictionary<string, object>? fraudDict)
    {
        if (fraudDict == null) return;

        ExtractIfExists(telemetry, fraudDict, "confidence", "ML_FraudConfidence");
        ExtractIfExists(telemetry, fraudDict, "ruleTriggered", "ML_RuleTriggered");
        ExtractIfExists(telemetry, fraudDict, "anomalyScore", "ML_AnomalyScore");
        ExtractIfExists(telemetry, fraudDict, "velocity", "ML_TransactionVelocity");
        ExtractIfExists(telemetry, fraudDict, "deviation", "ML_BehaviorDeviation");
        ExtractIfExists(telemetry, fraudDict, "historicalPattern", "ML_HistoricalPattern");
        ExtractIfExists(telemetry, fraudDict, "isSuspicious", "ML_IsSuspicious");
    }

    private void ExtractIfExists(EventTelemetry telemetry, Dictionary<string, object> dict, string sourceKey, string targetKey)
    {
        if (dict.TryGetValue(sourceKey, out var value) && value != null)
        {
            telemetry.Properties[targetKey] = value.ToString();
        }
    }

    private string GetApplicationVersion()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "1.0.0.0";
        }
        catch
        {
            return "1.0.0.0";
        }
    }
} 