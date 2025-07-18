using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;

Console.WriteLine("🛡️ LABORATORIO 2: NETWORK SECURITY GROUPS (NSGs)");
Console.WriteLine("===============================================");
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

Console.WriteLine("📋 CONFIGURACIÓN DE NSGs Y REGLAS DE SEGURIDAD");
Console.WriteLine("===============================================");
Console.WriteLine();

Console.WriteLine("🎯 Objetivo: Implementar seguridad granular por subnet");
Console.WriteLine();

Console.WriteLine("🛡️ NSGs A CREAR:");
Console.WriteLine("================");
Console.WriteLine();

// NSG DMZ
Console.WriteLine("1. NSG-DMZ (Zona Desmilitarizada):");
Console.WriteLine("   Nombre: nsg-dmz-[sunombre]");
Console.WriteLine("   Reglas:");
Console.WriteLine("   ✅ Allow HTTP/HTTPS desde Internet (80,443)");
Console.WriteLine("   ✅ Allow SSH desde Management (22)");
Console.WriteLine("   ❌ Deny todo lo demás");
Console.WriteLine();

// NSG Private
Console.WriteLine("2. NSG-Private (Aplicaciones Internas):");
Console.WriteLine("   Nombre: nsg-private-[sunombre]");
Console.WriteLine("   Reglas:");
Console.WriteLine("   ✅ Allow desde DMZ (80,443,8080,8443)");
Console.WriteLine("   ✅ Allow comunicación interna");
Console.WriteLine("   ✅ Allow acceso a Data subnet (DB ports)");
Console.WriteLine("   ❌ Deny Internet directo");
Console.WriteLine();

// NSG Data
Console.WriteLine("3. NSG-Data (Bases de Datos):");
Console.WriteLine("   Nombre: nsg-data-[sunombre]");
Console.WriteLine("   Reglas:");
Console.WriteLine("   ✅ Allow SOLO desde Private subnet");
Console.WriteLine("   ✅ Allow backup desde Management");
Console.WriteLine("   ❌ Deny todo lo demás (máxima seguridad)");
Console.WriteLine();

// NSG Management
Console.WriteLine("4. NSG-Management (Administración):");
Console.WriteLine("   Nombre: nsg-management-[sunombre]");
Console.WriteLine("   Reglas:");
Console.WriteLine("   ✅ Allow SSH/RDP desde IP específica");
Console.WriteLine("   ✅ Allow acceso a todas las subnets");
Console.WriteLine();

Console.WriteLine("⚡ COMANDOS AZURE CLI PARA NSGs:");
Console.WriteLine("===============================");
Console.WriteLine();

