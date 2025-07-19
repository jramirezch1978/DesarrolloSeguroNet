using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;

Console.WriteLine("🧪 LABORATORIO 4: TESTING & ARQUITECTURA HUB-AND-SPOKE");
Console.WriteLine("======================================================");
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

Console.WriteLine("📋 VALIDACIÓN DE INFRAESTRUCTURA IMPLEMENTADA");
Console.WriteLine("=============================================");
Console.WriteLine();

Console.WriteLine("🎯 Objetivo: Validar arquitectura y preparar escalabilidad futura");
Console.WriteLine();

Console.WriteLine("✅ CHECKLIST DE VALIDACIÓN:");
Console.WriteLine("===========================");
Console.WriteLine();

Console.WriteLine("🌐 1. VIRTUAL NETWORK:");
Console.WriteLine("   ☐ VNET creada: vnet-principal-[sunombre] (10.1.0.0/16)");
Console.WriteLine("   ☐ DMZ Subnet: snet-dmz (10.1.1.0/24)");
Console.WriteLine("   ☐ Private Subnet: snet-private (10.1.2.0/24)");
Console.WriteLine("   ☐ Data Subnet: snet-data (10.1.3.0/24)");
Console.WriteLine("   ☐ Management Subnet: snet-management (10.1.10.0/24)");
Console.WriteLine("   ☐ Azure Bastion Subnet: AzureBastionSubnet (10.1.100.0/26)");
Console.WriteLine();

Console.WriteLine("🛡️ 2. NETWORK SECURITY GROUPS:");
Console.WriteLine("   ☐ NSG-DMZ: Permite HTTP/HTTPS desde Internet");
Console.WriteLine("   ☐ NSG-Private: Solo acceso desde DMZ");
Console.WriteLine("   ☐ NSG-Data: Solo acceso desde Private");
Console.WriteLine("   ☐ NSG-Management: Acceso administrativo controlado");
Console.WriteLine("   ☐ Todos los NSGs asociados a sus subnets");
Console.WriteLine();

Console.WriteLine("🦘 3. ACCESO ADMINISTRATIVO:");
Console.WriteLine("   ☐ Azure Bastion deployado");
Console.WriteLine("   ☐ Jump Box VM sin Public IP");
Console.WriteLine("   ☐ Acceso seguro funcionando");
Console.WriteLine("   ☐ Zero exposición directa a Internet");
Console.WriteLine();

Console.WriteLine("⚡ COMANDOS DE TESTING CON AZURE CLI:");
Console.WriteLine("===================================");
Console.WriteLine();

var testingCommands = new[]
{
    "# ===== VERIFICAR RECURSOS CREADOS =====",
    "# Listar VNET y subnets",
    "az network vnet subnet list \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --vnet-name vnet-principal-[sunombre] \\",
    "  --output table",
    "",
    "# Verificar NSGs",
    "az network nsg list \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --output table",
    "",
    "# ===== VERIFICAR REGLAS DE SEGURIDAD =====",
    "# Verificar reglas efectivas para DMZ",
    "az network nsg show \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --name nsg-dmz-[sunombre] \\",
    "  --query 'securityRules[].{Name:name,Priority:priority,Access:access,Protocol:protocol,Direction:direction}' \\",
    "  --output table",
    "",
    "# ===== TESTING DE CONECTIVIDAD =====",
    "# Verificar Network Watcher habilitado",
    "az network watcher list --output table",
    "",
    "# Test 1: ¿DMZ puede acceder a Private?",
    "az network watcher test-ip-flow \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --vm vm-jumpbox-[sunombre] \\",
    "  --direction Outbound \\",
    "  --protocol TCP \\",
    "  --local 10.1.10.10:443 \\",
    "  --remote 10.1.2.10:443",
    "",
    "# Test 2: ¿Internet puede acceder a Data? (debe ser DENY)",
    "az network watcher test-ip-flow \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --vm vm-jumpbox-[sunombre] \\",
    "  --direction Inbound \\",
    "  --protocol TCP \\",
    "  --local 10.1.3.10:1433 \\",
    "  --remote 1.1.1.1:80"
};

foreach (var command in testingCommands)
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
Console.WriteLine("🏗️ PLANIFICACIÓN HUB-AND-SPOKE:");
Console.WriteLine("===============================");
Console.WriteLine();

Console.WriteLine("📊 ARQUITECTURA ACTUAL (HUB):");
Console.WriteLine("   Hub VNET: vnet-principal-[sunombre] (10.1.0.0/16)");
Console.WriteLine("   ✅ Servicios compartidos: Bastion, NSGs, Management");
Console.WriteLine("   ✅ Conectividad: Centralizada y segura");
Console.WriteLine();

Console.WriteLine("🎯 EXPANSIÓN FUTURA (SPOKES):");
Console.WriteLine("   Spoke 1 - Production: 10.2.0.0/16");
Console.WriteLine("   Spoke 2 - Development: 10.3.0.0/16");
Console.WriteLine("   Spoke 3 - Testing: 10.4.0.0/16");
Console.WriteLine();

Console.WriteLine("🔗 COMANDOS PARA HUB-AND-SPOKE (FUTUROS):");
Console.WriteLine("========================================");
Console.WriteLine();

var hubSpokeCommands = new[]
{
    "# ===== CREAR SPOKE PRODUCTION =====",
    "az network vnet create \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --name vnet-production-spoke \\",
    "  --address-prefix 10.2.0.0/16 \\",
    "  --location eastus",
    "",
    "# ===== CREAR VNET PEERING HUB → SPOKE =====",
    "az network vnet peering create \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --vnet-name vnet-principal-[sunombre] \\",
    "  --name hub-to-production \\",
    "  --remote-vnet vnet-production-spoke \\",
    "  --allow-vnet-access",
    "",
    "# ===== CREAR VNET PEERING SPOKE → HUB =====",
    "az network vnet peering create \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --vnet-name vnet-production-spoke \\",
    "  --name production-to-hub \\",
    "  --remote-vnet vnet-principal-[sunombre] \\",
    "  --allow-vnet-access"
};

foreach (var command in hubSpokeCommands)
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
logger.LogInformation("Guía de testing y arquitectura mostrada correctamente");

Console.WriteLine("📋 PRÓXIMOS PASOS:");
Console.WriteLine("==================");
Console.WriteLine("1. ✅ Ejecutar comandos de verificación");
Console.WriteLine("2. ✅ Validar conectividad con Network Watcher");
Console.WriteLine("3. ✅ Documentar arquitectura implementada");
Console.WriteLine("4. ✅ Confirmar principios de seguridad aplicados");
Console.WriteLine("5. ✅ Planificar expansión Hub-and-Spoke");
Console.WriteLine();

Console.WriteLine("🎖️ PRINCIPIOS DE SEGURIDAD IMPLEMENTADOS:");
Console.WriteLine("=========================================");
Console.WriteLine("✅ Defense in Depth: Múltiples capas de seguridad");
Console.WriteLine("✅ Least Privilege: Acceso mínimo necesario");
Console.WriteLine("✅ Network Segmentation: Aislamiento por función");
Console.WriteLine("✅ Zero Trust: Verificación explícita de acceso");
Console.WriteLine("✅ Secure by Default: NSGs con deny implícito");
Console.WriteLine();

Console.WriteLine("🎉 LABORATORIO 4 - VALIDACIÓN COMPLETADA");
Console.WriteLine("✅ Arquitectura de red segura implementada y validada");
Console.WriteLine("✅ Base sólida para escalabilidad futura");
Console.WriteLine();

return 0;