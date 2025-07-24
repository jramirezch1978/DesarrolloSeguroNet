using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Monitor;
using Azure.ResourceManager.Network;
using DDoSMonitor.Models;
using Microsoft.Extensions.Logging;

namespace DDoSMonitor.Services;

/// <summary>
/// Implementación del servicio de métricas
/// </summary>
public class MetricsService : IMetricsService
{
    private readonly ILogger<MetricsService> _logger;
    private readonly ArmClient _armClient;

    public MetricsService(ILogger<MetricsService> logger)
    {
        _logger = logger;
        _armClient = new ArmClient(new DefaultAzureCredential());
    }

    public async Task<DDoSMetrics> GetDDoSMetricsAsync(string subscriptionId, string resourceGroup, string publicIpName, DateTime? startTime = null, DateTime? endTime = null)
    {
        try
        {
            // Para este laboratorio, trabajamos en modo simulado para demostrar funcionalidad
            _logger.LogInformation("🔍 Simulando obtención de métricas DDoS para {PublicIp}", publicIpName);
            
            // Simular una pequeña demora para realismo
            await Task.Delay(100);

            // En un escenario real, aquí se consultarían las métricas reales de Azure Monitor
            // Para el laboratorio, simulamos métricas
            var metrics = new DDoSMetrics
            {
                Timestamp = DateTime.UtcNow,
                ResourceName = publicIpName,
                PublicIpAddress = $"20.{Random.Shared.Next(1, 255)}.{Random.Shared.Next(1, 255)}.{Random.Shared.Next(1, 255)}", // IP simulada
                UnderDDoSAttack = false, // Simular que no hay ataque por defecto
                PacketsDroppedDDoS = Random.Shared.Next(0, 100),
                BytesDroppedDDoS = Random.Shared.Next(0, 1000),
                PacketsInDDoS = Random.Shared.Next(1000, 10000),
                BytesInDDoS = Random.Shared.Next(10000, 100000),
                MaxAttackVectorCount = 0
            };

            // Simular ocasionalmente un ataque para demostración
            if (Random.Shared.Next(1, 100) <= 10) // 10% chance para más demos
            {
                metrics.UnderDDoSAttack = true;
                metrics.PacketsDroppedDDoS = Random.Shared.Next(1000, 50000);
                metrics.BytesDroppedDDoS = Random.Shared.Next(100000, 1000000);
                metrics.MaxAttackVectorCount = Random.Shared.Next(1, 5);
                _logger.LogWarning("🚨 ¡Simulando ataque DDoS detectado en {PublicIp}!", publicIpName);
            }

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo métricas DDoS para {PublicIp}", publicIpName);
            return new DDoSMetrics
            {
                Timestamp = DateTime.UtcNow,
                ResourceName = publicIpName,
                PublicIpAddress = "0.0.0.0",
                UnderDDoSAttack = false
            };
        }
    }

    public async Task<List<string>> GetAvailableMetricsAsync(string subscriptionId, string resourceGroup, string publicIpName)
    {
        // Lista de métricas estándar disponibles para Public IP con DDoS Protection
        return await Task.FromResult(new List<string>
        {
            "UnderDDoSAttack",
            "PacketsDroppedDDoS", 
            "BytesDroppedDDoS",
            "PacketsInDDoS",
            "BytesInDDoS",
            "MaxAttackVectorCount",
            "DDoSMitigationFlowRate",
            "DDoSMitigationPacketRate"
        });
    }

    public async Task<bool> IsDDoSProtectionEnabledAsync(string subscriptionId, string resourceGroup, string publicIpName)
    {
        try
        {
            // Para el laboratorio, simulamos que DDoS Protection está habilitado
            _logger.LogInformation("🛡️ Verificando DDoS Protection para {PublicIp} (Simulado)", publicIpName);
            await Task.Delay(50);
            
            // Simular habilitado en 80% de los casos
            return Random.Shared.Next(1, 100) <= 80;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verificando DDoS Protection para {PublicIp}", publicIpName);
            return false;
        }
    }

    public async Task<AttackAnalysis> AnalyzeHistoricalMetricsAsync(AnalysisOptions options)
    {
        try
        {
            _logger.LogInformation("📊 Generando análisis histórico simulado para {PublicIp}", options.PublicIpName);
            
            var metrics = new List<DDoSMetrics>();
            var current = options.StartTime;
            
            while (current <= options.EndTime)
            {
                var metric = await GetDDoSMetricsAsync(options.SubscriptionId!, options.ResourceGroupName!, options.PublicIpName!, current, current.AddMinutes(5));
                metrics.Add(metric);
                current = current.AddMinutes(5);
            }

            var analysis = new AttackAnalysis
            {
                StartTime = options.StartTime,
                EndTime = options.EndTime,
                Metrics = metrics,
                MaxPacketsDropped = metrics.Count > 0 ? metrics.Max(m => m.PacketsDroppedDDoS) : 0,
                MaxBytesDropped = metrics.Count > 0 ? metrics.Max(m => m.BytesDroppedDDoS) : 0,
                MaxVectorCount = metrics.Count > 0 ? metrics.Max(m => m.MaxAttackVectorCount) : 0
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

            // Determinar tipo de ataque basado en las métricas
            var attackEvents = metrics.Count(m => m.UnderDDoSAttack);
            if (attackEvents > 0)
            {
                if (analysis.MaxVectorCount > 3)
                    analysis.AttackType = "Multi-Vector Attack";
                else if (analysis.MaxPacketsDropped > 50000)
                    analysis.AttackType = "Volumetric Attack";
                else
                    analysis.AttackType = "Protocol Attack";
            }
            else
            {
                analysis.AttackType = "No Attack Detected";
            }

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analizando métricas históricas");
            return new AttackAnalysis
            {
                StartTime = options.StartTime,
                EndTime = options.EndTime,
                Severity = "Unknown",
                AttackType = "Error"
            };
        }
    }
} 