#!/usr/bin/env pwsh
#
# 🚀 NSG Lab - Script de Configuración Automatizada
# 📁 Archivo: setup-nsg-lab.ps1
# 🎯 Propósito: Automatizar la configuración inicial del laboratorio NSG
# 👨‍💻 Autor: NSG Manager v2.1
# 📅 Fecha: Enero 2025
#
# 📋 Uso:
#   .\setup-nsg-lab.ps1 -ResourceGroupName "rg-nsg-lab-tuusuario"
#   .\setup-nsg-lab.ps1 -ResourceGroupName "rg-nsg-lab-tuusuario" -Location "westus2"
#

param(
    [Parameter(Mandatory=$true, HelpMessage="Nombre del Resource Group para el laboratorio")]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$false, HelpMessage="Región de Azure donde crear/verificar el Resource Group")]
    [string]$Location = "eastus2",
    
    [Parameter(Mandatory=$false, HelpMessage="Ejecutar directamente create-advanced después de la configuración")]
    [switch]$AutoRun
)

# Colores para output
$Green = "Green"
$Yellow = "Yellow" 
$Red = "Red"
$Cyan = "Cyan"
$White = "White"

Write-Host "🚀 NSG Lab - Configuración Automatizada v2.1" -ForegroundColor $Green
Write-Host "================================================" -ForegroundColor $Green
Write-Host ""

