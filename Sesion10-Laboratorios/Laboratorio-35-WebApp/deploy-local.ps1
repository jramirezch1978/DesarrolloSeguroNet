# ==========================================
# SCRIPT DE DEPLOYMENT LOCAL - LABORATORIO 35
# ==========================================
# Script para desplegar SecureShop en entorno local de desarrollo

param(
    [string]$Environment = "Development",
    [switch]$SetupDatabase = $false,
    [switch]$OpenBrowser = $true,
    [string]$Port = "7000"
)

Write-Host "`n🚀 DEPLOYMENT LOCAL DE SECURESHOP" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

# Variables de configuración
$projectPath = "src\SecureShop.Web\SecureShop.Web.csproj"
$solutionPath = "src\SecureShop.sln"
$localUrl = "https://localhost:$Port"

# Verificar prerrequisitos
Write-Host "`n🔍 Verificando prerrequisitos..." -ForegroundColor Yellow

# Verificar .NET SDK
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET SDK: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Error "❌ .NET SDK no encontrado. Instalar desde: https://dotnet.microsoft.com/download"
    exit 1
}

# Verificar Entity Framework Tools
try {
    dotnet ef --version 2>$null
    Write-Host "✅ Entity Framework Tools: Instalado" -ForegroundColor Green
} catch {
    Write-Host "⚠️ Entity Framework Tools no encontrado. Instalando..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
}

# Navegar al directorio del proyecto
if (-not (Test-Path $solutionPath)) {
    Write-Error "❌ No se encontró la solución en: $solutionPath"
    Write-Host "Asegúrate de ejecutar este script desde el directorio Laboratorio-35-WebApp" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Directorio del proyecto verificado" -ForegroundColor Green

# ===== FASE 1: LIMPIEZA Y RESTAURACIÓN =====
Write-Host "`n🧹 Fase 1: Limpieza y restauración..." -ForegroundColor Cyan

Write-Host "🔄 Limpiando solución..." -ForegroundColor White
dotnet clean $solutionPath --verbosity quiet

Write-Host "📦 Restaurando paquetes NuGet..." -ForegroundColor White
dotnet restore $solutionPath --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ Error en restauración de paquetes"
    exit 1
}

Write-Host "✅ Limpieza y restauración completadas" -ForegroundColor Green

# ===== FASE 2: CONFIGURACIÓN DE BASE DE DATOS =====
if ($SetupDatabase) {
    Write-Host "`n💾 Fase 2: Configuración de base de datos..." -ForegroundColor Cyan
    
    Write-Host "📊 Configurando Entity Framework..." -ForegroundColor White
    
    # Verificar si hay migraciones
    $migrationsPath = "src\SecureShop.Data\Migrations"
    if (-not (Test-Path $migrationsPath)) {
        Write-Host "🔧 Creando migración inicial..." -ForegroundColor White
        dotnet ef migrations add InitialCreate --project src\SecureShop.Data --startup-project src\SecureShop.Web
    }
    
    # Aplicar migraciones
    Write-Host "🔄 Aplicando migraciones a base de datos..." -ForegroundColor White
    dotnet ef database update --project src\SecureShop.Data --startup-project src\SecureShop.Web
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "❌ Error configurando base de datos"
        exit 1
    }
    
    Write-Host "✅ Base de datos configurada" -ForegroundColor Green
} else {
    Write-Host "`n💾 Fase 2: Base de datos (omitida - usar -SetupDatabase para configurar)" -ForegroundColor Yellow
}

# ===== FASE 3: COMPILACIÓN =====
Write-Host "`n🔨 Fase 3: Compilación..." -ForegroundColor Cyan

Write-Host "⚙️ Compilando aplicación..." -ForegroundColor White
dotnet build $solutionPath --configuration Debug --no-restore --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ Error en compilación"
    exit 1
}

Write-Host "✅ Compilación exitosa" -ForegroundColor Green

# ===== FASE 4: CONFIGURACIÓN DEL ENTORNO =====
Write-Host "`n⚙️ Fase 4: Configuración del entorno..." -ForegroundColor Cyan

# Configurar variables de entorno
$env:ASPNETCORE_ENVIRONMENT = $Environment
$env:ASPNETCORE_URLS = "https://localhost:$Port;http://localhost:5000"

Write-Host "🔧 Environment: $Environment" -ForegroundColor White
Write-Host "🌐 URLs: $($env:ASPNETCORE_URLS)" -ForegroundColor White

# Verificar certificados de desarrollo HTTPS
Write-Host "🔐 Verificando certificados HTTPS..." -ForegroundColor White
dotnet dev-certs https --trust 2>$null

Write-Host "✅ Configuración del entorno completada" -ForegroundColor Green

# ===== FASE 5: INICIO DE LA APLICACIÓN =====
Write-Host "`n🚀 Fase 5: Iniciando aplicación..." -ForegroundColor Cyan

Write-Host "🌟 Iniciando SecureShop..." -ForegroundColor White
Write-Host "📍 URL Principal: $localUrl" -ForegroundColor Yellow
Write-Host "📊 Health Check: $localUrl/health" -ForegroundColor Yellow
Write-Host "🎛️ Dashboard: $localUrl/dashboard" -ForegroundColor Yellow

# Abrir navegador si se solicita
if ($OpenBrowser) {
    Write-Host "🌐 Abriendo navegador en 3 segundos..." -ForegroundColor White
    Start-Job -ScriptBlock {
        Start-Sleep 3
        Start-Process $using:localUrl
    } | Out-Null
}

Write-Host "`n🎯 Para detener la aplicación, presiona Ctrl+C" -ForegroundColor Yellow
Write-Host "📝 Para ver logs detallados, agrega --verbosity detailed" -ForegroundColor Gray

# Ejecutar la aplicación
try {
    dotnet run --project $projectPath --environment $Environment --urls $env:ASPNETCORE_URLS
} catch {
    Write-Host "`n⚠️ Aplicación detenida por el usuario" -ForegroundColor Yellow
}

Write-Host "`n✅ Deployment local completado" -ForegroundColor Green
Write-Host "📋 Resumen:" -ForegroundColor Cyan
Write-Host "   🎯 Aplicación: SecureShop" -ForegroundColor White
Write-Host "   🌍 Environment: $Environment" -ForegroundColor White
Write-Host "   🔗 URL: $localUrl" -ForegroundColor White
Write-Host "   💾 Base de datos: LocalDB/InMemory" -ForegroundColor White

# ===== INFORMACIÓN ADICIONAL =====
Write-Host "`n💡 Información adicional:" -ForegroundColor Cyan
Write-Host "📚 Para ver documentación: Abrir README.md" -ForegroundColor White
Write-Host "🔧 Para reconfigurar BD: Ejecutar con -SetupDatabase" -ForegroundColor White
Write-Host "☁️ Para deployment Azure: Usar deploy-azure.ps1" -ForegroundColor White
Write-Host "🐳 Para Docker: Usar docker-compose up" -ForegroundColor White