// Mostrar comandos CLI para NSGs
var nsgCommands = new[]
{
    "# ===== NSG DMZ =====",
    "az network nsg create \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --name nsg-dmz-[sunombre] \\",
    "  --location eastus",
    "",
    "# Regla 1: HTTP/HTTPS desde Internet",
    "az network nsg rule create \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --nsg-name nsg-dmz-[sunombre] \\",
    "  --name Allow-HTTP-HTTPS-Inbound \\",
    "  --priority 100 \\",
    "  --source-address-prefixes '*' \\",
    "  --destination-port-ranges 80 443 \\",
    "  --access Allow \\",
    "  --protocol Tcp",
    "",
    "# Regla 2: SSH desde Management",
    "az network nsg rule create \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --nsg-name nsg-dmz-[sunombre] \\",
    "  --name Allow-SSH-Management \\",
    "  --priority 110 \\",
    "  --source-address-prefixes 10.1.10.0/24 \\",
    "  --destination-port-ranges 22 \\",
    "  --access Allow \\",
    "  --protocol Tcp",
    "",
    "# ===== NSG PRIVATE =====",
    "az network nsg create \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --name nsg-private-[sunombre] \\",
    "  --location eastus",
    "",
    "# Regla: Permitir desde DMZ",
    "az network nsg rule create \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --nsg-name nsg-private-[sunombre] \\",
    "  --name Allow-DMZ-to-Private \\",
    "  --priority 100 \\",
    "  --source-address-prefixes 10.1.1.0/24 \\",
    "  --destination-address-prefixes 10.1.2.0/24 \\",
    "  --destination-port-ranges 80 443 8080 8443 \\",
    "  --access Allow \\",
    "  --protocol Tcp",
    "",
    "# ===== NSG DATA =====",
    "az network nsg create \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --name nsg-data-[sunombre] \\",
    "  --location eastus",
    "",
    "# Regla: Solo Private puede acceder",
    "az network nsg rule create \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --nsg-name nsg-data-[sunombre] \\",
    "  --name Allow-Private-to-Data-DB \\",
    "  --priority 100 \\",
    "  --source-address-prefixes 10.1.2.0/24 \\",
    "  --destination-address-prefixes 10.1.3.0/24 \\",
    "  --destination-port-ranges 1433 3306 5432 6379 \\",
    "  --access Allow \\",
    "  --protocol Tcp",
    "",
    "# ===== NSG MANAGEMENT =====",
    "az network nsg create \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --name nsg-management-[sunombre] \\",
    "  --location eastus",
    "",
    "# Regla: SSH/RDP desde IP específica",
    "az network nsg rule create \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --nsg-name nsg-management-[sunombre] \\",
    "  --name Allow-Admin-Access \\",
    "  --priority 100 \\",
    "  --source-address-prefixes [SU-IP-PUBLICA] \\",
    "  --destination-address-prefixes 10.1.10.0/24 \\",
    "  --destination-port-ranges 22 3389 \\",
    "  --access Allow \\",
    "  --protocol Tcp"
};

foreach (var command in nsgCommands)
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
Console.WriteLine("🔗 ASOCIAR NSGs CON SUBNETS:");
Console.WriteLine("============================");
Console.WriteLine();

var associationCommands = new[]
{
    "# Asociar NSG-DMZ con DMZ Subnet",
    "az network vnet subnet update \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --vnet-name vnet-principal-[sunombre] \\",
    "  --name snet-dmz \\",
    "  --network-security-group nsg-dmz-[sunombre]",
    "",
    "# Asociar NSG-Private con Private Subnet",
    "az network vnet subnet update \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --vnet-name vnet-principal-[sunombre] \\",
    "  --name snet-private \\",
    "  --network-security-group nsg-private-[sunombre]",
    "",
    "# Asociar NSG-Data con Data Subnet",
    "az network vnet subnet update \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --vnet-name vnet-principal-[sunombre] \\",
    "  --name snet-data \\",
    "  --network-security-group nsg-data-[sunombre]",
    "",
    "# Asociar NSG-Management con Management Subnet",
    "az network vnet subnet update \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --vnet-name vnet-principal-[sunombre] \\",
    "  --name snet-management \\",
    "  --network-security-group nsg-management-[sunombre]"
};

foreach (var command in associationCommands)
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
logger.LogInformation("Guía de comandos NSG mostrada correctamente");

Console.WriteLine("📋 PRÓXIMOS PASOS:");
Console.WriteLine("==================");
Console.WriteLine("1. ✅ Crear los 4 NSGs con Azure CLI");
Console.WriteLine("2. ✅ Configurar reglas de seguridad específicas");
Console.WriteLine("3. ✅ Asociar cada NSG con su subnet correspondiente");
Console.WriteLine("4. ✅ Verificar reglas efectivas en Azure Portal");
Console.WriteLine("5. ✅ Preparar para Laboratorio 3 - Azure Bastion");
Console.WriteLine();

Console.WriteLine("🎉 LABORATORIO 2 - CONFIGURACIÓN COMPLETADA");
Console.WriteLine("✅ Las reglas de seguridad están listas para implementar");
Console.WriteLine();

return 0;