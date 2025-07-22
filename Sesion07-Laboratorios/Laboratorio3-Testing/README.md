# 🧪 Laboratorio 3: Testing y Simulación de Conectividad

## Información General
- **Duración:** 20 minutos
- **Objetivo:** Usar Azure Network Watcher para testing de conectividad y análisis de tráfico
- **Modalidad:** Práctica individual con código .NET y herramientas avanzadas

## Fundamentos Teóricos

### ¿Qué es Azure Network Watcher?

Azure Network Watcher es un conjunto de herramientas de monitoreo, diagnóstico y análisis de redes que permite supervisar, diagnosticar y obtener información sobre la conectividad de red en Azure. Proporciona capacidades avanzadas para troubleshooting y optimización de rendimiento.

### Arquitectura de Network Watcher

```
┌─────────────────────────────────────────────────────────┐
│                AZURE NETWORK WATCHER                    │
├─────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐    │
│  │ DIAGNOSTICS │  │ MONITORING  │  │  ANALYTICS  │    │
│  │             │  │             │  │             │    │
│  │ • IP Flow   │  │ • Topology  │  │ • Traffic   │    │
│  │   Verify    │  │ • Connection│  │   Analytics │    │
│  │ • Next Hop  │  │   Monitor   │  │ • NSG Flow  │    │
│  │ • Security  │  │ • Network   │  │   Logs      │    │
│  │   Rules     │  │   Insights  │  │ • Packet    │    │
│  │ • Packet    │  │             │  │   Capture   │    │
│  │   Capture   │  │             │  │             │    │
│  └─────────────┘  └─────────────┘  └─────────────┘    │
└─────────────────────────────────────────────────────────┘
```

### Herramientas Clave de Network Watcher

#### 1. IP Flow Verify
Verifica si un paquete está permitido o denegado hacia o desde una máquina virtual:

```csharp
// Conceptual IP Flow Verification Logic
public class IPFlowVerifier
{
    public async Task<FlowVerificationResult> VerifyFlow(FlowTestParameters parameters)
    {
        var effectiveRules = await GetEffectiveSecurityRules(parameters.TargetNIC);
        
        foreach (var rule in effectiveRules.OrderBy(r => r.Priority))
        {
            if (RuleMatches(rule, parameters))
            {
                return new FlowVerificationResult
                {
                    Access = rule.Access, // Allow or Deny
                    RuleName = rule.Name,
                    Priority = rule.Priority,
                    Reason = $"Traffic {rule.Access.ToLower()}ed by rule {rule.Name}"
                };
            }
        }
        
        return FlowVerificationResult.DefaultDeny();
    }
}
```

#### 2. Connectivity Test
Prueba la conectividad de extremo a extremo:

```csharp
public class ConnectivityTester
{
    public async Task<ConnectivityResult> TestConnectivity(ConnectivityParameters parameters)
    {
        var result = new ConnectivityResult();
        
        // Test Layer 3/4 connectivity
        var networkResult = await TestNetworkConnectivity(parameters);
        result.NetworkConnectivity = networkResult;
        
        // Test application layer if successful
        if (networkResult.Status == ConnectivityStatus.Reachable)
        {
            var appResult = await TestApplicationConnectivity(parameters);
            result.ApplicationConnectivity = appResult;
        }
        
        // Analyze path and hops
        result.NetworkPath = await TraceNetworkPath(parameters);
        
        return result;
    }
}
```

#### 3. Next Hop Analysis
Determina el siguiente salto en la ruta de red:

```csharp
public class NextHopAnalyzer
{
    public async Task<NextHopResult> GetNextHop(NextHopParameters parameters)
    {
        var routingTable = await GetEffectiveRoutes(parameters.SourceNIC);
        var nextHop = FindBestMatchingRoute(parameters.DestinationIP, routingTable);
        
        return new NextHopResult
        {
            NextHopType = nextHop.NextHopType,
            NextHopIP = nextHop.NextHopIpAddress,
            RouteTableId = nextHop.Source,
            Explanation = GenerateExplanation(nextHop, parameters)
        };
    }
}
```

