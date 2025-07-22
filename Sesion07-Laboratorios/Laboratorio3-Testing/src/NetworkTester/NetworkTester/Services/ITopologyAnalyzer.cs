using NetworkTester.Models;

namespace NetworkTester.Services;

public interface ITopologyAnalyzer
{
    Task AnalyzeTopologyAsync(string resourceGroup, string? outputFormat = "json");
    Task<NetworkTopologyReport> GenerateTopologyReportAsync(string resourceGroup);
    Task<string> GenerateTopologyVisualizationAsync(string resourceGroup, string format = "svg");
}

public class NetworkTopologyReport
{
    public string ResourceGroup { get; set; } = string.Empty;
    public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
    public List<TopologyNode> Nodes { get; set; } = new();
    public List<TopologyConnection> Connections { get; set; } = new();
    public TopologyMetrics Metrics { get; set; } = new();
    public List<TopologyInsight> Insights { get; set; } = new();
}

public class TopologyNode
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty; // Web, App, Data
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
    public List<string> SecurityGroups { get; set; } = new();
}

public class TopologyConnection
{
    public string SourceId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string ConnectionType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Allowed, Blocked, Unknown
    public List<int> AllowedPorts { get; set; } = new();
    public List<int> BlockedPorts { get; set; } = new();
}

public class TopologyMetrics
{
    public int TotalNodes { get; set; }
    public int TotalConnections { get; set; }
    public int AllowedConnections { get; set; }
    public int BlockedConnections { get; set; }
    public Dictionary<string, int> NodesByType { get; set; } = new();
    public Dictionary<string, int> NodesByTier { get; set; } = new();
}

public class TopologyInsight
{
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> AffectedResources { get; set; } = new();
    public string Recommendation { get; set; } = string.Empty;
} 