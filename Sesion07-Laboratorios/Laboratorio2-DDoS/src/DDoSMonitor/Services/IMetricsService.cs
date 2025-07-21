using DDoSMonitor.Models;

namespace DDoSMonitor.Services;

/// <summary>
/// Servicio para obtener métricas de Azure Monitor
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Obtiene métricas específicas de DDoS Protection
    /// </summary>
    Task<DDoSMetrics> GetDDoSMetricsAsync(string subscriptionId, string resourceGroup, string publicIpName, DateTime? startTime = null, DateTime? endTime = null);
    
    /// <summary>
    /// Obtiene todas las métricas disponibles para un recurso
    /// </summary>
    Task<List<string>> GetAvailableMetricsAsync(string subscriptionId, string resourceGroup, string publicIpName);
    
    /// <summary>
    /// Verifica si las métricas de DDoS están habilitadas
    /// </summary>
    Task<bool> IsDDoSProtectionEnabledAsync(string subscriptionId, string resourceGroup, string publicIpName);
    
    /// <summary>
    /// Analiza métricas históricas
    /// </summary>
    Task<AttackAnalysis> AnalyzeHistoricalMetricsAsync(AnalysisOptions options);
} 