### Traffic Analytics y Flow Logs

#### NSG Flow Logs v2
Los Flow Logs proporcionan información detallada sobre el tráfico IP:

```json
{
  "time": "2025-07-21T19:30:00.0000000Z",
  "systemId": "subscription-id",
  "category": "NetworkSecurityGroupFlowEvent",
  "resourceId": "/subscriptions/.../networkSecurityGroups/nsg-web",
  "operationName": "NetworkSecurityGroupFlowEvents",
  "properties": {
    "Version": 2,
    "flows": [
      {
        "rule": "Allow-HTTPS-Inbound",
        "flows": [
          {
            "mac": "00:1B:21:6C:00:47",
            "flowTuples": [
              "1642793400,203.0.113.12,10.2.1.4,54321,443,T,I,A,B,10,1024,5,512"
            ]
          }
        ]
      }
    ]
  }
}
```

#### Estructura de Flow Tuple
```
Timestamp,SourceIP,DestIP,SourcePort,DestPort,Protocol,Direction,Decision,FlowState,PacketsS2D,BytesS2D,PacketsD2S,BytesD2S
```

### Packet Capture

Captura de paquetes para análisis profundo:

```csharp
public class PacketCaptureManager
{
    public async Task<PacketCaptureSession> StartCapture(CaptureParameters parameters)
    {
        var session = new PacketCaptureSession
        {
            Target = parameters.TargetVM,
            Filters = parameters.Filters,
            Duration = parameters.MaxDuration,
            StorageLocation = parameters.StorageAccount
        };
        
        // Configure capture filters
        if (parameters.ProtocolFilter != null)
            session.AddProtocolFilter(parameters.ProtocolFilter);
            
        if (parameters.PortFilter != null)
            session.AddPortFilter(parameters.PortFilter);
            
        await NetworkWatcherClient.StartPacketCapture(session);
        return session;
    }
}
```

## Laboratorio Práctico

### Paso 1: Configuración del Entorno de Testing (5 minutos)

#### Verificar Network Watcher
```bash
# Variables del laboratorio
$resourceGroup = "rg-nsg-lab-$env:USERNAME"
$location = "eastus"
$vnetName = "vnet-nsg-lab"

# Verificar que Network Watcher esté habilitado
az network watcher list --output table

# Si no existe, crear uno
az network watcher configure \
  --resource-group NetworkWatcherRG \
  --location $location \
  --enabled true
```

#### Crear VMs de Testing
```bash
# VM en subnet web para testing
az vm create \
  --resource-group $resourceGroup \
  --name vm-web-test \
  --image Ubuntu2204 \
  --vnet-name $vnetName \
  --subnet snet-web \
  --public-ip-address "" \
  --size Standard_B1s \
  --admin-username azureuser \
  --generate-ssh-keys \
  --nsg "" \
  --tags Purpose=Testing Environment=Lab

# VM en subnet app para testing
az vm create \
  --resource-group $resourceGroup \
  --name vm-app-test \
  --image Ubuntu2204 \
  --vnet-name $vnetName \
  --subnet snet-app \
  --public-ip-address "" \
  --size Standard_B1s \
  --admin-username azureuser \
  --generate-ssh-keys \
  --nsg "" \
  --tags Purpose=Testing Environment=Lab

# VM en subnet data para testing
az vm create \
  --resource-group $resourceGroup \
  --name vm-data-test \
  --image Ubuntu2204 \
  --vnet-name $vnetName \
  --subnet snet-data \
  --public-ip-address "" \
  --size Standard_B1s \
  --admin-username azureuser \
  --generate-ssh-keys \
  --nsg "" \
  --tags Purpose=Testing Environment=Lab
```

### Paso 2: Testing con IP Flow Verify (7 minutos)

