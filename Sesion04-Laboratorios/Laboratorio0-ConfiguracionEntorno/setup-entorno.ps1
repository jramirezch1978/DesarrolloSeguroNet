# 🛠️ Script de Configuración Automatizada del Entorno - Laboratorio 0
# Diseño Seguro de Aplicaciones .NET en Azure - Sesión 4

Write-Host "🚀 Iniciando configuración del entorno de desarrollo..." -ForegroundColor Green
Write-Host "📋 Laboratorio 0: Configuración del Entorno de Desarrollo" -ForegroundColor Yellow

# Verificar si se está ejecutando como administrador
function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Función para verificar si un comando existe
function Test-Command {
    param($Command)
    try {
        Get-Command $Command -ErrorAction Stop
        return $true
    } catch {
        return $false
    }
}

# Función para instalar Chocolatey
function Install-Chocolatey {
    Write-Host "🍫 Instalando Chocolatey..." -ForegroundColor Cyan
    
    # Verificar si ya está instalado
    if (Test-Command "choco") {
        Write-Host "✅ Chocolatey ya está instalado" -ForegroundColor Green
        choco --version
        return $true
    }
    
    try {
        # Configurar política de ejecución
        Set-ExecutionPolicy Bypass -Scope Process -Force
        
        # Instalar Chocolatey
        [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
        iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
        
        # Verificar instalación
        if (Test-Command "choco") {
            Write-Host "✅ Chocolatey instalado exitosamente" -ForegroundColor Green
            choco --version
            return $true
        }
    } catch {
        Write-Host "❌ Error al instalar Chocolatey: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Función para instalar .NET 9
function Install-DotNet9 {
    Write-Host "🔧 Instalando .NET 9 SDK..." -ForegroundColor Cyan
    
    # Verificar si ya está instalado
    if (Test-Command "dotnet") {
        $dotnetVersion = dotnet --version
        if ($dotnetVersion -like "9.*") {
            Write-Host "✅ .NET 9 ya está instalado: $dotnetVersion" -ForegroundColor Green
            return $true
        }
    }
    
    try {
        # Instalar .NET 9 usando Chocolatey
        choco install dotnet-9.0-sdk -y
        
        # Refrescar variables de entorno
        $env:PATH = [System.Environment]::GetEnvironmentVariable("PATH", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("PATH", "User")
        
        # Verificar instalación
        if (Test-Command "dotnet") {
            $dotnetVersion = dotnet --version
            Write-Host "✅ .NET 9 instalado exitosamente: $dotnetVersion" -ForegroundColor Green
            
            # Mostrar SDKs instalados
            Write-Host "📦 SDKs instalados:" -ForegroundColor Yellow
            dotnet --list-sdks
            return $true
        }
    } catch {
        Write-Host "❌ Error al instalar .NET 9: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Función para instalar Visual Studio Code
function Install-VSCode {
    Write-Host "📝 Instalando Visual Studio Code..." -ForegroundColor Cyan
    
    # Verificar si ya está instalado
    if (Test-Command "code") {
        Write-Host "✅ Visual Studio Code ya está instalado" -ForegroundColor Green
        code --version
        return $true
    }
    
    try {
        # Instalar VS Code usando Chocolatey
        choco install vscode -y
        
        # Refrescar variables de entorno
        $env:PATH = [System.Environment]::GetEnvironmentVariable("PATH", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("PATH", "User")
        
        # Verificar instalación
        if (Test-Command "code") {
            Write-Host "✅ Visual Studio Code instalado exitosamente" -ForegroundColor Green
            code --version
            return $true
        }
    } catch {
        Write-Host "❌ Error al instalar Visual Studio Code: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Función para instalar extensiones de VS Code
function Install-VSCodeExtensions {
    Write-Host "🧩 Instalando extensiones de Visual Studio Code..." -ForegroundColor Cyan
    
    $extensions = @(
        "ms-dotnettools.csharp",
        "ms-dotnettools.csdevkit",
        "ms-vscode.vscode-node-azure-pack",
        "ms-vscode.vscode-json",
        "eamodio.gitlens",
        "ms-vscode.PowerShell",
        "ms-dotnettools.vscode-dotnet-runtime"
    )
    
    foreach ($extension in $extensions) {
        try {
            Write-Host "  📦 Instalando $extension..." -ForegroundColor Yellow
            code --install-extension $extension --force
            Write-Host "  ✅ $extension instalado" -ForegroundColor Green
        } catch {
            Write-Host "  ❌ Error al instalar $extension" -ForegroundColor Red
        }
    }
}

# Función para verificar el entorno completo
function Test-Environment {
    Write-Host "🔍 Verificando entorno de desarrollo..." -ForegroundColor Cyan
    
    $results = @()
    
    # Verificar Chocolatey
    if (Test-Command "choco") {
        $chocoVersion = choco --version
        $results += "✅ Chocolatey: $chocoVersion"
    } else {
        $results += "❌ Chocolatey: No instalado"
    }
    
    # Verificar .NET
    if (Test-Command "dotnet") {
        $dotnetVersion = dotnet --version
        $results += "✅ .NET SDK: $dotnetVersion"
    } else {
        $results += "❌ .NET SDK: No instalado"
    }
    
    # Verificar VS Code
    if (Test-Command "code") {
        $codeVersion = (code --version)[0]
        $results += "✅ VS Code: $codeVersion"
    } else {
        $results += "❌ VS Code: No instalado"
    }
    
    # Mostrar resultados
    Write-Host "📊 Resumen del entorno:" -ForegroundColor Yellow
    foreach ($result in $results) {
        Write-Host "  $result"
    }
    
    return $results
}

# Función para crear proyecto de prueba
function New-TestProject {
    Write-Host "🧪 Creando proyecto de prueba..." -ForegroundColor Cyan
    
    $testDir = "test-dotnet9-lab0"
    
    try {
        # Crear directorio de prueba
        if (Test-Path $testDir) {
            Remove-Item $testDir -Recurse -Force
        }
        
        New-Item -ItemType Directory -Name $testDir
        Set-Location $testDir
        
        # Crear proyecto console
        dotnet new console -n "TestApp"
        Set-Location "TestApp"
        
        # Ejecutar proyecto
        Write-Host "🏃 Ejecutando proyecto de prueba..." -ForegroundColor Yellow
        $output = dotnet run
        
        if ($output -eq "Hello, World!") {
            Write-Host "✅ Proyecto de prueba ejecutado exitosamente: $output" -ForegroundColor Green
        } else {
            Write-Host "⚠️  Proyecto ejecutado con salida: $output" -ForegroundColor Yellow
        }
        
        # Regresar al directorio original
        Set-Location ..
        Set-Location ..
        
        # Limpiar proyecto de prueba
        Remove-Item $testDir -Recurse -Force
        
        return $true
    } catch {
        Write-Host "❌ Error al crear/ejecutar proyecto de prueba: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Función principal
function Main {
    try {
        Write-Host "🎯 Iniciando configuración automatizada del entorno..." -ForegroundColor Green
        
        # Verificar permisos de administrador
        if (-not (Test-Administrator)) {
            Write-Host "❌ Este script debe ejecutarse como administrador" -ForegroundColor Red
            Write-Host "💡 Haga clic derecho en PowerShell y seleccione 'Ejecutar como administrador'" -ForegroundColor Yellow
            return
        }
        
        Write-Host "✅ Ejecutándose con permisos de administrador" -ForegroundColor Green
        
        # Instalación paso a paso
        $steps = @(
            @{ Name = "Chocolatey"; Function = { Install-Chocolatey } },
            @{ Name = ".NET 9 SDK"; Function = { Install-DotNet9 } },
            @{ Name = "Visual Studio Code"; Function = { Install-VSCode } },
            @{ Name = "Extensiones VS Code"; Function = { Install-VSCodeExtensions } }
        )
        
        $successCount = 0
        foreach ($step in $steps) {
            Write-Host "`n🔄 Paso: $($step.Name)" -ForegroundColor Blue
            if (& $step.Function) {
                $successCount++
                Write-Host "✅ $($step.Name) completado exitosamente`n" -ForegroundColor Green
            } else {
                Write-Host "❌ $($step.Name) falló`n" -ForegroundColor Red
            }
        }
        
        # Verificar entorno completo
        Write-Host "`n🔍 Verificación final del entorno:" -ForegroundColor Blue
        Test-Environment
        
        # Crear proyecto de prueba
        Write-Host "`n🧪 Prueba del entorno:" -ForegroundColor Blue
        New-TestProject
        
        # Resumen final
        Write-Host "`n📋 Resumen de la configuración:" -ForegroundColor Yellow
        Write-Host "✅ Pasos completados exitosamente: $successCount de $($steps.Count)" -ForegroundColor Green
        
        if ($successCount -eq $steps.Count) {
            Write-Host "🎉 ¡Entorno de desarrollo configurado exitosamente!" -ForegroundColor Green
            Write-Host "🚀 Puede proceder con el Laboratorio 1: Registro y configuración de aplicación en Azure AD" -ForegroundColor Cyan
        } else {
            Write-Host "⚠️  Algunos pasos fallaron. Revise los mensajes de error anteriores." -ForegroundColor Yellow
            Write-Host "💡 Consulte el archivo README.md para troubleshooting manual" -ForegroundColor Yellow
        }
        
    } catch {
        Write-Host "❌ Error general en la configuración: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Ejecutar función principal
Main

Write-Host "`n🏁 Script de configuración completado." -ForegroundColor Green
Write-Host "📖 Para más información, consulte el archivo README.md" -ForegroundColor Cyan 