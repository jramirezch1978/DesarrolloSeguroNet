# InstalarHerramientas.ps1
# Laboratorio 0: Instalación automatizada de herramientas de penetration testing

param(
    [switch]$Force,
    [switch]$SkipChocolatey,
    [switch]$Verbose
)

Write-Host "🛠️  INSTALADOR DE HERRAMIENTAS DE PENETRATION TESTING" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "Curso: Diseño Seguro de Aplicaciones (.NET en Azure)" -ForegroundColor Green
Write-Host "Laboratorio 0: Configuración del Entorno" -ForegroundColor Green
Write-Host ""

# Verificar permisos de administrador
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "❌ ERROR: Este script requiere permisos de administrador." -ForegroundColor Red
    Write-Host "💡 Solución: Ejecute PowerShell como Administrador y vuelva a ejecutar el script." -ForegroundColor Yellow
    exit 1
}

$instalaciones = @()
$errores = @()

function Install-ChocolateyPackage {
    param(
        [string]$PackageName,
        [string]$DisplayName,
        [array]$Parameters = @(),
        [switch]$Opcional
    )
    
    Write-Host "📦 Instalando $DisplayName..." -ForegroundColor Yellow
    
    try {
        # Verificar si ya está instalado
        $installed = choco list --local-only $PackageName 2>$null
        if ($installed -match $PackageName -and -not $Force) {
            Write-Host "  ✅ $DisplayName ya está instalado" -ForegroundColor Green
            $instalaciones += [PSCustomObject]@{
                Herramienta = $DisplayName
                Estado = "✅ YA INSTALADO"
                Paquete = $PackageName
            }
            return $true
        }
        
        # Preparar comando de instalación
        $comando = "choco install $PackageName -y"
        if ($Parameters.Count -gt 0) {
            $comando += " " + ($Parameters -join " ")
        }
        
        if ($Verbose) {
            Write-Host "  💻 Ejecutando: $comando" -ForegroundColor Gray
        }
        
        # Ejecutar instalación
        Invoke-Expression $comando
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✅ $DisplayName instalado exitosamente" -ForegroundColor Green
            $instalaciones += [PSCustomObject]@{
                Herramienta = $DisplayName
                Estado = "✅ INSTALADO"
                Paquete = $PackageName
            }
            return $true
        } else {
            throw "Chocolatey retornó código de error: $LASTEXITCODE"
        }
    } catch {
        if ($Opcional) {
            Write-Host "  ⚠️  Error instalando $DisplayName (opcional): $($_.Exception.Message)" -ForegroundColor Yellow
            $instalaciones += [PSCustomObject]@{
                Herramienta = $DisplayName
                Estado = "⚠️ ERROR (Opcional)"
                Paquete = $PackageName
            }
        } else {
            Write-Host "  ❌ Error instalando $DisplayName : $($_.Exception.Message)" -ForegroundColor Red
            $errores += "Error instalando $DisplayName : $($_.Exception.Message)"
            $instalaciones += [PSCustomObject]@{
                Herramienta = $DisplayName
                Estado = "❌ ERROR"
                Paquete = $PackageName
            }
        }
        return $false
    }
}

function Install-VSCodeExtension {
    param(
        [string]$ExtensionId,
        [string]$DisplayName
    )
    
    Write-Host "🔌 Instalando extensión $DisplayName..." -ForegroundColor Yellow
    
    try {
        $resultado = code --install-extension $ExtensionId --force 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✅ Extensión $DisplayName instalada" -ForegroundColor Green
            return $true
        } else {
            Write-Host "  ❌ Error instalando extensión $DisplayName" -ForegroundColor Red
            $errores += "Error instalando extensión $DisplayName"
            return $false
        }
    } catch {
        Write-Host "  ❌ Error instalando extensión $DisplayName : $($_.Exception.Message)" -ForegroundColor Red
        $errores += "Error instalando extensión $DisplayName : $($_.Exception.Message)"
        return $false
    }
}