#### Test 1 - Verificar Reglas NSG
```bash
# Test: Tráfico HTTPS permitido desde Internet
az network watcher test-ip-flow \
  --vm vm-web-test \
  --direction Inbound \
  --local 10.2.1.4:443 \
  --remote 203.0.113.1:12345 \
  --protocol TCP \
  --resource-group $resourceGroup

# Test: Tráfico SSH bloqueado desde Internet
az network watcher test-ip-flow \
  --vm vm-web-test \
  --direction Inbound \
  --local 10.2.1.4:22 \
  --remote 203.0.113.1:54321 \
  --protocol TCP \
  --resource-group $resourceGroup

# Test: Comunicación entre tiers (Web -> App)
az network watcher test-ip-flow \
  --vm vm-app-test \
  --direction Inbound \
  --local 10.2.2.4:8080 \
  --remote 10.2.1.4:54321 \
  --protocol TCP \
  --resource-group $resourceGroup
```

### Paso 3: Connectivity Testing (8 minutos)

#### Test de Conectividad End-to-End
```bash
# Test 1: Conectividad externa (web tier a Internet)
az network watcher test-connectivity \
  --source-resource vm-web-test \
  --dest-address www.microsoft.com \
  --dest-port 443 \
  --resource-group $resourceGroup

# Test 2: Conectividad interna (web tier a app tier)
az network watcher test-connectivity \
  --source-resource vm-web-test \
  --dest-resource vm-app-test \
  --dest-port 8080 \
  --resource-group $resourceGroup

# Test 3: Conectividad bloqueada (web tier a data tier)
az network watcher test-connectivity \
  --source-resource vm-web-test \
  --dest-resource vm-data-test \
  --dest-port 1433 \
  --resource-group $resourceGroup
```

#### Análisis de Next Hop
```bash
# Analizar ruta desde web tier
az network watcher show-next-hop \
  --vm vm-web-test \
  --dest-ip 8.8.8.8 \
  --source-ip 10.2.1.4 \
  --resource-group $resourceGroup

# Analizar ruta interna
az network watcher show-next-hop \
  --vm vm-web-test \
  --dest-ip 10.2.3.4 \
  --source-ip 10.2.1.4 \
  --resource-group $resourceGroup
```

### Paso 4: Aplicación .NET NetworkTester

La aplicación `NetworkTester` proporciona testing automatizado y análisis avanzado.

#### Ejecutar NetworkTester
```bash
# Navegar al directorio del proyecto
cd src/NetworkTester

# Configurar variables de entorno
$env:AZURE_SUBSCRIPTION_ID = "your-subscription-id"
$env:AZURE_RESOURCE_GROUP = $resourceGroup

# Ejecutar suite completa de tests
dotnet run -- test-all --verbose

# Ejecutar tests específicos
dotnet run -- test-connectivity --source vm-web-test --target vm-app-test
dotnet run -- test-security-rules --vm vm-web-test
dotnet run -- analyze-topology --resource-group $resourceGroup
```

#### Funcionalidades de NetworkTester
- **Suite completa de conectividad** automatizada
- **Análisis de reglas de seguridad** efectivas
- **Generación de topología** de red visual
- **Reportes detallados** en múltiples formatos
- **Testing continuo** con alerting
- **Packet capture** automatizado para troubleshooting

## Casos de Uso Avanzados

### Escenario 1: Troubleshooting de Aplicación Web

