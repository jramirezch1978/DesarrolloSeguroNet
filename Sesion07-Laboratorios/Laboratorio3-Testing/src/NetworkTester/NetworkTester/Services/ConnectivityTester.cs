using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetworkTester.Models;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.Identity;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace NetworkTester.Services;

public class ConnectivityTester : IConnectivityTester
{
    private readonly ILogger<ConnectivityTester> _logger;
    private readonly IConfiguration _configuration;
    private readonly IAzureNetworkWatcherService _networkWatcherService;
    private readonly INetworkResourceService _networkResourceService;

    public ConnectivityTester(
        ILogger<ConnectivityTester> logger,
        IConfiguration configuration,
        IAzureNetworkWatcherService networkWatcherService,
        INetworkResourceService networkResourceService)
    {
        _logger = logger;
        _configuration = configuration;
        _networkWatcherService = networkWatcherService;
        _networkResourceService = networkResourceService;
    }

    public async Task RunFullTestSuiteAsync(string resourceGroup, bool verbose = false, string? outputFile = null)
    {
        _logger.LogInformation("🚀 Iniciando suite completa de tests de conectividad...");
        
        try
        {
            var testResults = new List<ConnectivityTestResult>();
            
            // Obtener VMs del resource group
            var vms = await _networkResourceService.GetVirtualMachinesAsync(resourceGroup);
            _logger.LogInformation($"📋 Encontradas {vms.Count} VMs para testing");

            if (vms.Count == 0)
            {
                _logger.LogWarning("⚠️ No se encontraron VMs en el resource group especificado");
                return;
            }

            // Definir matriz de tests de conectividad
            var testMatrix = await BuildConnectivityTestMatrix(vms);
            _logger.LogInformation($"🔍 Ejecutando {testMatrix.Count} tests de conectividad...");

            var totalTests = testMatrix.Count;
            var completedTests = 0;

            foreach (var test in testMatrix)
            {
                try
                {
                    var result = await TestConnectivityAsync(test.Source, test.Target, test.Port, test.Protocol);
                    testResults.Add(result);
                    
                    completedTests++;
                    var progress = (completedTests * 100) / totalTests;
                    
                    var statusIcon = result.Status == ConnectivityStatus.Reachable ? "✅" : "❌";
                    var logMessage = $"{statusIcon} [{progress:D2}%] {test.Source} -> {test.Target}:{test.Port} [{result.Status}]";
                    
                    if (verbose)
                    {
                        logMessage += $" - Latencia: {result.Latency.TotalMilliseconds:F2}ms";
                        if (!string.IsNullOrEmpty(result.StatusReason))
                            logMessage += $" - Razón: {result.StatusReason}";
                    }
                    
                    _logger.LogInformation(logMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Error ejecutando test: {test.Source} -> {test.Target}:{test.Port}");
                    
                    testResults.Add(new ConnectivityTestResult
                    {
                        Source = test.Source,
                        Target = test.Target,
                        Port = test.Port,
                        Protocol = test.Protocol,
                        Status = ConnectivityStatus.Error,
                        ErrorMessage = ex.Message,
                        TestTime = DateTime.UtcNow
                    });
                }
            }

            // Generar resumen de resultados
            await GenerateTestSummary(testResults, verbose);

            // Guardar reporte si se especifica archivo
            if (!string.IsNullOrEmpty(outputFile))
            {
                await SaveTestResults(testResults, outputFile);
                _logger.LogInformation($"📄 Reporte guardado en: {outputFile}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error ejecutando suite completa de tests");
            throw;
        }
    }

    public async Task<ConnectivityTestResult> TestConnectivityAsync(string source, string target, int port = 0, string protocol = "TCP")
    {
        _logger.LogDebug($"🔍 Testing conectividad: {source} -> {target}:{port} ({protocol})");
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Resolver recursos de origen y destino
            var sourceVmId = await _networkResourceService.ResolveVMResourceIdAsync(source);
            if (string.IsNullOrEmpty(sourceVmId))
            {
                return new ConnectivityTestResult
                {
                    Source = source,
                    Target = target,
                    Port = port,
                    Protocol = protocol,
                    Status = ConnectivityStatus.Error,
                    ErrorMessage = $"No se pudo resolver la VM origen: {source}"
                };
            }

            // Ejecutar test de conectividad usando Azure Network Watcher
            var connectivityResult = await _networkWatcherService.TestConnectivityAsync(
                sourceVmId, target, port, protocol);

            stopwatch.Stop();

            return new ConnectivityTestResult
            {
                Source = source,
                Target = target,
                Port = port,
                Protocol = protocol,
                Status = MapConnectivityStatus(connectivityResult.ConnectionStatus),
                StatusReason = connectivityResult.StatusReason,
                Latency = stopwatch.Elapsed,
                NetworkPath = MapNetworkHops(connectivityResult.Hops),
                TestTime = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, $"❌ Error testing conectividad: {source} -> {target}:{port}");
            
            return new ConnectivityTestResult
            {
                Source = source,
                Target = target,
                Port = port,
                Protocol = protocol,
                Status = ConnectivityStatus.Error,
                ErrorMessage = ex.Message,
                Latency = stopwatch.Elapsed,
                TestTime = DateTime.UtcNow
            };
        }
    }

    public async Task<List<ConnectivityTestResult>> TestConnectivityMatrixAsync(string resourceGroup)
    {
        var results = new List<ConnectivityTestResult>();
        var vms = await _networkResourceService.GetVirtualMachinesAsync(resourceGroup);
        
        // Test conectividad entre todas las VMs
        foreach (var sourceVm in vms)
        {
            foreach (var targetVm in vms)
            {
                if (sourceVm.Name == targetVm.Name) continue;
                
                // Tests comunes por tier
                var ports = GetCommonPortsForTier(targetVm.Name);
                
                foreach (var port in ports)
                {
                    var result = await TestConnectivityAsync(sourceVm.Name, targetVm.PrivateIpAddress, port);
                    results.Add(result);
                }
            }
        }
        
        return results;
    }

    public async Task<PerformanceTestResult> MeasurePerformanceAsync(string source, string target, int iterations = 10)
    {
        _logger.LogInformation($"📊 Midiendo performance: {source} -> {target} ({iterations} iteraciones)");
        
        var latencyMeasurements = new List<TimeSpan>();
        var successfulPings = 0;
        
        for (int i = 0; i < iterations; i++)
        {
            try
            {
                var ping = new Ping();
                var reply = await ping.SendPingAsync(target, 5000);
                
                if (reply.Status == IPStatus.Success)
                {
                    latencyMeasurements.Add(TimeSpan.FromMilliseconds(reply.RoundtripTime));
                    successfulPings++;
                }
                
                await Task.Delay(100); // Pequeña pausa entre pings
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ Error en ping {i + 1}: {ex.Message}");
            }
        }

        var packetLossPercentage = ((double)(iterations - successfulPings) / iterations) * 100;
        
        if (latencyMeasurements.Count > 0)
        {
            var sortedLatencies = latencyMeasurements.OrderBy(l => l).ToList();
            var p95Index = (int)Math.Ceiling(sortedLatencies.Count * 0.95) - 1;
            
            return new PerformanceTestResult
            {
                Source = source,
                Target = target,
                AverageLatency = TimeSpan.FromTicks((long)latencyMeasurements.Average(l => l.Ticks)),
                MinLatency = latencyMeasurements.Min(),
                MaxLatency = latencyMeasurements.Max(),
                P95Latency = sortedLatencies[Math.Max(0, p95Index)],
                PacketLossPercentage = packetLossPercentage,
                LatencyMeasurements = latencyMeasurements
            };
        }
        
        return new PerformanceTestResult
        {
            Source = source,
            Target = target,
            PacketLossPercentage = 100,
            LatencyMeasurements = new List<TimeSpan>()
        };
    }

    private async Task<List<ConnectivityTestDefinition>> BuildConnectivityTestMatrix(List<VirtualMachineInfo> vms)
    {
        var tests = new List<ConnectivityTestDefinition>();
        
        // Definir tests basados en arquitectura de 3 tiers
        var webVMs = vms.Where(vm => vm.Name.Contains("web", StringComparison.OrdinalIgnoreCase)).ToList();
        var appVMs = vms.Where(vm => vm.Name.Contains("app", StringComparison.OrdinalIgnoreCase)).ToList();
        var dataVMs = vms.Where(vm => vm.Name.Contains("data", StringComparison.OrdinalIgnoreCase)).ToList();

        // Tests Web -> App (deben pasar)
        foreach (var webVm in webVMs)
        {
            foreach (var appVm in appVMs)
            {
                tests.AddRange(new[]
                {
                    new ConnectivityTestDefinition { Source = webVm.Name, Target = appVm.PrivateIpAddress, Port = 8080, Protocol = "TCP" },
                    new ConnectivityTestDefinition { Source = webVm.Name, Target = appVm.PrivateIpAddress, Port = 8443, Protocol = "TCP" }
                });
            }
        }

        // Tests App -> Data (deben pasar)
        foreach (var appVm in appVMs)
        {
            foreach (var dataVm in dataVMs)
            {
                tests.AddRange(new[]
                {
                    new ConnectivityTestDefinition { Source = appVm.Name, Target = dataVm.PrivateIpAddress, Port = 1433, Protocol = "TCP" },
                    new ConnectivityTestDefinition { Source = appVm.Name, Target = dataVm.PrivateIpAddress, Port = 3306, Protocol = "TCP" }
                });
            }
        }

        // Tests Web -> Data (deben fallar - security validation)
        foreach (var webVm in webVMs)
        {
            foreach (var dataVm in dataVMs)
            {
                tests.Add(new ConnectivityTestDefinition 
                { 
                    Source = webVm.Name, 
                    Target = dataVm.PrivateIpAddress, 
                    Port = 1433, 
                    Protocol = "TCP" 
                });
            }
        }

        // Tests hacia Internet desde Web tier
        foreach (var webVm in webVMs)
        {
            tests.AddRange(new[]
            {
                new ConnectivityTestDefinition { Source = webVm.Name, Target = "www.microsoft.com", Port = 443, Protocol = "TCP" },
                new ConnectivityTestDefinition { Source = webVm.Name, Target = "8.8.8.8", Port = 53, Protocol = "UDP" }
            });
        }

        return tests;
    }

    private async Task GenerateTestSummary(List<ConnectivityTestResult> results, bool verbose)
    {
        var total = results.Count;
        var successful = results.Count(r => r.Status == ConnectivityStatus.Reachable);
        var failed = results.Count(r => r.Status == ConnectivityStatus.Unreachable);
        var errors = results.Count(r => r.Status == ConnectivityStatus.Error);
        
        _logger.LogInformation("📊 RESUMEN DE TESTS DE CONECTIVIDAD");
        _logger.LogInformation("==========================================");
        _logger.LogInformation($"✅ Total de tests: {total}");
        _logger.LogInformation($"✅ Exitosos: {successful} ({(successful * 100.0 / total):F1}%)");
        _logger.LogInformation($"❌ Fallidos: {failed} ({(failed * 100.0 / total):F1}%)");
        _logger.LogInformation($"⚠️ Errores: {errors} ({(errors * 100.0 / total):F1}%)");
        
        if (verbose && results.Any())
        {
            var avgLatency = results.Where(r => r.Status == ConnectivityStatus.Reachable)
                                   .Average(r => r.Latency.TotalMilliseconds);
            _logger.LogInformation($"⏱️ Latencia promedio: {avgLatency:F2}ms");
        }
        
        // Mostrar fallos de seguridad esperados
        var securityTests = results.Where(r => 
            r.Source.Contains("web", StringComparison.OrdinalIgnoreCase) && 
            (r.Target.Contains("10.2.3.") || r.Port == 1433) &&
            r.Status == ConnectivityStatus.Unreachable).ToList();
            
        if (securityTests.Any())
        {
            _logger.LogInformation($"🔒 Tests de seguridad exitosos (acceso bloqueado correctamente): {securityTests.Count}");
        }
    }

    private async Task SaveTestResults(List<ConnectivityTestResult> results, string outputFile)
    {
        try
        {
            var reportGenerator = new ReportGenerator(_logger);
            await reportGenerator.GenerateConnectivityReportAsync(results, outputFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error guardando reporte en {outputFile}");
        }
    }

    private int[] GetCommonPortsForTier(string vmName)
    {
        var lowerName = vmName.ToLowerInvariant();
        
        if (lowerName.Contains("web"))
            return new[] { 80, 443 };
        else if (lowerName.Contains("app"))
            return new[] { 8080, 8443 };
        else if (lowerName.Contains("data"))
            return new[] { 1433, 3306, 5432 };
        else
            return new[] { 22, 3389 }; // SSH/RDP para VMs genéricas
    }

    private ConnectivityStatus MapConnectivityStatus(string azureStatus)
    {
        return azureStatus?.ToLowerInvariant() switch
        {
            "reachable" => ConnectivityStatus.Reachable,
            "unreachable" => ConnectivityStatus.Unreachable,
            "unknown" => ConnectivityStatus.Unknown,
            _ => ConnectivityStatus.Error
        };
    }

    private List<NetworkHop> MapNetworkHops(IEnumerable<dynamic>? hops)
    {
        if (hops == null) return new List<NetworkHop>();
        
        return hops.Select((hop, index) => new NetworkHop
        {
            HopNumber = (index + 1).ToString(),
            Address = hop.address?.ToString() ?? "Unknown",
            NextHopType = hop.nextHopType?.ToString() ?? "Unknown",
            ResourceId = hop.resourceId?.ToString() ?? string.Empty
        }).ToList();
    }
}

public class ConnectivityTestDefinition
{
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Protocol { get; set; } = "TCP";
} 