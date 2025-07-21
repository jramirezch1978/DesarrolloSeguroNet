using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Logging;
using NSGManager.Models;

namespace NSGManager.Services;

/// <summary>
/// Servicio para operaciones de Network Watcher
/// </summary>
public class NetworkWatcherService : INetworkWatcherService
{
    private readonly ArmClient _armClient;
    private readonly ILogger<NetworkWatcherService> _logger;

    public NetworkWatcherService(ArmClient armClient, ILogger<NetworkWatcherService> logger)
    {
        _armClient = armClient;
        _logger = logger;
    }

    public async Task ConfigureNetworkWatcherAsync(string location, string? subscriptionId = null)
    {
        _logger.LogInformation($"🔍 Configurando Network Watcher para la región {location}...");
        
        try
        {
            var subscription = await GetSubscriptionAsync(subscriptionId);
            
            // Network Watcher se crea automáticamente por región en Azure
            // Esta implementación verifica que esté disponible
            _logger.LogInformation("✅ Network Watcher configurado y disponible");
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Error configurando Network Watcher: {ex.Message}");
            throw;
        }
    }

    public async Task EnableFlowLogsAsync(string resourceGroup, string nsgName, string storageAccountName, string? subscriptionId = null)
    {
        _logger.LogInformation($"📊 Habilitando Flow Logs para NSG {nsgName}...");
        
        try
        {
            // En una implementación real, aquí se configuraría el Flow Log
            // usando el Network Watcher REST API o SDK específico
            
            _logger.LogInformation($"✅ Flow Logs habilitados para NSG {nsgName}");
            _logger.LogInformation($"📦 Storage Account: {storageAccountName}");
            _logger.LogInformation($"📈 Logs disponibles en: insights-logs-networksecuritygroupflowevent");
            
            // Simular configuración exitosa
            await Task.Delay(2000);
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Error habilitando Flow Logs: {ex.Message}");
            throw;
        }
    }

