#Requires -RunAsAdministrator

<#
.SYNOPSIS
Script de configuración automática del entorno para laboratorios de Azure Security

.DESCRIPTION
Este script instala y configura todas las herramientas necesarias para completar
los laboratorios de Network Security Groups y DDoS Protection en Azure.

.PARAMETER SkipChocolatey
Omite la instalación de Chocolatey si ya está instalado

.PARAMETER SkipAzureLogin
Omite el proceso de login a Azure

.EXAMPLE
.\setup-environment.ps1
Ejecuta la configuración completa

.EXAMPLE
.\setup-environment.ps1 -SkipChocolatey -SkipAzureLogin
Omite Chocolatey y Azure login
#>

param(
    [switch]$SkipChocolatey,
    [switch]$SkipAzureLogin
)

# Configuración de colores para output
$colors = @{
    Success = "Green"
    Warning = "Yellow"
    Error = "Red"
    Info = "Cyan"
    Header = "Magenta"
}

function Write-ColorMessage {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $colors[$Color]
}

function Test-IsAdministrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Install-ChocoIfNeeded {
    if ($SkipChocolatey) {
        Write-ColorMessage "⏭️  Omitiendo instalación de Chocolatey" "Warning"
        return
    }

    Write-ColorMessage "🍫 Verificando Chocolatey..." "Header"
    
    try {
        $chocoVersion = choco --version 2>$null
        if ($chocoVersion) {
            Write-ColorMessage "✅ Chocolatey ya está instalado (versión: $chocoVersion)" "Success"
            return
        }
    }
    catch {
        Write-ColorMessage "📦 Instalando Chocolatey..." "Info"
    }

    try {
        # Configurar TLS 1.2
        [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
        
        # Instalar Chocolatey
        Set-ExecutionPolicy Bypass -Scope Process -Force
        Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
        
        # Refrescar variables de entorno
        refreshenv
        
        # Verificar instalación
        $chocoVersion = choco --version 2>$null
        if ($chocoVersion) {
            Write-ColorMessage "✅ Chocolatey instalado exitosamente (versión: $chocoVersion)" "Success"
        } else {
            throw "Chocolatey no se instaló correctamente"
        }
    }
    catch {
        Write-ColorMessage "❌ Error instalando Chocolatey: $($_.Exception.Message)" "Error"
        exit 1
    }
}

function Install-DevelopmentTools {
    Write-ColorMessage "🛠️  Instalando herramientas de desarrollo..." "Header"
    
    $tools = @(
        @{Name = "dotnet-9.0-sdk"; Description = ".NET 9 SDK"},
        @{Name = "azure-cli"; Description = "Azure CLI"},
        @{Name = "git"; Description = "Git"},
        @{Name = "vscode"; Description = "Visual Studio Code"}
    )

    foreach ($tool in $tools) {
        Write-ColorMessage "📦 Instalando $($tool.Description)..." "Info"
        
        try {
            choco install $tool.Name -y --no-progress
            Write-ColorMessage "✅ $($tool.Description) instalado" "Success"
        }
        catch {
            Write-ColorMessage "❌ Error instalando $($tool.Description): $($_.Exception.Message)" "Error"
        }
    }

    # Refrescar variables de entorno
    refreshenv
    
    # Pequeña pausa para que las variables se actualicen
    Start-Sleep -Seconds 3
}

function Install-VSCodeExtensions {
    Write-ColorMessage "🔌 Instalando extensiones de VS Code..." "Header"
    
    $extensions = @(
        "ms-dotnettools.csdevkit",
        "ms-vscode.azure-account", 
        "ms-azuretools.vscode-azureresourcegroups",
        "ms-vscode.azurecli",
        "humao.rest-client",
        "ms-azure-devops.azure-pipelines"
    )

    foreach ($extension in $extensions) {
        Write-ColorMessage "🔌 Instalando extensión: $extension" "Info"
        
        try {
            Start-Process -FilePath "code" -ArgumentList "--install-extension", $extension -Wait -NoNewWindow
            Write-ColorMessage "✅ Extensión $extension instalada" "Success"
        }
        catch {
            Write-ColorMessage "⚠️  Error instalando extensión $extension" "Warning"
        }
    }
}

function Test-Installations {
    Write-ColorMessage "🔍 Verificando instalaciones..." "Header"
    
    $verifications = @(
        @{
            Name = ".NET SDK"
            Command = "dotnet --version"
            ExpectedPattern = "^9\."
        },
        @{
            Name = "Azure CLI"
            Command = "az --version"
            ExpectedPattern = "azure-cli"
        },
        @{
            Name = "Git"
            Command = "git --version"
            ExpectedPattern = "git version"
        },
        @{
            Name = "VS Code"
            Command = "code --version"
            ExpectedPattern = "^\d+\.\d+\."
        }
    )

    $allSuccess = $true

    foreach ($verification in $verifications) {
        try {
            $output = Invoke-Expression $verification.Command 2>$null
            if ($output -match $verification.ExpectedPattern) {
                Write-ColorMessage "✅ $($verification.Name): OK" "Success"
            } else {
                Write-ColorMessage "❌ $($verification.Name): No se pudo verificar" "Error"
                $allSuccess = $false
            }
        }
        catch {
            Write-ColorMessage "❌ $($verification.Name): Error en verificación" "Error"
            $allSuccess = $false
        }
    }

    return $allSuccess
}

function Initialize-AzureAuthentication {
    if ($SkipAzureLogin) {
        Write-ColorMessage "⏭️  Omitiendo login a Azure" "Warning"
        return
    }

    Write-ColorMessage "☁️  Configurando autenticación con Azure..." "Header"
    
    try {
        # Verificar si ya está autenticado
        $currentAccount = az account show 2>$null | ConvertFrom-Json
        if ($currentAccount) {
            Write-ColorMessage "✅ Ya está autenticado como: $($currentAccount.user.name)" "Success"
            Write-ColorMessage "📋 Suscripción actual: $($currentAccount.name)" "Info"
            return
        }
    }
    catch {
        # No está autenticado, proceder con login
    }

    Write-ColorMessage "🔐 Iniciando proceso de autenticación..." "Info"
    Write-ColorMessage "Se abrirá su navegador para completar la autenticación" "Warning"
    
    try {
        az login
        
        # Verificar autenticación exitosa
        $currentAccount = az account show | ConvertFrom-Json
        Write-ColorMessage "✅ Autenticación exitosa como: $($currentAccount.user.name)" "Success"
        Write-ColorMessage "📋 Suscripción: $($currentAccount.name)" "Info"
        
        # Configurar ubicación por defecto
        az configure --defaults location=eastus
        Write-ColorMessage "📍 Ubicación por defecto configurada: East US" "Info"
        
    }
    catch {
        Write-ColorMessage "❌ Error en autenticación con Azure: $($_.Exception.Message)" "Error"
        Write-ColorMessage "💡 Puede ejecutar 'az login' manualmente después" "Warning"
    }
}

function Show-Summary {
    Write-ColorMessage "`n🎉 CONFIGURACIÓN COMPLETADA" "Header"
    Write-ColorMessage "════════════════════════════════════════" "Header"
    
    Write-ColorMessage "📋 Resumen de herramientas instaladas:" "Info"
    Write-ColorMessage "  • Chocolatey Package Manager" "Success"
    Write-ColorMessage "  • .NET 9 SDK" "Success"
    Write-ColorMessage "  • Azure CLI" "Success"
    Write-ColorMessage "  • Git" "Success"
    Write-ColorMessage "  • Visual Studio Code" "Success"
    Write-ColorMessage "  • Extensiones de Azure para VS Code" "Success"
    
    Write-ColorMessage "`n🚀 Próximos pasos:" "Info"
    Write-ColorMessage "  1. Ejecutar verificación: .\verify-setup.ps1" "Warning"
    Write-ColorMessage "  2. Proceder al Laboratorio 1: Network Security Groups" "Warning"
    
    Write-ColorMessage "`n💡 Consejos útiles:" "Info"
    Write-ColorMessage "  • Mantener VS Code abierto durante los laboratorios" "Warning"
    Write-ColorMessage "  • Usar Azure Portal en una pestaña separada" "Warning"
    Write-ColorMessage "  • Guardar credenciales de Azure seguramente" "Warning"
}

# ========================================
# EJECUCIÓN PRINCIPAL
# ========================================

try {
    Write-ColorMessage "🚀 CONFIGURACIÓN DEL ENTORNO PARA LABORATORIOS AZURE" "Header"
    Write-ColorMessage "════════════════════════════════════════════════════════" "Header"
    
    # Verificar permisos de administrador
    if (-not (Test-IsAdministrator)) {
        Write-ColorMessage "❌ Este script requiere permisos de administrador" "Error"
        Write-ColorMessage "💡 Haga clic derecho y seleccione 'Ejecutar como administrador'" "Warning"
        exit 1
    }

    # Instalar Chocolatey
    Install-ChocoIfNeeded
    
    # Instalar herramientas de desarrollo
    Install-DevelopmentTools
    
    # Instalar extensiones de VS Code
    Install-VSCodeExtensions
    
    # Verificar instalaciones
    $verificationsSuccess = Test-Installations
    
    if ($verificationsSuccess) {
        # Configurar Azure
        Initialize-AzureAuthentication
        
        # Mostrar resumen
        Show-Summary
        
        Write-ColorMessage "`n✅ ¡CONFIGURACIÓN EXITOSA!" "Success"
        Write-ColorMessage "El entorno está listo para los laboratorios de Azure Security" "Success"
    } else {
        Write-ColorMessage "`n⚠️  CONFIGURACIÓN PARCIAL" "Warning"
        Write-ColorMessage "Algunas herramientas no se verificaron correctamente" "Warning"
        Write-ColorMessage "Ejecute verify-setup.ps1 para diagnóstico detallado" "Info"
    }
}
catch {
    Write-ColorMessage "`n❌ ERROR EN CONFIGURACIÓN" "Error"
    Write-ColorMessage "Error: $($_.Exception.Message)" "Error"
    Write-ColorMessage "`nPuede intentar ejecutar pasos individuales manualmente" "Warning"
    exit 1
}
finally {
    Write-ColorMessage "`n⏸️  Presione cualquier tecla para continuar..." "Info"
    Read-Host
} 