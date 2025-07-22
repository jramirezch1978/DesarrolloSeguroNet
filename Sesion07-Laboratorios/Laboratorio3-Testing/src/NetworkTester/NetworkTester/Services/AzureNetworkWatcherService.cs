using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetworkTester.Models;
using System.Text.Json;

namespace NetworkTester.Services;

public class AzureNetworkWatcherService : IAzureNetworkWatcherService
{
    private readonly ILogger<AzureNetworkWatcherService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ArmClient _armClient;

    public AzureNetworkWatcherService(ILogger<AzureNetworkWatcherService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _armClient = new ArmClient(new DefaultAzureCredential());
    }

    public async Task<ConnectivityResult> TestConnectivityAsync(string sourceResourceId, string destinationAddress, int destinationPort, string protocol = "TCP")
    {
        try
        {
            _logger.LogDebug($"Testing conectividad desde {sourceResourceId} hacia {destinationAddress}:{destinationPort}");

            var networkWatcher = await GetNetworkWatcherAsync();
            if (networkWatcher == null)
            {
                throw new InvalidOperationException("Network Watcher no disponible");
            }

            // Preparar parámetros para connectivity check
            var connectivityParams = new
            {
                source = new { resourceId = sourceResourceId },
                destination = new { address = destinationAddress, port = destinationPort },
                protocol = protocol.ToUpperInvariant(),
                preferredIPVersion = "IPv4"
            };

            // Simular llamada a Network Watcher API
            // En implementación real, usar Azure.ResourceManager.Network APIs
            var result = await SimulateConnectivityTestAsync(sourceResourceId, destinationAddress, destinationPort, protocol);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error testing conectividad: {sourceResourceId} -> {destinationAddress}:{destinationPort}");
            
            return new ConnectivityResult
            {
                ConnectionStatus = "Error",
                StatusReason = ex.Message,
                AvgLatencyInMs = 0,
                ProbesSent = 0,
                ProbesFailed = 1
            };
        }
    }

    public async Task<IPFlowVerifyResult> VerifyIPFlowAsync(string vmResourceId, string direction, string localAddress, int localPort, string remoteAddress, int remotePort, string protocol)
    {
        try
        {
            _logger.LogDebug($"Verificando IP flow: {vmResourceId} - {direction} - {localAddress}:{localPort} <-> {remoteAddress}:{remotePort}");

            var networkWatcher = await GetNetworkWatcherAsync();
            if (networkWatcher == null)
            {
                throw new InvalidOperationException("Network Watcher no disponible");
            }

            // Simular verificación de IP Flow
            var result = await SimulateIPFlowVerifyAsync(vmResourceId, direction, localAddress, localPort, remoteAddress, remotePort, protocol);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error verificando IP flow para {vmResourceId}");
            
            return new IPFlowVerifyResult
            {
                Access = "Error",
                RuleName = "Error",
                Priority = 0,
                Direction = direction
            };
        }
    }

    public async Task<NextHopResult> GetNextHopAsync(string vmResourceId, string sourceIP, string destinationIP)
    {
        try
        {
            _logger.LogDebug($"Obteniendo next hop: {vmResourceId} - {sourceIP} -> {destinationIP}");

            var networkWatcher = await GetNetworkWatcherAsync();
            if (networkWatcher == null)
            {
                throw new InvalidOperationException("Network Watcher no disponible");
            }

            // Simular obtención de next hop
            var result = await SimulateNextHopAsync(vmResourceId, sourceIP, destinationIP);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo next hop para {vmResourceId}");
            
            return new NextHopResult
            {
                NextHopType = "Error",
                Explanation = ex.Message
            };
        }
    }

    public async Task<List<EffectiveSecurityRule>> GetEffectiveSecurityRulesAsync(string networkInterfaceId)
    {
        try
        {
            _logger.LogDebug($"Obteniendo reglas de seguridad efectivas para {networkInterfaceId}");

            // Obtener NIC resource
            var nicResource = _armClient.GetNetworkInterfaceResource(new Azure.Core.ResourceIdentifier(networkInterfaceId));
            
            // Simular obtención de reglas efectivas
            var effectiveRules = await SimulateEffectiveSecurityRulesAsync(networkInterfaceId);
            
            return effectiveRules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo reglas efectivas para {networkInterfaceId}");
            return new List<EffectiveSecurityRule>();
        }
    }

    public async Task<TopologyResult> GetTopologyAsync(string resourceGroupName)
    {
        try
        {
            _logger.LogDebug($"Obteniendo topología de red para {resourceGroupName}");

            var networkWatcher = await GetNetworkWatcherAsync();
            if (networkWatcher == null)
            {
                throw new InvalidOperationException("Network Watcher no disponible");
            }

            // Simular obtención de topología
            var topology = await SimulateTopologyAsync(resourceGroupName);
            
            return topology;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo topología para {resourceGroupName}");
            return new TopologyResult();
        }
    }

