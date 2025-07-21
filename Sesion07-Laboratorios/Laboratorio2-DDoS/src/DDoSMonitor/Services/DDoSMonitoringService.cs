using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Monitor;
using DDoSMonitor.Models;
using Microsoft.Extensions.Logging;

namespace DDoSMonitor.Services;

/// <summary>
/// Implementación del servicio de monitoreo DDoS
/// </summary>
public class DDoSMonitoringService : IDDoSMonitoringService
{
    private readonly ILogger<DDoSMonitoringService> _logger;
    private readonly IMetricsService _metricsService;
    private readonly IAlertService _alertService;

    public DDoSMonitoringService(
        ILogger<DDoSMonitoringService> logger,
        IMetricsService metricsService,
        IAlertService alertService)
    {
        _logger = logger;
        _metricsService = metricsService;
        _alertService = alertService;
    }

    public async Task StartRealTimeMonitoringAsync(MonitoringOptions options)
    {
        await StartMonitoringAsync(options, CancellationToken.None);
    }

    public async Task StartMonitoringAsync(MonitoringOptions options, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔍 Iniciando monitoreo DDoS para {PublicIp}", options.PublicIpName);
        
        var lastMetrics = new DDoSMetrics();
        var consecutiveAttacks = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var metrics = await GetCurrentMetricsAsync(
                    options.SubscriptionId!, 
                    options.ResourceGroup!, 
                    options.PublicIpName!);

                if (options.ShowDashboard)
                {
                    DisplayDashboard(metrics, options);
                }

                // Verificar alertas
                if (await _alertService.ShouldTriggerAlertAsync(metrics, options.AlertThreshold))
                {
                    consecutiveAttacks++;
                    if (consecutiveAttacks >= 2) // Confirmar ataque después de 2 lecturas consecutivas
                    {
                        await _alertService.SendAlertAsync(
                            $"🚨 ATAQUE DDOS DETECTADO en {options.PublicIpName}: {metrics.PacketsDroppedDDoS:N0} paquetes bloqueados",
                            "Critical");
                        consecutiveAttacks = 0;
                    }
                }
                else
                {
                    consecutiveAttacks = 0;
                }

                lastMetrics = metrics;
                await Task.Delay(TimeSpan.FromSeconds(options.IntervalSeconds), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el monitoreo");
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
    }

    public async Task<DDoSMetrics> GetCurrentMetricsAsync(string subscriptionId, string resourceGroup, string publicIpName)
    {
        try
        {
            return await _metricsService.GetDDoSMetricsAsync(subscriptionId, resourceGroup, publicIpName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo métricas para {PublicIp}", publicIpName);
            return new DDoSMetrics
            {
                Timestamp = DateTime.UtcNow,
                ResourceName = publicIpName,
                UnderDDoSAttack = false
            };
        }
    }

    public async Task<List<DDoSMetrics>> GetHistoricalMetricsAsync(string subscriptionId, string resourceGroup, string publicIpName, DateTime startTime, DateTime endTime)
    {
        var metrics = new List<DDoSMetrics>();
        var current = startTime;
        
        while (current <= endTime)
        {
            try
            {
                var metric = await _metricsService.GetDDoSMetricsAsync(subscriptionId, resourceGroup, publicIpName, current, current.AddMinutes(5));
                metrics.Add(metric);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error obteniendo métricas históricas para {Timestamp}", current);
            }
            
            current = current.AddMinutes(5);
        }

        return metrics;
    }

    public async Task<AttackAnalysis> AnalyzeAttackStatusAsync(string subscriptionId, string resourceGroup, string publicIpName)
    {
        var endTime = DateTime.UtcNow;
        var startTime = endTime.AddHours(-1);
        
        var metrics = await GetHistoricalMetricsAsync(subscriptionId, resourceGroup, publicIpName, startTime, endTime);
        
        var analysis = new AttackAnalysis
        {
            StartTime = startTime,
            EndTime = endTime,
            Metrics = metrics,
            MaxPacketsDropped = metrics.Max(m => m.PacketsDroppedDDoS),
            MaxBytesDropped = metrics.Max(m => m.BytesDroppedDDoS),
            MaxVectorCount = metrics.Max(m => m.MaxAttackVectorCount)
        };

        // Determinar severidad
        if (analysis.MaxPacketsDropped > 100000)
            analysis.Severity = "Critical";
        else if (analysis.MaxPacketsDropped > 10000)
            analysis.Severity = "High";
        else if (analysis.MaxPacketsDropped > 1000)
            analysis.Severity = "Medium";
        else
            analysis.Severity = "Low";

        // Determinar tipo de ataque
        if (analysis.MaxVectorCount > 3)
            analysis.AttackType = "Multi-vector";
        else if (analysis.MaxBytesDropped > analysis.MaxPacketsDropped * 1000)
            analysis.AttackType = "Volumetric";
        else
            analysis.AttackType = "Protocol";

        return analysis;
    }

    public async Task RunLoadTestWithMonitoringAsync(LoadTestOptions options)
    {
        _logger.LogInformation("🚀 Iniciando test de carga con monitoreo para {TargetUrl}", options.TargetUrl);
        
        // Simular test de carga
        Console.WriteLine($"🎯 Objetivo: {options.TargetUrl}");
        Console.WriteLine($"📊 Configuración: {options.ConcurrentRequests} requests concurrentes, {options.TotalRequests} total");
        Console.WriteLine($"⏱️ Duración: {options.DurationSeconds} segundos");
        Console.WriteLine();

        if (options.MonitorDuringTest)
        {
            Console.WriteLine("🔍 Iniciando monitoreo en paralelo...");
            
            // Simular monitoreo durante el test
            for (int i = 0; i < options.DurationSeconds; i += 10)
            {
                var metrics = await GetCurrentMetricsAsync(
                    options.SubscriptionId!, 
                    options.ResourceGroupName!, 
                    options.PublicIpName!);
                    
                Console.WriteLine($"[{i:D3}s] Packets dropped: {metrics.PacketsDroppedDDoS:N0}, Under attack: {(metrics.UnderDDoSAttack ? "YES" : "NO")}");
                
                if (metrics.UnderDDoSAttack)
                {
                    await _alertService.SendAlertAsync($"DDoS detectado durante test de carga en {options.TargetUrl}", "Warning");
                }
                
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }
        
        Console.WriteLine("✅ Test de carga completado");
    }

    private void DisplayDashboard(DDoSMetrics metrics, MonitoringOptions options)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("🛡️  AZURE DDOS PROTECTION MONITOR");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.ResetColor();
        
        Console.WriteLine($"📊 Resource: {metrics.ResourceName ?? options.PublicIpName}");
        Console.WriteLine($"⏰ Last Update: {metrics.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine($"🔄 Refresh Interval: {options.IntervalSeconds}s");
        Console.WriteLine();

        // Estado del ataque
        if (metrics.UnderDDoSAttack)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("🚨 UNDER DDOS ATTACK!");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ No DDoS Attack Detected");
        }
        Console.ResetColor();
        Console.WriteLine();

        // Métricas
        Console.WriteLine("📈 Current Metrics:");
        Console.WriteLine($"   Packets Dropped: {metrics.PacketsDroppedDDoS:N0}");
        Console.WriteLine($"   Bytes Dropped: {metrics.BytesDroppedDDoS:N0}");
        Console.WriteLine($"   Packets In: {metrics.PacketsInDDoS:N0}");
        Console.WriteLine($"   Bytes In: {metrics.BytesInDDoS:N0}");
        Console.WriteLine($"   Attack Vectors: {metrics.MaxAttackVectorCount}");
        Console.WriteLine();

        // Indicador visual
        var droppedRatio = metrics.PacketsInDDoS > 0 ? metrics.PacketsDroppedDDoS / metrics.PacketsInDDoS : 0;
        Console.Write("Drop Rate: [");
        var barLength = 40;
        var filledLength = (int)(droppedRatio * barLength);
        
        for (int i = 0; i < barLength; i++)
        {
            if (i < filledLength)
            {
                Console.ForegroundColor = droppedRatio > 0.1 ? ConsoleColor.Red : ConsoleColor.Yellow;
                Console.Write("█");
            }
            else
            {
                Console.Write("░");
            }
        }
        Console.ResetColor();
        Console.WriteLine($"] {droppedRatio:P1}");
        
        Console.WriteLine();
        Console.WriteLine("Press Ctrl+C to stop monitoring...");
    }
} 