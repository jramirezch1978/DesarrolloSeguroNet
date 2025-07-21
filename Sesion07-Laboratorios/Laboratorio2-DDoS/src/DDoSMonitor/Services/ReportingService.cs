using System.Text.Json;
using DDoSMonitor.Models;
using Microsoft.Extensions.Logging;

namespace DDoSMonitor.Services;

/// <summary>
/// Implementación del servicio de reportes
/// </summary>
public class ReportingService : IReportingService
{
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(ILogger<ReportingService> logger)
    {
        _logger = logger;
    }

    public async Task<string> GenerateDDoSReportAsync(ReportOptions options)
    {
        // Obtener métricas simuladas o de Azure
        var metrics = new List<DDoSMetrics>();
        for (int i = 0; i < 10; i++)
        {
            metrics.Add(new DDoSMetrics
            {
                Timestamp = options.StartTime.AddMinutes(i * 5),
                ResourceName = options.PublicIpName,
                UnderDDoSAttack = Random.Shared.Next(1, 100) <= 5,
                PacketsDroppedDDoS = Random.Shared.Next(0, 1000),
                BytesDroppedDDoS = Random.Shared.Next(0, 10000)
            });
        }

        return await GenerateReportAsync(options, metrics);
    }

    public async Task<string> GenerateReportAsync(ReportOptions options, List<DDoSMetrics> metrics)
    {
        await Task.CompletedTask;
        
        var report = new
        {
            ReportGenerated = DateTime.UtcNow,
            Period = new { Start = options.StartTime, End = options.EndTime },
            Summary = new
            {
                TotalDataPoints = metrics.Count,
                AttacksDetected = metrics.Count(m => m.UnderDDoSAttack),
                MaxPacketsDropped = metrics.Max(m => m.PacketsDroppedDDoS),
                MaxBytesDropped = metrics.Max(m => m.BytesDroppedDDoS),
                MaxAttackVectors = metrics.Max(m => m.MaxAttackVectorCount)
            },
            Metrics = metrics
        };

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        
        if (!string.IsNullOrEmpty(options.OutputPath))
        {
            await File.WriteAllTextAsync(options.OutputPath, json);
            _logger.LogInformation("Reporte guardado en {Path}", options.OutputPath);
        }

        return json;
    }

    public async Task ExportMetricsAsync(List<DDoSMetrics> metrics, string format, string filePath)
    {
        switch (format.ToLower())
        {
            case "json":
                var json = JsonSerializer.Serialize(metrics, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);
                break;
                
            case "csv":
                var csv = "Timestamp,UnderAttack,PacketsDropped,BytesDropped,PacketsIn,BytesIn,AttackVectors\n";
                csv += string.Join("\n", metrics.Select(m => 
                    $"{m.Timestamp:yyyy-MM-dd HH:mm:ss},{m.UnderDDoSAttack},{m.PacketsDroppedDDoS},{m.BytesDroppedDDoS},{m.PacketsInDDoS},{m.BytesInDDoS},{m.MaxAttackVectorCount}"));
                await File.WriteAllTextAsync(filePath, csv);
                break;
                
            default:
                throw new NotSupportedException($"Formato {format} no soportado");
        }

        _logger.LogInformation("Métricas exportadas a {Path} en formato {Format}", filePath, format);
    }

    public async Task<AttackAnalysis> GenerateAttackSummaryAsync(List<DDoSMetrics> metrics)
    {
        await Task.CompletedTask;
        
        var attackMetrics = metrics.Where(m => m.UnderDDoSAttack).ToList();
        
        var analysis = new AttackAnalysis
        {
            StartTime = attackMetrics.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow,
            EndTime = attackMetrics.LastOrDefault()?.Timestamp,
            MaxPacketsDropped = metrics.Max(m => m.PacketsDroppedDDoS),
            MaxBytesDropped = metrics.Max(m => m.BytesDroppedDDoS),
            MaxVectorCount = metrics.Max(m => m.MaxAttackVectorCount),
            Metrics = attackMetrics
        };

        // Clasificar severidad
        if (analysis.MaxPacketsDropped > 100000)
            analysis.Severity = "Critical";
        else if (analysis.MaxPacketsDropped > 10000)
            analysis.Severity = "High";
        else if (analysis.MaxPacketsDropped > 1000)
            analysis.Severity = "Medium";
        else
            analysis.Severity = "Low";

        return analysis;
    }

    public async Task<string> CreateHtmlDashboardAsync(List<DDoSMetrics> metrics)
    {
        await Task.CompletedTask;
        
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>DDoS Protection Dashboard</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .metric {{ background: #f5f5f5; padding: 10px; margin: 10px 0; border-radius: 5px; }}
        .alert {{ background: #ffebee; color: #c62828; }}
        .normal {{ background: #e8f5e8; color: #2e7d32; }}
    </style>
</head>
<body>
    <h1>🛡️ DDoS Protection Dashboard</h1>
    <p>Generado: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
    
    <h2>Resumen</h2>
    <div class='metric'>
        <strong>Total de métricas:</strong> {metrics.Count}<br>
        <strong>Ataques detectados:</strong> {metrics.Count(m => m.UnderDDoSAttack)}<br>
        <strong>Máximo paquetes bloqueados:</strong> {metrics.Max(m => m.PacketsDroppedDDoS):N0}<br>
        <strong>Máximo bytes bloqueados:</strong> {metrics.Max(m => m.BytesDroppedDDoS):N0}
    </div>
    
    <h2>Últimas Métricas</h2>
    {string.Join("", metrics.TakeLast(10).Select(m => $@"
    <div class='metric {(m.UnderDDoSAttack ? "alert" : "normal")}'>
        <strong>{m.Timestamp:HH:mm:ss}</strong> - 
        {(m.UnderDDoSAttack ? "🚨 ATAQUE DETECTADO" : "✅ Normal")} - 
        Paquetes bloqueados: {m.PacketsDroppedDDoS:N0}
    </div>"))}
</body>
</html>";

        return html;
    }
} 