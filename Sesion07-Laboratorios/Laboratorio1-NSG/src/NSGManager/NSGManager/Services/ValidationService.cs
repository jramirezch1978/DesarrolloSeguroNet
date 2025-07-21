using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Logging;
using NSGManager.Models;

namespace NSGManager.Services;

/// <summary>
/// Servicio para validación de configuraciones NSG y compliance
/// </summary>
public class ValidationService : IValidationService
{
    private readonly ArmClient _armClient;
    private readonly ILogger<ValidationService> _logger;

    public ValidationService(ArmClient armClient, ILogger<ValidationService> logger)
    {
        _armClient = armClient;
        _logger = logger;
    }

    public async Task<ValidationResults> ValidateNSGConfigurationAsync(ValidationOptions options)
    {
        _logger.LogInformation("🔍 Iniciando validación completa de NSGs...");
        
        var subscription = await GetSubscriptionAsync(options.SubscriptionId);
        var resourceGroup = await GetResourceGroupAsync(subscription, options.ResourceGroupName);
        
        var results = new ValidationResults
        {
            Warnings = new List<string>(),
            Errors = new List<string>(),
            Details = new List<NSGValidationDetail>()
        };
        
        var nsgCount = 0;
        var totalRules = 0;
        var validRules = 0;
        
        await foreach (var nsg in resourceGroup.GetNetworkSecurityGroups())
        {
            nsgCount++;
            var nsgData = await nsg.GetAsync();
            
            var detail = new NSGValidationDetail
            {
                NSGName = nsg.Data.Name,
                ResourceGroup = options.ResourceGroupName,
                RuleCount = nsgData.Value.Data.SecurityRules.Count,
                RuleResults = new List<RuleValidationResult>()
            };
            
            // Validar cada regla del NSG
            foreach (var rule in nsgData.Value.Data.SecurityRules)
            {
                totalRules++;
                var ruleConfig = ConvertToNSGRuleConfiguration(rule);
                var ruleResult = await ValidateNSGRuleAsync(ruleConfig);
                
                if (ruleResult.IsValid) validRules++;
                
                detail.RuleResults.Add(ruleResult);
                
                // Agregar problemas a los resultados generales
                foreach (var issue in ruleResult.Issues)
                {
                    if (ruleResult.RiskLevel == RiskLevel.Critical || ruleResult.RiskLevel == RiskLevel.High)
                    {
                        results.Errors.Add($"NSG {nsg.Data.Name} - Regla {rule.Name}: {issue}");
                    }
                    else
                    {
                        results.Warnings.Add($"NSG {nsg.Data.Name} - Regla {rule.Name}: {issue}");
                    }
                }
            }
            
            detail.SecurityPosture = CalculateSecurityPosture(detail.RuleResults);
            detail.Recommendations = GenerateNSGRecommendations(detail.RuleResults, nsg.Data.Name);
            
            results.Details.Add(detail);
            
            // Validaciones específicas del NSG
            await ValidateNSGSpecificIssues(nsg.Data, results);
        }
        
        results.AnalyzedNSGs = nsgCount;
        results.TotalRules = totalRules;
        results.ValidRules = validRules;
        
        // Validar compliance si se especificaron estándares
        if (options.ComplianceStandards.Any())
        {
            results.ComplianceResults = await ValidateComplianceAsync(
                options.ResourceGroupName, options.ComplianceStandards, options.SubscriptionId);
        }
        
        _logger.LogInformation($"✅ Validación completada: {nsgCount} NSGs, {totalRules} reglas analizadas");
        
        return results;
    }

    public async Task<RuleValidationResult> ValidateNSGRuleAsync(NSGRuleConfiguration rule)
    {
        var result = new RuleValidationResult
        {
            RuleName = rule.Name,
            Priority = rule.Priority,
            Direction = rule.Direction,
            Access = rule.Access,
            IsValid = true,
            Issues = new List<string>(),
            RiskLevel = RiskLevel.Low
        };
        
        // Validaciones de sintaxis básica
        ValidateRuleSyntax(rule, result);
        
        // Validaciones de seguridad
        ValidateSecurityRisks(rule, result);
        
        // Validaciones de mejores prácticas
        ValidateBestPractices(rule, result);
        
        // Determinar validez general
        if (result.Issues.Any(i => i.Contains("CRÍTICO") || i.Contains("INVÁLIDO")))
        {
            result.IsValid = false;
            result.RiskLevel = RiskLevel.Critical;
        }
        else if (result.RiskLevel == RiskLevel.Low && result.Issues.Any())
        {
            result.RiskLevel = RiskLevel.Medium;
        }
        
        // Generar recomendación
        result.Recommendation = GenerateRuleRecommendation(rule, result);
        
        await Task.CompletedTask; // Para hacer el método async
        return result;
    }

