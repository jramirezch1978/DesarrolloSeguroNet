# VerificarEntorno.ps1
# Laboratorio 0: Verificación completa del entorno de penetration testing

Write-Host "🛠️  VERIFICACIÓN DEL ENTORNO DE PENETRATION TESTING" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "Curso: Diseño Seguro de Aplicaciones (.NET en Azure)" -ForegroundColor Green
Write-Host "Laboratorio 0: Configuración del Entorno" -ForegroundColor Green
Write-Host ""

# Variables para seguimiento de estado
$verificaciones = @()
$errores = @()

function Test-Prerequisite {
    param(
        [string]$Nombre,
        [string]$Comando,
        [string]$VersionEsperada = "",
        [switch]$Opcional
    )
    
    Write-Host "🔍 Verificando $Nombre..." -ForegroundColor Yellow
    
    try {
        if ($Comando -like "*--version*") {
            $resultado = Invoke-Expression $Comando 2>$null
        } else {
            $resultado = Invoke-Expression "$Comando --version" 2>$null
        }
        
        if ($LASTEXITCODE -eq 0 -or $resultado) {
            Write-Host "  ✅ $Nombre: INSTALADO" -ForegroundColor Green
            if ($resultado) {
                Write-Host "  📝 Versión: $($resultado | Select-Object -First 1)" -ForegroundColor Gray
            }
            
            $verificaciones += [PSCustomObject]@{
                Herramienta = $Nombre
                Estado = "✅ INSTALADO"
                Version = ($resultado | Select-Object -First 1)
                Opcional = $Opcional
            }
            return $true
        } else {
            throw "No encontrado"
        }
    } catch {
        if ($Opcional) {
            Write-Host "  ⚠️  $Nombre: NO INSTALADO (Opcional)" -ForegroundColor Yellow
            $verificaciones += [PSCustomObject]@{
                Herramienta = $Nombre
                Estado = "⚠️ NO INSTALADO (Opcional)"
                Version = "N/A"
                Opcional = $true
            }
        } else {
            Write-Host "  ❌ $Nombre: NO INSTALADO (Requerido)" -ForegroundColor Red
            $errores += "$Nombre no está instalado (requerido)"
            $verificaciones += [PSCustomObject]@{
                Herramienta = $Nombre
                Estado = "❌ NO INSTALADO"
                Version = "N/A"
                Opcional = $false
            }
        }
        return $false
    }
}

function Test-WindowsFeature {
    param(
        [string]$FeatureName,
        [string]$DisplayName
    )
    
    Write-Host "🔍 Verificando $DisplayName..." -ForegroundColor Yellow
    
    try {
        if (Get-Command "Get-WindowsOptionalFeature" -ErrorAction SilentlyContinue) {
            $feature = Get-WindowsOptionalFeature -Online -FeatureName $FeatureName -ErrorAction SilentlyContinue
            if ($feature -and $feature.State -eq "Enabled") {
                Write-Host "  ✅ $DisplayName: HABILITADO" -ForegroundColor Green
                return $true
            }
        }
        
        Write-Host "  ⚠️  $DisplayName: NO HABILITADO" -ForegroundColor Yellow
        return $false
    } catch {
        Write-Host "  ⚠️  $DisplayName: NO SE PUEDE VERIFICAR" -ForegroundColor Yellow
        return $false
    }
}

# Verificar sistema operativo
Write-Host "🖥️  INFORMACIÓN DEL SISTEMA" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan

$osInfo = Get-ComputerInfo | Select-Object WindowsProductName, WindowsVersion, CsProcessors
Write-Host "  OS: $($osInfo.WindowsProductName)" -ForegroundColor Gray
Write-Host "  Versión: $($osInfo.WindowsVersion)" -ForegroundColor Gray
Write-Host "  Procesador: $($osInfo.CsProcessors[0].Name)" -ForegroundColor Gray
Write-Host ""

# Verificar PowerShell
Write-Host "⚡ VERIFICANDO POWERSHELL" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan
Write-Host "  Versión PowerShell: $($PSVersionTable.PSVersion)" -ForegroundColor Gray
Write-Host "  Política de Ejecución: $(Get-ExecutionPolicy)" -ForegroundColor Gray
Write-Host ""

