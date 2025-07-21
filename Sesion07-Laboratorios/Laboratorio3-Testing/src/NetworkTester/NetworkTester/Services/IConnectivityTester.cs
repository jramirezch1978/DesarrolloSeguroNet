using NetworkTester.Models;

namespace NetworkTester.Services;

public interface IConnectivityTester
{
    Task RunFullTestSuiteAsync(string resourceGroup, bool verbose = false, string? outputFile = null);
    Task<ConnectivityTestResult> TestConnectivityAsync(string source, string target, int port = 0, string protocol = "TCP");
    Task<List<ConnectivityTestResult>> TestConnectivityMatrixAsync(string resourceGroup);
    Task<PerformanceTestResult> MeasurePerformanceAsync(string source, string target, int iterations = 10);
}

public class ConnectivityTestResult
{
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Protocol { get; set; } = "TCP";
    public ConnectivityStatus Status { get; set; }
    public string StatusReason { get; set; } = string.Empty;
    public TimeSpan Latency { get; set; }
    public List<NetworkHop> NetworkPath { get; set; } = new();
    public DateTime TestTime { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
}

public class PerformanceTestResult
{
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public TimeSpan AverageLatency { get; set; }
    public TimeSpan MinLatency { get; set; }
    public TimeSpan MaxLatency { get; set; }
    public TimeSpan P95Latency { get; set; }
    public double PacketLossPercentage { get; set; }
    public double ThroughputMbps { get; set; }
    public List<TimeSpan> LatencyMeasurements { get; set; } = new();
}

public enum ConnectivityStatus
{
    Unknown,
    Reachable,
    Unreachable,
    PartiallyReachable,
    Timeout,
    Error
}

public class NetworkHop
{
    public string HopNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string NextHopType { get; set; } = string.Empty;
    public TimeSpan Latency { get; set; }
    public string ResourceId { get; set; } = string.Empty;
} 