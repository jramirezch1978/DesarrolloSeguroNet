using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Compute;

Console.WriteLine("🦘 LABORATORIO 3: AZURE BASTION & JUMP BOX");
Console.WriteLine("==========================================");
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

Console.WriteLine("📋 CONFIGURACIÓN DE ACCESO ADMINISTRATIVO SEGURO");
Console.WriteLine("================================================");
Console.WriteLine();

Console.WriteLine("🎯 Objetivo: Eliminar exposición directa a Internet para administración");
Console.WriteLine();

Console.WriteLine("🏗️ COMPONENTES A IMPLEMENTAR:");
Console.WriteLine("==============================");
Console.WriteLine();

// Azure Bastion Subnet
Console.WriteLine("1. Azure Bastion Subnet:");
Console.WriteLine("   Nombre: AzureBastionSubnet (OBLIGATORIO este nombre exacto)");
Console.WriteLine("   Address Range: 10.1.100.0/26 (mínimo /26 requerido)");
Console.WriteLine("   Propósito: Subnet dedicada para Azure Bastion");
Console.WriteLine();

// Azure Bastion
Console.WriteLine("2. Azure Bastion (Servicio Administrado):");
Console.WriteLine("   Nombre: bastion-[sunombre]");
Console.WriteLine("   SKU: Basic (para laboratorio)");
Console.WriteLine("   Public IP: pip-bastion-[sunombre]");
Console.WriteLine("   Beneficios:");
Console.WriteLine("   ✅ Acceso RDP/SSH sin exponer VMs a Internet");
Console.WriteLine("   ✅ Conexión segura vía HTTPS desde navegador");
Console.WriteLine("   ✅ Sin necesidad de VPN cliente");
Console.WriteLine();

// Jump Box VM
Console.WriteLine("3. Jump Box VM (Alternativa económica):");
Console.WriteLine("   Nombre: vm-jumpbox-[sunombre]");
Console.WriteLine("   Subnet: snet-management (10.1.10.0/24)");
Console.WriteLine("   OS: Windows Server 2022 Datacenter");
Console.WriteLine("   Size: Standard_B2s (2 vCPUs, 4 GB RAM)");
Console.WriteLine("   Public IP: None (acceso solo vía Bastion)");
Console.WriteLine();

Console.WriteLine("⚡ COMANDOS AZURE CLI PARA BASTION:");
Console.WriteLine("=================================");
Console.WriteLine();

// Mostrar comandos CLI para Bastion
var bastionCommands = new[]
{
    "# ===== PASO 1: CREAR AZURE BASTION SUBNET =====",
    "az network vnet subnet create \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --vnet-name vnet-principal-[sunombre] \\",
    "  --name AzureBastionSubnet \\",
    "  --address-prefix 10.1.100.0/26",
    "",
    "# ===== PASO 2: CREAR PUBLIC IP PARA BASTION =====",
    "az network public-ip create \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --name pip-bastion-[sunombre] \\",
    "  --sku Standard \\",
    "  --allocation-method Static \\",
    "  --location eastus",
    "",
    "# ===== PASO 3: CREAR AZURE BASTION =====",
    "az network bastion create \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --name bastion-[sunombre] \\",
    "  --public-ip-address pip-bastion-[sunombre] \\",
    "  --vnet-name vnet-principal-[sunombre] \\",
    "  --location eastus",
    "",
    "# NOTA: Azure Bastion toma 5-10 minutos en deployar",
};

foreach (var command in bastionCommands)
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
Console.WriteLine("🖥️ COMANDOS PARA JUMP BOX VM:");
Console.WriteLine("============================");
Console.WriteLine();

var jumpboxCommands = new[]
{
    "# ===== CREAR JUMP BOX VM =====",
    "az vm create \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --name vm-jumpbox-[sunombre] \\",
    "  --image Win2022Datacenter \\",
    "  --size Standard_B2s \\",
    "  --vnet-name vnet-principal-[sunombre] \\",
    "  --subnet snet-management \\",
    "  --public-ip-address \"\" \\",
    "  --nsg \"\" \\",
    "  --admin-username azureadmin \\",
    "  --admin-password \"JumpBox2024!\" \\",
    "  --location eastus",
    "",
    "# ===== CONFIGURAR AUTO-SHUTDOWN =====",
    "az vm auto-shutdown \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --name vm-jumpbox-[sunombre] \\",
    "  --time 2300",
};

foreach (var command in jumpboxCommands)
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
Console.WriteLine("🔐 ACTUALIZAR NSG PARA BASTION:");
Console.WriteLine("==============================");
Console.WriteLine();

var nsgUpdateCommands = new[]
{
    "# Agregar regla para permitir Bastion → Management subnet",
    "az network nsg rule create \\",
    "  --resource-group rg-infraestructura-segura-[SuNombre] \\",
    "  --nsg-name nsg-management-[sunombre] \\",
    "  --name Allow-Bastion-Inbound \\",
    "  --priority 90 \\",
    "  --source-address-prefixes 10.1.100.0/26 \\",
    "  --destination-address-prefixes 10.1.10.0/24 \\",
    "  --destination-port-ranges 3389 22 \\",
    "  --access Allow \\",
    "  --protocol Tcp",
};

foreach (var command in nsgUpdateCommands)
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
logger.LogInformation("Guía de comandos Azure Bastion mostrada correctamente");

Console.WriteLine("📋 PRÓXIMOS PASOS:");
Console.WriteLine("==================");
Console.WriteLine("1. ✅ Crear AzureBastionSubnet con el nombre exacto");
Console.WriteLine("2. ✅ Deployar Azure Bastion (5-10 minutos)");
Console.WriteLine("3. ✅ Crear Jump Box VM sin Public IP");
Console.WriteLine("4. ✅ Actualizar NSG para permitir tráfico desde Bastion");
Console.WriteLine("5. ✅ Probar acceso seguro vía Azure Portal");
Console.WriteLine("6. ✅ Preparar para Laboratorio 4 - Testing y Validación");
Console.WriteLine();

Console.WriteLine("💡 IMPORTANTE - BENEFICIOS DE SEGURIDAD:");
Console.WriteLine("========================================");
Console.WriteLine("✅ Zero Internet exposure para VMs administrativas");
Console.WriteLine("✅ Acceso centralizado y auditado");
Console.WriteLine("✅ No necesidad de VPN cliente");
Console.WriteLine("✅ Sesiones RDP/SSH encriptadas vía HTTPS");
Console.WriteLine("✅ Integration con Azure AD y RBAC");
Console.WriteLine();

Console.WriteLine("🎉 LABORATORIO 3 - CONFIGURACIÓN COMPLETADA");
Console.WriteLine("✅ Acceso administrativo seguro configurado sin exposición directa");
Console.WriteLine();

return 0;