try {
    # 1. Verificar PowerShell y Azure CLI
    Write-Host "🔍 Verificando prerrequisitos..." -ForegroundColor $Yellow
    
    # Verificar Azure CLI
    $azVersion = az version --output tsv 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Azure CLI no encontrado. Instalar desde: https://aka.ms/installazurecli" -ForegroundColor $Red
        exit 1
    }
    Write-Host "✅ Azure CLI detectado" -ForegroundColor $Green

    # Verificar .NET
    $dotnetVersion = dotnet --version 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ .NET SDK no encontrado. Instalar desde: https://dotnet.microsoft.com/download" -ForegroundColor $Red
        exit 1
    }
    Write-Host "✅ .NET SDK detectado: v$dotnetVersion" -ForegroundColor $Green

    # 2. Verificar autenticación de Azure
    Write-Host ""
    Write-Host "🔐 Verificando autenticación de Azure..." -ForegroundColor $Yellow
    
    $account = az account show --query name -o tsv 2>$null
    if ($LASTEXITCODE -ne 0 -or !$account) {
        Write-Host "❌ No autenticado en Azure. Iniciando az login..." -ForegroundColor $Red
        az login
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Error en autenticación. Verificar credenciales." -ForegroundColor $Red
            exit 1
        }
    }

    $subscription = az account show --query id -o tsv
    $subscriptionName = az account show --query name -o tsv
    Write-Host "✅ Autenticado como: $account" -ForegroundColor $Green
    Write-Host "✅ Suscripción activa: $subscriptionName" -ForegroundColor $Green
    Write-Host "✅ Subscription ID: $subscription" -ForegroundColor $Cyan

    # 3. Verificar/crear Resource Group
    Write-Host ""
    Write-Host "📋 Verificando Resource Group: $ResourceGroupName..." -ForegroundColor $Yellow
    
    $rg = az group show --name $ResourceGroupName --query name -o tsv 2>$null
    if ($LASTEXITCODE -ne 0 -or !$rg) {
        Write-Host "📋 Resource Group no encontrado. Creando en $Location..." -ForegroundColor $Yellow
        az group create --name $ResourceGroupName --location $Location --output table
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Error creando Resource Group. Verificar permisos." -ForegroundColor $Red
            exit 1
        }
        Write-Host "✅ Resource Group '$ResourceGroupName' creado exitosamente" -ForegroundColor $Green
        $actualLocation = $Location
    } else {
        $actualLocation = az group show --name $ResourceGroupName --query location -o tsv
        Write-Host "✅ Resource Group '$ResourceGroupName' ya existe en: $actualLocation" -ForegroundColor $Green
    }

    # 4. Configurar variables de entorno (sesión actual)
    Write-Host ""
    Write-Host "⚙️ Configurando variables de entorno..." -ForegroundColor $Yellow
    
    # Establecer variables usando el formato estándar
    $global:resourceGroup = $ResourceGroupName
    $global:location = $actualLocation
    $global:vnetName = "vnet-nsg-lab"
    $global:subscription_id = $subscription

    # 5. Navegar al proyecto y compilar
    Write-Host ""
    Write-Host "🔧 Preparando proyecto NSGManager..." -ForegroundColor $Yellow
    
    $originalPath = Get-Location
    $projectPath = Join-Path $originalPath "Laboratorio1-NSG\src\NSGManager"
    
    if (Test-Path $projectPath) {
        Set-Location $projectPath
        Write-Host "✅ Navegado a: $projectPath" -ForegroundColor $Green
    } else {
        Write-Host "❌ Directorio del proyecto no encontrado: $projectPath" -ForegroundColor $Red
        Write-Host "💡 Asegúrate de ejecutar este script desde el directorio raíz del laboratorio" -ForegroundColor $Yellow
        exit 1
    }

    Write-Host "📦 Restaurando paquetes NuGet..." -ForegroundColor $Yellow
    dotnet restore --quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Error restaurando paquetes NuGet" -ForegroundColor $Red
        exit 1
    }

    Write-Host "🏗️ Compilando proyecto..." -ForegroundColor $Yellow
    dotnet build --configuration Release --quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Error compilando proyecto. Verificar dependencias." -ForegroundColor $Red
        exit 1
    }

    # 6. Mostrar resumen de configuración
    Write-Host ""
    Write-Host "🎉 ¡Configuración completada exitosamente!" -ForegroundColor $Green
    Write-Host "==========================================" -ForegroundColor $Green
    Write-Host ""
    Write-Host "📋 Variables configuradas:" -ForegroundColor $Cyan
    Write-Host "   # Establecer variables" -ForegroundColor $White
    Write-Host "   `$resourceGroup = `"$ResourceGroupName`"" -ForegroundColor $White
    Write-Host "   `$location = `"$actualLocation`"" -ForegroundColor $White
    Write-Host "   `$vnetName = `"vnet-nsg-lab`"" -ForegroundColor $White
    Write-Host "   `$subscription_id = `"$subscription`"" -ForegroundColor $White
    Write-Host ""

    # 7. Comandos disponibles
    Write-Host "🚀 Comandos disponibles:" -ForegroundColor $Cyan
    Write-Host ""
    Write-Host "   📊 Crear NSGs Avanzados (RECOMENDADO):" -ForegroundColor $Yellow
    Write-Host "   dotnet run -- create-advanced --resource-group `$resourceGroup --location `$location --subscription `$subscription_id" -ForegroundColor $White
    Write-Host ""
    Write-Host "   🔍 Validar configuración:" -ForegroundColor $Yellow  
    Write-Host "   dotnet run -- validate --resource-group `$resourceGroup --location `$location --subscription `$subscription_id" -ForegroundColor $White
    Write-Host ""
    Write-Host "   📈 Generar reporte:" -ForegroundColor $Yellow
    Write-Host "   dotnet run -- security-report --resource-group `$resourceGroup --location `$location --subscription `$subscription_id" -ForegroundColor $White
    Write-Host ""

    # 8. Auto-ejecutar si se solicitó
    if ($AutoRun) {
        Write-Host "🏃‍♂️ Ejecutando create-advanced automáticamente..." -ForegroundColor $Green
        Write-Host ""
        dotnet run -- create-advanced --resource-group $ResourceGroupName --location $actualLocation --subscription $subscription
    } else {
        Write-Host "💡 Para ejecutar automáticamente, usar: .\setup-nsg-lab.ps1 -ResourceGroupName `"$ResourceGroupName`" -AutoRun" -ForegroundColor $Yellow
    }

    Write-Host ""
    Write-Host "✅ ¡Listo para usar NSG Manager!" -ForegroundColor $Green

} catch {
    Write-Host ""
    Write-Host "❌ Error durante la configuración:" -ForegroundColor $Red
    Write-Host $_.Exception.Message -ForegroundColor $Red
    Write-Host ""
    Write-Host "💡 Para soporte, consultar la sección Troubleshooting en README.md" -ForegroundColor $Yellow
    exit 1
} finally {
    # Restaurar directorio original si es necesario
    if ($originalPath -and (Get-Location).Path -ne $originalPath.Path) {
        Set-Location $originalPath
    }
} 