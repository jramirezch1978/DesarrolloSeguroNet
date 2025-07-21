using NetworkTester.Models;

namespace NetworkTester.Services;

public interface IAzureNetworkWatcherService
{
    Task<ConnectivityResult> TestConnectivityAsync(string sourceResourceId, string destinationAddress, int destinationPort, string protocol = "TCP");
    Task<IPFlowVerifyResult> VerifyIPFlowAsync(string vmResourceId, string direction, string localAddress, int localPort, string remoteAddress, int remotePort, string protocol);
    Task<NextHopResult> GetNextHopAsync(string vmResourceId, string sourceIP, string destinationIP);
    Task<List<EffectiveSecurityRule>> GetEffectiveSecurityRulesAsync(string networkInterfaceId);
    Task<TopologyResult> GetTopologyAsync(string resourceGroupName);
    Task<string> StartPacketCaptureAsync(string vmResourceId, PacketCaptureParameters parameters);
    Task<PacketCaptureStatus> GetPacketCaptureStatusAsync(string vmResourceId, string captureId);
    Task StopPacketCaptureAsync(string vmResourceId, string captureId);
}

public class ConnectivityResult
{
    public string ConnectionStatus { get; set; } = string.Empty;
    public string StatusReason { get; set; } = string.Empty;
    public int AvgLatencyInMs { get; set; }
    public int MinLatencyInMs { get; set; }
    public int MaxLatencyInMs { get; set; }
    public int ProbesSent { get; set; }
    public int ProbesFailed { get; set; }
    public List<dynamic> Hops { get; set; } = new();
}

public class IPFlowVerifyResult
{
    public string Access { get; set; } = string.Empty; // Allow or Deny
    public string RuleName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string Direction { get; set; } = string.Empty;
}

public class NextHopResult
{
    public string NextHopType { get; set; } = string.Empty;
    public string? NextHopIpAddress { get; set; }
    public string? RouteTableId { get; set; }
    public string? Explanation { get; set; }
}

public class EffectiveSecurityRule
{
    public string Name { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public string SourcePortRange { get; set; } = string.Empty;
    public string DestinationPortRange { get; set; } = string.Empty;
    public string SourceAddressPrefix { get; set; } = string.Empty;
    public string DestinationAddressPrefix { get; set; } = string.Empty;
    public string Access { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // NetworkSecurityGroup or DefaultSecurityRules
}

public class TopologyResult
{
    public List<TopologyResource> Resources { get; set; } = new();
    public List<TopologyAssociation> Associations { get; set; } = new();
}

public class TopologyResource
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class TopologyAssociation
{
    public string Name { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string AssociationType { get; set; } = string.Empty;
}

public class PacketCaptureParameters
{
    public string? StorageAccountId { get; set; }
    public string? StoragePath { get; set; }
    public string? LocalFilePath { get; set; }
    public int TimeLimitInSeconds { get; set; } = 300; // 5 minutos por defecto
    public int BytesToCapturePerPacket { get; set; } = 0; // 0 = capture entire packet
    public int TotalBytesPerSession { get; set; } = 0; // 0 = no limit
    public List<PacketCaptureFilter> Filters { get; set; } = new();
}

public class PacketCaptureFilter
{
    public string Protocol { get; set; } = string.Empty; // TCP, UDP, Any
    public string? LocalIPAddress { get; set; }
    public string? RemoteIPAddress { get; set; }
    public string? LocalPort { get; set; }
    public string? RemotePort { get; set; }
}

public class PacketCaptureStatus
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CaptureStatus { get; set; } = string.Empty; // NotStarted, Running, Stopped, Error, Unknown
    public string? StopReason { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public List<string> CaptureFileLocation { get; set; } = new();
} 