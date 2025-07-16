# Verificación de Setup - Laboratorio Sesión 5
Write-Host "=== Verificación de Setup - Sesión 5 ===" -ForegroundColor Green

# Verificar .NET 9
Write-Host "1. Verificando .NET 9..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version
if ($dotnetVersion -like "9.*") {
    Write-Host "✅ .NET $dotnetVersion detectado" -ForegroundColor Green
} else {
    Write-Host "❌ .NET 9 no encontrado. Versión actual: $dotnetVersion" -ForegroundColor Red
}

# Verificar Azure CLI
Write-Host "2. Verificando Azure CLI..." -ForegroundColor Yellow
try {
    $azVersion = az --version | Select-String "azure-cli" | Select-Object -First 1
    Write-Host "✅ Azure CLI detectado" -ForegroundColor Green
} catch {
    Write-Host "❌ Azure CLI no encontrado" -ForegroundColor Red
}

# Verificar proyecto existe
Write-Host "3. Verificando estructura de proyecto..." -ForegroundColor Yellow
if (Test-Path "DevSeguroWebApp.csproj") {
    Write-Host "✅ Proyecto DevSeguroWebApp encontrado" -ForegroundColor Green
    
    # Verificar packages críticos
    Write-Host "4. Verificando packages Azure..." -ForegroundColor Yellow
    $packages = dotnet list package
    
    $requiredPackages = @(
        "Azure.Security.KeyVault.Keys",
        "Azure.Security.KeyVault.Secrets",
        "Azure.Identity",
        "Microsoft.AspNetCore.DataProtection.AzureStorage"
    )
    
    foreach ($package in $requiredPackages) {
        if ($packages -like "*$package*") {
            Write-Host "✅ $package instalado" -ForegroundColor Green
        } else {
            Write-Host "❌ $package NO instalado" -ForegroundColor Red
        }
    }
} else {
    Write-Host "❌ Proyecto no encontrado en directorio actual" -ForegroundColor Red
}

# Verificar conectividad a Azure
Write-Host "5. Verificando conectividad Azure..." -ForegroundColor Yellow
try {
    $azAccount = az account show --query "name" -o tsv 2>$null
    if ($azAccount) {
        Write-Host "✅ Conectado a Azure: $azAccount" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Azure CLI no autenticado. Ejecutar: az login" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️  Verificar conexión a Azure" -ForegroundColor Yellow
}

Write-Host "=== Verificación Completada ===" -ForegroundColor Green 