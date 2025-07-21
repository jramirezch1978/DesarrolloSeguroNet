using Microsoft.Extensions.Logging;
using NSGManager.Models;
using Newtonsoft.Json;
using System.Text;

namespace NSGManager.Services;

/// <summary>
/// Servicio para generación de reportes de seguridad
/// </summary>
public class ReportingService : IReportingService
{
    private readonly INSGService _nsgService;
    private readonly IValidationService _validationService;
    private readonly INetworkWatcherService _networkWatcherService;
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(
        INSGService nsgService, 
        IValidationService validationService,
        INetworkWatcherService networkWatcherService,
        ILogger<ReportingService> logger)
    {
        _nsgService = nsgService;
        _validationService = validationService;
        _networkWatcherService = networkWatcherService;
        _logger = logger;
    }

    public async Task GenerateSecurityReportAsync(ReportOptions options)
    {
        _logger.LogInformation($"📊 Generando reporte de seguridad en formato {options.Format}...");
        
        try
        {
            // Recopilar datos para el reporte
            var nsgConfigurations = await _nsgService.GetNSGConfigurationAsync(options.ResourceGroupName, options.SubscriptionId);
            
            var validationOptions = new ValidationOptions
            {
                ResourceGroupName = options.ResourceGroupName,
                SubscriptionId = options.SubscriptionId,
                DetailedAnalysis = true
            };
            
            var validationResults = await _validationService.ValidateNSGConfigurationAsync(validationOptions);
            
            List<string>? recommendations = null;
            if (options.IncludeRecommendations)
            {
                recommendations = await _validationService.GenerateSecurityRecommendationsAsync(
                    options.ResourceGroupName, options.SubscriptionId);
            }
            
            // Generar reporte según formato
            string reportContent = options.Format.ToLower() switch
            {
                "json" => GenerateJsonReport(nsgConfigurations, validationResults, recommendations),
                "html" => GenerateHtmlReport(nsgConfigurations, validationResults, recommendations),
                "csv" => GenerateCsvReport(nsgConfigurations, validationResults),
                "console" => GenerateConsoleReport(nsgConfigurations, validationResults, recommendations),
                _ => throw new ArgumentException($"Formato no soportado: {options.Format}")
            };
            
            // Guardar o mostrar reporte
            if (!string.IsNullOrEmpty(options.OutputFile))
            {
                await File.WriteAllTextAsync(options.OutputFile, reportContent, Encoding.UTF8);
                _logger.LogInformation($"📄 Reporte guardado en: {options.OutputFile}");
            }
            else if (options.Format.ToLower() == "console")
            {
                Console.WriteLine(reportContent);
            }
            
            _logger.LogInformation("✅ Reporte de seguridad generado exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Error generando reporte: {ex.Message}");
            throw;
        }
    }

