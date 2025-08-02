# 🚀 GUÍA DE DEPLOYMENT - LABORATORIO 35 SECURESHOP

## 🎯 Opciones de Deployment

Esta guía cubre tres escenarios de deployment:
1. **🔧 Desarrollo Local** - Para testing y desarrollo
2. **☁️ Azure App Service** - Para producción en la nube
3. **🐳 Docker Container** - Para despliegue containerizado

---

## 🔧 1. DEPLOYMENT LOCAL (DESARROLLO)

### **Prerrequisitos:**
- .NET 9.0 SDK instalado
- SQL Server LocalDB o SQL Server Express
- Visual Studio 2022 o VS Code

### **Paso 1: Preparar la Base de Datos Local**

```powershell
# Navegar al directorio del proyecto
cd "Laboratorio-35-WebApp\src"

# Configurar base de datos con Entity Framework
dotnet ef database update --project SecureShop.Data --startup-project SecureShop.Web

# Si no tienes EF Tools instalado:
dotnet tool install --global dotnet-ef
```

### **Paso 2: Configurar appsettings.Development.json**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SecureShopDev;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "localhost",
    "TenantId": "development-tenant",
    "ClientId": "development-client",
    "CallbackPath": "/signin-oidc"
  }
}
```

### **Paso 3: Ejecutar la Aplicación**

```powershell
# Opción 1: Usando dotnet CLI
dotnet run --project SecureShop.Web

# Opción 2: Usando Visual Studio
# Abrir SecureShop.sln y presionar F5

# Opción 3: Con configuración específica
dotnet run --project SecureShop.Web --environment Development
```

### **Paso 4: Verificar el Deployment Local**

La aplicación estará disponible en:
- **HTTPS**: `https://localhost:7000`
- **HTTP**: `http://localhost:5000` (redirige a HTTPS)

**Endpoints de verificación:**
- `/health` - Health check
- `/` - Página principal
- `/dashboard` - Dashboard (requiere autenticación)

---

## ☁️ 2. DEPLOYMENT A AZURE APP SERVICE

### **Prerrequisitos Azure:**
- Azure Subscription activa
- Azure CLI instalado y configurado
- Recursos creados: Resource Group, App Service Plan, SQL Database

### **Paso 1: Crear Recursos de Azure**

```powershell
# Variables de configuración
$resourceGroup = "SecureShop-RG"
$location = "East US"
$appServicePlan = "SecureShop-Plan"
$webAppName = "secureshop-web-$(Get-Random -Maximum 9999)"
$sqlServerName = "secureshop-sql-$(Get-Random -Maximum 9999)"
$sqlDatabaseName = "SecureShopDB"

# Crear Resource Group
az group create --name $resourceGroup --location $location

# Crear App Service Plan
az appservice plan create `
    --name $appServicePlan `
    --resource-group $resourceGroup `
    --location $location `
    --sku S1 `
    --is-linux

# Crear Web App
az webapp create `
    --name $webAppName `
    --resource-group $resourceGroup `
    --plan $appServicePlan `
    --runtime "DOTNETCORE:8.0"

# Crear SQL Server
az sql server create `
    --name $sqlServerName `
    --resource-group $resourceGroup `
    --location $location `
    --admin-user secureshopAdmin `
    --admin-password "SecureShop2024!"

# Crear SQL Database
az sql db create `
    --name $sqlDatabaseName `
    --server $sqlServerName `
    --resource-group $resourceGroup `
    --edition Basic
```

### **Paso 2: Configurar Variables de Entorno**

```powershell
# Configurar connection string
$connectionString = "Server=tcp:$sqlServerName.database.windows.net,1433;Database=$sqlDatabaseName;User ID=secureshopAdmin;Password=SecureShop2024!;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az webapp config appsettings set `
    --name $webAppName `
    --resource-group $resourceGroup `
    --settings "ConnectionStrings__DefaultConnection=$connectionString" `
              "ASPNETCORE_ENVIRONMENT=Production" `
              "HTTPS_PORT=443" `
              "ASPNETCORE_FORWARDEDHEADERS_ENABLED=true"
```

### **Paso 3: Deployment con GitHub Actions**

Crear `.github/workflows/deploy-azure.yml`:

