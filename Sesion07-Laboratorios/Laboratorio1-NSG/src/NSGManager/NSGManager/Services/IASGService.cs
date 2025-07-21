using NSGManager.Models;

namespace NSGManager.Services;

/// <summary>
/// Interfaz para el servicio de gestión de Application Security Groups
/// </summary>
public interface IASGService
{
    /// <summary>
    /// Crea Application Security Groups para todos los tiers
    /// </summary>
    Task CreateApplicationSecurityGroupsAsync(NSGCreationOptions options);
    
    /// <summary>
    /// Obtiene la configuración actual de ASGs
    /// </summary>
    Task<List<ASGConfiguration>> GetASGConfigurationAsync(string resourceGroup, string? subscriptionId = null);
    
    /// <summary>
    /// Asigna máquinas virtuales a Application Security Groups
    /// </summary>
    Task AssignVirtualMachinesToASGsAsync(string resourceGroup, Dictionary<string, string[]> vmToAsgMapping, string? subscriptionId = null);
    
    /// <summary>
    /// Valida la configuración de ASGs
    /// </summary>
    Task<ValidationResults> ValidateASGConfigurationAsync(string resourceGroup, string? subscriptionId = null);
} 