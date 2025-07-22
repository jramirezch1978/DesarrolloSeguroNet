using NetworkTester.Models;

namespace NetworkTester.Services;

public interface IFlowLogAnalyzer
{
    Task AnalyzeFlowLogsAsync(string storageAccount, string? container = null, DateTime? startTime = null, DateTime? endTime = null, string? outputFile = null);
    Task<FlowLogAnalysisReport> GenerateFlowAnalysisReportAsync(string storageAccount, string container, DateTime startTime, DateTime endTime);
    Task<List<SuspiciousActivity>> DetectSuspiciousActivitiesAsync(string storageAccount, string container, TimeSpan analysisWindow);
}

public class FlowLogAnalysisReport
{
    public string StorageAccount { get; set; } = string.Empty;
    public string Container { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
    
    public FlowLogStatistics Statistics { get; set; } = new();
    public List<TopTalker> TopTalkers { get; set; } = new();
    public List<ProtocolDistribution> ProtocolBreakdown { get; set; } = new();
    public List<TrafficPattern> TrafficPatterns { get; set; } = new();
    public List<SuspiciousActivity> SuspiciousActivities { get; set; } = new();
    public List<SecurityInsight> SecurityInsights { get; set; } = new();
}

public class FlowLogStatistics
{
    public long TotalFlows { get; set; }
    public long AllowedFlows { get; set; }
    public long DeniedFlows { get; set; }
    public long TotalBytes { get; set; }
    public long TotalPackets { get; set; }
    public double AllowedPercentage => TotalFlows > 0 ? (AllowedFlows * 100.0) / TotalFlows : 0;
    public double DeniedPercentage => TotalFlows > 0 ? (DeniedFlows * 100.0) / TotalFlows : 0;
}

public class TopTalker
{
    public string SourceIP { get; set; } = string.Empty;
    public string DestinationIP { get; set; } = string.Empty;
    public long FlowCount { get; set; }
    public long ByteCount { get; set; }
    public long PacketCount { get; set; }
    public List<int> CommonPorts { get; set; } = new();
    public string TrafficType { get; set; } = string.Empty; // Internal, Inbound, Outbound
}

public class ProtocolDistribution
{
    public string Protocol { get; set; } = string.Empty;
    public long FlowCount { get; set; }
    public long ByteCount { get; set; }
    public double Percentage { get; set; }
}

public class TrafficPattern
{
    public string PatternType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public int Frequency { get; set; }
    public List<string> InvolvedIPs { get; set; } = new();
}

public class SuspiciousActivity
{
    public string ActivityType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string SourceIP { get; set; } = string.Empty;
    public string TargetIP { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime FirstDetected { get; set; }
    public DateTime LastDetected { get; set; }
    public int EventCount { get; set; }
    public List<int> TargetPorts { get; set; } = new();
    public string RecommendedAction { get; set; } = string.Empty;
}

public class SecurityInsight
{
    public string InsightType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public List<string> RelatedFlows { get; set; } = new();
    public Dictionary<string, object> Evidence { get; set; } = new();
} 