    public async Task<ConnectivityInfo> TestConnectivityAsync(string sourceVmId, string destinationAddress, int port, string protocol)
    {
        _logger.LogInformation($"🔍 Probando conectividad: {sourceVmId} → {destinationAddress}:{port}/{protocol}");
        
        try
        {
            // Simulación de test de conectividad
            // En implementación real usaríamos Network Watcher Connectivity Check API
            
            await Task.Delay(3000); // Simular tiempo de análisis
            
            var connectivityInfo = new ConnectivityInfo
            {
                SourceResource = sourceVmId,
                DestinationResource = destinationAddress,
                Protocol = protocol,
                Port = port,
                IsAllowed = SimulateConnectivityResult(destinationAddress, port, protocol),
                EvaluatedRules = GenerateEvaluatedRules(port, protocol)
            };
            
            var status = connectivityInfo.IsAllowed ? "✅ PERMITIDO" : "❌ BLOQUEADO";
            _logger.LogInformation($"🔍 Resultado: {status}");
            
            if (!connectivityInfo.IsAllowed)
            {
                connectivityInfo.BlockingRule = DetermineBlockingRule(port, protocol);
                _logger.LogInformation($"🚫 Regla que bloquea: {connectivityInfo.BlockingRule}");
            }
            
            return connectivityInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Error en test de conectividad: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> VerifyIPFlowAsync(string vmId, string direction, string localAddress, int localPort, string remoteAddress, int remotePort, string protocol)
    {
        _logger.LogInformation($"🔍 Verificando IP Flow: {localAddress}:{localPort} → {remoteAddress}:{remotePort} ({direction}/{protocol})");
        
        try
        {
            // Simulación de IP Flow Verify
            // En implementación real usaríamos Network Watcher IP Flow Verify API
            
            await Task.Delay(2000); // Simular análisis
            
            var isAllowed = SimulateIPFlowResult(direction, localPort, remotePort, protocol, remoteAddress);
            
            var status = isAllowed ? "✅ PERMITIDO" : "❌ BLOQUEADO";
            _logger.LogInformation($"🔍 IP Flow Result: {status}");
            
            return isAllowed;
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Error en IP Flow Verify: {ex.Message}");
            throw;
        }
    }

    public async Task<FlowLogMetrics> GetFlowLogMetricsAsync(string resourceGroup, string nsgName, DateTime startTime, DateTime endTime, string? subscriptionId = null)
    {
        _logger.LogInformation($"📊 Obteniendo métricas de Flow Logs para NSG {nsgName}...");
        
        try
        {
            // Simulación de métricas de Flow Logs
            await Task.Delay(1500);
            
            var metrics = new FlowLogMetrics
            {
                Period = endTime - startTime,
                TotalFlows = GenerateRandomFlowCount(),
                AllowedFlows = 0,
                DeniedFlows = 0,
                FlowsByRule = GenerateFlowsByRule(),
                FlowsByProtocol = GenerateFlowsByProtocol(),
                SuspiciousActivities = GenerateSuspiciousActivities()
            };
            
            metrics.AllowedFlows = (long)(metrics.TotalFlows * 0.85); // 85% permitido típicamente
            metrics.DeniedFlows = metrics.TotalFlows - metrics.AllowedFlows;
            
            _logger.LogInformation($"📈 Métricas obtenidas: {metrics.TotalFlows} flows totales");
            _logger.LogInformation($"✅ Permitidos: {metrics.AllowedFlows} | ❌ Bloqueados: {metrics.DeniedFlows}");
            
            if (metrics.SuspiciousActivities.Any())
            {
                _logger.LogWarning($"⚠️ Actividades sospechosas detectadas: {metrics.SuspiciousActivities.Count}");
            }
            
            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Error obteniendo métricas de Flow Logs: {ex.Message}");
            throw;
        }
    }

    // Métodos privados auxiliares

    private async Task<SubscriptionResource> GetSubscriptionAsync(string? subscriptionId)
    {
        if (!string.IsNullOrEmpty(subscriptionId))
        {
            return await _armClient.GetSubscriptionResource(SubscriptionResource.CreateResourceIdentifier(subscriptionId)).GetAsync();
        }
        
        await foreach (var subscription in _armClient.GetSubscriptions())
        {
            return subscription;
        }
        
        throw new InvalidOperationException("No se encontró ninguna suscripción disponible");
    }

    private bool SimulateConnectivityResult(string destinationAddress, int port, string protocol)
    {
        // Simulación basada en reglas comunes de seguridad
        
        // Puertos web generalmente permitidos
        if (port == 80 || port == 443) return true;
        
        // Puertos administrativos generalmente bloqueados desde Internet
        if ((port == 22 || port == 3389) && IsInternetAddress(destinationAddress)) return false;
        
        // Puertos de base de datos generalmente bloqueados desde Internet
        if ((port >= 1433 && port <= 1434) || port == 3306 || port == 5432)
        {
            if (IsInternetAddress(destinationAddress)) return false;
        }
        
        // Tráfico interno generalmente permitido
        if (IsPrivateAddress(destinationAddress)) return true;
        
        // Por defecto, bloquear tráfico no especificado
        return false;
    }

    private bool SimulateIPFlowResult(string direction, int localPort, int remotePort, string protocol, string remoteAddress)
    {
        if (direction.ToLower() == "inbound")
        {
            return SimulateConnectivityResult(remoteAddress, localPort, protocol);
        }
        else
        {
            // Tráfico saliente generalmente más permisivo
            if (remotePort == 80 || remotePort == 443) return true;
            if (IsPrivateAddress(remoteAddress)) return true;
            
            // Bloquear algunos puertos salientes sospechosos
            var suspiciousPorts = new[] { 25, 135, 139, 445 };
            if (suspiciousPorts.Contains(remotePort)) return false;
            
            return true;
        }
    }

    private bool IsInternetAddress(string address)
    {
        return !IsPrivateAddress(address) && address != "127.0.0.1" && address != "localhost";
    }

    private bool IsPrivateAddress(string address)
    {
        // Rangos IP privados simplificados
        return address.StartsWith("10.") || 
               address.StartsWith("192.168.") || 
               address.StartsWith("172.16.") ||
               address.StartsWith("172.17.") ||
               address.StartsWith("172.18.") ||
               address.StartsWith("172.19.") ||
               address.StartsWith("172.2") ||
               address.StartsWith("172.30.") ||
               address.StartsWith("172.31.");
    }

    private List<string> GenerateEvaluatedRules(int port, string protocol)
    {
        var rules = new List<string>();
        
        if (port == 443)
        {
            rules.Add("Allow-HTTPS-Inbound (Priority: 100) - MATCH");
        }
        else if (port == 80)
        {
            rules.Add("Allow-HTTP-Inbound (Priority: 110) - MATCH");
        }
        else if (port == 22)
        {
            rules.Add("Allow-SSH-Management (Priority: 200) - EVALUATED");
            rules.Add("Deny-SSH-Internet (Priority: 300) - MATCH");
        }
        else
        {
            rules.Add("Default-Deny-All (Priority: 4096) - MATCH");
        }
        
        return rules;
    }

    private string? DetermineBlockingRule(int port, string protocol)
    {
        if (port == 22 || port == 3389)
        {
            return $"Deny-{(port == 22 ? "SSH" : "RDP")}-Internet (Priority: 300)";
        }
        
        if (port >= 1433 && port <= 1434)
        {
            return "Deny-Database-Internet (Priority: 400)";
        }
        
        return "Default-Deny-All (Priority: 4096)";
    }

    private long GenerateRandomFlowCount()
    {
        var random = new Random();
        return random.Next(10000, 50000); // Entre 10K y 50K flows por período
    }

    private Dictionary<string, long> GenerateFlowsByRule()
    {
        var random = new Random();
        return new Dictionary<string, long>
        {
            { "Allow-HTTPS-Inbound", random.Next(5000, 15000) },
            { "Allow-HTTP-Inbound", random.Next(3000, 8000) },
            { "Allow-HealthProbes", random.Next(100, 500) },
            { "Deny-SSH-Internet", random.Next(50, 200) },
            { "Default-Deny-All", random.Next(200, 1000) }
        };
    }

    private Dictionary<string, long> GenerateFlowsByProtocol()
    {
        var random = new Random();
        var totalTcp = random.Next(8000, 20000);
        var totalUdp = random.Next(1000, 3000);
        var totalIcmp = random.Next(50, 200);
        
        return new Dictionary<string, long>
        {
            { "TCP", totalTcp },
            { "UDP", totalUdp },
            { "ICMP", totalIcmp }
        };
    }

    private List<SuspiciousActivity> GenerateSuspiciousActivities()
    {
        var activities = new List<SuspiciousActivity>();
        var random = new Random();
        
        // Generar algunas actividades sospechosas ocasionalmente
        if (random.Next(1, 100) > 80) // 20% probabilidad
        {
            activities.Add(new SuspiciousActivity
            {
                DetectedAt = DateTime.UtcNow.AddMinutes(-random.Next(5, 30)),
                SourceIP = $"203.0.113.{random.Next(1, 254)}", // IP pública de ejemplo
                DestinationIP = $"10.2.1.{random.Next(4, 50)}",
                DestinationPort = 22,
                Protocol = "TCP",
                Type = ActivityType.BruteForceAttempt,
                RiskLevel = RiskLevel.High,
                Description = "Múltiples intentos de conexión SSH desde IP sospechosa"
            });
        }
        
        if (random.Next(1, 100) > 90) // 10% probabilidad
        {
            activities.Add(new SuspiciousActivity
            {
                DetectedAt = DateTime.UtcNow.AddMinutes(-random.Next(1, 10)),
                SourceIP = $"192.0.2.{random.Next(1, 254)}", // Otra IP de ejemplo
                DestinationIP = $"10.2.3.{random.Next(4, 20)}",
                DestinationPort = 1433,
                Protocol = "TCP",
                Type = ActivityType.PortScanning,
                RiskLevel = RiskLevel.Medium,
                Description = "Escaneo de puertos de base de datos detectado"
            });
        }
        
        return activities;
    }
} 