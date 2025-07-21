using DDoSMonitor.Models;

namespace DDoSMonitor.Services;

/// <summary>
/// Servicio para generación de reportes
/// </summary>
public interface IReportingService
{
    /// <summary>
    /// Genera un reporte de actividad DDoS
    /// </summary>
    Task<string> GenerateReportAsync(ReportOptions options, List<DDoSMetrics> metrics);
    
    /// <summary>
    /// Genera un reporte específico de DDoS
    /// </summary>
    Task<string> GenerateDDoSReportAsync(ReportOptions options);
    
    /// <summary>
    /// Exporta métricas a formato específico
    /// </summary>
    Task ExportMetricsAsync(List<DDoSMetrics> metrics, string format, string filePath);
    
    /// <summary>
    /// Genera un resumen de ataques
    /// </summary>
    Task<AttackAnalysis> GenerateAttackSummaryAsync(List<DDoSMetrics> metrics);
    
    /// <summary>
    /// Crea un dashboard en HTML
    /// </summary>
    Task<string> CreateHtmlDashboardAsync(List<DDoSMetrics> metrics);
} 