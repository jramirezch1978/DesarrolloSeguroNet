using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;

Console.WriteLine("🌐 LABORATORIO 1: VIRTUAL NETWORK CONFIGURATION");
Console.WriteLine("==============================================");
Console.WriteLine();

// Configurar logging
using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
        });
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

Console.WriteLine("📋 CONFIGURACIÓN DE VNET Y SUBNETS");
Console.WriteLine("===================================");
Console.WriteLine();

// Configuración de la VNET
Console.WriteLine("🎯 Objetivo: Crear infraestructura de red segura");
Console.WriteLine();

Console.WriteLine("📊 VNET Principal:");
Console.WriteLine("   Nombre: vnet-principal-[sunombre]");
Console.WriteLine("   Address Space: 10.1.0.0/16");
Console.WriteLine("   Region: East US");
Console.WriteLine("   DNS: Azure Default");
Console.WriteLine();

Console.WriteLine("🔗 SUBNETS A CREAR:");
Console.WriteLine("   1. DMZ Subnet:");
Console.WriteLine("      - Nombre: snet-dmz");
Console.WriteLine("      - Address Range: 10.1.1.0/24");
Console.WriteLine("      - Propósito: Servicios expuestos a Internet");
Console.WriteLine();

Console.WriteLine("   2. Private Subnet:");
Console.WriteLine("      - Nombre: snet-private");
Console.WriteLine("      - Address Range: 10.1.2.0/24");
Console.WriteLine("      - Propósito: Aplicaciones internas");
Console.WriteLine();

Console.WriteLine("   3. Data Subnet:");
Console.WriteLine("      - Nombre: snet-data");
Console.WriteLine("      - Address Range: 10.1.3.0/24");
Console.WriteLine("      - Propósito: Bases de datos y almacenamiento");
Console.WriteLine();

Console.WriteLine("   4. Management Subnet:");
Console.WriteLine("      - Nombre: snet-management");
Console.WriteLine("      - Address Range: 10.1.10.0/24");
Console.WriteLine("      - Propósito: Administración y monitoreo");
Console.WriteLine();

Console.WriteLine("⚡ COMANDOS AZURE CLI PARA CREAR VNET:");
Console.WriteLine("=====================================");
Console.WriteLine();

// Mostrar comandos CLI
var commands = new[]
{
    "# 1. Crear grupo de recursos",
    "az group create \\",
    "  --name rg-infraestructura-segura-[SuNombre] \\",
    "  --location eastus",
    "",
    "# 2. Crear VNET principal",
    "az network vnet create \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --name vnet-principal-[sunombre] \\",
    "  --address-prefix 10.1.0.0/16 \\",
    "  --location eastus",
    "",
    "# 3. Crear DMZ Subnet",
    "az network vnet subnet create \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --vnet-name vnet-principal-[sunombre] \\",
    "  --name snet-dmz \\",
    "  --address-prefix 10.1.1.0/24",
    "",
    "# 4. Crear Private Subnet",
    "az network vnet subnet create \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --vnet-name vnet-principal-[sunombre] \\",
    "  --name snet-private \\",
    "  --address-prefix 10.1.2.0/24",
    "",
    "# 5. Crear Data Subnet",
    "az network vnet subnet create \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --vnet-name vnet-principal-[sunombre] \\",
    "  --name snet-data \\",
    "  --address-prefix 10.1.3.0/24",
    "",
    "# 6. Crear Management Subnet",
    "az network vnet subnet create \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --vnet-name vnet-principal-[sunombre] \\",
    "  --name snet-management \\",
    "  --address-prefix 10.1.10.0/24"
};

foreach (var command in commands)
{
    if (command.StartsWith("#"))
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(command);
        Console.ResetColor();
    }
    else if (string.IsNullOrEmpty(command))
    {
        Console.WriteLine();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(command);
        Console.ResetColor();
    }
}

Console.WriteLine();
logger.LogInformation("Guía de comandos Azure CLI mostrada correctamente");

Console.WriteLine("📋 PRÓXIMOS PASOS:");
Console.WriteLine("==================");
Console.WriteLine("1. ✅ Ejecutar comandos Azure CLI en el orden mostrado");
Console.WriteLine("2. ✅ Verificar creación de VNET en Azure Portal");
Console.WriteLine("3. ✅ Confirmar que las 4 subnets están creadas");
Console.WriteLine("4. ✅ Documentar los recursos creados");
Console.WriteLine("5. ✅ Preparar para Laboratorio 2 - Network Security Groups");
Console.WriteLine();

Console.WriteLine("🎉 LABORATORIO 1 - CONFIGURACIÓN COMPLETADA");
Console.WriteLine("✅ La infraestructura de red está lista para implementar NSGs");
Console.WriteLine();

return 0;