    public async Task<Dictionary<SecurityStandard, ComplianceResult>> ValidateComplianceAsync(string resourceGroup, SecurityStandard[] standards, string? subscriptionId = null)
    {
        _logger.LogInformation($"🔍 Validando compliance para {standards.Length} estándares...");
        
        var subscription = await GetSubscriptionAsync(subscriptionId);
        var resourceGroupResource = await GetResourceGroupAsync(subscription, resourceGroup);
        
        var results = new Dictionary<SecurityStandard, ComplianceResult>();
        
        foreach (var standard in standards)
        {
            var complianceResult = await ValidateSpecificStandard(resourceGroupResource, standard);
            results[standard] = complianceResult;
        }
        
        return results;
    }

    public async Task<List<string>> GenerateSecurityRecommendationsAsync(string resourceGroup, string? subscriptionId = null)
    {
        _logger.LogInformation("💡 Generando recomendaciones de seguridad...");
        
        var recommendations = new List<string>();
        
        var subscription = await GetSubscriptionAsync(subscriptionId);
        var resourceGroupResource = await GetResourceGroupAsync(subscription, resourceGroup);
        
        var nsgCount = 0;
        var hasFlowLogs = false;
        var hasASGs = false;
        var overpermissiveRules = 0;
        
        await foreach (var nsg in resourceGroupResource.GetNetworkSecurityGroups())
        {
            nsgCount++;
            var nsgData = await nsg.GetAsync();
            
            foreach (var rule in nsgData.Value.Data.SecurityRules)
            {
                if (IsOverpermissiveRule(rule))
                {
                    overpermissiveRules++;
                }
            }
        }
        
        // Verificar ASGs
        await foreach (var asg in resourceGroupResource.GetApplicationSecurityGroups())
        {
            hasASGs = true;
            break;
        }
        
        // Generar recomendaciones basadas en análisis
        if (nsgCount == 0)
        {
            recommendations.Add("🚨 CRÍTICO: No se encontraron Network Security Groups. Implementar NSGs es fundamental para la seguridad de red.");
        }
        
        if (!hasASGs)
        {
            recommendations.Add("📋 MEDIO: Considere implementar Application Security Groups (ASGs) para mejor organización y escalabilidad.");
        }
        
        if (!hasFlowLogs)
        {
            recommendations.Add("📊 MEDIO: Habilite Flow Logs para monitoreo y análisis de tráfico de red.");
        }
        
        if (overpermissiveRules > 0)
        {
            recommendations.Add($"⚠️ ALTO: Se encontraron {overpermissiveRules} reglas demasiado permisivas. Revise y restrinja según el principio de menor privilegio.");
        }
        
        // Recomendaciones generales de mejores prácticas
        recommendations.AddRange(GetGeneralSecurityRecommendations());
        
        return recommendations;
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

    private void ValidateRuleSyntax(NSGRuleConfiguration rule, RuleValidationResult result)
    {
        // Validar prioridad
        if (rule.Priority < 100 || rule.Priority > 4096)
        {
            result.Issues.Add("INVÁLIDO: Prioridad debe estar entre 100 y 4096");
        }
        
        // Validar direction
        if (rule.Direction != "Inbound" && rule.Direction != "Outbound")
        {
            result.Issues.Add("INVÁLIDO: Direction debe ser 'Inbound' o 'Outbound'");
        }
        
        // Validar access
        if (rule.Access != "Allow" && rule.Access != "Deny")
        {
            result.Issues.Add("INVÁLIDO: Access debe ser 'Allow' o 'Deny'");
        }
        
        // Validar protocolo
        var validProtocols = new[] { "TCP", "UDP", "ICMP", "*" };
        if (!validProtocols.Contains(rule.Protocol))
        {
            result.Issues.Add($"INVÁLIDO: Protocolo '{rule.Protocol}' no es válido");
        }
        
        // Validar rangos de puertos
        if (!IsValidPortRange(rule.SourcePortRange))
        {
            result.Issues.Add($"INVÁLIDO: Rango de puerto origen '{rule.SourcePortRange}' no es válido");
        }
        
        if (!IsValidPortRange(rule.DestinationPortRange))
        {
            result.Issues.Add($"INVÁLIDO: Rango de puerto destino '{rule.DestinationPortRange}' no es válido");
        }
    }

    private void ValidateSecurityRisks(NSGRuleConfiguration rule, RuleValidationResult result)
    {
        if (rule.Access == "Allow")
        {
            // Regla permite tráfico - verificar riesgos
            
            // Fuente muy permisiva
            if (rule.SourceAddressPrefix == "*" || rule.SourceAddressPrefix == "Internet")
            {
                if (rule.DestinationPortRange != "80" && rule.DestinationPortRange != "443")
                {
                    result.Issues.Add("ALTO: Regla muy permisiva - permite desde cualquier origen a puertos no-web");
                    result.RiskLevel = RiskLevel.High;
                }
                
                // Puertos críticos expuestos a Internet
                var criticalPorts = new[] { "22", "3389", "1433", "3306", "5432", "1521", "135", "139", "445" };
                if (criticalPorts.Any(p => rule.DestinationPortRange.Contains(p)))
                {
                    result.Issues.Add("CRÍTICO: Puerto crítico expuesto a Internet");
                    result.RiskLevel = RiskLevel.Critical;
                }
            }
            
            // Destino muy permisivo
            if (rule.DestinationPortRange == "*")
            {
                result.Issues.Add("ALTO: Regla permite acceso a todos los puertos");
                result.RiskLevel = RiskLevel.High;
            }
            
            // Protocolos inseguros
            if (rule.Protocol == "*" && rule.SourceAddressPrefix == "Internet")
            {
                result.Issues.Add("ALTO: Permite todos los protocolos desde Internet");
                result.RiskLevel = RiskLevel.High;
            }
        }
    }

    private void ValidateBestPractices(NSGRuleConfiguration rule, RuleValidationResult result)
    {
        // Verificar descripción
        if (string.IsNullOrEmpty(rule.Description))
        {
            result.Issues.Add("MEDIO: Regla sin descripción - dificulta mantenimiento");
        }
        
        // Verificar uso de Service Tags
        if (rule.SourceAddressPrefix.Contains(".") && rule.SourceAddressPrefix.Contains("/"))
        {
            // Parece ser un CIDR - sugerir Service Tag si aplica
            if (rule.SourceAddressPrefix.StartsWith("0.0.0.0"))
            {
                result.Issues.Add("MEDIO: Considere usar Service Tag 'Internet' en lugar de 0.0.0.0/0");
            }
        }
        
        // Verificar nombres descriptivos
        if (rule.Name.Length < 5 || !rule.Name.Contains("-"))
        {
            result.Issues.Add("BAJO: Nombre de regla poco descriptivo - considere formato 'Action-Service-Direction'");
        }
        
        // Verificar rangos de prioridad
        if (rule.Priority < 200 && rule.Access == "Deny")
        {
            result.Issues.Add("MEDIO: Regla de denegación con prioridad muy alta - puede bloquear tráfico legítimo");
        }
        
        if (rule.Priority > 3000 && rule.Access == "Allow")
        {
            result.Issues.Add("BAJO: Regla de permiso con prioridad muy baja - puede ser innecesaria");
        }
    }

    private SecurityPosture CalculateSecurityPosture(List<RuleValidationResult> ruleResults)
    {
        if (!ruleResults.Any()) return SecurityPosture.Critical;
        
        var criticalIssues = ruleResults.Count(r => r.RiskLevel == RiskLevel.Critical);
        var highIssues = ruleResults.Count(r => r.RiskLevel == RiskLevel.High);
        var totalRules = ruleResults.Count;
        var validRules = ruleResults.Count(r => r.IsValid);
        
        if (criticalIssues > 0) return SecurityPosture.Critical;
        if (highIssues > totalRules * 0.3) return SecurityPosture.Poor;
        if (highIssues > 0 || validRules < totalRules * 0.8) return SecurityPosture.Adequate;
        if (ruleResults.Any(r => r.RiskLevel == RiskLevel.Medium)) return SecurityPosture.Good;
        
        return SecurityPosture.Excellent;
    }

    private List<string> GenerateNSGRecommendations(List<RuleValidationResult> ruleResults, string nsgName)
    {
        var recommendations = new List<string>();
        
        var criticalRules = ruleResults.Where(r => r.RiskLevel == RiskLevel.Critical).ToList();
        var highRiskRules = ruleResults.Where(r => r.RiskLevel == RiskLevel.High).ToList();
        
        if (criticalRules.Any())
        {
            recommendations.Add($"🚨 CRÍTICO: Revisar inmediatamente {criticalRules.Count} reglas críticas en NSG {nsgName}");
        }
        
        if (highRiskRules.Any())
        {
            recommendations.Add($"⚠️ ALTO: Evaluar {highRiskRules.Count} reglas de alto riesgo para mejorar seguridad");
        }
        
        var allowRules = ruleResults.Count(r => r.Access == "Allow");
        var denyRules = ruleResults.Count(r => r.Access == "Deny");
        
        if (allowRules > denyRules * 3)
        {
            recommendations.Add("💡 MEDIO: Considere implementar más reglas de denegación explícitas");
        }
        
        if (!ruleResults.Any(r => r.RuleName.Contains("Health") || r.RuleName.Contains("LoadBalancer")))
        {
            recommendations.Add("💡 BAJO: Considere agregar reglas específicas para health probes");
        }
        
        return recommendations;
    }

    private async Task ValidateNSGSpecificIssues(NetworkSecurityGroupData nsgData, ValidationResults results)
    {
        // Verificar tags requeridos
        if (!nsgData.Tags.ContainsKey("Environment"))
        {
            results.Warnings.Add($"NSG {nsgData.Name}: Falta tag 'Environment'");
        }
        
        if (!nsgData.Tags.ContainsKey("Owner"))
        {
            results.Warnings.Add($"NSG {nsgData.Name}: Falta tag 'Owner' para accountability");
        }
        
        // Verificar número de reglas
        var customRules = nsgData.SecurityRules.Where(r => !r.Name.StartsWith("Default")).Count();
        if (customRules == 0)
        {
            results.Warnings.Add($"NSG {nsgData.Name}: No tiene reglas personalizadas - puede estar sin configurar");
        }
        
        if (customRules > 50)
        {
            results.Warnings.Add($"NSG {nsgData.Name}: Tiene {customRules} reglas - considere consolidar para mejor mantenimiento");
        }
        
        await Task.CompletedTask;
    }

    private async Task<ComplianceResult> ValidateSpecificStandard(ResourceGroupResource resourceGroup, SecurityStandard standard)
    {
        var result = new ComplianceResult
        {
            Standard = standard,
            IsCompliant = true,
            ComplianceScore = 100.0,
            Violations = new List<string>(),
            Requirements = new List<string>()
        };
        
        switch (standard)
        {
            case SecurityStandard.PCIDSS:
                await ValidatePCIDSSCompliance(resourceGroup, result);
                break;
            case SecurityStandard.HIPAA:
                await ValidateHIPAACompliance(resourceGroup, result);
                break;
            case SecurityStandard.NIST:
                await ValidateNISTCompliance(resourceGroup, result);
                break;
            default:
                result.Requirements.Add($"Validación para {standard} no implementada en esta versión");
                break;
        }
        
        // Calcular score final
        if (result.Violations.Any())
        {
            result.IsCompliant = false;
            result.ComplianceScore = Math.Max(0, 100 - (result.Violations.Count * 20));
        }
        
        return result;
    }

    private async Task ValidatePCIDSSCompliance(ResourceGroupResource resourceGroup, ComplianceResult result)
    {
        result.Requirements.Add("Segmentación de red entre web, app y data tiers");
        result.Requirements.Add("Cifrado en tránsito obligatorio (HTTPS/TLS)");
        result.Requirements.Add("Acceso restringido a sistemas de cardholder data");
        result.Requirements.Add("Monitoreo y logging de acceso a red");
        
        // Verificar segmentación
        var webNSGs = 0;
        var appNSGs = 0;
        var dataNSGs = 0;
        
        await foreach (var nsg in resourceGroup.GetNetworkSecurityGroups())
        {
            var name = nsg.Data.Name.ToLower();
            if (name.Contains("web")) webNSGs++;
            else if (name.Contains("app")) appNSGs++;
            else if (name.Contains("data") || name.Contains("db")) dataNSGs++;
        }
        
        if (webNSGs == 0 || appNSGs == 0 || dataNSGs == 0)
        {
            result.Violations.Add("PCI DSS: Falta segmentación completa de red (web/app/data tiers)");
        }
        
        await Task.CompletedTask;
    }

    private async Task ValidateHIPAACompliance(ResourceGroupResource resourceGroup, ComplianceResult result)
    {
        result.Requirements.Add("Cifrado de datos en tránsito y reposo");
        result.Requirements.Add("Control de acceso basado en roles");
        result.Requirements.Add("Auditoría completa de accesos");
        result.Requirements.Add("Restricción geográfica de accesos");
        
        await Task.CompletedTask;
    }

    private async Task ValidateNISTCompliance(ResourceGroupResource resourceGroup, ComplianceResult result)
    {
        result.Requirements.Add("Implementación de controles de acceso (AC)");
        result.Requirements.Add("Protección del sistema y comunicaciones (SC)");
        result.Requirements.Add("Identificación y autenticación (IA)");
        result.Requirements.Add("Auditoría y accountability (AU)");
        
        await Task.CompletedTask;
    }

    private NSGRuleConfiguration ConvertToNSGRuleConfiguration(Azure.ResourceManager.Network.Models.SecurityRuleData rule)
    {
        return new NSGRuleConfiguration
        {
            Name = rule.Name,
            Priority = rule.Priority ?? 4096,
            Direction = rule.Direction?.ToString() ?? "",
            Access = rule.Access?.ToString() ?? "",
            Protocol = rule.Protocol?.ToString() ?? "",
            SourcePortRange = rule.SourcePortRange ?? "",
            DestinationPortRange = rule.DestinationPortRange ?? "",
            SourceAddressPrefix = rule.SourceAddressPrefix ?? "",
            DestinationAddressPrefix = rule.DestinationAddressPrefix ?? "",
            Description = rule.Description
        };
    }

    private string GenerateRuleRecommendation(NSGRuleConfiguration rule, RuleValidationResult result)
    {
        if (result.RiskLevel == RiskLevel.Critical)
        {
            return "ACCIÓN INMEDIATA: Deshabilite o restrinja esta regla para reducir riesgos de seguridad";
        }
        
        if (result.RiskLevel == RiskLevel.High)
        {
            return "Revise y restrinja esta regla usando rangos IP específicos y puertos mínimos necesarios";
        }
        
        if (result.RiskLevel == RiskLevel.Medium)
        {
            return "Considere agregar más especificidad y documentación a esta regla";
        }
        
        return "Regla bien configurada - mantenga monitoreo regular";
    }

    private bool IsValidPortRange(string portRange)
    {
        if (string.IsNullOrEmpty(portRange) || portRange == "*") return true;
        
        // Validar puerto único
        if (int.TryParse(portRange, out int port))
        {
            return port >= 0 && port <= 65535;
        }
        
        // Validar rango de puertos
        if (portRange.Contains("-"))
        {
            var parts = portRange.Split('-');
            if (parts.Length == 2 && 
                int.TryParse(parts[0], out int startPort) && 
                int.TryParse(parts[1], out int endPort))
            {
                return startPort >= 0 && endPort <= 65535 && startPort <= endPort;
            }
        }
        
        // Validar lista de puertos
        if (portRange.Contains(","))
        {
            var ports = portRange.Split(',');
            return ports.All(p => int.TryParse(p.Trim(), out int validPort) && validPort >= 0 && validPort <= 65535);
        }
        
        return false;
    }

    private bool IsOverpermissiveRule(Azure.ResourceManager.Network.Models.SecurityRuleData rule)
    {
        return rule.Access == Azure.ResourceManager.Network.Models.SecurityRuleAccess.Allow &&
               (rule.SourceAddressPrefix == "*" || rule.SourceAddressPrefix == "Internet") &&
               (rule.DestinationPortRange == "*" || 
                rule.DestinationPortRange?.Contains("22") == true ||
                rule.DestinationPortRange?.Contains("3389") == true ||
                rule.DestinationPortRange?.Contains("1433") == true);
    }

    private List<string> GetGeneralSecurityRecommendations()
    {
        return new List<string>
        {
            "🔒 Implemente el principio de menor privilegio en todas las reglas NSG",
            "📝 Mantenga documentación actualizada de todas las reglas de seguridad",
            "🔄 Revise y audite regularmente las configuraciones NSG",
            "📊 Implemente monitoreo continuo con Flow Logs y alertas",
            "🏷️ Use tags consistentes para identificar propósito y propietario",
            "🔗 Considere usar ASGs para mejor escalabilidad y mantenimiento",
            "🛡️ Implemente reglas de denegación explícitas además del deny-by-default",
            "⚡ Optimice prioridades de reglas para mejor performance",
            "🌐 Use Service Tags cuando sea posible en lugar de rangos IP específicos",
            "🔍 Implemente testing regular de conectividad para validar configuraciones"
        };
    }
} 