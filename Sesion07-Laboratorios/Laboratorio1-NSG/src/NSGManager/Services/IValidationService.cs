using NSGManager.Models;

namespace NSGManager.Services;

/// <summary>
/// Interfaz para el servicio de validación de configuraciones NSG
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Valida la configuración completa de NSGs
    /// </summary>
    Task<ValidationResults> ValidateNSGConfigurationAsync(ValidationOptions options);
    
    /// <summary>
    /// Valida una regla específica de NSG
    /// </summary>
    Task<RuleValidationResult> ValidateNSGRuleAsync(NSGRuleConfiguration rule);
    
    /// <summary>
    /// Verifica compliance con estándares de seguridad
    /// </summary>
    Task<Dictionary<SecurityStandard, ComplianceResult>> ValidateComplianceAsync(string resourceGroup, SecurityStandard[] standards, string? subscriptionId = null);
    
    /// <summary>
    /// Genera recomendaciones de seguridad
    /// </summary>
    Task<List<string>> GenerateSecurityRecommendationsAsync(string resourceGroup, string? subscriptionId = null);
} 