    public async Task<string> GenerateComplianceReportAsync(string resourceGroup, SecurityStandard[] standards, string? subscriptionId = null)
    {
        _logger.LogInformation($"📋 Generando reporte de compliance para {standards.Length} estándares...");
        
        var complianceResults = await _validationService.ValidateComplianceAsync(resourceGroup, standards, subscriptionId);
        
        var report = new StringBuilder();
        report.AppendLine("# Reporte de Compliance de Seguridad");
        report.AppendLine($"**Fecha:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"**Resource Group:** {resourceGroup}");
        report.AppendLine();
        
        foreach (var result in complianceResults)
        {
            var status = result.Value.IsCompliant ? "✅ CUMPLE" : "❌ NO CUMPLE";
            report.AppendLine($"## {result.Key} - {status}");
            report.AppendLine($"**Score:** {result.Value.ComplianceScore:F1}%");
            report.AppendLine();
            
            if (result.Value.Violations.Any())
            {
                report.AppendLine("### Violaciones:");
                foreach (var violation in result.Value.Violations)
                {
                    report.AppendLine($"- {violation}");
                }
                report.AppendLine();
            }
            
            if (result.Value.Requirements.Any())
            {
                report.AppendLine("### Requerimientos:");
                foreach (var requirement in result.Value.Requirements)
                {
                    report.AppendLine($"- {requirement}");
                }
                report.AppendLine();
            }
        }
        
        return report.ToString();
    }

    public async Task<string> GenerateFlowLogReportAsync(string resourceGroup, DateTime startTime, DateTime endTime, string? subscriptionId = null)
    {
        _logger.LogInformation($"📈 Generando reporte de Flow Logs...");
        
        // Simular obtención de múltiples NSGs
        var report = new StringBuilder();
        report.AppendLine("# Reporte de Flow Logs");
        report.AppendLine($"**Período:** {startTime:yyyy-MM-dd} - {endTime:yyyy-MM-dd}");
        report.AppendLine($"**Resource Group:** {resourceGroup}");
        report.AppendLine();
        
        // En una implementación real, iteraríamos sobre NSGs reales
        var sampleNSGs = new[] { "nsg-web-advanced", "nsg-app-advanced", "nsg-data-advanced" };
        
        foreach (var nsgName in sampleNSGs)
        {
            var metrics = await _networkWatcherService.GetFlowLogMetricsAsync(resourceGroup, nsgName, startTime, endTime, subscriptionId);
            
            report.AppendLine($"## {nsgName}");
            report.AppendLine($"- **Flows Totales:** {metrics.TotalFlows:N0}");
            report.AppendLine($"- **Permitidos:** {metrics.AllowedFlows:N0} ({(double)metrics.AllowedFlows/metrics.TotalFlows*100:F1}%)");
            report.AppendLine($"- **Bloqueados:** {metrics.DeniedFlows:N0} ({(double)metrics.DeniedFlows/metrics.TotalFlows*100:F1}%)");
            report.AppendLine();
            
            if (metrics.SuspiciousActivities.Any())
            {
                report.AppendLine("### Actividades Sospechosas:");
                foreach (var activity in metrics.SuspiciousActivities)
                {
                    report.AppendLine($"- **{activity.Type}** desde {activity.SourceIP} a {activity.DestinationIP}:{activity.DestinationPort} - {activity.Description}");
                }
                report.AppendLine();
            }
        }
        
        return report.ToString();
    }

    public async Task ExportNSGConfigurationAsync(string resourceGroup, string outputPath, string format = "json", string? subscriptionId = null)
    {
        _logger.LogInformation($"📤 Exportando configuración NSG en formato {format}...");
        
        var nsgConfigurations = await _nsgService.GetNSGConfigurationAsync(resourceGroup, subscriptionId);
        
        string content = format.ToLower() switch
        {
            "json" => JsonConvert.SerializeObject(nsgConfigurations, Formatting.Indented),
            "xml" => SerializeToXml(nsgConfigurations),
            _ => throw new ArgumentException($"Formato de exportación no soportado: {format}")
        };
        
        await File.WriteAllTextAsync(outputPath, content, Encoding.UTF8);
        _logger.LogInformation($"✅ Configuración exportada a: {outputPath}");
    }

    // Métodos privados para generación de reportes

    private string GenerateJsonReport(List<NSGValidationDetail> nsgConfigurations, ValidationResults validationResults, List<string>? recommendations)
    {
        var report = new
        {
            GeneratedAt = DateTime.UtcNow,
            Summary = new
            {
                TotalNSGs = validationResults.AnalyzedNSGs,
                TotalRules = validationResults.TotalRules,
                ValidRules = validationResults.ValidRules,
                SecurityScore = CalculateOverallSecurityScore(validationResults),
                Warnings = validationResults.Warnings.Count,
                Errors = validationResults.Errors.Count
            },
            NSGDetails = nsgConfigurations,
            ValidationResults = validationResults,
            Recommendations = recommendations
        };
        
        return JsonConvert.SerializeObject(report, Formatting.Indented);
    }

    private string GenerateHtmlReport(List<NSGValidationDetail> nsgConfigurations, ValidationResults validationResults, List<string>? recommendations)
    {
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("<title>Reporte de Seguridad NSG</title>");
        html.AppendLine("<style>");
        html.AppendLine(GetHtmlStyles());
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        
        // Header
        html.AppendLine("<div class='header'>");
        html.AppendLine("<h1>🛡️ Reporte de Seguridad - Network Security Groups</h1>");
        html.AppendLine($"<p>Generado: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        html.AppendLine("</div>");
        
        // Resumen ejecutivo
        html.AppendLine("<div class='summary'>");
        html.AppendLine("<h2>📊 Resumen Ejecutivo</h2>");
        html.AppendLine("<table>");
        html.AppendLine($"<tr><td>NSGs Analizados</td><td>{validationResults.AnalyzedNSGs}</td></tr>");
        html.AppendLine($"<tr><td>Reglas Totales</td><td>{validationResults.TotalRules}</td></tr>");
        html.AppendLine($"<tr><td>Reglas Válidas</td><td>{validationResults.ValidRules}</td></tr>");
        html.AppendLine($"<tr><td>Advertencias</td><td class='warning'>{validationResults.Warnings.Count}</td></tr>");
        html.AppendLine($"<tr><td>Errores</td><td class='error'>{validationResults.Errors.Count}</td></tr>");
        html.AppendLine("</table>");
        html.AppendLine("</div>");
        
        // Detalles por NSG
        html.AppendLine("<div class='details'>");
        html.AppendLine("<h2>🔍 Detalles por NSG</h2>");
        foreach (var nsg in nsgConfigurations)
        {
            html.AppendLine($"<div class='nsg-detail'>");
            html.AppendLine($"<h3>{nsg.NSGName}</h3>");
            html.AppendLine($"<p>Reglas: {nsg.RuleCount} | Postura: {nsg.SecurityPosture}</p>");
            
            if (nsg.Recommendations.Any())
            {
                html.AppendLine("<h4>Recomendaciones:</h4>");
                html.AppendLine("<ul>");
                foreach (var rec in nsg.Recommendations)
                {
                    html.AppendLine($"<li>{rec}</li>");
                }
                html.AppendLine("</ul>");
            }
            html.AppendLine("</div>");
        }
        html.AppendLine("</div>");
        
        // Recomendaciones generales
        if (recommendations?.Any() == true)
        {
            html.AppendLine("<div class='recommendations'>");
            html.AppendLine("<h2>💡 Recomendaciones Generales</h2>");
            html.AppendLine("<ul>");
            foreach (var rec in recommendations)
            {
                html.AppendLine($"<li>{rec}</li>");
            }
            html.AppendLine("</ul>");
            html.AppendLine("</div>");
        }
        
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        
        return html.ToString();
    }

    private string GenerateCsvReport(List<NSGValidationDetail> nsgConfigurations, ValidationResults validationResults)
    {
        var csv = new StringBuilder();
        csv.AppendLine("NSG Name,Rule Count,Security Posture,Valid Rules,Issues");
        
        foreach (var nsg in nsgConfigurations)
        {
            var validRules = nsg.RuleResults.Count(r => r.IsValid);
            var issues = nsg.RuleResults.SelectMany(r => r.Issues).Count();
            
            csv.AppendLine($"{nsg.NSGName},{nsg.RuleCount},{nsg.SecurityPosture},{validRules},{issues}");
        }
        
        return csv.ToString();
    }

    private string GenerateConsoleReport(List<NSGValidationDetail> nsgConfigurations, ValidationResults validationResults, List<string>? recommendations)
    {
        var report = new StringBuilder();
        
        report.AppendLine("🛡️  REPORTE DE SEGURIDAD - NETWORK SECURITY GROUPS");
        report.AppendLine("═══════════════════════════════════════════════════════════════");
        report.AppendLine($"📅 Generado: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();
        
        // Resumen
        report.AppendLine("📊 RESUMEN EJECUTIVO");
        report.AppendLine("───────────────────────────────────────");
        report.AppendLine($"• NSGs Analizados: {validationResults.AnalyzedNSGs}");
        report.AppendLine($"• Reglas Totales: {validationResults.TotalRules}");
        report.AppendLine($"• Reglas Válidas: {validationResults.ValidRules}");
        report.AppendLine($"• Advertencias: {validationResults.Warnings.Count}");
        report.AppendLine($"• Errores: {validationResults.Errors.Count}");
        report.AppendLine($"• Puntuación de Seguridad: {CalculateOverallSecurityScore(validationResults):F1}/100");
        report.AppendLine();
        
        // Detalles por NSG
        report.AppendLine("🔍 DETALLES POR NSG");
        report.AppendLine("───────────────────────────────────────");
        foreach (var nsg in nsgConfigurations)
        {
            var statusIcon = nsg.SecurityPosture switch
            {
                SecurityPosture.Excellent => "🟢",
                SecurityPosture.Good => "🔵",
                SecurityPosture.Adequate => "🟡",
                SecurityPosture.Poor => "🟠",
                SecurityPosture.Critical => "🔴",
                _ => "⚪"
            };
            
            report.AppendLine($"{statusIcon} {nsg.NSGName}");
            report.AppendLine($"   Reglas: {nsg.RuleCount} | Postura: {nsg.SecurityPosture}");
            
            if (nsg.Recommendations.Any())
            {
                report.AppendLine("   Recomendaciones:");
                foreach (var rec in nsg.Recommendations.Take(3))
                {
                    report.AppendLine($"   • {rec}");
                }
            }
            report.AppendLine();
        }
        
        // Recomendaciones generales
        if (recommendations?.Any() == true)
        {
            report.AppendLine("💡 RECOMENDACIONES GENERALES");
            report.AppendLine("───────────────────────────────────────");
            foreach (var rec in recommendations.Take(5))
            {
                report.AppendLine($"• {rec}");
            }
        }
        
        return report.ToString();
    }

    private string GetHtmlStyles()
    {
        return @"
            body { font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }
            .header { background-color: #2c3e50; color: white; padding: 20px; border-radius: 5px; text-align: center; }
            .summary { background-color: white; padding: 15px; margin: 20px 0; border-radius: 5px; box-shadow: 0 2px 5px rgba(0,0,0,0.1); }
            .details { background-color: white; padding: 15px; margin: 20px 0; border-radius: 5px; box-shadow: 0 2px 5px rgba(0,0,0,0.1); }
            .recommendations { background-color: #e8f5e8; padding: 15px; margin: 20px 0; border-radius: 5px; border-left: 4px solid #27ae60; }
            .nsg-detail { border-left: 3px solid #3498db; padding-left: 15px; margin: 10px 0; }
            table { width: 100%; border-collapse: collapse; }
            td { padding: 8px; border-bottom: 1px solid #ddd; }
            .warning { color: #f39c12; font-weight: bold; }
            .error { color: #e74c3c; font-weight: bold; }
            h1, h2 { color: #2c3e50; }
            h3 { color: #3498db; }
        ";
    }

    private string SerializeToXml(object obj)
    {
        // Implementación simplificada de serialización XML
        return $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<NSGConfiguration>\n<!-- XML serialization no implementada en esta versión -->\n</NSGConfiguration>";
    }

    private double CalculateOverallSecurityScore(ValidationResults results)
    {
        if (results.TotalRules == 0) return 0;
        
        var baseScore = (double)results.ValidRules / results.TotalRules * 100;
        var errorPenalty = results.Errors.Count * 10;
        var warningPenalty = results.Warnings.Count * 5;
        
        return Math.Max(0, baseScore - errorPenalty - warningPenalty);
    }
} 