# Paso 1: Instalar o verificar Chocolatey
if (-not $SkipChocolatey) {
    Write-Host "🍫 INSTALANDO CHOCOLATEY" -ForegroundColor Cyan
    Write-Host "========================" -ForegroundColor Cyan
    
    if (-not (Get-Command "choco" -ErrorAction SilentlyContinue)) {
        Write-Host "📦 Chocolatey no encontrado. Instalando..." -ForegroundColor Yellow
        
        try {
            # Configurar política de ejecución temporalmente
            Set-ExecutionPolicy Bypass -Scope Process -Force
            
            # Configurar protocolos de seguridad
            [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
            
            # Instalar Chocolatey
            Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
            
            # Refrescar variables de entorno
            $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
            
            if (Get-Command "choco" -ErrorAction SilentlyContinue) {
                Write-Host "  ✅ Chocolatey instalado exitosamente" -ForegroundColor Green
            } else {
                throw "Chocolatey no está disponible después de la instalación"
            }
        } catch {
            Write-Host "  ❌ Error instalando Chocolatey: $($_.Exception.Message)" -ForegroundColor Red
            $errores += "Error instalando Chocolatey: $($_.Exception.Message)"
        }
    } else {
        Write-Host "  ✅ Chocolatey ya está instalado" -ForegroundColor Green
        
        # Actualizar Chocolatey
        Write-Host "📦 Actualizando Chocolatey..." -ForegroundColor Yellow
        choco upgrade chocolatey -y
    }
    
    Write-Host ""
}

# Paso 2: Instalar herramientas principales
Write-Host "🔧 INSTALANDO HERRAMIENTAS PRINCIPALES" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

Install-ChocolateyPackage -PackageName "dotnet-9.0-sdk" -DisplayName ".NET Core 9 SDK"
Install-ChocolateyPackage -PackageName "git" -DisplayName "Git"
Install-ChocolateyPackage -PackageName "azure-cli" -DisplayName "Azure CLI"
Install-ChocolateyPackage -PackageName "vscode" -DisplayName "Visual Studio Code"

Write-Host ""

# Paso 3: Instalar herramientas de penetration testing
Write-Host "🔍 INSTALANDO HERRAMIENTAS DE PENETRATION TESTING" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

Install-ChocolateyPackage -PackageName "nmap" -DisplayName "Nmap"
Install-ChocolateyPackage -PackageName "wireshark" -DisplayName "Wireshark" -Opcional
Install-ChocolateyPackage -PackageName "burp-suite-free-edition" -DisplayName "Burp Suite Community"
Install-ChocolateyPackage -PackageName "postman" -DisplayName "Postman"

Write-Host ""

# Paso 4: Instalar herramientas adicionales útiles
Write-Host "🛡️  INSTALANDO HERRAMIENTAS ADICIONALES" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan

Install-ChocolateyPackage -PackageName "curl" -DisplayName "cURL" -Opcional
Install-ChocolateyPackage -PackageName "wget" -DisplayName "Wget" -Opcional
Install-ChocolateyPackage -PackageName "jq" -DisplayName "jq (JSON processor)" -Opcional

Write-Host ""

# Paso 5: Refrescar variables de entorno
Write-Host "🔄 REFRESCANDO VARIABLES DE ENTORNO" -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan

try {
    # Refrescar PATH
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
    Write-Host "  ✅ Variables de entorno refrescadas" -ForegroundColor Green
} catch {
    Write-Host "  ⚠️  Error refrescando variables de entorno" -ForegroundColor Yellow
}

Write-Host ""

# Paso 6: Instalar extensiones de Visual Studio Code
Write-Host "🔌 INSTALANDO EXTENSIONES DE VISUAL STUDIO CODE" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan

if (Get-Command "code" -ErrorAction SilentlyContinue) {
    $extensiones = @(
        @{Id = "ms-dotnettools.csdevkit"; Name = "C# Dev Kit"},
        @{Id = "ms-vscode.azure-account"; Name = "Azure Account"},
        @{Id = "ms-azuretools.vscode-azureresourcegroups"; Name = "Azure Resources"},
        @{Id = "ms-vscode.azurecli"; Name = "Azure CLI Tools"},
        @{Id = "humao.rest-client"; Name = "REST Client"},
        @{Id = "ms-azuretools.vscode-azuresecuritycenter"; Name = "Azure Security Center"}
    )
    
    foreach ($extension in $extensiones) {
        Install-VSCodeExtension -ExtensionId $extension.Id -DisplayName $extension.Name
    }
} else {
    Write-Host "  ⚠️  Visual Studio Code no encontrado. Saltando instalación de extensiones." -ForegroundColor Yellow
}

Write-Host ""

# Paso 7: Verificar instalaciones
Write-Host "✅ VERIFICANDO INSTALACIONES" -ForegroundColor Cyan
Write-Host "============================" -ForegroundColor Cyan

$verificaciones = @(
    @{Comando = "choco --version"; Nombre = "Chocolatey"},
    @{Comando = "dotnet --version"; Nombre = ".NET Core"},
    @{Comando = "git --version"; Nombre = "Git"},
    @{Comando = "az --version"; Nombre = "Azure CLI"},
    @{Comando = "code --version"; Nombre = "Visual Studio Code"},
    @{Comando = "nmap --version"; Nombre = "Nmap"},
    @{Comando = "curl --version"; Nombre = "cURL"}
)

foreach ($verificacion in $verificaciones) {
    Write-Host "🔍 Verificando $($verificacion.Nombre)..." -ForegroundColor Yellow
    try {
        $resultado = Invoke-Expression $verificacion.Comando 2>$null
        if ($LASTEXITCODE -eq 0 -or $resultado) {
            Write-Host "  ✅ $($verificacion.Nombre): FUNCIONAL" -ForegroundColor Green
        } else {
            Write-Host "  ❌ $($verificacion.Nombre): NO FUNCIONAL" -ForegroundColor Red
        }
    } catch {
        Write-Host "  ❌ $($verificacion.Nombre): ERROR" -ForegroundColor Red
    }
}

Write-Host ""

# Generar reporte final
Write-Host "📊 REPORTE DE INSTALACIÓN" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan

# Mostrar tabla de instalaciones
$instalaciones | Format-Table -AutoSize

Write-Host ""

# Mostrar errores si los hay
if ($errores.Count -gt 0) {
    Write-Host "❌ ERRORES DURANTE LA INSTALACIÓN:" -ForegroundColor Red
    Write-Host "==================================" -ForegroundColor Red
    foreach ($error in $errores) {
        Write-Host "  • $error" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "💡 RECOMENDACIÓN:" -ForegroundColor Yellow
    Write-Host "Revise los errores arriba y ejecute el script nuevamente si es necesario." -ForegroundColor Yellow
    Write-Host ""
} else {
    Write-Host "🎉 ¡INSTALACIÓN COMPLETADA EXITOSAMENTE!" -ForegroundColor Green
    Write-Host "=======================================" -ForegroundColor Green
    Write-Host "Todas las herramientas se instalaron correctamente." -ForegroundColor Green
    Write-Host ""
}

# Guardar reporte de instalación
$reportePath = ".\reporte-instalacion.json"
$reporteData = @{
    FechaInstalacion = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Instalaciones = $instalaciones
    Errores = $errores
    EstadoInstalacion = if ($errores.Count -eq 0) { "EXITOSA" } else { "CON ERRORES" }
}

$reporteData | ConvertTo-Json -Depth 3 | Out-File -FilePath $reportePath -Encoding UTF8
Write-Host "📄 Reporte de instalación guardado en: $reportePath" -ForegroundColor Gray

Write-Host ""
Write-Host "🚀 PRÓXIMOS PASOS:" -ForegroundColor Cyan
Write-Host "1. Ejecute 'VerificarEntorno.ps1' para confirmar la instalación" -ForegroundColor White
Write-Host "2. Configure el acceso a Azure con 'ConfigurarAzure.ps1'" -ForegroundColor White
Write-Host "3. ¡Esté listo para los laboratorios de penetration testing!" -ForegroundColor White