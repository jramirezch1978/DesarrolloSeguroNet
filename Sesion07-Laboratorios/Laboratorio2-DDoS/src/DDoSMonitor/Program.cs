using System.CommandLine;
using Azure.Identity;
using Azure.ResourceManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DDoSMonitor.Services;
using DDoSMonitor.Models;

namespace DDoSMonitor;

/// <summary>
/// Monitor en tiempo real para Azure DDoS Protection
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            ShowWelcomeBanner();
            
            // Configurar host con DI y logging
            var host = CreateHostBuilder(args).Build();
            
            // Crear comandos CLI
            var rootCommand = CreateRootCommand(host.Services);
            
            // Ejecutar comando
            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Error fatal: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    static void ShowWelcomeBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("🛡️  AZURE DDOS PROTECTION MONITOR");
        Console.WriteLine("═══════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine("Monitor en tiempo real para Azure DDoS Protection Standard");
        Console.WriteLine($"Versión 1.0 - {DateTime.Now:yyyy-MM-dd}");
        Console.WriteLine();
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true);
                config.AddEnvironmentVariables("DDOS_");
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;
                
                // Configurar Azure Resource Manager
                services.AddSingleton<ArmClient>(provider =>
                {
                    var credential = new DefaultAzureCredential();
                    return new ArmClient(credential);
                });
                
                // Registrar servicios
                services.AddSingleton<IDDoSMonitoringService, DDoSMonitoringService>();
                services.AddSingleton<IMetricsService, MetricsService>();
                services.AddSingleton<IAlertService, AlertService>();
                services.AddSingleton<IReportingService, ReportingService>();
                
                // Configurar opciones
                services.Configure<DDoSMonitorOptions>(configuration.GetSection("DDoSMonitor"));
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });

    static RootCommand CreateRootCommand(IServiceProvider services)
    {
        var rootCommand = new RootCommand("🛡️ DDoS Monitor - Monitoreo avanzado de Azure DDoS Protection")
        {
            CreateMonitorCommand(services),
            CreateAnalyzeCommand(services),
            CreateReportCommand(services),
            CreateSimulateCommand(services)
        };

        // Opciones globales
        var subscriptionOption = new Option<string>(
            "--subscription",
            description: "ID de la suscripción de Azure");
            
        var resourceGroupOption = new Option<string>(
            "--resource-group",
            description: "Nombre del resource group") { IsRequired = true };
            
        var publicIpOption = new Option<string>(
            "--public-ip",
            description: "Nombre del Public IP a monitorear") { IsRequired = true };

        rootCommand.AddGlobalOption(subscriptionOption);
        rootCommand.AddGlobalOption(resourceGroupOption);
        rootCommand.AddGlobalOption(publicIpOption);

        return rootCommand;
    }

    static Command CreateMonitorCommand(IServiceProvider services)
    {
        var command = new Command("monitor", "🔍 Monitoreo en tiempo real de métricas DDoS");
        
        var intervalOption = new Option<int>(
            "--interval",
            getDefaultValue: () => 30,
            description: "Intervalo de actualización en segundos");
            
        var alertThresholdOption = new Option<int>(
            "--alert-threshold",
            getDefaultValue: () => 1000,
            description: "Umbral de alerta para paquetes bloqueados");
            
        var dashboardOption = new Option<bool>(
            "--dashboard",
            getDefaultValue: () => true,
            description: "Mostrar dashboard visual en consola");

        command.AddOption(intervalOption);
        command.AddOption(alertThresholdOption);
        command.AddOption(dashboardOption);
        
        command.SetHandler(async (subscription, resourceGroup, publicIp, interval, alertThreshold, dashboard) =>
        {
            var monitoringService = services.GetRequiredService<IDDoSMonitoringService>();
            var logger = services.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation("🔍 Iniciando monitoreo en tiempo real...");
            
            var options = new MonitoringOptions
            {
                SubscriptionId = subscription,
                ResourceGroupName = resourceGroup,
                PublicIpName = publicIp,
                UpdateIntervalSeconds = interval,
                AlertThreshold = alertThreshold,
                ShowDashboard = dashboard
            };
            
            await monitoringService.StartRealTimeMonitoringAsync(options);
            
        }, new Argument<string>("subscription"), new Argument<string>("resource-group"), 
           new Argument<string>("public-ip"), intervalOption, alertThresholdOption, dashboardOption);

        return command;
    }

    static Command CreateAnalyzeCommand(IServiceProvider services)
    {
        var command = new Command("analyze", "📊 Analizar métricas históricas de DDoS");
        
        var startTimeOption = new Option<DateTime>(
            "--start-time",
            getDefaultValue: () => DateTime.UtcNow.AddHours(-24),
            description: "Tiempo de inicio del análisis (UTC)");
            
        var endTimeOption = new Option<DateTime>(
            "--end-time", 
            getDefaultValue: () => DateTime.UtcNow,
            description: "Tiempo de fin del análisis (UTC)");
            
        var detailedOption = new Option<bool>(
            "--detailed",
            getDefaultValue: () => false,
            description: "Mostrar análisis detallado");

        command.AddOption(startTimeOption);
        command.AddOption(endTimeOption);
        command.AddOption(detailedOption);
        
        command.SetHandler(async (subscription, resourceGroup, publicIp, startTime, endTime, detailed) =>
        {
            var metricsService = services.GetRequiredService<IMetricsService>();
            var logger = services.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation($"📊 Analizando métricas desde {startTime:yyyy-MM-dd HH:mm} hasta {endTime:yyyy-MM-dd HH:mm}");
            
            var options = new AnalysisOptions
            {
                SubscriptionId = subscription,
                ResourceGroupName = resourceGroup,
                PublicIpName = publicIp,
                StartTime = startTime,
                EndTime = endTime,
                DetailedAnalysis = detailed
            };
            
            await metricsService.AnalyzeHistoricalMetricsAsync(options);
            
        }, new Argument<string>("subscription"), new Argument<string>("resource-group"), 
           new Argument<string>("public-ip"), startTimeOption, endTimeOption, detailedOption);

        return command;
    }

    static Command CreateReportCommand(IServiceProvider services)
    {
        var command = new Command("report", "📄 Generar reporte de actividad DDoS");
        
        var formatOption = new Option<string>(
            "--format",
            getDefaultValue: () => "console",
            description: "Formato del reporte: console, json, html, csv");
            
        var outputOption = new Option<string>(
            "--output",
            description: "Archivo de salida (opcional)");
            
        var includeChartsOption = new Option<bool>(
            "--include-charts",
            getDefaultValue: () => true,
            description: "Incluir gráficos en el reporte");

        command.AddOption(formatOption);
        command.AddOption(outputOption);
        command.AddOption(includeChartsOption);
        
        command.SetHandler(async (subscription, resourceGroup, publicIp, format, output, includeCharts) =>
        {
            var reportingService = services.GetRequiredService<IReportingService>();
            var logger = services.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation($"📄 Generando reporte en formato {format}...");
            
            var options = new ReportOptions
            {
                SubscriptionId = subscription,
                ResourceGroupName = resourceGroup,
                PublicIpName = publicIp,
                Format = format,
                OutputFile = output,
                IncludeCharts = includeCharts
            };
            
            await reportingService.GenerateDDoSReportAsync(options);
            
        }, new Argument<string>("subscription"), new Argument<string>("resource-group"), 
           new Argument<string>("public-ip"), formatOption, outputOption, includeChartsOption);

        return command;
    }

    static Command CreateSimulateCommand(IServiceProvider services)
    {
        var command = new Command("simulate", "⚡ Simular carga para testing (SOLO en recursos propios)");
        
        var targetUrlOption = new Option<string>(
            "--target-url",
            description: "URL objetivo para simulación de carga") { IsRequired = true };
            
        var concurrencyOption = new Option<int>(
            "--concurrency",
            getDefaultValue: () => 10,
            description: "Número de conexiones concurrentes");
            
        var durationOption = new Option<int>(
            "--duration",
            getDefaultValue: () => 60,
            description: "Duración de la simulación en segundos");
            
        var ethicalOption = new Option<bool>(
            "--i-own-this-resource",
            getDefaultValue: () => false,
            description: "Confirmo que este recurso es de mi propiedad") { IsRequired = true };

        command.AddOption(targetUrlOption);
        command.AddOption(concurrencyOption);
        command.AddOption(durationOption);
        command.AddOption(ethicalOption);
        
        command.SetHandler(async (subscription, resourceGroup, publicIp, targetUrl, concurrency, duration, ethical) =>
        {
            if (!ethical)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Debe confirmar que el recurso es de su propiedad usando --i-own-this-resource");
                Console.ResetColor();
                return;
            }
            
            var monitoringService = services.GetRequiredService<IDDoSMonitoringService>();
            var logger = services.GetRequiredService<ILogger<Program>>();
            
            logger.LogWarning("⚡ INICIANDO SIMULACIÓN DE CARGA - SOLO PARA TESTING ÉTICO");
            logger.LogInformation($"🎯 Objetivo: {targetUrl}");
            logger.LogInformation($"⚙️ Concurrencia: {concurrency}");
            logger.LogInformation($"⏱️ Duración: {duration} segundos");
            
            var options = new LoadTestOptions
            {
                TargetUrl = targetUrl,
                Concurrency = concurrency,
                DurationSeconds = duration,
                MonitorDDoSMetrics = true,
                SubscriptionId = subscription,
                ResourceGroupName = resourceGroup,
                PublicIpName = publicIp
            };
            
            await monitoringService.RunLoadTestWithMonitoringAsync(options);
            
        }, new Argument<string>("subscription"), new Argument<string>("resource-group"), 
           new Argument<string>("public-ip"), targetUrlOption, concurrencyOption, durationOption, ethicalOption);

        return command;
    }
} 