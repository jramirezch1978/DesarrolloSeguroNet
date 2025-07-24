using Microsoft.Extensions.Logging;
using NetworkTester.Models;

namespace NetworkTester.Services;

public interface IReportGenerator
{
    Task GenerateReportAsync(string reportType, string? resourceGroup = null, string format = "html", string? outputFile = null);
    Task GenerateConnectivityReportAsync(List<ConnectivityTestResult> results, string outputFile);
    Task GenerateSecurityReportAsync(SecurityAnalysisReport report, string outputFile, string format = "html");
    Task GeneratePerformanceReportAsync(List<PerformanceTestResult> results, string outputFile, string format = "html");
    Task GenerateTopologyReportAsync(NetworkTopologyReport report, string outputFile, string format = "html");
    Task GenerateFlowLogReportAsync(FlowLogAnalysisReport report, string outputFile, string format = "html");
}

public class ReportGenerator : IReportGenerator
{
    private readonly ILogger<ReportGenerator> _logger;

    public ReportGenerator(ILogger<ReportGenerator> logger)
    {
        _logger = logger;
    }

    public async Task GenerateReportAsync(string reportType, string? resourceGroup = null, string format = "html", string? outputFile = null)
    {
        try
        {
            _logger.LogInformation($"Generando reporte de tipo: {reportType}");
            
            outputFile ??= $"report-{reportType}-{DateTime.UtcNow:yyyyMMddHHmmss}.{format}";
            
            switch (reportType.ToLowerInvariant())
            {
                case "connectivity":
                    await GenerateConnectivityReportPlaceholder(outputFile, format);
                    break;
                case "security":
                    await GenerateSecurityReportPlaceholder(outputFile, format);
                    break;
                case "performance":
                    await GeneratePerformanceReportPlaceholder(outputFile, format);
                    break;
                case "topology":
                    await GenerateTopologyReportPlaceholder(outputFile, format);
                    break;
                default:
                    throw new ArgumentException($"Tipo de reporte no soportado: {reportType}");
            }
            
            _logger.LogInformation($"Reporte generado: {outputFile}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generando reporte {reportType}");
            throw;
        }
    }

    public async Task GenerateConnectivityReportAsync(List<ConnectivityTestResult> results, string outputFile)
    {
        try
        {
            var html = GenerateConnectivityHtml(results);
            await File.WriteAllTextAsync(outputFile, html);
            _logger.LogInformation($"Reporte de conectividad guardado en: {outputFile}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generando reporte de conectividad");
            throw;
        }
    }

    public async Task GenerateSecurityReportAsync(SecurityAnalysisReport report, string outputFile, string format = "html")
    {
        try
        {
            string content = format.ToLowerInvariant() switch
            {
                "html" => GenerateSecurityHtml(report),
                "json" => System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                _ => throw new ArgumentException($"Formato no soportado: {format}")
            };
            
            await File.WriteAllTextAsync(outputFile, content);
            _logger.LogInformation($"Reporte de seguridad guardado en: {outputFile}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generando reporte de seguridad");
            throw;
        }
    }

