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

Write-Host "`n[LOCAL] DEPLOYMENT LOCAL DE SECURESHOP" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

# Variables de configuración
$projectPath = "src\SecureShop.Web\SecureShop.Web.csproj"
$solutionPath = "src\SecureShop.sln"
$localUrl = "https://localhost:$Port"

# Verificar prerrequisitos
Write-Host "`n[CHECK] Verificando prerrequisitos..." -ForegroundColor Yellow

# Verificar .NET SDK
try {
    $dotnetVersion = dotnet --version
    Write-Host "[OK] .NET SDK: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Error "❌ .NET SDK no encontrado. Instalar desde: https://dotnet.microsoft.com/download"
    exit 1
}

# Verificar Entity Framework Tools
try {
    dotnet ef --version 2>$null
    Write-Host "[OK] Entity Framework Tools: Instalado" -ForegroundColor Green
} catch {
    Write-Host "[WARNING] Entity Framework Tools no encontrado. Instalando..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
}

# Navegar al directorio del proyecto
if (-not (Test-Path $solutionPath)) {
    Write-Error "❌ No se encontró la solución en: $solutionPath"
    Write-Host "Asegúrate de ejecutar este script desde el directorio Laboratorio-35-WebApp" -ForegroundColor Red
    exit 1
}

Write-Host "[OK] Directorio del proyecto verificado" -ForegroundColor Green

# ===== FASE 1: LIMPIEZA Y RESTAURACIÓN =====
Write-Host "`n[PHASE1] Fase 1: Limpieza y restauracion..." -ForegroundColor Cyan

Write-Host "[CLEAN] Limpiando solucion..." -ForegroundColor White
dotnet clean $solutionPath --verbosity quiet

Write-Host "[RESTORE] Restaurando paquetes NuGet..." -ForegroundColor White
dotnet restore $solutionPath --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ Error en restauración de paquetes"
    exit 1
}

Write-Host "[OK] Limpieza y restauracion completadas" -ForegroundColor Green

# ===== FASE 2: CONFIGURACIÓN DE BASE DE DATOS =====
if ($SetupDatabase) {
    Write-Host "`n[PHASE2] Fase 2: Configuracion de base de datos..." -ForegroundColor Cyan
    
    Write-Host "[EF] Configurando Entity Framework..." -ForegroundColor White
    
    # Verificar si hay migraciones
    $migrationsPath = "src\SecureShop.Data\Migrations"
    if (-not (Test-Path $migrationsPath)) {
        Write-Host "[MIGRATION] Creando migracion inicial..." -ForegroundColor White
        dotnet ef migrations add InitialCreate --project src\SecureShop.Data --startup-project src\SecureShop.Web
    }
    
    # Aplicar migraciones
    Write-Host "[UPDATE] Aplicando migraciones a base de datos..." -ForegroundColor White
    dotnet ef database update --project src\SecureShop.Data --startup-project src\SecureShop.Web
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "❌ Error configurando base de datos"
        exit 1
    }
    
    Write-Host "[OK] Base de datos configurada" -ForegroundColor Green
} else {
    Write-Host "`n[PHASE2] Fase 2: Base de datos (omitida - usar -SetupDatabase para configurar)" -ForegroundColor Yellow
}

# ===== FASE 3: COMPILACIÓN =====
Write-Host "`n[PHASE3] Fase 3: Compilacion..." -ForegroundColor Cyan

Write-Host "[BUILD] Compilando aplicacion..." -ForegroundColor White
dotnet build $solutionPath --configuration Debug --no-restore --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ Error en compilación"
    exit 1
}

Write-Host "[OK] Compilacion exitosa" -ForegroundColor Green

# ===== FASE 4: CONFIGURACIÓN DEL ENTORNO =====
Write-Host "`n[PHASE4] Fase 4: Configuracion del entorno..." -ForegroundColor Cyan

# Configurar variables de entorno
$env:ASPNETCORE_ENVIRONMENT = $Environment
$env:ASPNETCORE_URLS = "https://localhost:$Port;http://localhost:5000"

Write-Host "[ENV] Environment: $Environment" -ForegroundColor White
Write-Host "[URLS] URLs: $($env:ASPNETCORE_URLS)" -ForegroundColor White

# Verificar certificados de desarrollo HTTPS
Write-Host "[HTTPS] Verificando certificados HTTPS..." -ForegroundColor White
dotnet dev-certs https --trust 2>$null

Write-Host "[OK] Configuracion del entorno completada" -ForegroundColor Green

# ===== FASE 5: INICIO DE LA APLICACIÓN =====
Write-Host "`n[PHASE5] Fase 5: Iniciando aplicacion..." -ForegroundColor Cyan

Write-Host "[START] Iniciando SecureShop..." -ForegroundColor White
Write-Host "[URL] URL Principal: $localUrl" -ForegroundColor Yellow
Write-Host "[HEALTH] Health Check: $localUrl/health" -ForegroundColor Yellow
Write-Host "[DASH] Dashboard: $localUrl/dashboard" -ForegroundColor Yellow

# Abrir navegador si se solicita
if ($OpenBrowser) {
    Write-Host "[BROWSER] Abriendo navegador en 3 segundos..." -ForegroundColor White
    Start-Job -ScriptBlock {
        Start-Sleep 3
        Start-Process $using:localUrl
    } | Out-Null
}

Write-Host "`n[INFO] Para detener la aplicacion, presiona Ctrl+C" -ForegroundColor Yellow
Write-Host "[LOGS] Para ver logs detallados, agrega --verbosity detailed" -ForegroundColor Gray

# Ejecutar la aplicación
try {
    dotnet run --project $projectPath --environment $Environment --urls $env:ASPNETCORE_URLS
} catch {
    Write-Host "`n[WARNING] Aplicacion detenida por el usuario" -ForegroundColor Yellow
}

Write-Host "`n[SUCCESS] Deployment local completado" -ForegroundColor Green
Write-Host "[SUMMARY] Resumen:" -ForegroundColor Cyan
Write-Host "   [APP] Aplicacion: SecureShop" -ForegroundColor White
Write-Host "   [ENV] Environment: $Environment" -ForegroundColor White
Write-Host "   [URL] URL: $localUrl" -ForegroundColor White
Write-Host "   [DB] Base de datos: LocalDB/InMemory" -ForegroundColor White

# ===== INFORMACIÓN ADICIONAL =====
Write-Host "`n[INFO] Informacion adicional:" -ForegroundColor Cyan
Write-Host "[DOCS] Para ver documentacion: Abrir README.md" -ForegroundColor White
Write-Host "[CONFIG] Para reconfigurar BD: Ejecutar con -SetupDatabase" -ForegroundColor White
Write-Host "[AZURE] Para deployment Azure: Usar deploy-azure.ps1" -ForegroundColor White
Write-Host "[DOCKER] Para Docker: Usar docker-compose up" -ForegroundColor White