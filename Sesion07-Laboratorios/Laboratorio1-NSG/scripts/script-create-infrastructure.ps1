#Requires -Version 5.1

<#
.SYNOPSIS
Script para crear la infraestructura base del Laboratorio 1: Network Security Groups

.DESCRIPTION
Este script crea toda la infraestructura necesaria para el laboratorio de NSGs avanzados,
incluyendo resource group, virtual network, subredes y storage account para Flow Logs.

.PARAMETER ResourceGroupName
Nombre del resource group a crear

.PARAMETER Location
Ubicación de Azure donde crear los recursos

.PARAMETER VNetName
Nombre de la virtual network

.PARAMETER StorageAccountName
Nombre del storage account para Flow Logs

.EXAMPLE
.\script-create-infrastructure.ps1 -ResourceGroupName "rg-nsg-lab-juan" -Location "eastus"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory = $false)]
    [string]$Location = "eastus",
    
    [Parameter(Mandatory = $false)]
    [string]$VNetName = "vnet-nsg-lab",
    
    [Parameter(Mandatory = $false)]
    [string]$StorageAccountName = "stflowlogs$(Get-Random -Minimum 1000 -Maximum 9999)"
)

# Funciones auxiliares
function Write-ColorMessage {
    param(
        [string]$Message,
        [ValidateSet("Green", "Yellow", "Red", "Cyan", "Magenta")]
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Test-AzureLogin {
    try {
        $context = Get-AzContext
        if ($null -eq $context) {
            return $false
        }
        return $true
    }
    catch {
        return $false
    }
}

# Script principal
try {
    Write-ColorMessage "🚀 CREACIÓN DE INFRAESTRUCTURA - LABORATORIO NSG" "Magenta"
    Write-ColorMessage "═══════════════════════════════════════════════════════" "Magenta"
    
    # Verificar autenticación con Azure
    if (-not (Test-AzureLogin)) {
        Write-ColorMessage "⚠️ No está autenticado con Azure. Ejecutando az login..." "Yellow"
        az login
        
        if ($LASTEXITCODE -ne 0) {
            throw "Error en autenticación con Azure"
        }
    }
    
    Write-ColorMessage "✅ Autenticación verificada" "Green"
    
    # Crear Resource Group
    Write-ColorMessage "📦 Creando Resource Group: $ResourceGroupName" "Cyan"
    az group create --name $ResourceGroupName --location $Location --tags Environment=Lab Project=NSGLab CreatedBy=PowerShell
    
    if ($LASTEXITCODE -ne 0) {
        throw "Error creando Resource Group"
    }
    
    Write-ColorMessage "✅ Resource Group creado exitosamente" "Green"
    
    # Crear Virtual Network con subredes
    Write-ColorMessage "🌐 Creando Virtual Network: $VNetName" "Cyan"
    
    # VNET principal con subred web
    az network vnet create `
        --resource-group $ResourceGroupName `
        --name $VNetName `
        --address-prefix 10.2.0.0/16 `
        --subnet-name snet-web `
        --subnet-prefix 10.2.1.0/24 `
        --location $Location `
        --tags Environment=Lab Tier=Networking
    
    if ($LASTEXITCODE -ne 0) {
        throw "Error creando Virtual Network"
    }
    
    # Subred para aplicaciones
    Write-ColorMessage "📡 Creando subred de aplicaciones..." "Cyan"
    az network vnet subnet create `
        --resource-group $ResourceGroupName `
        --vnet-name $VNetName `
        --name snet-app `
        --address-prefix 10.2.2.0/24
    
    # Subred para datos
    Write-ColorMessage "🗄️ Creando subred de datos..." "Cyan"
    az network vnet subnet create `
        --resource-group $ResourceGroupName `
        --vnet-name $VNetName `
        --name snet-data `
        --address-prefix 10.2.3.0/24
    
    # Subred para Application Gateway (si se necesita)
    Write-ColorMessage "⚖️ Creando subred para Application Gateway..." "Cyan"
    az network vnet subnet create `
        --resource-group $ResourceGroupName `
        --vnet-name $VNetName `
        --name snet-appgw `
        --address-prefix 10.2.4.0/24
    
    Write-ColorMessage "✅ Virtual Network y subredes creadas exitosamente" "Green"
    
    # Crear Storage Account para Flow Logs
    Write-ColorMessage "📦 Creando Storage Account: $StorageAccountName" "Cyan"
    az storage account create `
        --name $StorageAccountName `
        --resource-group $ResourceGroupName `
        --location $Location `
        --sku Standard_LRS `
        --kind StorageV2 `
        --access-tier Cool `
        --tags Purpose=FlowLogs Environment=Lab
    
    if ($LASTEXITCODE -ne 0) {
        throw "Error creando Storage Account"
    }
    
    Write-ColorMessage "✅ Storage Account creado exitosamente" "Green"
    
    # Crear Log Analytics Workspace
    Write-ColorMessage "📊 Creando Log Analytics Workspace..." "Cyan"
    $workspaceName = "law-nsg-lab-$(Get-Random -Minimum 100 -Maximum 999)"
    
    az monitor log-analytics workspace create `
        --resource-group $ResourceGroupName `
        --workspace-name $workspaceName `
        --location $Location `
        --sku PerGB2018 `
        --retention-time 30 `
        --tags Purpose=Monitoring Environment=Lab
    
    Write-ColorMessage "✅ Log Analytics Workspace creado" "Green"
    
    # Registrar Network Watcher provider si no está registrado
    Write-ColorMessage "🔍 Verificando Network Watcher..." "Cyan"
    az provider register --namespace Microsoft.Network
    
    Write-ColorMessage "✅ Network Watcher verificado" "Green"
    
    # Mostrar resumen de recursos creados
    Write-ColorMessage "`n📋 RESUMEN DE INFRAESTRUCTURA CREADA" "Magenta"
    Write-ColorMessage "════════════════════════════════════════" "Magenta"
    Write-ColorMessage "Resource Group: $ResourceGroupName" "Green"
    Write-ColorMessage "Ubicación: $Location" "Green"
    Write-ColorMessage "Virtual Network: $VNetName (10.2.0.0/16)" "Green"
    Write-ColorMessage "  • snet-web: 10.2.1.0/24" "Cyan"
    Write-ColorMessage "  • snet-app: 10.2.2.0/24" "Cyan"
    Write-ColorMessage "  • snet-data: 10.2.3.0/24" "Cyan"
    Write-ColorMessage "  • snet-appgw: 10.2.4.0/24" "Cyan"
    Write-ColorMessage "Storage Account: $StorageAccountName" "Green"
    Write-ColorMessage "Log Analytics: $workspaceName" "Green"
    
    # Próximos pasos
    Write-ColorMessage "`n🚀 PRÓXIMOS PASOS" "Magenta"
    Write-ColorMessage "════════════════════════════════════════" "Magenta"
    Write-ColorMessage "1. Ejecutar NSGManager para crear NSGs:" "Yellow"
    Write-ColorMessage "   cd ../src/NSGManager" "Cyan"
    Write-ColorMessage "   dotnet run -- create-advanced --resource-group $ResourceGroupName" "Cyan"
    Write-ColorMessage "`n2. O usar script de deployment:" "Yellow"
    Write-ColorMessage "   .\script-deploy-nsgs.ps1 -ResourceGroupName $ResourceGroupName" "Cyan"
    
    # Guardar información para otros scripts
    $configPath = Join-Path $PSScriptRoot "lab-config.json"
    $config = @{
        ResourceGroupName = $ResourceGroupName
        Location = $Location
        VNetName = $VNetName
        StorageAccountName = $StorageAccountName
        WorkspaceName = $workspaceName
        CreatedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    }
    
    $config | ConvertTo-Json | Set-Content $configPath
    Write-ColorMessage "`n💾 Configuración guardada en: $configPath" "Green"
    
    Write-ColorMessage "`n🎉 ¡INFRAESTRUCTURA CREADA EXITOSAMENTE!" "Green"
}
catch {
    Write-ColorMessage "`n❌ ERROR EN CREACIÓN DE INFRAESTRUCTURA" "Red"
    Write-ColorMessage "Error: $($_.Exception.Message)" "Red"
    
    # Mostrar información para troubleshooting
    Write-ColorMessage "`n🔧 TROUBLESHOOTING:" "Yellow"
    Write-ColorMessage "1. Verificar autenticación: az account show" "Cyan"
    Write-ColorMessage "2. Verificar permisos en la suscripción" "Cyan"
    Write-ColorMessage "3. Verificar que el nombre del storage account sea único" "Cyan"
    
    exit 1
}
finally {
    Write-ColorMessage "`n⏸️ Presione cualquier tecla para continuar..." "Yellow"
    Read-Host
} 