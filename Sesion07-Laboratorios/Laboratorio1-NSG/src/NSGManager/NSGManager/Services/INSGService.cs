using NSGManager.Models;

namespace NSGManager.Services;

/// <summary>
/// Interfaz para el servicio de gestión de Network Security Groups
/// </summary>
public interface INSGService
{
    /// <summary>
    /// Crea NSGs básicos con reglas estándar
    /// </summary>
    Task CreateBasicNSGsAsync(NSGCreationOptions options);
    
    /// <summary>
    /// Crea NSGs avanzados con reglas granulares y ASGs
    /// </summary>
    Task CreateAdvancedNSGsAsync(NSGCreationOptions options);
    
    /// <summary>
    /// Obtiene la configuración actual de NSGs
    /// </summary>
    Task<List<NSGValidationDetail>> GetNSGConfigurationAsync(string resourceGroup, string? subscriptionId = null);
    
    /// <summary>
    /// Actualiza reglas de un NSG existente
    /// </summary>
    Task UpdateNSGRulesAsync(string resourceGroup, string nsgName, List<NSGRuleConfiguration> rules, string? subscriptionId = null);
    
    /// <summary>
    /// Elimina recursos creados por el laboratorio
    /// </summary>
    Task CleanupResourcesAsync(CleanupOptions options);
    
    /// <summary>
    /// Aplica template de NSG predefinido
    /// </summary>
    Task ApplyNSGTemplateAsync(string resourceGroup, NSGTemplate template, string? subscriptionId = null);
    
    /// <summary>
    /// Verifica conectividad entre recursos usando NSG rules
    /// </summary>
    Task<ConnectivityInfo> TestConnectivityAsync(string sourceResource, string destinationResource, int port, string protocol, string resourceGroup, string? subscriptionId = null);
    
    /// <summary>
    /// Habilita Flow Logs para NSGs
    /// </summary>
    Task EnableFlowLogsAsync(string resourceGroup, string storageAccountName, string? subscriptionId = null);
} 