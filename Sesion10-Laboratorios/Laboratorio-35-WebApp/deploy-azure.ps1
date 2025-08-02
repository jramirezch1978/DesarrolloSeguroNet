# ==========================================
# SCRIPT DE DEPLOYMENT AZURE - LABORATORIO 35
# ==========================================
# Script para desplegar SecureShop a Azure App Service

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$true)]
    [string]$WebAppName,
    
    [string]$Location = "East US",
    [string]$AppServicePlan = "SecureShop-Plan",
    [string]$Environment = "Production",
    [switch]$CreateResources = $false,
    [switch]$SetupDatabase = $false
)

Write-Host "`n[CLOUD] DEPLOYMENT DE SECURESHOP A AZURE" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

# Variables de configuración
$solutionPath = "src\SecureShop.sln"
$projectPath = "src\SecureShop.Web\SecureShop.Web.csproj"
$publishPath = ".\publish"
$packagePath = ".\secureshop.zip"

# ===== VERIFICAR PRERREQUISITOS =====
Write-Host "`n[CHECK] Verificando prerrequisitos..." -ForegroundColor Yellow

# Verificar Azure CLI
try {
    $azVersion = az --version | Select-String "azure-cli" | ForEach-Object { $_.ToString().Split()[1] }
    Write-Host "[OK] Azure CLI: $azVersion" -ForegroundColor Green
} catch {
    Write-Error "❌ Azure CLI no encontrado. Instalar desde: https://aka.ms/installazurecli"
    exit 1
}

# Verificar autenticación
try {
    $currentUser = az account show --query user.name --output tsv 2>$null
    if (-not $currentUser) {
        Write-Host "[AUTH] Iniciando sesion en Azure..." -ForegroundColor Yellow
        az login
        $currentUser = az account show --query user.name --output tsv
    }
    Write-Host "[OK] Autenticado como: $currentUser" -ForegroundColor Green
} catch {
    Write-Error "❌ Error en autenticación Azure"
    exit 1
}

# Verificar proyecto
if (-not (Test-Path $solutionPath)) {
    Write-Error "❌ No se encontró la solución en: $solutionPath"
    exit 1
}

# ===== CREAR RECURSOS DE AZURE (OPCIONAL) =====
if ($CreateResources) {
    Write-Host "`n[BUILD] Creando recursos de Azure..." -ForegroundColor Cyan
    
    # Crear Resource Group
    Write-Host "[RG] Creando Resource Group: $ResourceGroupName" -ForegroundColor White
    az group create --name $ResourceGroupName --location $Location
    
    # Crear App Service Plan
    Write-Host "[PLAN] Creando App Service Plan: $AppServicePlan" -ForegroundColor White
    az appservice plan create `
        --name $AppServicePlan `
        --resource-group $ResourceGroupName `
        --location $Location `
        --sku S1 `
        --is-linux
    
    # Crear Web App
    Write-Host "[APP] Creando Web App: $WebAppName" -ForegroundColor White
    az webapp create `
        --name $WebAppName `
        --resource-group $ResourceGroupName `
        --plan $AppServicePlan `
        --runtime "DOTNETCORE:8.0"
    
    Write-Host "[OK] Recursos de Azure creados" -ForegroundColor Green
}

# ===== CONFIGURAR BASE DE DATOS AZURE (OPCIONAL) =====
if ($SetupDatabase) {
    Write-Host "`n[DB] Configurando Azure SQL Database..." -ForegroundColor Cyan
    
    $sqlServerName = "$WebAppName-sql"
    $sqlDatabaseName = "SecureShopDB"
    $adminUser = "secureshopAdmin"
    $adminPassword = "SecureShop2024!"
    
    # Crear SQL Server
    Write-Host "[SQL] Creando SQL Server: $sqlServerName" -ForegroundColor White
    az sql server create `
        --name $sqlServerName `
        --resource-group $ResourceGroupName `
        --location $Location `
        --admin-user $adminUser `
        --admin-password $adminPassword
    
    # Crear SQL Database
    Write-Host "[DB] Creando SQL Database: $sqlDatabaseName" -ForegroundColor White
    az sql db create `
        --name $sqlDatabaseName `
        --server $sqlServerName `
        --resource-group $ResourceGroupName `
        --edition Basic
    
    # Configurar firewall para Azure services
    Write-Host "[FW] Configurando firewall..." -ForegroundColor White
    az sql server firewall-rule create `
        --resource-group $ResourceGroupName `
        --server $sqlServerName `
        --name "AllowAzureServices" `
        --start-ip-address 0.0.0.0 `
        --end-ip-address 0.0.0.0
    
    # Configurar connection string
    $connectionString = "Server=tcp:$sqlServerName.database.windows.net,1433;Database=$sqlDatabaseName;User ID=$adminUser;Password=$adminPassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
    
    Write-Host "[OK] Base de datos Azure configurada" -ForegroundColor Green
}