    public async Task GeneratePerformanceReportAsync(List<PerformanceTestResult> results, string outputFile, string format = "html")
    {
        try
        {
            string content = format.ToLowerInvariant() switch
            {
                "html" => GeneratePerformanceHtml(results),
                "json" => System.Text.Json.JsonSerializer.Serialize(results, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                _ => throw new ArgumentException($"Formato no soportado: {format}")
            };
            
            await File.WriteAllTextAsync(outputFile, content);
            _logger.LogInformation($"Reporte de performance guardado en: {outputFile}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generando reporte de performance");
            throw;
        }
    }

    public async Task GenerateTopologyReportAsync(NetworkTopologyReport report, string outputFile, string format = "html")
    {
        try
        {
            string content = format.ToLowerInvariant() switch
            {
                "html" => GenerateTopologyHtml(report),
                "json" => System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                _ => throw new ArgumentException($"Formato no soportado: {format}")
            };
            
            await File.WriteAllTextAsync(outputFile, content);
            _logger.LogInformation($"Reporte de topología guardado en: {outputFile}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generando reporte de topología");
            throw;
        }
    }

    public async Task GenerateFlowLogReportAsync(FlowLogAnalysisReport report, string outputFile, string format = "html")
    {
        try
        {
            string content = format.ToLowerInvariant() switch
            {
                "html" => GenerateFlowLogHtml(report),
                "json" => System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                _ => throw new ArgumentException($"Formato no soportado: {format}")
            };
            
            await File.WriteAllTextAsync(outputFile, content);
            _logger.LogInformation($"Reporte de flow logs guardado en: {outputFile}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generando reporte de flow logs");
            throw;
        }
    }

    private string GenerateConnectivityHtml(List<ConnectivityTestResult> results)
    {
        var successCount = results.Count(r => r.Status == ConnectivityStatus.Reachable);
        var failureCount = results.Count(r => r.Status == ConnectivityStatus.Unreachable);
        var errorCount = results.Count(r => r.Status == ConnectivityStatus.Error);
        
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Reporte de Conectividad - NetworkTester</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background-color: #2c3e50; color: white; padding: 20px; border-radius: 5px; }}
        .summary {{ background-color: #ecf0f1; padding: 15px; margin: 20px 0; border-radius: 5px; }}
        .success {{ color: #27ae60; }}
        .failure {{ color: #e74c3c; }}
        .error {{ color: #f39c12; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
        th, td {{ padding: 10px; text-align: left; border-bottom: 1px solid #ddd; }}
        th {{ background-color: #34495e; color: white; }}
        .status-reachable {{ background-color: #d5f4e6; }}
        .status-unreachable {{ background-color: #fadbd8; }}
        .status-error {{ background-color: #fdeaa7; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>🔍 Reporte de Conectividad</h1>
        <p>Generado el {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
    </div>
    
    <div class='summary'>
        <h2>📊 Resumen</h2>
        <p><strong>Total de tests:</strong> {results.Count}</p>
        <p class='success'><strong>✅ Exitosos:</strong> {successCount} ({(successCount * 100.0 / results.Count):F1}%)</p>
        <p class='failure'><strong>❌ Fallidos:</strong> {failureCount} ({(failureCount * 100.0 / results.Count):F1}%)</p>
        <p class='error'><strong>⚠️ Errores:</strong> {errorCount} ({(errorCount * 100.0 / results.Count):F1}%)</p>
    </div>
    
    <h2>📋 Resultados Detallados</h2>
    <table>
        <thead>
            <tr>
                <th>Origen</th>
                <th>Destino</th>
                <th>Puerto</th>
                <th>Protocolo</th>
                <th>Estado</th>
                <th>Latencia</th>
                <th>Motivo</th>
            </tr>
        </thead>
        <tbody>";

        foreach (var result in results.OrderBy(r => r.Source).ThenBy(r => r.Target))
        {
            var statusClass = result.Status switch
            {
                ConnectivityStatus.Reachable => "status-reachable",
                ConnectivityStatus.Unreachable => "status-unreachable",
                _ => "status-error"
            };
            
            var statusIcon = result.Status switch
            {
                ConnectivityStatus.Reachable => "✅",
                ConnectivityStatus.Unreachable => "❌",
                _ => "⚠️"
            };

            html += $@"
            <tr class='{statusClass}'>
                <td>{result.Source}</td>
                <td>{result.Target}</td>
                <td>{result.Port}</td>
                <td>{result.Protocol}</td>
                <td>{statusIcon} {result.Status}</td>
                <td>{result.Latency.TotalMilliseconds:F2}ms</td>
                <td>{result.StatusReason}</td>
            </tr>";
        }

        html += @"
        </tbody>
    </table>
</body>
</html>";

        return html;
    }

    private string GenerateSecurityHtml(SecurityAnalysisReport report)
    {
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Reporte de Seguridad - NetworkTester</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background-color: #2c3e50; color: white; padding: 20px; border-radius: 5px; }}
        .score {{ background-color: #ecf0f1; padding: 15px; margin: 20px 0; border-radius: 5px; }}
        .violation {{ background-color: #fadbd8; padding: 10px; margin: 10px 0; border-radius: 5px; }}
        .recommendation {{ background-color: #d5f4e6; padding: 10px; margin: 10px 0; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>🔒 Reporte de Análisis de Seguridad</h1>
        <p>Resource Group: {report.ResourceGroup}</p>
        <p>Generado el {report.AnalysisDate:yyyy-MM-dd HH:mm:ss} UTC</p>
    </div>
    
    <div class='score'>
        <h2>📊 Puntuación de Seguridad</h2>
        <p><strong>Puntuación General:</strong> {report.PostureScore.OverallScore}/100 ({report.PostureScore.OverallRating})</p>
        <p><strong>Segmentación de Red:</strong> {report.PostureScore.NetworkSegmentationScore}/100</p>
        <p><strong>Control de Acceso:</strong> {report.PostureScore.AccessControlScore}/100</p>
    </div>
    
    <h2>⚠️ Violaciones de Seguridad</h2>";

        foreach (var violation in report.Violations)
        {
            html += $@"
            <div class='violation'>
                <h3>{violation.ViolationType} - {violation.Severity}</h3>
                <p><strong>Recurso:</strong> {violation.ResourceName}</p>
                <p><strong>Descripción:</strong> {violation.Description}</p>
                <p><strong>Remediación:</strong> {violation.Remediation}</p>
            </div>";
        }

        html += "<h2>💡 Recomendaciones</h2>";

        foreach (var recommendation in report.Recommendations)
        {
            html += $@"
            <div class='recommendation'>
                <h3>{recommendation.Title} - {recommendation.Priority}</h3>
                <p><strong>Categoría:</strong> {recommendation.Category}</p>
                <p><strong>Descripción:</strong> {recommendation.Description}</p>
                <p><strong>Implementación:</strong> {recommendation.Implementation}</p>
            </div>";
        }

        html += "</body></html>";
        return html;
    }

    private string GeneratePerformanceHtml(List<PerformanceTestResult> results)
    {
        return "<html><body><h1>Reporte de Performance</h1><p>Implementación pendiente</p></body></html>";
    }

    private string GenerateTopologyHtml(NetworkTopologyReport report)
    {
        return "<html><body><h1>Reporte de Topología</h1><p>Implementación pendiente</p></body></html>";
    }

    private string GenerateFlowLogHtml(FlowLogAnalysisReport report)
    {
        return "<html><body><h1>Reporte de Flow Logs</h1><p>Implementación pendiente</p></body></html>";
    }

    private async Task GenerateConnectivityReportPlaceholder(string outputFile, string format)
    {
        var placeholder = "<html><body><h1>Reporte de Conectividad</h1><p>Para generar este reporte, ejecute tests de conectividad primero.</p></body></html>";
        await File.WriteAllTextAsync(outputFile, placeholder);
    }

    private async Task GenerateSecurityReportPlaceholder(string outputFile, string format)
    {
        var placeholder = "<html><body><h1>Reporte de Seguridad</h1><p>Para generar este reporte, ejecute análisis de seguridad primero.</p></body></html>";
        await File.WriteAllTextAsync(outputFile, placeholder);
    }

    private async Task GeneratePerformanceReportPlaceholder(string outputFile, string format)
    {
        var placeholder = "<html><body><h1>Reporte de Performance</h1><p>Para generar este reporte, ejecute tests de performance primero.</p></body></html>";
        await File.WriteAllTextAsync(outputFile, placeholder);
    }

    private async Task GenerateTopologyReportPlaceholder(string outputFile, string format)
    {
        var placeholder = "<html><body><h1>Reporte de Topología</h1><p>Para generar este reporte, ejecute análisis de topología primero.</p></body></html>";
        await File.WriteAllTextAsync(outputFile, placeholder);
    }
} 