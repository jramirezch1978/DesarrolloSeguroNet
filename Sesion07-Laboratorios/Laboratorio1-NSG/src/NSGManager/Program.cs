using System.CommandLine;
using Azure.Identity;
using Azure.ResourceManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSGManager.Services;
using NSGManager.Models;

namespace NSGManager;

/// <summary>
/// Programa principal para gestión avanzada de Network Security Groups
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
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

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true);
                config.AddEnvironmentVariables("NSG_");
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
                services.AddSingleton<INSGService, NSGService>();
                services.AddSingleton<IASGService, ASGService>();
                services.AddSingleton<INetworkWatcherService, NetworkWatcherService>();
                services.AddSingleton<IValidationService, ValidationService>();
                services.AddSingleton<IReportingService, ReportingService>();
                
                // Configurar opciones
                services.Configure<NSGManagerOptions>(configuration.GetSection("NSGManager"));
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });

    static RootCommand CreateRootCommand(IServiceProvider services)
    {
        // Opciones globales
        var resourceGroupOption = new Option<string>(
            "--resource-group",
            description: "Nombre del resource group donde operar") { IsRequired = true };
        
        var locationOption = new Option<string>(
            "--location", 
            getDefaultValue: () => "eastus",
            description: "Ubicación de Azure para crear recursos");
            
        var subscriptionOption = new Option<string>(
            "--subscription",
            description: "ID de la suscripción de Azure (opcional)");

        var rootCommand = new RootCommand("🛡️ NSG Manager - Gestión Avanzada de Network Security Groups")
        {
            CreateBasicCommand(services, resourceGroupOption, locationOption, subscriptionOption),
            CreateAdvancedCommand(services, resourceGroupOption, locationOption, subscriptionOption),
            CreateValidateCommand(services, resourceGroupOption, locationOption, subscriptionOption),
            CreateReportCommand(services, resourceGroupOption, locationOption, subscriptionOption),
            CreateCleanupCommand(services, resourceGroupOption, locationOption, subscriptionOption)
        };

        rootCommand.AddGlobalOption(resourceGroupOption);
        rootCommand.AddGlobalOption(locationOption);
        rootCommand.AddGlobalOption(subscriptionOption);

        return rootCommand;
    }

    static Command CreateBasicCommand(IServiceProvider services, Option<string> resourceGroupOption, Option<string> locationOption, Option<string> subscriptionOption)
    {
        var command = new Command("create-basic", "🔧 Crear NSGs básicos con reglas estándar");
        
        var vnetNameOption = new Option<string>(
            "--vnet-name",
            getDefaultValue: () => "vnet-nsg-lab",
            description: "Nombre de la Virtual Network");

        command.AddOption(vnetNameOption);
        
        command.SetHandler(async (resourceGroup, location, subscription, vnetName) =>
        {
            var nsgService = services.GetRequiredService<INSGService>();
            var logger = services.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation("🚀 Iniciando creación de NSGs básicos...");
            
            var options = new NSGCreationOptions
            {
                ResourceGroupName = resourceGroup,
                Location = location,
                SubscriptionId = subscription,
                VNetName = vnetName,
                CreateBasicRules = true
            };
            
            await nsgService.CreateBasicNSGsAsync(options);
            
            logger.LogInformation("✅ NSGs básicos creados exitosamente");
            
        }, resourceGroupOption, locationOption, subscriptionOption, vnetNameOption);

        return command;
    }

    static Command CreateAdvancedCommand(IServiceProvider services, Option<string> resourceGroupOption, Option<string> locationOption, Option<string> subscriptionOption)
    {
        var command = new Command("create-advanced", "🎯 Crear NSGs avanzados con ASGs y reglas granulares");
        
        var enableASGsOption = new Option<bool>(
            "--enable-asgs",
            getDefaultValue: () => true,
            description: "Habilitar Application Security Groups");
            
        var enableFlowLogsOption = new Option<bool>(
            "--enable-flow-logs",
            getDefaultValue: () => true,
            description: "Habilitar Flow Logs para monitoreo");

        command.AddOption(enableASGsOption);
        command.AddOption(enableFlowLogsOption);
        
        command.SetHandler(async (resourceGroup, location, subscription, enableASGs, enableFlowLogs) =>
        {
            var nsgService = services.GetRequiredService<INSGService>();
            var asgService = services.GetRequiredService<IASGService>();
            var logger = services.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation("🚀 Iniciando creación de NSGs avanzados...");
            
            var options = new NSGCreationOptions
            {
                ResourceGroupName = resourceGroup,
                Location = location,
                SubscriptionId = subscription,
                EnableApplicationSecurityGroups = enableASGs,
                EnableFlowLogs = enableFlowLogs,
                CreateAdvancedRules = true
            };
            
            // Crear ASGs primero si están habilitados
            if (enableASGs)
            {
                logger.LogInformation("📋 Creando Application Security Groups...");
                await asgService.CreateApplicationSecurityGroupsAsync(options);
            }
            
            // Crear NSGs avanzados
            await nsgService.CreateAdvancedNSGsAsync(options);
            
            logger.LogInformation("✅ NSGs avanzados creados exitosamente");
            
        }, resourceGroupOption, locationOption, subscriptionOption, enableASGsOption, enableFlowLogsOption);

        return command;
    }

    static Command CreateValidateCommand(IServiceProvider services, Option<string> resourceGroupOption, Option<string> locationOption, Option<string> subscriptionOption)
    {
        var command = new Command("validate", "🔍 Validar configuración de NSGs existentes");
        
        var detailedOption = new Option<bool>(
            "--detailed",
            getDefaultValue: () => false,
            description: "Mostrar análisis detallado de cada regla");

        command.AddOption(detailedOption);
        
        command.SetHandler(async (resourceGroup, location, subscription, detailed) =>
        {
            var validationService = services.GetRequiredService<IValidationService>();
            var logger = services.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation("🔍 Iniciando validación de NSGs...");
            
            var options = new ValidationOptions
            {
                ResourceGroupName = resourceGroup,
                SubscriptionId = subscription,
                DetailedAnalysis = detailed
            };
            
            var results = await validationService.ValidateNSGConfigurationAsync(options);
            
            // Mostrar resultados
            DisplayValidationResults(results, logger);
            
        }, resourceGroupOption, locationOption, subscriptionOption, detailedOption);

        return command;
    }

    static Command CreateReportCommand(IServiceProvider services, Option<string> resourceGroupOption, Option<string> locationOption, Option<string> subscriptionOption)
    {
        var command = new Command("security-report", "📊 Generar reporte de seguridad completo");
        
        var outputFormatOption = new Option<string>(
            "--format",
            getDefaultValue: () => "console",
            description: "Formato de salida: console, json, html, csv");
        
        // Configurar valores válidos para la opción format
        outputFormatOption.AddValidator(result =>
        {
            var value = result.GetValueForOption(outputFormatOption);
            if (value != null && !new[] { "console", "json", "html", "csv" }.Contains(value.ToLower()))
            {
                result.ErrorMessage = $"Formato '{value}' no válido. Usar: console, json, html, csv";
            }
        });
            
        var outputFileOption = new Option<string>(
            "--output",
            description: "Archivo de salida (opcional)");

        command.AddOption(outputFormatOption);
        command.AddOption(outputFileOption);
        
        command.SetHandler(async (resourceGroup, location, subscription, format, output) =>
        {
            var reportingService = services.GetRequiredService<IReportingService>();
            var logger = services.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation("📊 Generando reporte de seguridad...");
            
            var options = new ReportOptions
            {
                ResourceGroupName = resourceGroup,
                SubscriptionId = subscription,
                Format = format?.ToLower() ?? "console",
                OutputFile = output,
                IncludeRecommendations = true,
                IncludeComplianceCheck = true
            };
            
            await reportingService.GenerateSecurityReportAsync(options);
            
            logger.LogInformation("✅ Reporte generado exitosamente");
            
        }, resourceGroupOption, locationOption, subscriptionOption, outputFormatOption, outputFileOption);

        return command;
    }

    static Command CreateCleanupCommand(IServiceProvider services, Option<string> resourceGroupOption, Option<string> locationOption, Option<string> subscriptionOption)
    {
        var command = new Command("cleanup", "🧹 Limpiar recursos creados por el laboratorio");
        
        var confirmOption = new Option<bool>(
            "--confirm",
            getDefaultValue: () => false,
            description: "Confirmar eliminación sin prompt interactivo");

        command.AddOption(confirmOption);
        
        command.SetHandler(async (resourceGroup, location, subscription, confirm) =>
        {
            var nsgService = services.GetRequiredService<INSGService>();
            var logger = services.GetRequiredService<ILogger<Program>>();
            
            if (!confirm)
            {
                Console.Write("⚠️  ¿Está seguro de que desea eliminar todos los recursos? (y/N): ");
                var response = Console.ReadLine();
                if (response?.ToLower() != "y" && response?.ToLower() != "yes")
                {
                    logger.LogInformation("❌ Operación cancelada por el usuario");
                    return;
                }
            }
            
            logger.LogInformation("🧹 Iniciando limpieza de recursos...");
            
            var options = new CleanupOptions
            {
                ResourceGroupName = resourceGroup,
                SubscriptionId = subscription
            };
            
            await nsgService.CleanupResourcesAsync(options);
            
            logger.LogInformation("✅ Recursos eliminados exitosamente");
            
        }, resourceGroupOption, locationOption, subscriptionOption, confirmOption);

        return command;
    }

    static void DisplayValidationResults(ValidationResults results, ILogger logger)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("📋 RESULTADOS DE VALIDACIÓN");
        Console.WriteLine("════════════════════════════════════════");
        Console.ResetColor();
        
        // Resumen general
        Console.WriteLine($"📊 NSGs analizados: {results.AnalyzedNSGs}");
        Console.WriteLine($"📊 Reglas evaluadas: {results.TotalRules}");
        Console.WriteLine($"✅ Reglas válidas: {results.ValidRules}");
        Console.WriteLine($"⚠️  Advertencias: {results.Warnings.Count}");
        Console.WriteLine($"❌ Errores: {results.Errors.Count}");
        
        // Mostrar advertencias
        if (results.Warnings.Any())
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  ADVERTENCIAS:");
            Console.ResetColor();
            foreach (var warning in results.Warnings)
            {
                Console.WriteLine($"  • {warning}");
            }
        }
        
        // Mostrar errores
        if (results.Errors.Any())
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ ERRORES:");
            Console.ResetColor();
            foreach (var error in results.Errors)
            {
                Console.WriteLine($"  • {error}");
            }
        }
        
        // Score de seguridad
        var securityScore = CalculateSecurityScore(results);
        Console.WriteLine();
        Console.ForegroundColor = GetScoreColor(securityScore);
        Console.WriteLine($"🛡️  Puntuación de Seguridad: {securityScore:F1}/100");
        Console.ResetColor();
        
        // Recomendaciones
        var recommendations = GenerateRecommendations(results);
        if (recommendations.Any())
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("💡 RECOMENDACIONES:");
            Console.ResetColor();
            foreach (var recommendation in recommendations)
            {
                Console.WriteLine($"  • {recommendation}");
            }
        }
    }
    
    static double CalculateSecurityScore(ValidationResults results)
    {
        if (results.TotalRules == 0) return 0;
        
        var baseScore = (double)results.ValidRules / results.TotalRules * 100;
        var penaltyForErrors = results.Errors.Count * 10;
        var penaltyForWarnings = results.Warnings.Count * 5;
        
        return Math.Max(0, baseScore - penaltyForErrors - penaltyForWarnings);
    }
    
    static ConsoleColor GetScoreColor(double score)
    {
        return score switch
        {
            >= 80 => ConsoleColor.Green,
            >= 60 => ConsoleColor.Yellow,
            _ => ConsoleColor.Red
        };
    }
    
    static IEnumerable<string> GenerateRecommendations(ValidationResults results)
    {
        var recommendations = new List<string>();
        
        if (results.Errors.Any(e => e.Contains("too permissive")))
        {
            recommendations.Add("Considere hacer las reglas más restrictivas usando rangos de puertos específicos");
        }
        
        if (results.Warnings.Any(w => w.Contains("Service Tag")))
        {
            recommendations.Add("Use Service Tags en lugar de rangos IP específicos para mejor mantenimiento");
        }
        
        if (results.ValidRules < results.TotalRules * 0.8)
        {
            recommendations.Add("Revise y corrija las reglas inválidas para mejorar la postura de seguridad");
        }
        
        return recommendations;
    }
} 