# ===== CONFIGURAR VARIABLES DE ENTORNO =====
Write-Host "`n[CONFIG] Configurando variables de entorno..." -ForegroundColor Cyan

$appSettings = @(
    "ASPNETCORE_ENVIRONMENT=$Environment"
    "HTTPS_PORT=443"
    "ASPNETCORE_FORWARDEDHEADERS_ENABLED=true"
    "WEBSITE_RUN_FROM_PACKAGE=1"
)

if ($SetupDatabase -and $connectionString) {
    $appSettings += "ConnectionStrings__DefaultConnection=$connectionString"
}

Write-Host "[SETTINGS] Aplicando configuracion a Web App..." -ForegroundColor White
az webapp config appsettings set `
    --name $WebAppName `
    --resource-group $ResourceGroupName `
    --settings $appSettings

Write-Host "[OK] Variables de entorno configuradas" -ForegroundColor Green

# ===== COMPILACIÓN Y PUBLICACIÓN =====
Write-Host "`n[BUILD] Compilando aplicacion para produccion..." -ForegroundColor Cyan

# Limpiar directorio de publicación
if (Test-Path $publishPath) {
    Remove-Item -Recurse -Force $publishPath
}

# Limpiar y restaurar
Write-Host "[CLEAN] Limpiando y restaurando..." -ForegroundColor White
dotnet clean $solutionPath --verbosity quiet
dotnet restore $solutionPath --verbosity quiet

# Compilar en modo Release
Write-Host "[COMPILE] Compilando en modo Release..." -ForegroundColor White
dotnet build $solutionPath --configuration Release --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ Error en compilación"
    exit 1
}

# Publicar aplicación
Write-Host "[PUBLISH] Publicando aplicacion..." -ForegroundColor White
dotnet publish $projectPath `
    --configuration Release `
    --output $publishPath `
    --no-build `
    --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ Error en publicación"
    exit 1
}

Write-Host "[OK] Compilacion y publicacion completadas" -ForegroundColor Green

# ===== CREAR PAQUETE DE DEPLOYMENT =====
Write-Host "`n[PACKAGE] Creando paquete de deployment..." -ForegroundColor Cyan

if (Test-Path $packagePath) {
    Remove-Item -Force $packagePath
}

Write-Host "[ZIP] Comprimiendo archivos..." -ForegroundColor White
Compress-Archive -Path "$publishPath\*" -DestinationPath $packagePath

$packageSize = [math]::Round((Get-Item $packagePath).Length / 1MB, 2)
Write-Host "[OK] Paquete creado: $packagePath ($packageSize MB)" -ForegroundColor Green

# ===== DEPLOYMENT A AZURE =====
Write-Host "`n[DEPLOY] Deploying a Azure App Service..." -ForegroundColor Cyan

Write-Host "[UPLOAD] Subiendo paquete a Azure..." -ForegroundColor White
az webapp deployment source config-zip `
    --name $WebAppName `
    --resource-group $ResourceGroupName `
    --src $packagePath

if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ Error en deployment"
    exit 1
}

# ===== APLICAR MIGRACIONES DE BASE DE DATOS =====
if ($SetupDatabase -and $connectionString) {
    Write-Host "`n[DB] Aplicando migraciones de base de datos..." -ForegroundColor Cyan
    
    # Aplicar migraciones usando connection string de Azure
    $env:ConnectionStrings__DefaultConnection = $connectionString
    dotnet ef database update --project src\SecureShop.Data --startup-project src\SecureShop.Web
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Migraciones aplicadas correctamente" -ForegroundColor Green
    } else {
        Write-Host "[WARNING] Error aplicando migraciones - verificar manualmente" -ForegroundColor Yellow
    }
}