# Verificar herramientas principales
Write-Host "🔧 VERIFICANDO HERRAMIENTAS PRINCIPALES" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

# Chocolatey
Test-Prerequisite -Nombre "Chocolatey" -Comando "choco"

# .NET Core
Test-Prerequisite -Nombre ".NET Core SDK" -Comando "dotnet"

# Git
Test-Prerequisite -Nombre "Git" -Comando "git"

# Azure CLI
Test-Prerequisite -Nombre "Azure CLI" -Comando "az"

Write-Host ""

# Verificar herramientas de penetration testing
Write-Host "🔍 VERIFICANDO HERRAMIENTAS DE PENETRATION TESTING" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# Nmap
Test-Prerequisite -Nombre "Nmap" -Comando "nmap"

# Wireshark (verificar si tshark está disponible)
Test-Prerequisite -Nombre "Wireshark (tshark)" -Comando "tshark" -Opcional

# Burp Suite (verificar instalación en Chocolatey)
$burpPath = "${env:ProgramFiles}\BurpSuiteCommunity\BurpSuiteCommunity.exe"
if (Test-Path $burpPath) {
    Write-Host "🔍 Verificando Burp Suite..." -ForegroundColor Yellow
    Write-Host "  ✅ Burp Suite Community: INSTALADO" -ForegroundColor Green
    $verificaciones += [PSCustomObject]@{
        Herramienta = "Burp Suite Community"
        Estado = "✅ INSTALADO"
        Version = "Community Edition"
        Opcional = $false
    }
} else {
    Write-Host "🔍 Verificando Burp Suite..." -ForegroundColor Yellow
    Write-Host "  ❌ Burp Suite Community: NO INSTALADO" -ForegroundColor Red
    $errores += "Burp Suite Community no está instalado"
    $verificaciones += [PSCustomObject]@{
        Herramienta = "Burp Suite Community"
        Estado = "❌ NO INSTALADO"
        Version = "N/A"
        Opcional = $false
    }
}

# Postman
$postmanPath = "${env:LOCALAPPDATA}\Postman\Postman.exe"
if (Test-Path $postmanPath) {
    Write-Host "🔍 Verificando Postman..." -ForegroundColor Yellow
    Write-Host "  ✅ Postman: INSTALADO" -ForegroundColor Green
    $verificaciones += [PSCustomObject]@{
        Herramienta = "Postman"
        Estado = "✅ INSTALADO"
        Version = "Desktop App"
        Opcional = $false
    }
} else {
    Write-Host "🔍 Verificando Postman..." -ForegroundColor Yellow
    Write-Host "  ❌ Postman: NO INSTALADO" -ForegroundColor Red
    $errores += "Postman no está instalado"
    $verificaciones += [PSCustomObject]@{
        Herramienta = "Postman"
        Estado = "❌ NO INSTALADO"
        Version = "N/A"
        Opcional = $false
    }
}

Write-Host ""

# Verificar Visual Studio Code
Write-Host "💻 VERIFICANDO VISUAL STUDIO CODE" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

Test-Prerequisite -Nombre "Visual Studio Code" -Comando "code"

# Verificar extensiones de VS Code
if (Get-Command "code" -ErrorAction SilentlyContinue) {
    Write-Host "🔍 Verificando extensiones de VS Code..." -ForegroundColor Yellow
    
    $extensionesRequeridas = @(
        "ms-dotnettools.csdevkit",
        "ms-vscode.azure-account",
        "ms-azuretools.vscode-azureresourcegroups",
        "ms-vscode.azurecli",
        "humao.rest-client"
    )
    
    $extensionesInstaladas = code --list-extensions 2>$null
    
    foreach ($extension in $extensionesRequeridas) {
        if ($extensionesInstaladas -contains $extension) {
            Write-Host "  ✅ Extensión $extension: INSTALADA" -ForegroundColor Green
        } else {
            Write-Host "  ❌ Extensión $extension: NO INSTALADA" -ForegroundColor Red
            $errores += "Extensión VS Code $extension no está instalada"
        }
    }
}

Write-Host ""

# Verificar conectividad de red
Write-Host "🌐 VERIFICANDO CONECTIVIDAD DE RED" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan

$sitiosTest = @("github.com", "portal.azure.com", "chocolatey.org")

foreach ($sitio in $sitiosTest) {
    Write-Host "🔍 Probando conectividad a $sitio..." -ForegroundColor Yellow
    try {
        $resultado = Test-NetConnection -ComputerName $sitio -Port 443 -InformationLevel Quiet
        if ($resultado) {
            Write-Host "  ✅ $sitio: ACCESIBLE" -ForegroundColor Green
        } else {
            Write-Host "  ❌ $sitio: NO ACCESIBLE" -ForegroundColor Red
            $errores += "No se puede conectar a $sitio"
        }
    } catch {
        Write-Host "  ❌ $sitio: ERROR DE CONEXIÓN" -ForegroundColor Red
        $errores += "Error de conexión a $sitio"
    }
}

Write-Host ""

# Verificar acceso a Azure (si Azure CLI está instalado)
Write-Host "☁️  VERIFICANDO ACCESO A AZURE" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan

if (Get-Command "az" -ErrorAction SilentlyContinue) {
    try {
        Write-Host "🔍 Verificando autenticación Azure..." -ForegroundColor Yellow
        $azAccount = az account show 2>$null | ConvertFrom-Json
        
        if ($azAccount) {
            Write-Host "  ✅ Azure CLI: AUTENTICADO" -ForegroundColor Green
            Write-Host "  📝 Cuenta: $($azAccount.user.name)" -ForegroundColor Gray
            Write-Host "  📝 Suscripción: $($azAccount.name)" -ForegroundColor Gray
        } else {
            Write-Host "  ⚠️  Azure CLI: NO AUTENTICADO" -ForegroundColor Yellow
            Write-Host "  💡 Ejecute 'az login' para autenticarse" -ForegroundColor Gray
        }
    } catch {
        Write-Host "  ⚠️  Azure CLI: NO SE PUEDE VERIFICAR" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ❌ Azure CLI: NO INSTALADO" -ForegroundColor Red
}

Write-Host ""

# Generar reporte final
Write-Host "📊 REPORTE FINAL DE VERIFICACIÓN" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Mostrar tabla de verificaciones
$verificaciones | Format-Table -AutoSize

Write-Host ""

# Mostrar errores si los hay
if ($errores.Count -gt 0) {
    Write-Host "❌ ERRORES ENCONTRADOS:" -ForegroundColor Red
    Write-Host "=====================" -ForegroundColor Red
    foreach ($error in $errores) {
        Write-Host "  • $error" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "💡 SOLUCIÓN:" -ForegroundColor Yellow
    Write-Host "Ejecute el script 'InstalarHerramientas.ps1' para instalar las herramientas faltantes." -ForegroundColor Yellow
    Write-Host ""
} else {
    Write-Host "🎉 ¡ENTORNO COMPLETAMENTE CONFIGURADO!" -ForegroundColor Green
    Write-Host "====================================" -ForegroundColor Green
    Write-Host "Todas las herramientas requeridas están instaladas y configuradas." -ForegroundColor Green
    Write-Host ""
}

# Guardar reporte en archivo
$reportePath = ".\reporte-verificacion-entorno.json"
$reporteData = @{
    FechaVerificacion = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    SistemaOperativo = $osInfo.WindowsProductName
    VersionPowerShell = $PSVersionTable.PSVersion.ToString()
    Verificaciones = $verificaciones
    Errores = $errores
    EstadoGeneral = if ($errores.Count -eq 0) { "COMPLETO" } else { "INCOMPLETO" }
}

$reporteData | ConvertTo-Json -Depth 3 | Out-File -FilePath $reportePath -Encoding UTF8
Write-Host "📄 Reporte guardado en: $reportePath" -ForegroundColor Gray

Write-Host ""
Write-Host "🚀 ¿Listo para continuar?" -ForegroundColor Cyan
if ($errores.Count -eq 0) {
    Write-Host "Su entorno está completamente configurado para los laboratorios de penetration testing." -ForegroundColor Green
} else {
    Write-Host "Complete la instalación de herramientas faltantes antes de proceder." -ForegroundColor Yellow
}