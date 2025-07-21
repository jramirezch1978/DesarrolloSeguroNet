using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Logging;
using NSGManager.Models;

namespace NSGManager.Services;

/// <summary>
/// Servicio para gestión de Application Security Groups
/// </summary>
public class ASGService : IASGService
{
    private readonly ArmClient _armClient;
    private readonly ILogger<ASGService> _logger;

    public ASGService(ArmClient armClient, ILogger<ASGService> logger)
    {
        _armClient = armClient;
        _logger = logger;
    }

    public async Task CreateApplicationSecurityGroupsAsync(NSGCreationOptions options)
    {
        _logger.LogInformation("📋 Iniciando creación de Application Security Groups...");
        
        var subscription = await GetSubscriptionAsync(options.SubscriptionId);
        var resourceGroup = await GetResourceGroupAsync(subscription, options.ResourceGroupName);
        
        var asgConfigurations = GetASGConfigurations();
        
        foreach (var asgConfig in asgConfigurations)
        {
            await CreateASGAsync(resourceGroup, asgConfig, options.Location);
        }
        
        _logger.LogInformation("✅ Application Security Groups creados exitosamente");
    }

    public async Task<List<ASGConfiguration>> GetASGConfigurationAsync(string resourceGroup, string? subscriptionId = null)
    {
        var subscription = await GetSubscriptionAsync(subscriptionId);
        var resourceGroupResource = await GetResourceGroupAsync(subscription, resourceGroup);
        
        var configurations = new List<ASGConfiguration>();
        
        await foreach (var asg in resourceGroupResource.GetApplicationSecurityGroups())
        {
            var asgData = await asg.GetAsync();
            var config = new ASGConfiguration
            {
                Name = asg.Data.Name,
                Description = asg.Data.Tags.GetValueOrDefault("Description", ""),
                Purpose = asg.Data.Tags.GetValueOrDefault("Purpose", ""),
                Tags = asg.Data.Tags.ToDictionary(t => t.Key, t => t.Value)
            };
            
            configurations.Add(config);
        }
        
        return configurations;
    }

