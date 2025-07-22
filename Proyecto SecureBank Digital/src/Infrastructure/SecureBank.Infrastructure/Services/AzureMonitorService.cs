using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureBank.Application.Common.Interfaces;
using System.Text.Json;

namespace SecureBank.Infrastructure.Services;

/// <summary>
/// Implementación del servicio Azure Monitor para SecureBank Digital
/// Integra con Application Insights para logging estructurado y ML
/// </summary>
public class AzureMonitorService : IAzureMonitorService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<AzureMonitorService> _logger;
    private readonly IConfiguration _configuration;

    public AzureMonitorService(
        TelemetryClient telemetryClient,
        ILogger<AzureMonitorService> logger,
        IConfiguration configuration)
    {
        _telemetryClient = telemetryClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task LogSecurityEventAsync(SecurityEvent securityEvent)
    {
        try
        {
            var eventTelemetry = new EventTelemetry("SecurityEvent");
            
            // Propiedades básicas
            eventTelemetry.Properties["EventType"] = securityEvent.EventType;
            eventTelemetry.Properties["UserId"] = securityEvent.UserId?.ToString() ?? "";
            eventTelemetry.Properties["IpAddress"] = securityEvent.IpAddress ?? "";
            eventTelemetry.Properties["UserAgent"] = securityEvent.UserAgent ?? "";
            eventTelemetry.Properties["DeviceFingerprint"] = securityEvent.DeviceFingerprint ?? "";
            eventTelemetry.Properties["RiskLevel"] = securityEvent.RiskLevel.ToString();
            eventTelemetry.Properties["Timestamp"] = securityEvent.Timestamp.ToString("O");

            // Propiedades adicionales
            foreach (var prop in securityEvent.Properties)
            {
                eventTelemetry.Properties[$"Custom_{prop.Key}"] = prop.Value?.ToString() ?? "";
            }

            _telemetryClient.TrackEvent(eventTelemetry);
            await _telemetryClient.FlushAsync(CancellationToken.None);

            _logger.LogInformation("Security event logged to Azure Monitor: {EventType}", securityEvent.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging security event to Azure Monitor");
        }
    }

    public async Task LogTransactionEventAsync(TransactionEvent transactionEvent)
    {
        try
        {
            var eventTelemetry = new EventTelemetry("TransactionEvent");
            
            // Propiedades básicas
            eventTelemetry.Properties["TransactionId"] = transactionEvent.TransactionId.ToString();
            eventTelemetry.Properties["UserId"] = transactionEvent.UserId.ToString();
            eventTelemetry.Properties["TransactionType"] = transactionEvent.TransactionType.ToString();
            eventTelemetry.Properties["Currency"] = transactionEvent.Currency;
            eventTelemetry.Properties["Status"] = transactionEvent.Status.ToString();
            eventTelemetry.Properties["RiskLevel"] = transactionEvent.RiskLevel.ToString();
            eventTelemetry.Properties["IpAddress"] = transactionEvent.IpAddress ?? "";
            eventTelemetry.Properties["DeviceFingerprint"] = transactionEvent.DeviceFingerprint ?? "";
            eventTelemetry.Properties["Timestamp"] = transactionEvent.Timestamp.ToString("O");

            // Métricas
            eventTelemetry.Metrics["Amount"] = (double)transactionEvent.Amount;
            eventTelemetry.Metrics["FraudScore"] = transactionEvent.FraudScore;

            // Propiedades adicionales
            foreach (var prop in transactionEvent.Properties)
            {
                eventTelemetry.Properties[$"Custom_{prop.Key}"] = prop.Value?.ToString() ?? "";
            }

            _telemetryClient.TrackEvent(eventTelemetry);
            await _telemetryClient.FlushAsync(CancellationToken.None);

            _logger.LogInformation("Transaction event logged to Azure Monitor: {TransactionId}", transactionEvent.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging transaction event to Azure Monitor");
        }
    }

    public async Task LogUserBehaviorAsync(UserBehaviorEvent behaviorEvent)
    {
        try
        {
            var eventTelemetry = new EventTelemetry("UserBehaviorEvent");
            
            // Propiedades básicas
            eventTelemetry.Properties["UserId"] = behaviorEvent.UserId.ToString();
            eventTelemetry.Properties["Action"] = behaviorEvent.Action;
            eventTelemetry.Properties["IpAddress"] = behaviorEvent.IpAddress ?? "";
            eventTelemetry.Properties["UserAgent"] = behaviorEvent.UserAgent ?? "";
            eventTelemetry.Properties["DeviceFingerprint"] = behaviorEvent.DeviceFingerprint ?? "";
            eventTelemetry.Properties["Country"] = behaviorEvent.Country ?? "";
            eventTelemetry.Properties["City"] = behaviorEvent.City ?? "";
            eventTelemetry.Properties["IsTrustedDevice"] = behaviorEvent.IsTrustedDevice.ToString();
            eventTelemetry.Properties["Timestamp"] = behaviorEvent.Timestamp.ToString("O");

            // Métricas
            eventTelemetry.Metrics["SessionDurationMinutes"] = behaviorEvent.SessionDuration.TotalMinutes;

            // Propiedades adicionales
            foreach (var prop in behaviorEvent.Properties)
            {
                eventTelemetry.Properties[$"Custom_{prop.Key}"] = prop.Value?.ToString() ?? "";
            }

            _telemetryClient.TrackEvent(eventTelemetry);
            await _telemetryClient.FlushAsync(CancellationToken.None);

            _logger.LogInformation("User behavior event logged to Azure Monitor: {Action}", behaviorEvent.Action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging user behavior event to Azure Monitor");
        }
    }

    public async Task LogFraudEventAsync(FraudEvent fraudEvent)
    {
        try
        {
            var eventTelemetry = new EventTelemetry("FraudEvent");
            
            // Propiedades básicas
            eventTelemetry.Properties["TransactionId"] = fraudEvent.TransactionId.ToString();
            eventTelemetry.Properties["UserId"] = fraudEvent.UserId.ToString();
            eventTelemetry.Properties["FraudType"] = fraudEvent.FraudType;
            eventTelemetry.Properties["IpAddress"] = fraudEvent.IpAddress ?? "";
            eventTelemetry.Properties["DeviceFingerprint"] = fraudEvent.DeviceFingerprint ?? "";
            eventTelemetry.Properties["Currency"] = fraudEvent.Currency;
            eventTelemetry.Properties["FraudIndicators"] = JsonSerializer.Serialize(fraudEvent.FraudIndicators);
            eventTelemetry.Properties["Timestamp"] = fraudEvent.Timestamp.ToString("O");

            // Métricas
            eventTelemetry.Metrics["FraudScore"] = fraudEvent.FraudScore;
            eventTelemetry.Metrics["Amount"] = (double)fraudEvent.Amount;

            // Propiedades adicionales
            foreach (var prop in fraudEvent.Properties)
            {
                eventTelemetry.Properties[$"Custom_{prop.Key}"] = prop.Value?.ToString() ?? "";
            }

            _telemetryClient.TrackEvent(eventTelemetry);
            await _telemetryClient.FlushAsync(CancellationToken.None);

            _logger.LogWarning("Fraud event logged to Azure Monitor: {FraudType} - Score: {FraudScore}", 
                fraudEvent.FraudType, fraudEvent.FraudScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging fraud event to Azure Monitor");
        }
    }

    public async Task LogCustomMetricAsync(string metricName, double value, Dictionary<string, string>? properties = null)
    {
        try
        {
            var metricTelemetry = new MetricTelemetry(metricName, value);
            
            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    metricTelemetry.Properties[prop.Key] = prop.Value;
                }
            }

            _telemetryClient.TrackMetric(metricTelemetry);
            await _telemetryClient.FlushAsync(CancellationToken.None);

            _logger.LogInformation("Custom metric logged to Azure Monitor: {MetricName} = {Value}", metricName, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging custom metric to Azure Monitor");
        }
    }

    public async Task LogExceptionAsync(Exception exception, Dictionary<string, string>? properties = null)
    {
        try
        {
            var exceptionTelemetry = new ExceptionTelemetry(exception);
            
            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    exceptionTelemetry.Properties[prop.Key] = prop.Value;
                }
            }

            _telemetryClient.TrackException(exceptionTelemetry);
            await _telemetryClient.FlushAsync(CancellationToken.None);

            _logger.LogError(exception, "Exception logged to Azure Monitor");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging exception to Azure Monitor");
        }
    }
} 