# ===== VERIFICACIÓN POST-DEPLOYMENT =====
Write-Host "`n[CHECK] Verificando deployment..." -ForegroundColor Cyan

$appUrl = "https://$WebAppName.azurewebsites.net"
$healthUrl = "$appUrl/health"

Write-Host "[URL] URL de la aplicacion: $appUrl" -ForegroundColor Yellow
Write-Host "[HEALTH] Health check: $healthUrl" -ForegroundColor Yellow

# Esperar a que la aplicación inicie
Write-Host "[WAIT] Esperando que la aplicacion inicie..." -ForegroundColor White
Start-Sleep -Seconds 30

# Verificar health check
try {
    $healthResponse = Invoke-RestMethod -Uri $healthUrl -TimeoutSec 30
    if ($healthResponse.Status -eq "Healthy") {
        Write-Host "[OK] Health check: SALUDABLE" -ForegroundColor Green
    } else {
        Write-Host "[WARNING] Health check: $($healthResponse.Status)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "[WARNING] No se pudo verificar health check - revisar logs" -ForegroundColor Yellow
}

# ===== LIMPIEZA =====
Write-Host "`n[CLEANUP] Limpiando archivos temporales..." -ForegroundColor Cyan

if (Test-Path $publishPath) {
    Remove-Item -Recurse -Force $publishPath
}

if (Test-Path $packagePath) {
    Remove-Item -Force $packagePath
}

Write-Host "[OK] Limpieza completada" -ForegroundColor Green

# ===== RESUMEN FINAL =====
Write-Host "`n[SUCCESS] DEPLOYMENT COMPLETADO!" -ForegroundColor Green
Write-Host "========================" -ForegroundColor Green

Write-Host "`n[SUMMARY] Resumen del deployment:" -ForegroundColor Cyan
Write-Host "   [APP] Aplicacion: SecureShop" -ForegroundColor White
Write-Host "   [RG] Resource Group: $ResourceGroupName" -ForegroundColor White
Write-Host "   [WEB] Web App: $WebAppName" -ForegroundColor White
Write-Host "   [ENV] Environment: $Environment" -ForegroundColor White
Write-Host "   [URL] URL: $appUrl" -ForegroundColor White

Write-Host "`n[LINKS] Enlaces utiles:" -ForegroundColor Cyan
Write-Host "   [HOME] Aplicacion: $appUrl" -ForegroundColor Yellow
Write-Host "   [HEALTH] Health Check: $healthUrl" -ForegroundColor Yellow
Write-Host "   [DASH] Dashboard: $appUrl/dashboard" -ForegroundColor Yellow
Write-Host "   [PORTAL] Azure Portal: https://portal.azure.com" -ForegroundColor Yellow

Write-Host "`n[INFO] Proximos pasos:" -ForegroundColor Cyan
Write-Host "   1. Verificar que la aplicación funciona correctamente" -ForegroundColor White
Write-Host "   2. Configurar dominio personalizado (opcional)" -ForegroundColor White
Write-Host "   3. Configurar Application Insights para monitoreo" -ForegroundColor White
Write-Host "   4. Configurar Azure AD para autenticación" -ForegroundColor White
Write-Host "   5. Configurar Key Vault para secretos" -ForegroundColor White

Write-Host "`n[TROUBLESHOOT] Troubleshooting:" -ForegroundColor Cyan
Write-Host "   [LOGS] Ver logs: az webapp log tail --name $WebAppName --resource-group $ResourceGroupName" -ForegroundColor Gray
Write-Host "   [RESTART] Reiniciar: az webapp restart --name $WebAppName --resource-group $ResourceGroupName" -ForegroundColor Gray