    public async Task<string> StartPacketCaptureAsync(string vmResourceId, PacketCaptureParameters parameters)
    {
        try
        {
            _logger.LogInformation($"Iniciando captura de paquetes en {vmResourceId}");

            var networkWatcher = await GetNetworkWatcherAsync();
            if (networkWatcher == null)
            {
                throw new InvalidOperationException("Network Watcher no disponible");
            }

            // Simular inicio de packet capture
            var captureId = await SimulateStartPacketCaptureAsync(vmResourceId, parameters);
            
            _logger.LogInformation($"Captura de paquetes iniciada con ID: {captureId}");
            return captureId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error iniciando captura de paquetes en {vmResourceId}");
            throw;
        }
    }

    public async Task<PacketCaptureStatus> GetPacketCaptureStatusAsync(string vmResourceId, string captureId)
    {
        try
        {
            _logger.LogDebug($"Obteniendo estado de captura {captureId} en {vmResourceId}");

            // Simular obtención de estado
            var status = await SimulateGetPacketCaptureStatusAsync(vmResourceId, captureId);
            
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo estado de captura {captureId}");
            
            return new PacketCaptureStatus
            {
                Id = captureId,
                CaptureStatus = "Error",
                StopReason = ex.Message
            };
        }
    }

    public async Task StopPacketCaptureAsync(string vmResourceId, string captureId)
    {
        try
        {
            _logger.LogInformation($"Deteniendo captura de paquetes {captureId} en {vmResourceId}");

            var networkWatcher = await GetNetworkWatcherAsync();
            if (networkWatcher == null)
            {
                throw new InvalidOperationException("Network Watcher no disponible");
            }

            // Simular detención de packet capture
            await SimulateStopPacketCaptureAsync(vmResourceId, captureId);
            
            _logger.LogInformation($"Captura de paquetes {captureId} detenida");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deteniendo captura {captureId}");
            throw;
        }
    }

    private async Task<NetworkWatcherResource?> GetNetworkWatcherAsync()
    {
        try
        {
            var subscription = await _armClient.GetDefaultSubscriptionAsync();
            var networkWatcherRG = _configuration["Azure:NetworkWatcherResourceGroup"] ?? "NetworkWatcherRG";
            var location = _configuration["Azure:DefaultLocation"] ?? "eastus";
            
            var resourceGroup = await subscription.GetResourceGroupAsync(networkWatcherRG);
            var networkWatcherName = $"NetworkWatcher_{location}";
            
            var networkWatcher = await resourceGroup.Value.GetNetworkWatcherAsync(networkWatcherName);
            return networkWatcher.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo Network Watcher");
            return null;
        }
    }

    #region Simulation Methods (Para propósitos del laboratorio)
    
    private async Task<ConnectivityResult> SimulateConnectivityTestAsync(string sourceResourceId, string destinationAddress, int destinationPort, string protocol)
    {
        await Task.Delay(500); // Simular latencia de API
        
        // Lógica de simulación basada en patrones conocidos
        var isInternet = !destinationAddress.StartsWith("10.") && !destinationAddress.StartsWith("192.168.");
        var isWebTier = sourceResourceId.Contains("web", StringComparison.OrdinalIgnoreCase);
        var isAppTier = sourceResourceId.Contains("app", StringComparison.OrdinalIgnoreCase);
        var isDataTier = sourceResourceId.Contains("data", StringComparison.OrdinalIgnoreCase);
        
        string connectionStatus = "Reachable";
        string statusReason = "Connection established successfully";
        
        // Simular reglas de NSG
        if (isWebTier && destinationAddress.StartsWith("10.2.3.")) // Web tier trying to access data tier
        {
            connectionStatus = "Unreachable";
            statusReason = "Blocked by NSG rule - direct access to data tier not allowed";
        }
        else if (destinationPort == 22 && isInternet) // SSH from internet
        {
            connectionStatus = "Unreachable";
            statusReason = "Blocked by NSG rule - SSH access from internet not allowed";
        }
        else if (destinationPort == 3389 && isInternet) // RDP from internet
        {
            connectionStatus = "Unreachable";
            statusReason = "Blocked by NSG rule - RDP access from internet not allowed";
        }

        var random = new Random();
        var latency = connectionStatus == "Reachable" ? random.Next(5, 50) : 0;

        return new ConnectivityResult
        {
            ConnectionStatus = connectionStatus,
            StatusReason = statusReason,
            AvgLatencyInMs = latency,
            MinLatencyInMs = Math.Max(1, latency - 10),
            MaxLatencyInMs = latency + 10,
            ProbesSent = 3,
            ProbesFailed = connectionStatus == "Unreachable" ? 3 : 0,
            Hops = new List<dynamic>
            {
                new { address = "10.2.1.1", nextHopType = "VirtualNetworkGateway" },
                new { address = destinationAddress, nextHopType = isInternet ? "Internet" : "VirtualNetwork" }
            }
        };
    }

