#Requires -Modules Az

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory = $true)]
    [string]$Location,
    
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'test', 'prod')]
    [string]$Environment,
    
    [Parameter(Mandatory = $true)]
    [string]$ProjectName,
    
    [Parameter(Mandatory = $false)]
    [string]$TenantId,
    
    [Parameter(Mandatory = $false)]
    [string]$ApiClientId,
    
    [Parameter(Mandatory = $false)]
    [string]$WebAppClientId
)

# Configuración de colores para output
$ErrorActionPreference = "Stop"

Write-Host "🚀 Iniciando despliegue de la aplicación segura .NET 9" -ForegroundColor Green
Write-Host "📍 Grupo de recursos: $ResourceGroupName" -ForegroundColor Yellow
Write-Host "🌍 Ubicación: $Location" -ForegroundColor Yellow
Write-Host "🏗️  Entorno: $Environment" -ForegroundColor Yellow

# Verificar si estamos conectados a Azure
try {
    $context = Get-AzContext
    if (-not $context) {
        throw "No hay contexto de Azure"
    }
    Write-Host "✅ Conectado a Azure con la cuenta: $($context.Account.Id)" -ForegroundColor Green
}
catch {
    Write-Host "❌ No estás conectado a Azure. Ejecuta 'Connect-AzAccount' primero." -ForegroundColor Red
    exit 1
}

# Obtener información del tenant si no se proporciona
if (-not $TenantId) {
    $TenantId = (Get-AzContext).Tenant.Id
    Write-Host "🔑 Usando Tenant ID del contexto actual: $TenantId" -ForegroundColor Yellow
}

# Crear grupo de recursos si no existe
Write-Host "📦 Verificando grupo de recursos..." -ForegroundColor Blue
$rg = Get-AzResourceGroup -Name $ResourceGroupName -ErrorAction SilentlyContinue
if (-not $rg) {
    Write-Host "📦 Creando grupo de recursos: $ResourceGroupName" -ForegroundColor Yellow
    New-AzResourceGroup -Name $ResourceGroupName -Location $Location
    Write-Host "✅ Grupo de recursos creado exitosamente" -ForegroundColor Green
} else {
    Write-Host "✅ Grupo de recursos ya existe" -ForegroundColor Green
}

# Configurar registro de aplicaciones en Azure AD si no se proporcionan
if (-not $ApiClientId -or -not $WebAppClientId) {
    Write-Host "🔑 Configurando aplicaciones de Azure AD..." -ForegroundColor Blue
    
    # Instalar módulo AzureAD si no está disponible
    if (-not (Get-Module -ListAvailable -Name AzureAD)) {
        Write-Host "📦 Instalando módulo AzureAD..." -ForegroundColor Yellow
        Install-Module -Name AzureAD -Force -AllowClobber
    }
    
    try {
        Connect-AzureAD -TenantId $TenantId
        
        if (-not $ApiClientId) {
            # Crear aplicación para la API
            Write-Host "🔑 Creando aplicación de Azure AD para la API..." -ForegroundColor Yellow
            $apiApp = New-AzureADApplication -DisplayName "$ProjectName-$Environment-api" `
                -IdentifierUris "api://$ProjectName-$Environment-api" `
                -ReplyUrls @("https://localhost:7000/signin-oidc")
            
            $ApiClientId = $apiApp.AppId
            Write-Host "✅ API App ID: $ApiClientId" -ForegroundColor Green
        }
        
        if (-not $WebAppClientId) {
            # Crear aplicación para la Web App
            Write-Host "🔑 Creando aplicación de Azure AD para la Web App..." -ForegroundColor Yellow
            $webApp = New-AzureADApplication -DisplayName "$ProjectName-$Environment-webapp" `
                -ReplyUrls @("https://localhost:7001/signin-oidc")
            
            $WebAppClientId = $webApp.AppId
            Write-Host "✅ Web App ID: $WebAppClientId" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "⚠️  No se pudieron crear las aplicaciones de Azure AD automáticamente." -ForegroundColor Yellow
        Write-Host "⚠️  Por favor, créalas manualmente y ejecuta el script nuevamente con los parámetros -ApiClientId y -WebAppClientId" -ForegroundColor Yellow
        Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Parámetros para el template de Bicep
$templateParameters = @{
    projectName = $ProjectName
    location = $Location
    environment = $Environment
    tenantId = $TenantId
    apiClientId = $ApiClientId
    webAppClientId = $WebAppClientId
}

# Desplegar infraestructura
Write-Host "🏗️  Desplegando infraestructura con Bicep..." -ForegroundColor Blue
try {
    $deployment = New-AzResourceGroupDeployment `
        -ResourceGroupName $ResourceGroupName `
        -TemplateFile "main.bicep" `
        -TemplateParameterObject $templateParameters `
        -Verbose

    if ($deployment.ProvisioningState -eq "Succeeded") {
        Write-Host "✅ Infraestructura desplegada exitosamente" -ForegroundColor Green
        
        # Mostrar outputs importantes
        Write-Host "📋 Información del despliegue:" -ForegroundColor Blue
        Write-Host "🔐 Key Vault URL: $($deployment.Outputs.keyVaultUrl.Value)" -ForegroundColor Yellow
        Write-Host "🌐 API URL: $($deployment.Outputs.apiUrl.Value)" -ForegroundColor Yellow
        Write-Host "🌐 Web App URL: $($deployment.Outputs.webAppUrl.Value)" -ForegroundColor Yellow
        Write-Host "🆔 Managed Identity Client ID: $($deployment.Outputs.managedIdentityClientId.Value)" -ForegroundColor Yellow
        
        # Guardar configuración en archivo
        $configFile = "deployment-config-$Environment.json"
        $config = @{
            ResourceGroupName = $ResourceGroupName
            Environment = $Environment
            TenantId = $TenantId
            ApiClientId = $ApiClientId
            WebAppClientId = $WebAppClientId
            KeyVaultUrl = $deployment.Outputs.keyVaultUrl.Value
            ApiUrl = $deployment.Outputs.apiUrl.Value
            WebAppUrl = $deployment.Outputs.webAppUrl.Value
            ManagedIdentityClientId = $deployment.Outputs.managedIdentityClientId.Value
            StorageAccountName = $deployment.Outputs.storageAccountName.Value
        }
        
        $config | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
        Write-Host "💾 Configuración guardada en: $configFile" -ForegroundColor Green
        
        Write-Host "`n🎉 ¡Despliegue completado exitosamente!" -ForegroundColor Green
        Write-Host "📝 Próximos pasos:" -ForegroundColor Blue
        Write-Host "   1. Actualizar los archivos appsettings.json con la configuración generada" -ForegroundColor White
        Write-Host "   2. Configurar secretos en Key Vault" -ForegroundColor White
        Write-Host "   3. Desplegar las aplicaciones a App Service" -ForegroundColor White
        
    } else {
        Write-Host "❌ Error en el despliegue: $($deployment.ProvisioningState)" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "❌ Error durante el despliegue: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n🚀 Proceso de despliegue completado" -ForegroundColor Green 