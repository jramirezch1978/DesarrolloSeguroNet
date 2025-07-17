# Script de Testing End-to-End - Laboratorio 4
# Sesión 5: Protección de Datos y Azure Key Vault

param(
    [string]$AppUrl = "https://localhost:7001",
    [string]$KeyVaultName = "kv-devsgro-[sunombre]-[numero]"
)

Write-Host "=== Testing End-to-End - Laboratorio 4 ===" -ForegroundColor Green
Write-Host "URL de la aplicación: $AppUrl" -ForegroundColor Yellow
Write-Host "Key Vault: $KeyVaultName" -ForegroundColor Yellow
Write-Host ""

# Verificar que la aplicación esté ejecutándose
Write-Host "1. Verificando que la aplicación esté ejecutándose..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri $AppUrl -UseBasicParsing -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Aplicación ejecutándose correctamente" -ForegroundColor Green
    } else {
        Write-Host "❌ Aplicación no responde correctamente" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ No se puede conectar a la aplicación. Asegúrese de que esté ejecutándose." -ForegroundColor Red
    Write-Host "   Ejecute: dotnet run" -ForegroundColor Yellow
    exit 1
}

# Verificar Azure CLI
Write-Host "2. Verificando Azure CLI..." -ForegroundColor Yellow
try {
    $azAccount = az account show --query "name" -o tsv 2>$null
    if ($azAccount) {
        Write-Host "✅ Azure CLI autenticado: $azAccount" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Azure CLI no autenticado. Ejecute: az login" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Azure CLI no encontrado. Instale Azure CLI primero." -ForegroundColor Red
}

# Verificar Key Vault
Write-Host "3. Verificando acceso a Key Vault..." -ForegroundColor Yellow
try {
    $secrets = az keyvault secret list --vault-name $KeyVaultName --query "[].name" -o tsv 2>$null
    if ($secrets) {
        Write-Host "✅ Key Vault accesible. Secrets encontrados: $($secrets.Count)" -ForegroundColor Green
        foreach ($secret in $secrets) {
            Write-Host "   - $secret" -ForegroundColor Gray
        }
    } else {
        Write-Host "⚠️  No se encontraron secrets en Key Vault" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ No se puede acceder al Key Vault. Verifique permisos y nombre." -ForegroundColor Red
}

# Verificar .NET 9
Write-Host "4. Verificando .NET 9..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version
if ($dotnetVersion -like "9.*") {
    Write-Host "✅ .NET $dotnetVersion detectado" -ForegroundColor Green
} else {
    Write-Host "❌ .NET 9 no encontrado. Versión actual: $dotnetVersion" -ForegroundColor Red
}

# Verificar paquetes
Write-Host "5. Verificando paquetes Azure..." -ForegroundColor Yellow
$packages = dotnet list package

$requiredPackages = @(
    "Azure.Security.KeyVault.Keys",
    "Azure.Security.KeyVault.Secrets",
    "Azure.Identity",
    "Microsoft.AspNetCore.DataProtection.AzureStorage"
)

$allPackagesFound = $true
foreach ($package in $requiredPackages) {
    if ($packages -like "*$package*") {
        Write-Host "✅ $package instalado" -ForegroundColor Green
    } else {
        Write-Host "❌ $package NO instalado" -ForegroundColor Red
        $allPackagesFound = $false
    }
}

# Resumen final
Write-Host ""
Write-Host "=== RESUMEN DE TESTING ===" -ForegroundColor Green

if ($allPackagesFound) {
    Write-Host "✅ Todos los paquetes requeridos están instalados" -ForegroundColor Green
} else {
    Write-Host "❌ Faltan algunos paquetes. Ejecute: dotnet restore" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== PRÓXIMOS PASOS ===" -ForegroundColor Green
Write-Host "1. Navegue a: $AppUrl" -ForegroundColor Yellow
Write-Host "2. Inicie sesión con Azure AD" -ForegroundColor Yellow
Write-Host "3. Haga clic en 'Datos Seguros' en el menú" -ForegroundColor Yellow
Write-Host "4. Pruebe la protección de datos con diferentes propósitos" -ForegroundColor Yellow
Write-Host "5. Verifique la gestión de secrets en Key Vault" -ForegroundColor Yellow

Write-Host ""
Write-Host "=== DATOS DE PRUEBA ===" -ForegroundColor Green
Write-Host "Datos Personales: 'Juan Pérez, CC: 1234567890, Tel: 300-123-4567'" -ForegroundColor Yellow
Write-Host "Datos Financieros: 'Cuenta: 123-456-789, Saldo: $50,000'" -ForegroundColor Yellow
Write-Host "Datos Médicos: 'Paciente: María López, Diagnóstico: Diabetes Tipo 2'" -ForegroundColor Yellow

Write-Host ""
Write-Host "Testing completado. ¡Listo para continuar!" -ForegroundColor Green 