```csharp
// Automated troubleshooting workflow
public class WebAppTroubleshooter
{
    public async Task<TroubleshootingReport> DiagnoseConnectivityIssue(
        string webTierVM, 
        string appTierVM, 
        string applicationPort)
    {
        var report = new TroubleshootingReport();
        
        // Step 1: Test basic connectivity
        var connectivityResult = await TestConnectivity(webTierVM, appTierVM, applicationPort);
        report.ConnectivityStatus = connectivityResult.Status;
        
        if (connectivityResult.Status == ConnectivityStatus.Failed)
        {
            // Step 2: Analyze security rules
            var securityAnalysis = await AnalyzeSecurityRules(webTierVM, appTierVM, applicationPort);
            report.SecurityRuleAnalysis = securityAnalysis;
            
            // Step 3: Check routing
            var routingAnalysis = await AnalyzeRouting(webTierVM, appTierVM);
            report.RoutingAnalysis = routingAnalysis;
            
            // Step 4: Perform packet capture if needed
            if (securityAnalysis.RulesAllowTraffic && routingAnalysis.RoutingCorrect)
            {
                var packetCapture = await CapturePackets(webTierVM, applicationPort);
                report.PacketCaptureAnalysis = packetCapture;
            }
        }
        
        // Generate recommendations
        report.Recommendations = GenerateRecommendations(report);
        
        return report;
    }
}
```

### Escenario 2: Performance Analysis

```csharp
public class NetworkPerformanceAnalyzer
{
    public async Task<PerformanceReport> AnalyzePerformance(string sourceVM, string targetVM)
    {
        var report = new PerformanceReport();
        
        // Latency testing
        var latencyResults = await MeasureLatency(sourceVM, targetVM, iterations: 100);
        report.AverageLatency = latencyResults.Average();
        report.LatencyP95 = latencyResults.Percentile(95);
        
        // Throughput testing
        var throughputResults = await MeasureThroughput(sourceVM, targetVM);
        report.ThroughputMbps = throughputResults.MegabitsPerSecond;
        
        // Path analysis
        var pathAnalysis = await AnalyzeNetworkPath(sourceVM, targetVM);
        report.NetworkHops = pathAnalysis.Hops.Count;
        report.PathBottlenecks = pathAnalysis.IdentifyBottlenecks();
        
        return report;
    }
}
```

### Escenario 3: Security Validation

```csharp
public class SecurityValidator
{
    public async Task<SecurityValidationReport> ValidateSecurityPosture(string resourceGroup)
    {
        var report = new SecurityValidationReport();
        
        // Test that unauthorized access is blocked
        var unauthorizedTests = new[]
        {
            TestUnauthorizedSSH(),
            TestDirectDatabaseAccess(),
            TestBypassWebTier(),
            TestInsecureProtocols()
        };
        
        foreach (var test in unauthorizedTests)
        {
            var result = await test;
            report.SecurityTests.Add(result);
            
            if (result.Status == TestStatus.Failed)
            {
                report.SecurityViolations.Add(new SecurityViolation
                {
                    Severity = result.Severity,
                    Description = result.Description,
                    Remediation = result.RecommendedAction
                });
            }
        }
        
        // Validate compliance with security frameworks
        report.ComplianceStatus = await ValidateCompliance();
        
        return report;
    }
}
```

## Flow Logs Analysis

### Configurar Traffic Analytics
```bash
# Crear Log Analytics Workspace
az monitor log-analytics workspace create \
  --resource-group $resourceGroup \
  --workspace-name law-traffic-analytics \
  --location $location \
  --sku PerGB2018

# Habilitar Traffic Analytics en Flow Logs existentes
$workspaceId = az monitor log-analytics workspace show \
  --resource-group $resourceGroup \
  --workspace-name law-traffic-analytics \
  --query customerId --output tsv

az network watcher flow-log update \
  --resource-group NetworkWatcherRG \
  --name flowlog-nsg-web \
  --workspace $workspaceId \
  --interval 10
```

