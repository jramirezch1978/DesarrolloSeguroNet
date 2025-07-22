using NSGManager.Models;

namespace NSGManager.Services;

/// <summary>
/// Interfaz para el servicio de Network Watcher
/// </summary>
public interface INetworkWatcherService
{
    /// <summary>
    /// Configura Network Watcher para la región
    /// </summary>
    Task ConfigureNetworkWatcherAsync(string location, string? subscriptionId = null);
    
    /// <summary>
    /// Habilita Flow Logs para NSGs
    /// </summary>
    Task EnableFlowLogsAsync(string resourceGroup, string nsgName, string storageAccountName, string? subscriptionId = null);
    
    /// <summary>
    /// Prueba conectividad entre dos recursos
    /// </summary>
    Task<ConnectivityInfo> TestConnectivityAsync(string sourceVmId, string destinationAddress, int port, string protocol);
    
    /// <summary>
    /// Verifica flujo IP para una VM específica
    /// </summary>
    Task<bool> VerifyIPFlowAsync(string vmId, string direction, string localAddress, int localPort, string remoteAddress, int remotePort, string protocol);
    
    /// <summary>
    /// Obtiene métricas de Flow Logs
    /// </summary>
    Task<FlowLogMetrics> GetFlowLogMetricsAsync(string resourceGroup, string nsgName, DateTime startTime, DateTime endTime, string? subscriptionId = null);
} 