```yaml
name: Deploy SecureShop to Azure

on:
  push:
    branches: [ main ]
    paths: [ 'Laboratorio-35-WebApp/**' ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore Laboratorio-35-WebApp/src/SecureShop.sln
    
    - name: Build
      run: dotnet build Laboratorio-35-WebApp/src/SecureShop.sln --configuration Release --no-restore
    
    - name: Publish
      run: dotnet publish Laboratorio-35-WebApp/src/SecureShop.Web/SecureShop.Web.csproj --configuration Release --output ./publish
    
    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: ${{ secrets.AZURE_WEBAPP_NAME }}
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

### **Paso 4: Deployment Manual con Azure CLI**

```powershell
# Compilar y publicar la aplicación
dotnet publish SecureShop.Web/SecureShop.Web.csproj `
    --configuration Release `
    --output ./publish

# Crear paquete ZIP
Compress-Archive -Path ./publish/* -DestinationPath ./secureshop.zip

# Deployar a Azure
az webapp deployment source config-zip `
    --name $webAppName `
    --resource-group $resourceGroup `
    --src ./secureshop.zip
```

### **Paso 5: Configurar Base de Datos en Azure**

```powershell
# Aplicar migraciones a Azure SQL
dotnet ef database update --connection $connectionString
```

---

## 🐳 3. DEPLOYMENT CON DOCKER

### **Paso 1: Crear Dockerfile**

```dockerfile
# Laboratorio-35-WebApp/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de proyecto
COPY ["src/SecureShop.Web/SecureShop.Web.csproj", "SecureShop.Web/"]
COPY ["src/SecureShop.Data/SecureShop.Data.csproj", "SecureShop.Data/"]
COPY ["src/SecureShop.Core/SecureShop.Core.csproj", "SecureShop.Core/"]
COPY ["src/SecureShop.Security/SecureShop.Security.csproj", "SecureShop.Security/"]

# Restaurar dependencias
RUN dotnet restore "SecureShop.Web/SecureShop.Web.csproj"

# Copiar código fuente
COPY src/ .

# Compilar aplicación
WORKDIR "/src/SecureShop.Web"
RUN dotnet build "SecureShop.Web.csproj" -c Release -o /app/build

# Publicar aplicación
FROM build AS publish
RUN dotnet publish "SecureShop.Web.csproj" -c Release -o /app/publish

# Imagen final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Configurar usuario no-root para seguridad
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "SecureShop.Web.dll"]
```

### **Paso 2: Crear docker-compose.yml**

```yaml
# Laboratorio-35-WebApp/docker-compose.yml
version: '3.8'

services:
  secureshop-web:
    build: .
    ports:
      - "8080:80"
      - "8443:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - ASPNETCORE_Kestrel__Certificates__Default__Password=SecureShop2024
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=SecureShopDB;User=sa;Password=SecureShop2024!;TrustServerCertificate=true
    volumes:
      - ~/.aspnet/https:/https:ro
    depends_on:
      - sqlserver
    networks:
      - secureshop-network

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=SecureShop2024!
    ports:
      - "1433:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql
    networks:
      - secureshop-network

volumes:
  sqlserver-data:

networks:
  secureshop-network:
    driver: bridge
```

### **Paso 3: Ejecutar con Docker**

```powershell
# Construir y ejecutar
docker-compose up --build

# Solo ejecutar (si ya está construido)
docker-compose up

# Ejecutar en background
docker-compose up -d
```

---

## 🔧 4. SCRIPTS DE DEPLOYMENT AUTOMATIZADO

### **Script de Deployment Local**

```powershell
# deploy-local.ps1
param(
    [string]$Environment = "Development"
)

Write-Host "🚀 Deploying SecureShop locally..." -ForegroundColor Cyan

# Limpiar y restaurar
dotnet clean
dotnet restore

# Configurar base de datos
Write-Host "📊 Configurando base de datos..." -ForegroundColor Yellow
dotnet ef database update --project SecureShop.Data --startup-project SecureShop.Web

# Compilar
Write-Host "🔨 Compilando aplicación..." -ForegroundColor Yellow
dotnet build --configuration Release

# Ejecutar
Write-Host "🚀 Iniciando aplicación..." -ForegroundColor Green
dotnet run --project SecureShop.Web --environment $Environment
```

### **Script de Deployment a Azure**

```powershell
# deploy-azure.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$true)]
    [string]$WebAppName,
    
    [string]$Environment = "Production"
)

Write-Host "☁️ Deploying SecureShop to Azure..." -ForegroundColor Cyan

# Compilar y publicar
Write-Host "🔨 Compilando para producción..." -ForegroundColor Yellow
dotnet publish SecureShop.Web/SecureShop.Web.csproj `
    --configuration Release `
    --output ./publish

# Crear paquete
Write-Host "📦 Creando paquete de deployment..." -ForegroundColor Yellow
Compress-Archive -Path ./publish/* -DestinationPath ./secureshop.zip -Force

# Deploy a Azure
Write-Host "🚀 Deploying a Azure App Service..." -ForegroundColor Green
az webapp deployment source config-zip `
    --name $WebAppName `
    --resource-group $ResourceGroupName `
    --src ./secureshop.zip

Write-Host "✅ Deployment completado!" -ForegroundColor Green
Write-Host "🌐 URL: https://$WebAppName.azurewebsites.net" -ForegroundColor Yellow
```

---

## 🔍 5. VERIFICACIÓN POST-DEPLOYMENT

### **Health Checks**

```powershell
# Verificar salud de la aplicación
$healthUrl = "https://your-app.azurewebsites.net/health"
$response = Invoke-RestMethod -Uri $healthUrl

if ($response.Status -eq "Healthy") {
    Write-Host "✅ Aplicación saludable" -ForegroundColor Green
} else {
    Write-Host "❌ Problemas detectados" -ForegroundColor Red
}
```

### **Pruebas de Endpoints**

```powershell
# Test endpoints críticos
$baseUrl = "https://your-app.azurewebsites.net"

# Test página principal
$homeResponse = Invoke-WebRequest -Uri "$baseUrl/" -UseBasicParsing
Write-Host "Home Status: $($homeResponse.StatusCode)"

# Test health check
$healthResponse = Invoke-WebRequest -Uri "$baseUrl/health" -UseBasicParsing  
Write-Host "Health Status: $($healthResponse.StatusCode)"
```

### **Logs de Troubleshooting**

```powershell
# Ver logs de Azure App Service
az webapp log tail --name $webAppName --resource-group $resourceGroup

# Descargar logs
az webapp log download --name $webAppName --resource-group $resourceGroup
```

---

## 🛡️ 6. CONFIGURACIÓN DE SEGURIDAD POST-DEPLOYMENT

### **SSL/TLS Certificates**

```powershell
# Configurar certificado SSL personalizado en Azure
az webapp config ssl upload `
    --name $webAppName `
    --resource-group $resourceGroup `
    --certificate-file path/to/certificate.pfx `
    --certificate-password your-password
```

### **Configurar Custom Domain**

```powershell
# Agregar dominio personalizado
az webapp config hostname add `
    --webapp-name $webAppName `
    --resource-group $resourceGroup `
    --hostname yourdomain.com
```

### **Application Insights**

```powershell
# Habilitar Application Insights
az monitor app-insights component create `
    --app secureshop-insights `
    --location $location `
    --resource-group $resourceGroup

# Configurar en Web App
az webapp config appsettings set `
    --name $webAppName `
    --resource-group $resourceGroup `
    --settings "APPLICATIONINSIGHTS_CONNECTION_STRING=your-connection-string"
```

---

## 🔧 7. TROUBLESHOOTING COMÚN

### **Error: Database Connection**
```
Solución: Verificar connection string y firewall de SQL Server
az sql server firewall-rule create --resource-group $rg --server $server --name AllowAzure --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
```

### **Error: Missing Dependencies**
```
Solución: Verificar que todas las referencias de proyecto están incluidas
dotnet restore --force
```

### **Error: Authentication Issues**
```
Solución: Verificar configuración de Azure AD en appsettings
```

---

## 📋 CHECKLIST DE DEPLOYMENT

### **Pre-Deployment:**
- [ ] Código compilado sin errores
- [ ] Tests unitarios pasando
- [ ] Configuración de base de datos lista
- [ ] Variables de entorno configuradas
- [ ] Certificados SSL preparados

### **Post-Deployment:**
- [ ] Health check respondiendo
- [ ] Base de datos conectada
- [ ] Logs sin errores críticos
- [ ] Performance aceptable
- [ ] Security headers configurados

---

**🎯 Tu aplicación SecureShop está lista para producción!** 🚀

*Para deployment específico, usa los scripts proporcionados según tu escenario.*