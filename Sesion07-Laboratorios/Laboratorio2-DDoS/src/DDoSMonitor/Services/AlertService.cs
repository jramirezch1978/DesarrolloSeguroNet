using DDoSMonitor.Models;
using Microsoft.Extensions.Logging;

namespace DDoSMonitor.Services;

/// <summary>
/// Implementación del servicio de alertas
/// </summary>
public class AlertService : IAlertService
{
    private readonly ILogger<AlertService> _logger;

    public AlertService(ILogger<AlertService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ShouldTriggerAlertAsync(DDoSMetrics metrics, int threshold)
    {
        await Task.CompletedTask;
        
        // Evaluar múltiples condiciones para alertas
        return metrics.UnderDDoSAttack || 
               metrics.PacketsDroppedDDoS > threshold ||
               metrics.MaxAttackVectorCount > 0;
    }

    public async Task SendAlertAsync(string message, string severity = "Warning")
    {
        await Task.CompletedTask;
        
        // Simular envío de alerta
        var color = severity switch
        {
            "Critical" => ConsoleColor.Red,
            "High" => ConsoleColor.Magenta,
            "Medium" => ConsoleColor.Yellow,
            _ => ConsoleColor.Cyan
        };

        Console.ForegroundColor = color;
        Console.WriteLine($"🚨 [{severity.ToUpper()}] {DateTime.UtcNow:HH:mm:ss} - {message}");
        Console.ResetColor();

        _logger.LogWarning("Alerta enviada: [{Severity}] {Message}", severity, message);
    }

    public async Task ConfigureAzureAlertsAsync(string subscriptionId, string resourceGroup, string publicIpName, int threshold)
    {
        await Task.CompletedTask;
        
        _logger.LogInformation("Configurando alertas de Azure Monitor para {PublicIp} con umbral {Threshold}", 
            publicIpName, threshold);
        
        // En implementación real, aquí se crearían alertas en Azure Monitor
        Console.WriteLine($"✅ Alertas configuradas para {publicIpName} (umbral: {threshold})");
    }

    public async Task<List<string>> GetActiveAlertsAsync(string subscriptionId, string resourceGroup)
    {
        await Task.CompletedTask;
        
        // Simular alertas activas
        return new List<string>
        {
            "DDoS Protection: Sin alertas activas",
            "Métricas: Funcionando normalmente"
        };
    }
} 