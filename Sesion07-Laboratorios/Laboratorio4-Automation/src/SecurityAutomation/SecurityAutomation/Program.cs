using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SecurityAutomation.Services;

namespace SecurityAutomation;

public class Program
{
    public static void Main()
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;
                
                // Configurar logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.AddApplicationInsights();
                });
                
                // Registrar HttpClient
                services.AddHttpClient();
                
                // Registrar servicios de automatización de seguridad
                services.AddScoped<IThreatAnalysisService, ThreatAnalysisService>();
                services.AddScoped<IIncidentResponseService, IncidentResponseService>();
                services.AddScoped<IIPBlockingService, IPBlockingService>();
                services.AddScoped<INotificationService, NotificationService>();
                services.AddScoped<IThreatIntelligenceService, ThreatIntelligenceService>();
                services.AddScoped<ISecurityEventService, SecurityEventService>();
                services.AddScoped<IComplianceMonitoringService, ComplianceMonitoringService>();
                services.AddScoped<IMLThreatDetectionService, MLThreatDetectionService>();
                services.AddScoped<IWorkflowOrchestrationService, WorkflowOrchestrationService>();
                
                // Configurar Azure clients
                services.AddSingleton<Azure.Identity.DefaultAzureCredential>();
                services.AddSingleton<Azure.ResourceManager.ArmClient>(provider =>
                {
                    var credential = provider.GetRequiredService<Azure.Identity.DefaultAzureCredential>();
                    return new Azure.ResourceManager.ArmClient(credential);
                });
                
                // Configurar opciones desde configuración
                services.Configure<ThreatIntelligenceOptions>(
                    configuration.GetSection("ThreatIntelligence"));
                services.Configure<SecurityAutomationOptions>(
                    configuration.GetSection("SecurityAutomation"));
                services.Configure<NotificationOptions>(
                    configuration.GetSection("Notifications"));
                services.Configure<ComplianceOptions>(
                    configuration.GetSection("Compliance"));
                
                // Configurar cache en memoria
                services.AddMemoryCache();
            })
            .Build();

        host.Run();
    }
} 