### Análisis de Flow Logs con .NET
```csharp
public class FlowLogAnalyzer
{
    public async Task<FlowAnalysisReport> AnalyzeFlowLogs(DateTime startTime, DateTime endTime)
    {
        var report = new FlowAnalysisReport();
        
        // Retrieve flow logs from storage
        var flowLogs = await GetFlowLogsFromStorage(startTime, endTime);
        
        // Analyze patterns
        report.TopTalkers = AnalyzeTopTalkers(flowLogs);
        report.SuspiciousActivity = DetectSuspiciousPatterns(flowLogs);
        report.TrafficTrends = AnalyzeTrafficTrends(flowLogs);
        report.ProtocolDistribution = AnalyzeProtocolDistribution(flowLogs);
        
        // Generate insights
        report.Insights = GenerateInsights(report);
        
        return report;
    }
    
    private List<SuspiciousActivity> DetectSuspiciousPatterns(IEnumerable<FlowLog> flowLogs)
    {
        var suspicious = new List<SuspiciousActivity>();
        
        // Port scanning detection
        var portScans = flowLogs
            .GroupBy(log => log.SourceIP)
            .Where(g => g.Select(log => log.DestinationPort).Distinct().Count() > 10)
            .Select(g => new SuspiciousActivity
            {
                Type = ActivityType.PortScan,
                SourceIP = g.Key,
                Description = $"Potential port scan from {g.Key} - {g.Select(l => l.DestinationPort).Distinct().Count()} unique ports accessed"
            });
        
        suspicious.AddRange(portScans);
        
        // Failed connection attempts
        var bruteForce = flowLogs
            .Where(log => log.FlowState == "D") // Denied
            .GroupBy(log => new { log.SourceIP, log.DestinationPort })
            .Where(g => g.Count() > 50)
            .Select(g => new SuspiciousActivity
            {
                Type = ActivityType.BruteForce,
                SourceIP = g.Key.SourceIP,
                Description = $"Potential brute force attack from {g.Key.SourceIP} to port {g.Key.DestinationPort} - {g.Count()} failed attempts"
            });
        
        suspicious.AddRange(bruteForce);
        
        return suspicious;
    }
}
```

## Packet Capture y Deep Analysis

### Automated Packet Capture
```csharp
public class PacketCaptureAutomation
{
    public async Task<PacketCaptureAnalysis> CaptureAndAnalyze(
        string targetVM, 
        string filter, 
        TimeSpan duration)
    {
        // Start packet capture
        var captureSession = await StartPacketCapture(targetVM, filter, duration);
        
        // Wait for capture to complete
        await WaitForCaptureCompletion(captureSession);
        
        // Download and analyze capture file
        var captureFile = await DownloadCaptureFile(captureSession);
        var analysis = await AnalyzeCaptureFile(captureFile);
        
        return analysis;
    }
    
    private async Task<PacketCaptureAnalysis> AnalyzeCaptureFile(byte[] captureData)
    {
        var analysis = new PacketCaptureAnalysis();
        
        // Parse PCAP file
        var packets = ParsePcapFile(captureData);
        
        // Analyze protocols
        analysis.ProtocolBreakdown = AnalyzeProtocols(packets);
        
        // Analyze conversations
        analysis.TopConversations = AnalyzeConversations(packets);
        
        // Detect anomalies
        analysis.Anomalies = DetectAnomalies(packets);
        
        return analysis;
    }
}
```

## Scripts de Automatización

### script-test-connectivity.ps1
```powershell
# Testing automatizado de conectividad
param(
    [string]$ResourceGroup,
    [string]$VNetName = "vnet-nsg-lab",
    [switch]$Verbose
)

# Test matrix de conectividad
$testMatrix = @(
    @{ Source = "vm-web-test"; Target = "vm-app-test"; Port = 8080; Expected = "Allow" },
    @{ Source = "vm-app-test"; Target = "vm-data-test"; Port = 1433; Expected = "Allow" },
    @{ Source = "vm-web-test"; Target = "vm-data-test"; Port = 1433; Expected = "Deny" },
    @{ Source = "vm-web-test"; Target = "www.microsoft.com"; Port = 443; Expected = "Allow" }
)

foreach ($test in $testMatrix) {
    Write-Host "Testing: $($test.Source) -> $($test.Target):$($test.Port)" -ForegroundColor Yellow
    
    $result = az network watcher test-connectivity `
        --source-resource $test.Source `
        --dest-address $test.Target `
        --dest-port $test.Port `
        --resource-group $ResourceGroup `
        --output json | ConvertFrom-Json
    
    $status = if ($result.connectionStatus -eq "Reachable") { "Allow" } else { "Deny" }
    
    if ($status -eq $test.Expected) {
        Write-Host "✅ PASS: $status (Expected: $($test.Expected))" -ForegroundColor Green
    } else {
        Write-Host "❌ FAIL: $status (Expected: $($test.Expected))" -ForegroundColor Red
    }
}
```

### script-analyze-flow-logs.ps1
```powershell
# Análisis automatizado de Flow Logs
param(
    [string]$StorageAccountName,
    [string]$ContainerName = "insights-logs-networksecuritygroupflowevent",
    [int]$HoursBack = 24
)

