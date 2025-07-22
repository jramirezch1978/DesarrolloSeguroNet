using NetworkTester.Models;

namespace NetworkTester.Services;

public interface IPacketCapturer
{
    Task CapturePacketsAsync(string vmName, string? filter = null, int durationMinutes = 5, string? outputFile = null);
    Task<string> StartPacketCaptureAsync(string vmName, PacketCaptureConfiguration config);
    Task<PacketCaptureStatus> GetCaptureStatusAsync(string vmName, string captureId);
    Task StopPacketCaptureAsync(string vmName, string captureId);
    Task<PacketAnalysisReport> AnalyzeCaptureFileAsync(string captureFilePath);
}

public class PacketCaptureConfiguration
{
    public int DurationMinutes { get; set; } = 5;
    public int MaxFileSizeMB { get; set; } = 100;
    public string? StorageAccountId { get; set; }
    public string? LocalFilePath { get; set; }
    public List<PacketCaptureFilter> Filters { get; set; } = new();
    public bool CaptureFullPacket { get; set; } = true;
}

public class PacketAnalysisReport
{
    public string CaptureFile { get; set; } = string.Empty;
    public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
    public PacketStatistics Statistics { get; set; } = new();
    public List<NetworkConversation> Conversations { get; set; } = new();
    public List<ProtocolUsage> ProtocolUsage { get; set; } = new();
    public List<PacketAnomaly> Anomalies { get; set; } = new();
    public List<SecurityEvent> SecurityEvents { get; set; } = new();
}

public class PacketStatistics
{
    public long TotalPackets { get; set; }
    public long TotalBytes { get; set; }
    public TimeSpan CaptureDuration { get; set; }
    public double PacketsPerSecond { get; set; }
    public double BytesPerSecond { get; set; }
    public Dictionary<string, long> PacketsByProtocol { get; set; } = new();
    public Dictionary<string, long> BytesByProtocol { get; set; } = new();
}

public class NetworkConversation
{
    public string SourceIP { get; set; } = string.Empty;
    public string DestinationIP { get; set; } = string.Empty;
    public int SourcePort { get; set; }
    public int DestinationPort { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public long PacketCount { get; set; }
    public long ByteCount { get; set; }
    public DateTime FirstPacket { get; set; }
    public DateTime LastPacket { get; set; }
    public TimeSpan Duration { get; set; }
}

public class ProtocolUsage
{
    public string Protocol { get; set; } = string.Empty;
    public long PacketCount { get; set; }
    public long ByteCount { get; set; }
    public double PacketPercentage { get; set; }
    public double BytePercentage { get; set; }
    public List<int> CommonPorts { get; set; } = new();
}

public class PacketAnomaly
{
    public string AnomalyType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public string SourceIP { get; set; } = string.Empty;
    public string DestinationIP { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
}

public class SecurityEvent
{
    public string EventType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string SourceIP { get; set; } = string.Empty;
    public string DestinationIP { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // Allowed, Blocked
    public string RecommendedResponse { get; set; } = string.Empty;
} 