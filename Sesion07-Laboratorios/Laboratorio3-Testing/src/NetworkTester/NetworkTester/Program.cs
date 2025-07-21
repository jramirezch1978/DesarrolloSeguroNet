using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetworkTester.Services;
using System.CommandLine;

namespace NetworkTester;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Configurar host y servicios
        var host = CreateHostBuilder(args).Build();
        
        // Configurar comandos
        var rootCommand = new RootCommand("NetworkTester - Herramienta avanzada para testing de conectividad de red en Azure");
        
        // Comando: test-all
        var testAllCommand = new Command("test-all", "Ejecutar suite completa de tests de conectividad");
        testAllCommand.AddOption(new Option<bool>("--verbose", "Mostrar output detallado"));
        testAllCommand.AddOption(new Option<string>("--resource-group", "Grupo de recursos a testear"));
        testAllCommand.AddOption(new Option<string>("--output", "Archivo de salida para reportes"));
        
        testAllCommand.SetHandler(async (bool verbose, string resourceGroup, string output) =>
        {
            var tester = host.Services.GetRequiredService<IConnectivityTester>();
            await tester.RunFullTestSuiteAsync(resourceGroup, verbose, output);
        });
        
        // Comando: test-connectivity
        var testConnectivityCommand = new Command("test-connectivity", "Probar conectividad específica");
        testConnectivityCommand.AddOption(new Option<string>("--source", "VM o recurso origen") { IsRequired = true });
        testConnectivityCommand.AddOption(new Option<string>("--target", "IP, FQDN o recurso destino") { IsRequired = true });
        testConnectivityCommand.AddOption(new Option<int>("--port", "Puerto de destino"));
        testConnectivityCommand.AddOption(new Option<string>("--protocol", "Protocolo (TCP/UDP)"));
        
        testConnectivityCommand.SetHandler(async (string source, string target, int port, string protocol) =>
        {
            var tester = host.Services.GetRequiredService<IConnectivityTester>();
            await tester.TestConnectivityAsync(source, target, port, protocol);
        });
        
        // Comando: test-security-rules
        var testSecurityRulesCommand = new Command("test-security-rules", "Validar reglas de seguridad");
        testSecurityRulesCommand.AddOption(new Option<string>("--vm", "VM a analizar") { IsRequired = true });
        testSecurityRulesCommand.AddOption(new Option<bool>("--show-effective", "Mostrar reglas efectivas"));
        
        testSecurityRulesCommand.SetHandler(async (string vm, bool showEffective) =>
        {
            var analyzer = host.Services.GetRequiredService<ISecurityRuleAnalyzer>();
            await analyzer.AnalyzeSecurityRulesAsync(vm, showEffective);
        });
        
        // Comando: analyze-topology
        var analyzeTopologyCommand = new Command("analyze-topology", "Analizar topología de red");
        analyzeTopologyCommand.AddOption(new Option<string>("--resource-group", "Grupo de recursos") { IsRequired = true });
        analyzeTopologyCommand.AddOption(new Option<string>("--output-format", "Formato de salida (json/svg/html)"));
        
        analyzeTopologyCommand.SetHandler(async (string resourceGroup, string outputFormat) =>
        {
            var analyzer = host.Services.GetRequiredService<ITopologyAnalyzer>();
            await analyzer.AnalyzeTopologyAsync(resourceGroup, outputFormat);
        });
        
        // Comando: analyze-flow-logs
        var analyzeFlowLogsCommand = new Command("analyze-flow-logs", "Analizar Flow Logs");
        analyzeFlowLogsCommand.AddOption(new Option<string>("--storage-account", "Cuenta de almacenamiento") { IsRequired = true });
        analyzeFlowLogsCommand.AddOption(new Option<string>("--container", "Contenedor de logs"));
        analyzeFlowLogsCommand.AddOption(new Option<DateTime>("--start-time", "Tiempo de inicio"));
        analyzeFlowLogsCommand.AddOption(new Option<DateTime>("--end-time", "Tiempo de fin"));
        analyzeFlowLogsCommand.AddOption(new Option<string>("--output", "Archivo de salida"));
        
        analyzeFlowLogsCommand.SetHandler(async (string storageAccount, string container, DateTime startTime, DateTime endTime, string output) =>
        {
            var analyzer = host.Services.GetRequiredService<IFlowLogAnalyzer>();
            await analyzer.AnalyzeFlowLogsAsync(storageAccount, container, startTime, endTime, output);
        });
        
        // Comando: capture-packets
        var capturePacketsCommand = new Command("capture-packets", "Capturar paquetes para análisis");
        capturePacketsCommand.AddOption(new Option<string>("--vm", "VM donde capturar") { IsRequired = true });
        capturePacketsCommand.AddOption(new Option<string>("--filter", "Filtro de captura"));
        capturePacketsCommand.AddOption(new Option<int>("--duration", "Duración en minutos"));
        capturePacketsCommand.AddOption(new Option<string>("--output", "Archivo de salida"));
        
        capturePacketsCommand.SetHandler(async (string vm, string filter, int duration, string output) =>
        {
            var capturer = host.Services.GetRequiredService<IPacketCapturer>();
            await capturer.CapturePacketsAsync(vm, filter, duration, output);
        });
        
        // Comando: generate-report
        var generateReportCommand = new Command("generate-report", "Generar reporte de análisis");
        generateReportCommand.AddOption(new Option<string>("--type", "Tipo de reporte (connectivity/security/performance)") { IsRequired = true });
        generateReportCommand.AddOption(new Option<string>("--resource-group", "Grupo de recursos"));
        generateReportCommand.AddOption(new Option<string>("--format", "Formato (html/pdf/json)"));
        generateReportCommand.AddOption(new Option<string>("--output", "Archivo de salida"));
        
        generateReportCommand.SetHandler(async (string type, string resourceGroup, string format, string output) =>
        {
            var reporter = host.Services.GetRequiredService<IReportGenerator>();
            await reporter.GenerateReportAsync(type, resourceGroup, format, output);
        });
        
        // Agregar comandos al root command
        rootCommand.AddCommand(testAllCommand);
        rootCommand.AddCommand(testConnectivityCommand);
        rootCommand.AddCommand(testSecurityRulesCommand);
        rootCommand.AddCommand(analyzeTopologyCommand);
        rootCommand.AddCommand(analyzeFlowLogsCommand);
        rootCommand.AddCommand(capturePacketsCommand);
        rootCommand.AddCommand(generateReportCommand);
        
        // Ejecutar comando
        return await rootCommand.InvokeAsync(args);
    }
    
    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddEnvironmentVariables()
                      .AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Registrar servicios
                services.AddScoped<IConnectivityTester, ConnectivityTester>();
                services.AddScoped<ISecurityRuleAnalyzer, SecurityRuleAnalyzer>();
                services.AddScoped<ITopologyAnalyzer, TopologyAnalyzer>();
                services.AddScoped<IFlowLogAnalyzer, FlowLogAnalyzer>();
                services.AddScoped<IPacketCapturer, PacketCapturer>();
                services.AddScoped<IReportGenerator, ReportGenerator>();
                services.AddScoped<IAzureNetworkWatcherService, AzureNetworkWatcherService>();
                services.AddScoped<INetworkResourceService, NetworkResourceService>();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });
} 