    public async Task AssignVirtualMachinesToASGsAsync(string resourceGroup, Dictionary<string, string[]> vmToAsgMapping, string? subscriptionId = null)
    {
        _logger.LogInformation("🔗 Asignando VMs a Application Security Groups...");
        
        var subscription = await GetSubscriptionAsync(subscriptionId);
        var resourceGroupResource = await GetResourceGroupAsync(subscription, resourceGroup);
        
        foreach (var mapping in vmToAsgMapping)
        {
            var vmName = mapping.Key;
            var asgNames = mapping.Value;
            
            try
            {
                var vm = await resourceGroupResource.GetVirtualMachineAsync(vmName);
                
                // Obtener la NIC principal de la VM
                var primaryNicId = vm.Value.Data.NetworkProfile.NetworkInterfaces.FirstOrDefault()?.Id;
                if (primaryNicId == null)
                {
                    _logger.LogWarning($"⚠️ VM {vmName} no tiene NIC principal");
                    continue;
                }
                
                var nicResource = await resourceGroupResource.GetNetworkInterfaceAsync(primaryNicId.Name);
                var nicData = nicResource.Value.Data;
                
                // Agregar ASGs a la configuración IP principal
                var primaryIpConfig = nicData.IPConfigurations.FirstOrDefault();
                if (primaryIpConfig != null)
                {
                    primaryIpConfig.ApplicationSecurityGroups.Clear();
                    
                    foreach (var asgName in asgNames)
                    {
                        try
                        {
                            var asg = await resourceGroupResource.GetApplicationSecurityGroupAsync(asgName);
                            primaryIpConfig.ApplicationSecurityGroups.Add(new WritableSubResource { Id = asg.Value.Id });
                            _logger.LogInformation($"✅ VM {vmName} asignada a ASG {asgName}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"⚠️ No se pudo asignar VM {vmName} a ASG {asgName}: {ex.Message}");
                        }
                    }
                    
                    await nicResource.Value.UpdateAsync(WaitUntil.Completed, nicData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error asignando VM {vmName}: {ex.Message}");
            }
        }
        
        _logger.LogInformation("✅ Asignación de VMs a ASGs completada");
    }

    public async Task<ValidationResults> ValidateASGConfigurationAsync(string resourceGroup, string? subscriptionId = null)
    {
        _logger.LogInformation("🔍 Validando configuración de ASGs...");
        
        var subscription = await GetSubscriptionAsync(subscriptionId);
        var resourceGroupResource = await GetResourceGroupAsync(subscription, resourceGroup);
        
        var results = new ValidationResults
        {
            Warnings = new List<string>(),
            Errors = new List<string>()
        };
        
        var asgCount = 0;
        var expectedASGs = new[] { "asg-webservers", "asg-appservers", "asg-dbservers", "asg-mgmtservers" };
        var foundASGs = new List<string>();
        
        await foreach (var asg in resourceGroupResource.GetApplicationSecurityGroups())
        {
            asgCount++;
            foundASGs.Add(asg.Data.Name);
            
            // Validar tags requeridos
            if (!asg.Data.Tags.ContainsKey("Purpose"))
            {
                results.Warnings.Add($"ASG {asg.Data.Name} no tiene tag 'Purpose' definido");
            }
            
            if (!asg.Data.Tags.ContainsKey("CreatedBy"))
            {
                results.Warnings.Add($"ASG {asg.Data.Name} no tiene tag 'CreatedBy' definido");
            }
        }
        
        // Verificar ASGs esperados
        foreach (var expectedASG in expectedASGs)
        {
            if (!foundASGs.Contains(expectedASG))
            {
                results.Errors.Add($"ASG esperado no encontrado: {expectedASG}");
            }
        }
        
        results.AnalyzedNSGs = asgCount; // Reutilizar campo para ASGs
        results.ValidRules = asgCount - results.Errors.Count;
        results.TotalRules = asgCount;
        
        _logger.LogInformation($"✅ Validación completada: {asgCount} ASGs analizados");
        
        return results;
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

    private async Task<ResourceGroupResource> GetResourceGroupAsync(SubscriptionResource subscription, string resourceGroupName)
    {
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName);
        return resourceGroup.Value;
    }

    private List<ASGConfiguration> GetASGConfigurations()
    {
        return new List<ASGConfiguration>
        {
            new ASGConfiguration
            {
                Name = "asg-webservers",
                Purpose = "WebServers",
                Description = "Servidores web que exponen aplicaciones públicas",
                Tags = new Dictionary<string, string>
                {
                    { "Tier", "Web" },
                    { "Environment", "Lab" },
                    { "CreatedBy", "NSGManager" }
                },
                AllowedInboundPorts = new List<string> { "80", "443" },
                AllowedOutboundPorts = new List<string> { "8080", "8443", "443", "80" }
            },
            new ASGConfiguration
            {
                Name = "asg-appservers",
                Purpose = "AppServers",
                Description = "Servidores de aplicación que procesan lógica de negocio",
                Tags = new Dictionary<string, string>
                {
                    { "Tier", "Application" },
                    { "Environment", "Lab" },
                    { "CreatedBy", "NSGManager" }
                },
                AllowedInboundPorts = new List<string> { "8080", "8443", "8000", "9000" },
                AllowedOutboundPorts = new List<string> { "1433", "3306", "5432", "443", "80" }
            },
            new ASGConfiguration
            {
                Name = "asg-dbservers",
                Purpose = "DBServers",
                Description = "Servidores de base de datos para almacenamiento persistente",
                Tags = new Dictionary<string, string>
                {
                    { "Tier", "Data" },
                    { "Environment", "Lab" },
                    { "CreatedBy", "NSGManager" }
                },
                AllowedInboundPorts = new List<string> { "1433", "3306", "5432", "1521" },
                AllowedOutboundPorts = new List<string> { "443", "80" } // Para actualizaciones y logs
            },
            new ASGConfiguration
            {
                Name = "asg-mgmtservers",
                Purpose = "ManagementServers",
                Description = "Servidores de gestión y administración (Bastion, Jump boxes)",
                Tags = new Dictionary<string, string>
                {
                    { "Tier", "Management" },
                    { "Environment", "Lab" },
                    { "CreatedBy", "NSGManager" }
                },
                AllowedInboundPorts = new List<string> { "22", "3389" },
                AllowedOutboundPorts = new List<string> { "*" } // Acceso completo para administración
            }
        };
    }

    private async Task CreateASGAsync(ResourceGroupResource resourceGroup, ASGConfiguration asgConfig, string location)
    {
        _logger.LogInformation($"📋 Creando ASG: {asgConfig.Name}");
        
        var asgData = new ApplicationSecurityGroupData()
        {
            Location = location,
            Tags = asgConfig.Tags.ToDictionary(t => t.Key, t => t.Value)
        };
        
        // Agregar tags adicionales
        asgData.Tags["Purpose"] = asgConfig.Purpose;
        asgData.Tags["Description"] = asgConfig.Description;
        asgData.Tags["CreatedAt"] = DateTime.UtcNow.ToString("yyyy-MM-dd");
        
        try
        {
            var asgOperation = await resourceGroup.GetApplicationSecurityGroups().CreateOrUpdateAsync(
                WaitUntil.Completed, asgConfig.Name, asgData);
            
            _logger.LogInformation($"✅ ASG creado: {asgConfig.Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Error creando ASG {asgConfig.Name}: {ex.Message}");
            throw;
        }
    }
} 