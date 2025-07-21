using DDoSMonitor.Models;

namespace DDoSMonitor.Services;

/// <summary>
/// Servicio para gestión de alertas
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Evalúa si las métricas actuales requieren una alerta
    /// </summary>
    Task<bool> ShouldTriggerAlertAsync(DDoSMetrics metrics, int threshold);
    
    /// <summary>
    /// Envía una alerta
    /// </summary>
    Task SendAlertAsync(string message, string severity = "Warning");
    
    /// <summary>
    /// Configura alertas automáticas en Azure Monitor
    /// </summary>
    Task ConfigureAzureAlertsAsync(string subscriptionId, string resourceGroup, string publicIpName, int threshold);
    
    /// <summary>
    /// Lista las alertas activas
    /// </summary>
    Task<List<string>> GetActiveAlertsAsync(string subscriptionId, string resourceGroup);
} 