# Download and analyze recent flow logs
$startTime = (Get-Date).AddHours(-$HoursBack)
$endTime = Get-Date

Write-Host "Analyzing flow logs from $startTime to $endTime" -ForegroundColor Cyan

# Call .NET application for detailed analysis
dotnet run --project src/NetworkTester -- analyze-flow-logs `
    --storage-account $StorageAccountName `
    --container $ContainerName `
    --start-time $startTime.ToString("yyyy-MM-ddTHH:mm:ssZ") `
    --end-time $endTime.ToString("yyyy-MM-ddTHH:mm:ssZ") `
    --output report.html
```

## Resultados de Aprendizaje

Al completar este laboratorio, habrán dominado:

### Conocimientos Técnicos
- ✅ **Azure Network Watcher** uso completo y avanzado
- ✅ **IP Flow Verify** para validación de reglas NSG
- ✅ **Connectivity Testing** end-to-end automatizado
- ✅ **Flow Logs Analysis** para insights de tráfico
- ✅ **Packet Capture** para troubleshooting profundo

### Habilidades Prácticas
- ✅ **Troubleshooting sistemático** de conectividad
- ✅ **Performance analysis** de redes
- ✅ **Security validation** automatizada
- ✅ **Automated testing** con .NET y PowerShell
- ✅ **Reporting avanzado** de análisis de red

### Competencias Empresariales
- ✅ **Network operations** proactivas
- ✅ **Incident response** estructurada
- ✅ **Capacity planning** basada en datos
- ✅ **Compliance validation** continua
- ✅ **Cost optimization** de recursos de red

## Métricas de Éxito

### Indicadores de Testing Efectivo
- **Cobertura de tests**: >95% de rutas críticas validadas
- **Automated detection**: Problemas identificados en <5 minutos
- **False positive rate**: <2% en detección de anomalías
- **MTTR**: Mean Time To Resolution <15 minutos

### KPIs de Network Operations
- **Network availability**: >99.9% según SLA
- **Latency P95**: <50ms para comunicación interna
- **Throughput efficiency**: >80% de capacidad teórica
- **Security posture**: 0 violaciones críticas

## Troubleshooting Común

### Error: "Network Watcher not available"
```bash
# Verificar registro del provider
az provider show --namespace Microsoft.Network --query registrationState

# Registrar si es necesario
az provider register --namespace Microsoft.Network

# Crear Network Watcher manualmente
az network watcher configure --resource-group NetworkWatcherRG --location eastus --enabled true
```

### Error: "VM not found for testing"
```bash
# Verificar VMs existentes
az vm list --resource-group $resourceGroup --output table

# Verificar estado de VMs
az vm get-instance-view --resource-group $resourceGroup --name vm-web-test --output table
```

### Error: "Connectivity test timeout"
```bash
# Verificar NSG rules que pueden estar bloqueando
az network nsg rule list --resource-group $resourceGroup --nsg-name nsg-web-advanced --output table

# Verificar routing efectivo
az network nic show-effective-route-table --resource-group $resourceGroup --name vm-web-testVMNic
```

---

**¡Excelente trabajo!** Han implementado un sistema completo de testing y monitoreo de conectividad que proporciona visibilidad total de la red y capacidades avanzadas de troubleshooting.

**Siguiente:** [Laboratorio 4 - Automatización y Alertas Avanzadas](../Laboratorio4-Automation/README.md) 