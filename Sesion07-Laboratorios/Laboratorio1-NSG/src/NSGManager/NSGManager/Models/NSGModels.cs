namespace NSGManager.Models;

/// <summary>
/// Opciones de configuración para NSG Manager
/// </summary>
public class NSGManagerOptions
{
    public string DefaultLocation { get; set; } = "eastus";
    public string[] DefaultServiceTags { get; set; } = Array.Empty<string>();
    public Dictionary<string, int> DefaultPorts { get; set; } = new();
    public bool EnableDetailedLogging { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
}

/// <summary>
/// Opciones para creación de NSGs
/// </summary>
public class NSGCreationOptions
{
    public string ResourceGroupName { get; set; } = string.Empty;
    public string Location { get; set; } = "eastus";
    public string? SubscriptionId { get; set; }
    public string VNetName { get; set; } = "vnet-nsg-lab";
    public bool CreateBasicRules { get; set; } = false;
    public bool CreateAdvancedRules { get; set; } = false;
    public bool EnableApplicationSecurityGroups { get; set; } = false;
    public bool EnableFlowLogs { get; set; } = false;
    public Dictionary<string, object> Tags { get; set; } = new();
}

/// <summary>
/// Opciones para validación de NSGs
/// </summary>
public class ValidationOptions
{
    public string ResourceGroupName { get; set; } = string.Empty;
    public string? SubscriptionId { get; set; }
    public bool DetailedAnalysis { get; set; } = false;
    public string[] RulesToValidate { get; set; } = Array.Empty<string>();
    public SecurityStandard[] ComplianceStandards { get; set; } = Array.Empty<SecurityStandard>();
}

/// <summary>
/// Opciones para generación de reportes
/// </summary>
public class ReportOptions
{
    public string ResourceGroupName { get; set; } = string.Empty;
    public string? SubscriptionId { get; set; }
    public string Format { get; set; } = "console"; // console, json, html, csv
    public string? OutputFile { get; set; }
    public bool IncludeRecommendations { get; set; } = true;
    public bool IncludeComplianceCheck { get; set; } = true;
}

/// <summary>
/// Opciones para limpieza de recursos
/// </summary>
public class CleanupOptions
{
    public string ResourceGroupName { get; set; } = string.Empty;
    public string? SubscriptionId { get; set; }
    public bool DeleteResourceGroup { get; set; } = false;
    public string[] ResourceTypesToDelete { get; set; } = Array.Empty<string>();
    public bool PreserveData { get; set; } = false;
}

/// <summary>
/// Resultados de validación de NSGs
/// </summary>
public class ValidationResults
{
    public int AnalyzedNSGs { get; set; }
    public int TotalRules { get; set; }
    public int ValidRules { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<NSGValidationDetail> Details { get; set; } = new();
    public Dictionary<string, ComplianceResult> ComplianceResults { get; set; } = new();
}

/// <summary>
/// Detalle de validación por NSG
/// </summary>
public class NSGValidationDetail
{
    public string NSGName { get; set; } = string.Empty;
    public string ResourceGroup { get; set; } = string.Empty;
    public int RuleCount { get; set; }
    public List<RuleValidationResult> RuleResults { get; set; } = new();
    public SecurityPosture SecurityPosture { get; set; }
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Resultado de validación por regla
/// </summary>
public class RuleValidationResult
{
    public string RuleName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string Access { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public List<string> Issues { get; set; } = new();
    public RiskLevel RiskLevel { get; set; }
    public string? Recommendation { get; set; }
}

/// <summary>
/// Resultado de compliance por estándar
/// </summary>
public class ComplianceResult
{
    public SecurityStandard Standard { get; set; }
    public bool IsCompliant { get; set; }
    public double ComplianceScore { get; set; }
    public List<string> Violations { get; set; } = new();
    public List<string> Requirements { get; set; } = new();
}

/// <summary>
/// Configuración de regla NSG
/// </summary>
public class NSGRuleConfiguration
{
    public string Name { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string Direction { get; set; } = string.Empty; // Inbound, Outbound
    public string Access { get; set; } = string.Empty; // Allow, Deny
    public string Protocol { get; set; } = string.Empty; // TCP, UDP, ICMP, *
    public string SourcePortRange { get; set; } = "*";
    public string DestinationPortRange { get; set; } = string.Empty;
    public string SourceAddressPrefix { get; set; } = string.Empty;
    public string DestinationAddressPrefix { get; set; } = string.Empty;
    public string[]? SourceApplicationSecurityGroups { get; set; }
    public string[]? DestinationApplicationSecurityGroups { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Configuración de Application Security Group
/// </summary>
public class ASGConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty; // WebServers, AppServers, DBServers, etc.
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> Tags { get; set; } = new();
    public List<string> AllowedInboundPorts { get; set; } = new();
    public List<string> AllowedOutboundPorts { get; set; } = new();
}

/// <summary>
/// Template de NSG predefinido
/// </summary>
public class NSGTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public NetworkTier Tier { get; set; }
    public List<NSGRuleConfiguration> Rules { get; set; } = new();
    public List<string> RequiredASGs { get; set; } = new();
    public SecurityLevel SecurityLevel { get; set; }
}

/// <summary>
/// Información de conectividad entre recursos
/// </summary>
public class ConnectivityInfo
{
    public string SourceResource { get; set; } = string.Empty;
    public string DestinationResource { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool IsAllowed { get; set; }
    public string? BlockingRule { get; set; }
    public List<string> EvaluatedRules { get; set; } = new();
}

/// <summary>
/// Métricas de Flow Logs
/// </summary>
public class FlowLogMetrics
{
    public DateTime Period { get; set; }
    public long TotalFlows { get; set; }
    public long AllowedFlows { get; set; }
    public long DeniedFlows { get; set; }
    public Dictionary<string, long> FlowsByRule { get; set; } = new();
    public Dictionary<string, long> FlowsByProtocol { get; set; } = new();
    public List<SuspiciousActivity> SuspiciousActivities { get; set; } = new();
}

/// <summary>
/// Actividad sospechosa detectada
/// </summary>
public class SuspiciousActivity
{
    public DateTime DetectedAt { get; set; }
    public string SourceIP { get; set; } = string.Empty;
    public string DestinationIP { get; set; } = string.Empty;
    public int DestinationPort { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public ActivityType Type { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public string Description { get; set; } = string.Empty;
}

// Enumeraciones
public enum NetworkTier
{
    Web,
    Application,
    Data,
    Management,
    DMZ
}

public enum SecurityLevel
{
    Basic,
    Standard,
    Enhanced,
    Maximum
}

public enum SecurityPosture
{
    Excellent,
    Good,
    Adequate,
    Poor,
    Critical
}

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum SecurityStandard
{
    PCIDSS,
    HIPAA,
    SOX,
    GDPR,
    NIST,
    CIS,
    ISO27001
}

public enum ActivityType
{
    PortScanning,
    BruteForceAttempt,
    UnusualTraffic,
    SuspiciousIP,
    DataExfiltration,
    DenialOfService
} 