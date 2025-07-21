using DDoSMonitor.Models;

namespace DDoSMonitor.Services;

/// <summary>
/// Servicio principal de monitoreo DDoS
/// </summary>
public interface IDDoSMonitoringService
{
    /// <summary>
    /// Inicia el monitoreo en tiempo real
    /// </summary>
    Task StartMonitoringAsync(MonitoringOptions options, CancellationToken cancellationToken);
    
    /// <summary>
    /// Inicia el monitoreo en tiempo real (alias)
    /// </summary>
    Task StartRealTimeMonitoringAsync(MonitoringOptions options);
    
    /// <summary>
    /// Obtiene las métricas actuales de DDoS
    /// </summary>
    Task<DDoSMetrics> GetCurrentMetricsAsync(string subscriptionId, string resourceGroup, string publicIpName);
    
    /// <summary>
    /// Obtiene métricas históricas
    /// </summary>
    Task<List<DDoSMetrics>> GetHistoricalMetricsAsync(string subscriptionId, string resourceGroup, string publicIpName, DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// Analiza si está bajo ataque
    /// </summary>
    Task<AttackAnalysis> AnalyzeAttackStatusAsync(string subscriptionId, string resourceGroup, string publicIpName);
    
    /// <summary>
    /// Ejecuta test de carga con monitoreo
    /// </summary>
    Task RunLoadTestWithMonitoringAsync(LoadTestOptions options);
} 