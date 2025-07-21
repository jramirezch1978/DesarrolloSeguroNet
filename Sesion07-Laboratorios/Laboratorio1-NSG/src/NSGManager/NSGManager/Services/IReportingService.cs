using NSGManager.Models;

namespace NSGManager.Services;

/// <summary>
/// Interfaz para el servicio de generación de reportes
/// </summary>
public interface IReportingService
{
    /// <summary>
    /// Genera un reporte completo de seguridad
    /// </summary>
    Task GenerateSecurityReportAsync(ReportOptions options);
    
    /// <summary>
    /// Genera reporte de compliance
    /// </summary>
    Task<string> GenerateComplianceReportAsync(string resourceGroup, SecurityStandard[] standards, string? subscriptionId = null);
    
    /// <summary>
    /// Genera reporte de Flow Logs
    /// </summary>
    Task<string> GenerateFlowLogReportAsync(string resourceGroup, DateTime startTime, DateTime endTime, string? subscriptionId = null);
    
    /// <summary>
    /// Exporta configuración de NSGs a archivo
    /// </summary>
    Task ExportNSGConfigurationAsync(string resourceGroup, string outputPath, string format = "json", string? subscriptionId = null);
} 