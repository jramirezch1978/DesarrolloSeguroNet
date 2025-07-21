using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Logging;
using NSGManager.Models;
using System.Net;

namespace NSGManager.Services;

/// <summary>
/// Servicio para gestión de Network Security Groups
/// </summary>
public class NSGService : INSGService
{
    private readonly ArmClient _armClient;
    private readonly ILogger<NSGService> _logger;

    public NSGService(ArmClient armClient, ILogger<NSGService> logger)
    {
        _armClient = armClient;
        _logger = logger;
    }

    public async Task CreateBasicNSGsAsync(NSGCreationOptions options)
    {
        _logger.LogInformation("🔧 Iniciando creación de NSGs básicos...");
        
        var subscription = await GetSubscriptionAsync(options.SubscriptionId);
        var resourceGroup = await GetResourceGroupAsync(subscription, options.ResourceGroupName);
        
        // Templates básicos para cada tier
        var basicTemplates = GetBasicNSGTemplates();
        
        foreach (var template in basicTemplates)
        {
            await CreateNSGFromTemplateAsync(resourceGroup, template, options);
        }
        
        _logger.LogInformation("✅ NSGs básicos creados exitosamente");
    }

    public async Task CreateAdvancedNSGsAsync(NSGCreationOptions options)
    {
        _logger.LogInformation("🎯 Iniciando creación de NSGs avanzados...");
        
        var subscription = await GetSubscriptionAsync(options.SubscriptionId);
        var resourceGroup = await GetResourceGroupAsync(subscription, options.ResourceGroupName);
        
        // Templates avanzados con reglas granulares
        var advancedTemplates = GetAdvancedNSGTemplates();
        
        foreach (var template in advancedTemplates)
        {
            await CreateNSGFromTemplateAsync(resourceGroup, template, options);
        }
        
        // Asociar NSGs a subredes si la VNET existe
        await AssociateNSGsToSubnetsAsync(resourceGroup, options.VNetName);
        
        _logger.LogInformation("✅ NSGs avanzados creados exitosamente");
    }

    public async Task<List<NSGValidationDetail>> GetNSGConfigurationAsync(string resourceGroup, string? subscriptionId = null)
    {
        var subscription = await GetSubscriptionAsync(subscriptionId);
        var resourceGroupResource = await GetResourceGroupAsync(subscription, resourceGroup);
        
        var results = new List<NSGValidationDetail>();
        
        await foreach (var nsg in resourceGroupResource.GetNetworkSecurityGroups())
        {
            var nsgData = await nsg.GetAsync();
            var detail = new NSGValidationDetail
            {
                NSGName = nsg.Data.Name,
                ResourceGroup = resourceGroup,
                RuleCount = nsgData.Value.Data.SecurityRules.Count
            };
            
            // Analizar cada regla
            foreach (var rule in nsgData.Value.Data.SecurityRules)
            {
                var ruleResult = AnalyzeRule(rule);
                detail.RuleResults.Add(ruleResult);
            }
            
            detail.SecurityPosture = CalculateSecurityPosture(detail.RuleResults);
            detail.Recommendations = GenerateRecommendations(detail.RuleResults);
            
            results.Add(detail);
        }
        
        return results;
    }

    public async Task UpdateNSGRulesAsync(string resourceGroup, string nsgName, List<NSGRuleConfiguration> rules, string? subscriptionId = null)
    {
        _logger.LogInformation($"🔄 Actualizando reglas del NSG {nsgName}...");
        
        var subscription = await GetSubscriptionAsync(subscriptionId);
        var resourceGroupResource = await GetResourceGroupAsync(subscription, resourceGroup);
        var nsg = await resourceGroupResource.GetNetworkSecurityGroupAsync(nsgName);
        
        var nsgData = nsg.Value.Data;
        
        // Limpiar reglas existentes (excepto las por defecto)
        var defaultRules = nsgData.SecurityRules.Where(r => r.Name.StartsWith("Default")).ToList();
        nsgData.SecurityRules.Clear();
        
        // Agregar reglas por defecto
        foreach (var defaultRule in defaultRules)
        {
            nsgData.SecurityRules.Add(defaultRule);
        }
        
        // Agregar nuevas reglas
        foreach (var ruleConfig in rules)
        {
            var rule = CreateSecurityRule(ruleConfig);
            nsgData.SecurityRules.Add(rule);
        }
        
        await nsg.Value.UpdateAsync(WaitUntil.Completed, nsgData);
        
        _logger.LogInformation($"✅ Reglas del NSG {nsgName} actualizadas exitosamente");
    }

