using System.Text.Json.Serialization;

namespace SecurityAutomation.Models;

/// <summary>
/// Evento de seguridad base
/// </summary>
public class SecurityEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string SourceIP { get; set; } = string.Empty;
    public string TargetResource { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public bool RequiresImmedateAction => Severity == "Critical" || Severity == "High";
}

/// <summary>
/// Evento específico de ataque DDoS
/// </summary>
public class DDoSAttackEvent : SecurityEvent
{
    public string PublicIPAddress { get; set; } = string.Empty;
    public double AttackMagnitude { get; set; }
    public string MitigationStatus { get; set; } = string.Empty;
    public List<string> AttackVectors { get; set; } = new();
    public double PacketsPerSecond { get; set; }
    public double BytesPerSecond { get; set; }
}

/// <summary>
/// Evento de actividad sospechosa en Flow Logs
/// </summary>
public class SuspiciousActivityEvent : SecurityEvent
{
    public string ActivityType { get; set; } = string.Empty; // PortScan, BruteForce, DataExfiltration
    public string DestinationIP { get; set; } = string.Empty;
    public int DestinationPort { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public long FlowCount { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> EvidenceFlows { get; set; } = new();
}

/// <summary>
/// Respuesta a incidente de seguridad
/// </summary>
public class SecurityResponse
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string IncidentId { get; set; } = string.Empty;
    public string ResponseType { get; set; } = string.Empty;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = "InProgress"; // InProgress, Completed, Failed, Cancelled
    public List<ResponseAction> Actions { get; set; } = new();
    public string ExecutedBy { get; set; } = "SecurityAutomation";
    public Dictionary<string, object> Results { get; set; } = new();
}

/// <summary>
/// Acción de respuesta específica
/// </summary>
public class ResponseAction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ActionType { get; set; } = string.Empty; // BlockIP, UpdateNSG, SendNotification, CreateTicket
    public string TargetResource { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsCritical { get; set; }
    public TimeSpan? Duration { get; set; }
}

/// <summary>
/// Análisis de amenaza usando inteligencia artificial
/// </summary>
public class ThreatAnalysisResult
{
    public string EventId { get; set; } = string.Empty;
    public double RiskScore { get; set; } // 0-100
    public string ThreatCategory { get; set; } = string.Empty;
    public string Confidence { get; set; } = string.Empty; // Low, Medium, High
    public List<string> RecommendedActions { get; set; } = new();
    public ThreatIntelligenceData? ThreatIntelligence { get; set; }
    public GeoLocationData? GeoLocation { get; set; }
    public HistoricalActivityData? HistoricalActivity { get; set; }
    public MLPredictionResult? MLPrediction { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Datos de threat intelligence
/// </summary>
public class ThreatIntelligenceData
{
    public string IPAddress { get; set; } = string.Empty;
    public bool IsMalicious { get; set; }
    public List<string> ThreatCategories { get; set; } = new();
    public List<string> Sources { get; set; } = new();
    public double ConfidenceScore { get; set; } // 0-1
    public DateTime LastUpdated { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

/// <summary>
/// Datos de geolocalización
/// </summary>
public class GeoLocationData
{
    public string IPAddress { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string ISP { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public bool IsHighRiskCountry { get; set; }
}

/// <summary>
/// Datos de actividad histórica
/// </summary>
public class HistoricalActivityData
{
    public string IPAddress { get; set; } = string.Empty;
    public int PreviousIncidentCount { get; set; }
    public DateTime? LastIncidentDate { get; set; }
    public List<string> PreviousActivityTypes { get; set; } = new();
    public double AverageActivityIntensity { get; set; }
    public bool IsKnownActor { get; set; }
    public string ActorProfile { get; set; } = string.Empty;
}

/// <summary>
/// Resultado de predicción ML
/// </summary>
public class MLPredictionResult
{
    public string ModelVersion { get; set; } = string.Empty;
    public double ThreatProbability { get; set; } // 0-1
    public string PredictedThreatType { get; set; } = string.Empty;
    public double ModelConfidence { get; set; } // 0-1
    public List<string> FeatureImportance { get; set; } = new();
    public Dictionary<string, double> PredictionScores { get; set; } = new();
}

/// <summary>
/// Notificación de seguridad
/// </summary>
public class SecurityNotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EventId { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty; // Email, Teams, Slack, SMS, Webhook
    public string Priority { get; set; } = string.Empty; // Critical, High, Medium, Low
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<string> Recipients { get; set; } = new();
    public Dictionary<string, object> TemplateData { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public bool IsDelivered { get; set; }
    public string? DeliveryError { get; set; }
}

/// <summary>
/// Configuración de automatización
/// </summary>
public class AutomationRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public List<string> TriggerEventTypes { get; set; } = new();
    public List<string> TriggerSeverities { get; set; } = new();
    public Dictionary<string, object> Conditions { get; set; } = new();
    public List<AutomationAction> Actions { get; set; } = new();
    public int Priority { get; set; } = 100;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModified { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Acción de automatización
/// </summary>
public class AutomationAction
{
    public string ActionType { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public int DelaySeconds { get; set; } = 0;
    public bool RequiresApproval { get; set; } = false;
    public List<string> ApprovalUsers { get; set; } = new();
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Incidente de seguridad correlacionado
/// </summary>
public class CorrelatedIncident
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PrimaryEventId { get; set; } = string.Empty;
    public List<string> RelatedEventIds { get; set; } = new();
    public double CorrelationScore { get; set; } // 0-1
    public List<string> CorrelationFactors { get; set; } = new();
    public string CombinedSeverity { get; set; } = string.Empty;
    public string IncidentType { get; set; } = string.Empty;
    public string Status { get; set; } = "Active"; // Active, Investigating, Resolved, Closed
    public DateTime FirstEventTime { get; set; }
    public DateTime LastEventTime { get; set; }
    public List<ResponseAction> RecommendedActions { get; set; } = new();
    public string AssignedTo { get; set; } = string.Empty;
}

/// <summary>
/// Configuraciones para el sistema
/// </summary>
public class ThreatIntelligenceOptions
{
    public string VirusTotalApiKey { get; set; } = string.Empty;
    public string AbuseIPDBApiKey { get; set; } = string.Empty;
    public int CacheExpirationMinutes { get; set; } = 60;
}

public class SecurityAutomationOptions
{
    public int DefaultBlockDurationHours { get; set; } = 24;
    public int MaxConcurrentResponses { get; set; } = 10;
    public bool EnableMLThreatDetection { get; set; } = true;
}

public class NotificationOptions
{
    public string TeamsWebhookUrl { get; set; } = string.Empty;
    public string SlackWebhookUrl { get; set; } = string.Empty;
    public string EmailSmtpServer { get; set; } = string.Empty;
    public string EmailUsername { get; set; } = string.Empty;
    public string EmailPassword { get; set; } = string.Empty;
}

public class ComplianceOptions
{
    public bool EnableAutoRemediation { get; set; } = true;
    public bool RequireApprovalForCritical { get; set; } = true;
    public int AuditLogRetentionDays { get; set; } = 90;
} 