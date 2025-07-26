# Script de Configuración del Entorno - Sesión 8
# Diseño Seguro de Aplicaciones (.NET en Azure)

param(
    [string]$ResourceGroupName = "rg-security-lab-$env:USERNAME",
    [string]$Location = "eastus"
)

Write-Host "🚀 Iniciando configuración del entorno para Sesión 8..." -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green

# Verificar si Chocolatey está instalado
Write-Host "`n🔍 Verificando Chocolatey..." -ForegroundColor Yellow
try {
    $chocoVersion = choco --version
    Write-Host "✅ Chocolatey ya está instalado: $chocoVersion" -ForegroundColor Green
} catch {
    Write-Host "📦 Instalando Chocolatey..." -ForegroundColor Yellow
    
    # Cambiar política de ejecución temporalmente
    Set-ExecutionPolicy Bypass -Scope Process -Force
    
    # Instalar Chocolatey
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
    iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
    
    # Refrescar variables de entorno
    refreshenv
    
    Write-Host "✅ Chocolatey instalado exitosamente" -ForegroundColor Green
}

# Instalar herramientas principales
Write-Host "`n🔧 Instalando herramientas principales..." -ForegroundColor Yellow

$tools = @(
    "dotnet-9.0-sdk",
    "azure-cli",
    "git",
    "nmap",
    "postman",
    "vscode"
)

foreach ($tool in $tools) {
    Write-Host "📦 Instalando $tool..." -ForegroundColor Cyan
    choco install $tool -y
}

# Refrescar variables de entorno
refreshenv

# Verificar instalaciones
Write-Host "`n🔍 Verificando instalaciones..." -ForegroundColor Yellow

$verifications = @{
    "dotnet" = "dotnet --version"
    "az" = "az --version"
    "git" = "git --version"
    "nmap" = "nmap --version"
    "code" = "code --version"
}

foreach ($tool in $verifications.Keys) {
    try {
        $version = Invoke-Expression $verifications[$tool] | Select-Object -First 1
        Write-Host "✅ ${tool}: $version" -ForegroundColor Green
    } catch {
        Write-Host "❌ ${tool}: No encontrado" -ForegroundColor Red
    }
}

# Instalar extensiones de VS Code
Write-Host "`n🔧 Instalando extensiones de VS Code..." -ForegroundColor Yellow

$extensions = @(
    "ms-dotnettools.csdevkit",
    "ms-vscode.azure-account",
    "ms-azuretools.vscode-azureresourcegroups",
    "ms-vscode.azurecli",
    "humao.rest-client",
    "ms-azuretools.vscode-azuresecuritycenter"
)

foreach ($extension in $extensions) {
    Write-Host "📦 Instalando extensión: $extension" -ForegroundColor Cyan
    code --install-extension $extension
}

# Verificar conexión Azure
Write-Host "`n🔐 Verificando conexión Azure..." -ForegroundColor Yellow

try {
    $account = az account show --output json | ConvertFrom-Json
    Write-Host "✅ Azure: Conectado como $($account.user.name)" -ForegroundColor Green
    Write-Host "   Suscripción: $($account.name)" -ForegroundColor Cyan
} catch {
    Write-Host "🔐 Iniciando login de Azure..." -ForegroundColor Yellow
    az login
}

# Crear resource group si no existe
Write-Host "`n🏗️ Verificando resource group..." -ForegroundColor Yellow

try {
    $rg = az group show --name $ResourceGroupName --output json | ConvertFrom-Json
    Write-Host "✅ Resource group ya existe: $($rg.name)" -ForegroundColor Green
} catch {
    Write-Host "🏗️ Creando resource group: $ResourceGroupName" -ForegroundColor Yellow
    az group create --name $ResourceGroupName --location $Location --tags Environment=Development Project=SecurityLab
    Write-Host "✅ Resource group creado exitosamente" -ForegroundColor Green
}

# Crear directorios de trabajo
Write-Host "`n📁 Creando directorios de trabajo..." -ForegroundColor Yellow

$directories = @(
    "scripts",
    "templates",
    "reports",
    "logs"
)

foreach ($dir in $directories) {
    if (!(Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force
        Write-Host "✅ Directorio creado: $dir" -ForegroundColor Green
    } else {
        Write-Host "✅ Directorio ya existe: $dir" -ForegroundColor Green
    }
}

# Crear script de verificación
Write-Host "`n📝 Creando script de verificación..." -ForegroundColor Yellow

$verificationScript = @"
# Script de verificación del entorno
Write-Host "=== VERIFICACIÓN DEL ENTORNO - SESIÓN 8 ===" -ForegroundColor Green

# Verificar herramientas
`$tools = @{
    "dotnet" = "dotnet --version"
    "az" = "az --version"
    "git" = "git --version"
    "nmap" = "nmap --version"
    "code" = "code --version"
}

foreach (`$tool in `$tools.Keys) {
    try {
        `$version = Invoke-Expression `$tools[`$tool] | Select-Object -First 1
        Write-Host "✅ `$tool`: `$version" -ForegroundColor Green
    } catch {
        Write-Host "❌ `$tool`: No encontrado" -ForegroundColor Red
    }
}

# Verificar Azure
try {
    `$account = az account show --output json | ConvertFrom-Json
    Write-Host "✅ Azure: Conectado como `$(`$account.user.name)" -ForegroundColor Green
} catch {
    Write-Host "❌ Azure: No conectado" -ForegroundColor Red
}

# Verificar resource group
try {
    `$rg = az group show --name "$ResourceGroupName" --output json | ConvertFrom-Json
    Write-Host "✅ Resource Group: `$(`$rg.name)" -ForegroundColor Green
} catch {
    Write-Host "❌ Resource Group: No encontrado" -ForegroundColor Red
}

Write-Host "`n=== VERIFICACIÓN COMPLETADA ===" -ForegroundColor Green
"@

$verificationScript | Out-File -FilePath "scripts/verify-environment.ps1" -Encoding UTF8

Write-Host "`n🎉 ¡Configuración del entorno completada exitosamente!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green
Write-Host "`n📋 Próximos pasos:" -ForegroundColor Cyan
Write-Host "1. Ejecutar: .\scripts\verify-environment.ps1" -ForegroundColor White
Write-Host "2. Comenzar con Laboratorio0-Configuracion.md" -ForegroundColor White
Write-Host "3. Seguir los laboratorios en orden secuencial" -ForegroundColor White

Write-Host "`n🔗 Recursos disponibles:" -ForegroundColor Cyan
Write-Host "- Laboratorio0-Configuracion.md" -ForegroundColor White
Write-Host "- Laboratorio1-SecurityCenter.md" -ForegroundColor White
Write-Host "- Laboratorio2-VulnerabilityAssessment.md" -ForegroundColor White
Write-Host "- Laboratorio3-Automatizacion.md" -ForegroundColor White

Write-Host "`n✅ ¡Listo para comenzar los laboratorios de seguridad!" -ForegroundColor Green 