    private async Task<IPFlowVerifyResult> SimulateIPFlowVerifyAsync(string vmResourceId, string direction, string localAddress, int localPort, string remoteAddress, int remotePort, string protocol)
    {
        await Task.Delay(200);
        
        // Simular verificación de reglas NSG
        var access = "Allow";
        var ruleName = "AllowVnetInBound";
        var priority = 65000;
        
        // Simular reglas específicas
        if (direction == "Inbound" && remotePort == 22 && !remoteAddress.StartsWith("10."))
        {
            access = "Deny";
            ruleName = "DenySSHFromInternet";
            priority = 1000;
        }
        else if (direction == "Inbound" && remotePort == 3389 && !remoteAddress.StartsWith("10."))
        {
            access = "Deny";
            ruleName = "DenyRDPFromInternet";
            priority = 1001;
        }
        else if (localAddress.StartsWith("10.2.1.") && remoteAddress.StartsWith("10.2.3.") && remotePort == 1433)
        {
            access = "Deny";
            ruleName = "DenyWebToDataDirect";
            priority = 200;
        }
        
        return new IPFlowVerifyResult
        {
            Access = access,
            RuleName = ruleName,
            Priority = priority,
            Direction = direction
        };
    }

    private async Task<NextHopResult> SimulateNextHopAsync(string vmResourceId, string sourceIP, string destinationIP)
    {
        await Task.Delay(200);
        
        var nextHopType = "VirtualNetwork";
        string? nextHopIP = null;
        
        if (!destinationIP.StartsWith("10."))
        {
            nextHopType = "Internet";
            nextHopIP = "10.2.1.1"; // Gateway IP
        }
        else if (destinationIP.StartsWith("10.2."))
        {
            nextHopType = "VirtualNetwork";
            nextHopIP = destinationIP;
        }
        
        return new NextHopResult
        {
            NextHopType = nextHopType,
            NextHopIpAddress = nextHopIP,
            Explanation = $"Traffic to {destinationIP} routed via {nextHopType}"
        };
    }

    private async Task<List<EffectiveSecurityRule>> SimulateEffectiveSecurityRulesAsync(string networkInterfaceId)
    {
        await Task.Delay(300);
        
        // Simular reglas efectivas típicas
        return new List<EffectiveSecurityRule>
        {
            new EffectiveSecurityRule
            {
                Name = "AllowVnetInBound",
                Protocol = "*",
                SourcePortRange = "*",
                DestinationPortRange = "*",
                SourceAddressPrefix = "VirtualNetwork",
                DestinationAddressPrefix = "VirtualNetwork",
                Access = "Allow",
                Priority = 65000,
                Direction = "Inbound",
                Source = "DefaultSecurityRules"
            },
            new EffectiveSecurityRule
            {
                Name = "AllowAzureLoadBalancerInBound",
                Protocol = "*",
                SourcePortRange = "*",
                DestinationPortRange = "*",
                SourceAddressPrefix = "AzureLoadBalancer",
                DestinationAddressPrefix = "*",
                Access = "Allow",
                Priority = 65001,
                Direction = "Inbound",
                Source = "DefaultSecurityRules"
            },
            new EffectiveSecurityRule
            {
                Name = "DenyAllInBound",
                Protocol = "*",
                SourcePortRange = "*",
                DestinationPortRange = "*",
                SourceAddressPrefix = "*",
                DestinationAddressPrefix = "*",
                Access = "Deny",
                Priority = 65500,
                Direction = "Inbound",
                Source = "DefaultSecurityRules"
            }
        };
    }

    private async Task<TopologyResult> SimulateTopologyAsync(string resourceGroupName)
    {
        await Task.Delay(500);
        
        // Simular topología básica
        return new TopologyResult
        {
            Resources = new List<TopologyResource>
            {
                new TopologyResource
                {
                    Id = $"/subscriptions/sub/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/virtualNetworks/vnet-nsg-lab",
                    Name = "vnet-nsg-lab",
                    Type = "Microsoft.Network/virtualNetworks",
                    Location = "eastus"
                }
            },
            Associations = new List<TopologyAssociation>()
        };
    }

    private async Task<string> SimulateStartPacketCaptureAsync(string vmResourceId, PacketCaptureParameters parameters)
    {
        await Task.Delay(1000);
        return $"capture-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    private async Task<PacketCaptureStatus> SimulateGetPacketCaptureStatusAsync(string vmResourceId, string captureId)
    {
        await Task.Delay(200);
        
        return new PacketCaptureStatus
        {
            Id = captureId,
            Name = captureId,
            CaptureStatus = "Running",
            StartTime = DateTime.UtcNow.AddMinutes(-2),
            CaptureFileLocation = new List<string> { $"https://storage.blob.core.windows.net/captures/{captureId}.cap" }
        };
    }

    private async Task SimulateStopPacketCaptureAsync(string vmResourceId, string captureId)
    {
        await Task.Delay(500);
        // Simular detención exitosa
    }
    
    #endregion
} 