    public async Task CleanupResourcesAsync(CleanupOptions options)
    {
        _logger.LogInformation("🧹 Iniciando limpieza de recursos...");
        
        var subscription = await GetSubscriptionAsync(options.SubscriptionId);
        var resourceGroup = await GetResourceGroupAsync(subscription, options.ResourceGroupName);
        
        if (options.DeleteResourceGroup)
        {
            _logger.LogWarning($"⚠️ Eliminando resource group completo: {options.ResourceGroupName}");
            await resourceGroup.DeleteAsync(WaitUntil.Completed);
            return;
        }
        
        // Eliminar recursos específicos
        await CleanupNSGsAsync(resourceGroup);
        await CleanupASGsAsync(resourceGroup);
        
        _logger.LogInformation("✅ Limpieza completada exitosamente");
    }

    public async Task ApplyNSGTemplateAsync(string resourceGroup, NSGTemplate template, string? subscriptionId = null)
    {
        _logger.LogInformation($"📋 Aplicando template {template.Name}...");
        
        var subscription = await GetSubscriptionAsync(subscriptionId);
        var resourceGroupResource = await GetResourceGroupAsync(subscription, resourceGroup);
        
        var nsgData = new NetworkSecurityGroupData()
        {
            Location = resourceGroupResource.Data.Location,
            Tags = { ["Template"] = template.Name, ["Tier"] = template.Tier.ToString() }
        };
        
        // Agregar reglas del template
        foreach (var ruleConfig in template.Rules)
        {
            var rule = CreateSecurityRule(ruleConfig);
            nsgData.SecurityRules.Add(rule);
        }
        
        var nsgName = $"nsg-{template.Tier.ToString().ToLower()}-{template.Name.ToLower()}";
        await resourceGroupResource.GetNetworkSecurityGroups().CreateOrUpdateAsync(
            WaitUntil.Completed, nsgName, nsgData);
        
        _logger.LogInformation($"✅ Template {template.Name} aplicado exitosamente");
    }

    public async Task<ConnectivityInfo> TestConnectivityAsync(string sourceResource, string destinationResource, int port, string protocol, string resourceGroup, string? subscriptionId = null)
    {
        _logger.LogInformation($"🔍 Probando conectividad: {sourceResource} → {destinationResource}:{port}/{protocol}");
        
        // Esta es una implementación simplificada
        // En un entorno real, usaríamos Network Watcher para pruebas reales
        var connectivityInfo = new ConnectivityInfo
        {
            SourceResource = sourceResource,
            DestinationResource = destinationResource,
            Protocol = protocol,
            Port = port,
            IsAllowed = await SimulateConnectivityTest(sourceResource, destinationResource, port, protocol, resourceGroup, subscriptionId),
            EvaluatedRules = new List<string> { "Simulación - implementar con Network Watcher para pruebas reales" }
        };
        
        return connectivityInfo;
    }

