namespace DDoSMonitor.Models;

/// <summary>
/// Opciones de configuración para el monitor DDoS
/// </summary>
public class DDoSMonitorOptions
{
    public string? SubscriptionId { get; set; }
    public string? ResourceGroup { get; set; }
    public string? PublicIpName { get; set; }
    public int MonitoringIntervalSeconds { get; set; } = 30;
    public int AlertThreshold { get; set; } = 1000;
    public bool EnableDashboard { get; set; } = true;
    public string? LogLevel { get; set; } = "Information";
}

/// <summary>
/// Opciones para una sesión de monitoreo específica
/// </summary>
public class MonitoringOptions
{
    public string? SubscriptionId { get; set; }
    public string? ResourceGroup { get; set; }
    public string? ResourceGroupName { get; set; }
    public string? PublicIpName { get; set; }
    public int IntervalSeconds { get; set; } = 30;
    public int UpdateIntervalSeconds { get; set; } = 30;
    public int AlertThreshold { get; set; } = 1000;
    public bool ShowDashboard { get; set; } = true;
    public bool ContinuousMode { get; set; } = true;
}

/// <summary>
/// Métricas de DDoS Protection
/// </summary>
public class DDoSMetrics
{
    public DateTime Timestamp { get; set; }
    public bool UnderDDoSAttack { get; set; }
    public double PacketsDroppedDDoS { get; set; }
    public double BytesDroppedDDoS { get; set; }
    public double PacketsInDDoS { get; set; }
    public double BytesInDDoS { get; set; }
    public int MaxAttackVectorCount { get; set; }
    public string? PublicIpAddress { get; set; }
    public string? ResourceName { get; set; }
}

/// <summary>
/// Resultado del análisis de ataque
/// </summary>
public class AttackAnalysis
{
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? DateTime.UtcNow.Subtract(StartTime);
    public double MaxPacketsDropped { get; set; }
    public double MaxBytesDropped { get; set; }
    public int MaxVectorCount { get; set; }
    public string? AttackType { get; set; }
    public string? Severity { get; set; }
    public List<DDoSMetrics> Metrics { get; set; } = new();
}

/// <summary>
/// Opciones para generar reportes
/// </summary>
public class ReportOptions
{
    public string? SubscriptionId { get; set; }
    public string? ResourceGroupName { get; set; }
    public string? PublicIpName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string OutputFormat { get; set; } = "json";
    public string Format { get; set; } = "json";
    public string? OutputPath { get; set; }
    public string? OutputFile { get; set; }
    public bool IncludeCharts { get; set; } = false;
}

/// <summary>
/// Opciones para análisis de métricas
/// </summary>
public class AnalysisOptions
{
    public string? SubscriptionId { get; set; }
    public string? ResourceGroupName { get; set; }
    public string? PublicIpName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Detailed { get; set; } = false;
    public bool DetailedAnalysis { get; set; } = false;
}

/// <summary>
/// Opciones para test de carga
/// </summary>
public class LoadTestOptions
{
    public string? SubscriptionId { get; set; }
    public string? ResourceGroupName { get; set; }
    public string? PublicIpName { get; set; }
    public string? TargetUrl { get; set; }
    public int ConcurrentRequests { get; set; } = 100;
    public int Concurrency { get; set; } = 100;
    public int TotalRequests { get; set; } = 10000;
    public int DurationSeconds { get; set; } = 300;
    public string RequestMethod { get; set; } = "GET";
    public bool MonitorDuringTest { get; set; } = true;
    public bool MonitorDDoSMetrics { get; set; } = true;
}

/// <summary>
/// Opciones para simulación de carga
/// </summary>
public class SimulationOptions
{
    public string? TargetUrl { get; set; }
    public int ConcurrentRequests { get; set; } = 100;
    public int TotalRequests { get; set; } = 10000;
    public int DurationSeconds { get; set; } = 300;
    public string RequestMethod { get; set; } = "GET";
} 