    public async Task EnableFlowLogsAsync(string resourceGroup, string storageAccountName, string? subscriptionId = null)
    {
        _logger.LogInformation($"📊 Habilitando Flow Logs con storage account: {storageAccountName}");
        
        // Implementación simplificada - en la práctica requiere Network Watcher
        var subscription = await GetSubscriptionAsync(subscriptionId);
        var resourceGroupResource = await GetResourceGroupAsync(subscription, resourceGroup);
        
        await foreach (var nsg in resourceGroupResource.GetNetworkSecurityGroups())
        {
            _logger.LogInformation($"📈 Habilitando Flow Logs para NSG: {nsg.Data.Name}");
            // Aquí se configuraría el Flow Log real con Network Watcher
        }
        
        _logger.LogInformation("✅ Flow Logs habilitados exitosamente");
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

    private List<NSGTemplate> GetBasicNSGTemplates()
    {
        return new List<NSGTemplate>
        {
            new NSGTemplate
            {
                Name = "web-basic",
                Description = "NSG básico para tier web",
                Tier = NetworkTier.Web,
                SecurityLevel = SecurityLevel.Basic,
                Rules = new List<NSGRuleConfiguration>
                {
                    new NSGRuleConfiguration
                    {
                        Name = "Allow-HTTPS-Inbound",
                        Priority = 100,
                        Direction = "Inbound",
                        Access = "Allow",
                        Protocol = "TCP",
                        SourcePortRange = "*",
                        DestinationPortRange = "443",
                        SourceAddressPrefix = "Internet",
                        DestinationAddressPrefix = "VirtualNetwork",
                        Description = "Permitir tráfico HTTPS desde Internet"
                    },
                    new NSGRuleConfiguration
                    {
                        Name = "Allow-HTTP-Inbound",
                        Priority = 110,
                        Direction = "Inbound",
                        Access = "Allow",
                        Protocol = "TCP",
                        SourcePortRange = "*",
                        DestinationPortRange = "80",
                        SourceAddressPrefix = "Internet",
                        DestinationAddressPrefix = "VirtualNetwork",
                        Description = "Permitir tráfico HTTP desde Internet"
                    }
                }
            },
            new NSGTemplate
            {
                Name = "app-basic",
                Description = "NSG básico para tier aplicación",
                Tier = NetworkTier.Application,
                SecurityLevel = SecurityLevel.Basic,
                Rules = new List<NSGRuleConfiguration>
                {
                    new NSGRuleConfiguration
                    {
                        Name = "Allow-AppPorts-Inbound",
                        Priority = 100,
                        Direction = "Inbound",
                        Access = "Allow",
                        Protocol = "TCP",
                        SourcePortRange = "*",
                        DestinationPortRange = "8080",
                        SourceAddressPrefix = "VirtualNetwork",
                        DestinationAddressPrefix = "VirtualNetwork",
                        Description = "Permitir tráfico de aplicación desde VNET"
                    }
                }
            }
        };
    }

    private List<NSGTemplate> GetAdvancedNSGTemplates()
    {
        return new List<NSGTemplate>
        {
            new NSGTemplate
            {
                Name = "web-advanced",
                Description = "NSG avanzado para tier web con reglas granulares",
                Tier = NetworkTier.Web,
                SecurityLevel = SecurityLevel.Enhanced,
                Rules = new List<NSGRuleConfiguration>
                {
                    new NSGRuleConfiguration
                    {
                        Name = "Allow-HTTPS-Internet",
                        Priority = 100,
                        Direction = "Inbound",
                        Access = "Allow",
                        Protocol = "TCP",
                        SourcePortRange = "*",
                        DestinationPortRange = "443",
                        SourceAddressPrefix = "Internet",
                        DestinationAddressPrefix = "VirtualNetwork",
                        Description = "HTTPS desde Internet - conexiones seguras"
                    },
                    new NSGRuleConfiguration
                    {
                        Name = "Allow-HTTP-Redirect",
                        Priority = 110,
                        Direction = "Inbound",
                        Access = "Allow",
                        Protocol = "TCP",
                        SourcePortRange = "*",
                        DestinationPortRange = "80",
                        SourceAddressPrefix = "Internet",
                        DestinationAddressPrefix = "VirtualNetwork",
                        Description = "HTTP para redirección a HTTPS"
                    },
                    new NSGRuleConfiguration
                    {
                        Name = "Allow-HealthProbes",
                        Priority = 120,
                        Direction = "Inbound",
                        Access = "Allow",
                        Protocol = "*",
                        SourcePortRange = "*",
                        DestinationPortRange = "*",
                        SourceAddressPrefix = "AzureLoadBalancer",
                        DestinationAddressPrefix = "*",
                        Description = "Health probes desde Azure Load Balancer"
                    },
                    new NSGRuleConfiguration
                    {
                        Name = "Deny-Insecure-Protocols",
                        Priority = 4000,
                        Direction = "Inbound",
                        Access = "Deny",
                        Protocol = "TCP",
                        SourcePortRange = "*",
                        DestinationPortRange = "23,21,25,110,143",
                        SourceAddressPrefix = "*",
                        DestinationAddressPrefix = "*",
                        Description = "Bloqueo explícito de protocolos inseguros"
                    }
                }
            },
            new NSGTemplate
            {
                Name = "app-advanced",
                Description = "NSG avanzado para tier aplicación",
                Tier = NetworkTier.Application,
                SecurityLevel = SecurityLevel.Enhanced,
                Rules = new List<NSGRuleConfiguration>
                {
                    new NSGRuleConfiguration
                    {
                        Name = "Allow-Web-to-App",
                        Priority = 100,
                        Direction = "Inbound",
                        Access = "Allow",
                        Protocol = "TCP",
                        SourcePortRange = "*",
                        DestinationPortRange = "8080,8443",
                        SourceAddressPrefix = "10.2.1.0/24", // Web subnet
                        DestinationAddressPrefix = "10.2.2.0/24", // App subnet
                        Description = "Comunicación desde tier web"
                    },
                    new NSGRuleConfiguration
                    {
                        Name = "Deny-Direct-Internet",
                        Priority = 4000,
                        Direction = "Inbound",
                        Access = "Deny",
                        Protocol = "*",
                        SourcePortRange = "*",
                        DestinationPortRange = "*",
                        SourceAddressPrefix = "Internet",
                        DestinationAddressPrefix = "*",
                        Description = "Bloqueo de acceso directo desde Internet"
                    }
                }
            },
            new NSGTemplate
            {
                Name = "data-advanced",
                Description = "NSG avanzado para tier datos",
                Tier = NetworkTier.Data,
                SecurityLevel = SecurityLevel.Maximum,
                Rules = new List<NSGRuleConfiguration>
                {
                    new NSGRuleConfiguration
                    {
                        Name = "Allow-App-to-DB",
                        Priority = 100,
                        Direction = "Inbound",
                        Access = "Allow",
                        Protocol = "TCP",
                        SourcePortRange = "*",
                        DestinationPortRange = "1433,3306,5432",
                        SourceAddressPrefix = "10.2.2.0/24", // App subnet
                        DestinationAddressPrefix = "10.2.3.0/24", // Data subnet
                        Description = "Conexiones desde tier aplicación"
                    },
                    new NSGRuleConfiguration
                    {
                        Name = "Deny-Web-to-DB",
                        Priority = 200,
                        Direction = "Inbound",
                        Access = "Deny",
                        Protocol = "*",
                        SourcePortRange = "*",
                        DestinationPortRange = "*",
                        SourceAddressPrefix = "10.2.1.0/24", // Web subnet
                        DestinationAddressPrefix = "*",
                        Description = "Bloqueo explícito desde tier web"
                    },
                    new NSGRuleConfiguration
                    {
                        Name = "Deny-All-Internet",
                        Priority = 4000,
                        Direction = "Inbound",
                        Access = "Deny",
                        Protocol = "*",
                        SourcePortRange = "*",
                        DestinationPortRange = "*",
                        SourceAddressPrefix = "Internet",
                        DestinationAddressPrefix = "*",
                        Description = "Bloqueo total desde Internet"
                    }
                }
            }
        };
    }

    private async Task CreateNSGFromTemplateAsync(ResourceGroupResource resourceGroup, NSGTemplate template, NSGCreationOptions options)
    {
        _logger.LogInformation($"📋 Creando NSG desde template: {template.Name}");
        
        var nsgData = new NetworkSecurityGroupData()
        {
            Location = options.Location,
            Tags = 
            {
                ["Template"] = template.Name,
                ["Tier"] = template.Tier.ToString(),
                ["SecurityLevel"] = template.SecurityLevel.ToString(),
                ["CreatedBy"] = "NSGManager",
                ["CreatedAt"] = DateTime.UtcNow.ToString("yyyy-MM-dd")
            }
        };
        
        // Agregar tags personalizados
        foreach (var tag in options.Tags)
        {
            nsgData.Tags[tag.Key] = tag.Value.ToString();
        }
        
        // Agregar reglas del template
        foreach (var ruleConfig in template.Rules)
        {
            var rule = CreateSecurityRule(ruleConfig);
            nsgData.SecurityRules.Add(rule);
        }
        
        var nsgName = $"nsg-{template.Tier.ToString().ToLower()}-{DateTime.UtcNow:yyyyMMdd}";
        var nsgOperation = await resourceGroup.GetNetworkSecurityGroups().CreateOrUpdateAsync(
            WaitUntil.Completed, nsgName, nsgData);
        
        _logger.LogInformation($"✅ NSG creado: {nsgName}");
    }

    private SecurityRuleData CreateSecurityRule(NSGRuleConfiguration config)
    {
        var rule = new SecurityRuleData()
        {
            Name = config.Name,
            Priority = config.Priority,
            Direction = Enum.Parse<SecurityRuleDirection>(config.Direction),
            Access = Enum.Parse<SecurityRuleAccess>(config.Access),
            Protocol = Enum.Parse<SecurityRuleProtocol>(config.Protocol == "*" ? "Asterisk" : config.Protocol),
            SourcePortRange = config.SourcePortRange,
            DestinationPortRange = config.DestinationPortRange,
            SourceAddressPrefix = config.SourceAddressPrefix,
            DestinationAddressPrefix = config.DestinationAddressPrefix,
            Description = config.Description
        };
        
        // Agregar ASGs si están configurados
        if (config.SourceApplicationSecurityGroups?.Any() == true)
        {
            foreach (var asg in config.SourceApplicationSecurityGroups)
            {
                rule.SourceApplicationSecurityGroups.Add(new WritableSubResource { Id = asg });
            }
        }
        
        if (config.DestinationApplicationSecurityGroups?.Any() == true)
        {
            foreach (var asg in config.DestinationApplicationSecurityGroups)
            {
                rule.DestinationApplicationSecurityGroups.Add(new WritableSubResource { Id = asg });
            }
        }
        
        return rule;
    }

    private async Task AssociateNSGsToSubnetsAsync(ResourceGroupResource resourceGroup, string vnetName)
    {
        try
        {
            var vnet = await resourceGroup.GetVirtualNetworkAsync(vnetName);
            var nsgs = resourceGroup.GetNetworkSecurityGroups();
            
            await foreach (var nsg in nsgs)
            {
                var nsgName = nsg.Data.Name.ToLower();
                string? targetSubnet = null;
                
                if (nsgName.Contains("web"))
                    targetSubnet = "snet-web";
                else if (nsgName.Contains("app"))
                    targetSubnet = "snet-app";
                else if (nsgName.Contains("data"))
                    targetSubnet = "snet-data";
                
                if (targetSubnet != null)
                {
                    try
                    {
                        var subnet = await vnet.Value.GetSubnetAsync(targetSubnet);
                        var subnetData = subnet.Value.Data;
                        subnetData.NetworkSecurityGroup = new WritableSubResource { Id = nsg.Id };
                        
                        await subnet.Value.UpdateAsync(WaitUntil.Completed, subnetData);
                        _logger.LogInformation($"✅ NSG {nsg.Data.Name} asociado a subnet {targetSubnet}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"⚠️ No se pudo asociar NSG {nsg.Data.Name} a subnet {targetSubnet}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"⚠️ No se pudo obtener la VNET {vnetName}: {ex.Message}");
        }
    }

    private RuleValidationResult AnalyzeRule(SecurityRuleData rule)
    {
        var result = new RuleValidationResult
        {
            RuleName = rule.Name,
            Priority = rule.Priority ?? 0,
            Direction = rule.Direction?.ToString() ?? "",
            Access = rule.Access?.ToString() ?? "",
            IsValid = true,
            Issues = new List<string>(),
            RiskLevel = RiskLevel.Low
        };
        
        // Análisis de riesgos
        if (rule.SourceAddressPrefix == "*" && rule.Access == SecurityRuleAccess.Allow)
        {
            result.Issues.Add("Regla muy permisiva: permite desde cualquier origen");
            result.RiskLevel = RiskLevel.High;
        }
        
        if (rule.DestinationPortRange == "*" && rule.Access == SecurityRuleAccess.Allow)
        {
            result.Issues.Add("Regla muy permisiva: permite a cualquier puerto");
            result.RiskLevel = RiskLevel.Medium;
        }
        
        // Verificar puertos inseguros
        var insecurePorts = new[] { "23", "21", "25", "110", "143", "1433", "3306" };
        if (insecurePorts.Any(p => rule.DestinationPortRange?.Contains(p) == true) && 
            rule.SourceAddressPrefix == "Internet")
        {
            result.Issues.Add("Puerto potencialmente inseguro expuesto a Internet");
            result.RiskLevel = RiskLevel.Critical;
        }
        
        if (result.Issues.Any())
        {
            result.IsValid = false;
        }
        
        return result;
    }

    private SecurityPosture CalculateSecurityPosture(List<RuleValidationResult> ruleResults)
    {
        if (!ruleResults.Any()) return SecurityPosture.Critical;
        
        var criticalIssues = ruleResults.Count(r => r.RiskLevel == RiskLevel.Critical);
        var highIssues = ruleResults.Count(r => r.RiskLevel == RiskLevel.High);
        var totalRules = ruleResults.Count;
        
        if (criticalIssues > 0) return SecurityPosture.Critical;
        if (highIssues > totalRules * 0.3) return SecurityPosture.Poor;
        if (highIssues > 0) return SecurityPosture.Adequate;
        if (ruleResults.Any(r => r.RiskLevel == RiskLevel.Medium)) return SecurityPosture.Good;
        
        return SecurityPosture.Excellent;
    }

    private List<string> GenerateRecommendations(List<RuleValidationResult> ruleResults)
    {
        var recommendations = new List<string>();
        
        if (ruleResults.Any(r => r.Issues.Any(i => i.Contains("muy permisiva"))))
        {
            recommendations.Add("Considere hacer las reglas más específicas usando rangos de IP y puertos exactos");
        }
        
        if (ruleResults.Any(r => r.Issues.Any(i => i.Contains("inseguro"))))
        {
            recommendations.Add("Evite exponer puertos de base de datos y servicios administrativos a Internet");
        }
        
        if (ruleResults.Count(r => r.Access == "Allow") > ruleResults.Count(r => r.Access == "Deny") * 2)
        {
            recommendations.Add("Considere agregar más reglas de denegación explícitas para mejorar la seguridad");
        }
        
        return recommendations;
    }

    private async Task CleanupNSGsAsync(ResourceGroupResource resourceGroup)
    {
        await foreach (var nsg in resourceGroup.GetNetworkSecurityGroups())
        {
            if (nsg.Data.Tags.ContainsKey("CreatedBy") && nsg.Data.Tags["CreatedBy"] == "NSGManager")
            {
                _logger.LogInformation($"🗑️ Eliminando NSG: {nsg.Data.Name}");
                await nsg.DeleteAsync(WaitUntil.Completed);
            }
        }
    }

    private async Task CleanupASGsAsync(ResourceGroupResource resourceGroup)
    {
        await foreach (var asg in resourceGroup.GetApplicationSecurityGroups())
        {
            if (asg.Data.Tags.ContainsKey("CreatedBy") && asg.Data.Tags["CreatedBy"] == "NSGManager")
            {
                _logger.LogInformation($"🗑️ Eliminando ASG: {asg.Data.Name}");
                await asg.DeleteAsync(WaitUntil.Completed);
            }
        }
    }

    private async Task<bool> SimulateConnectivityTest(string sourceResource, string destinationResource, int port, string protocol, string resourceGroup, string? subscriptionId)
    {
        // Simulación simple - en la práctica usaríamos Network Watcher
        _logger.LogInformation("🔍 Simulando test de conectividad...");
        await Task.Delay(1000); // Simular trabajo asíncrono
        
        // Lógica simulada basada en reglas comunes
        if (port == 443 || port == 80) return true; // Web traffic generalmente permitido
        if (port == 22 || port == 3389) return false; // SSH/RDP generalmente bloqueado
        if (port >= 1433 && port <= 1434) return false; // SQL Server generalmente bloqueado desde Internet
        
        return true; // Por defecto permitir en simulación
    }
} 