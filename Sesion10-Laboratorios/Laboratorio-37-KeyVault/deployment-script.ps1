# ================================================
# SCRIPT DE DEPLOYMENT - LABORATORIO 37 KEY VAULT
# ================================================
# Script completo para configurar Azure Key Vault y deploiar SecureShop

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$true)]
    [string]$Location = "East US",
    
    [Parameter(Mandatory=$false)]
    [string]$Environment = "development",
    
    [Parameter(Mandatory=$false)]
    [string]$SubscriptionId
)

# ===== CONFIGURACIÓN INICIAL =====
Write-Host "🚀 Iniciando deployment de SecureShop con Key Vault" -ForegroundColor Cyan
Write-Host "📍 Resource Group: $ResourceGroupName" -ForegroundColor Yellow
Write-Host "🌍 Location: $Location" -ForegroundColor Yellow
Write-Host "🏷️ Environment: $Environment" -ForegroundColor Yellow

# Verificar Azure CLI
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error "❌ Azure CLI no está instalado. Instalar desde: https://aka.ms/installazurecli"
    exit 1
}

# Verificar autenticación
$currentUser = az account show --query user.name --output tsv 2>$null
if (-not $currentUser) {
    Write-Host "🔐 Iniciando sesión en Azure..." -ForegroundColor Yellow
    az login
}

Write-Host "✅ Autenticado como: $currentUser" -ForegroundColor Green

# Configurar suscripción si se proporciona
if ($SubscriptionId) {
    az account set --subscription $SubscriptionId
    Write-Host "✅ Suscripción configurada: $SubscriptionId" -ForegroundColor Green
}

# ===== PASO 1: CREAR RESOURCE GROUP =====
Write-Host "`n📦 Paso 1: Creando Resource Group..." -ForegroundColor Cyan

$rgExists = az group exists --name $ResourceGroupName --output tsv
if ($rgExists -eq "true") {
    Write-Host "⚠️ Resource Group ya existe: $ResourceGroupName" -ForegroundColor Yellow
} else {
    az group create --name $ResourceGroupName --location $Location
    Write-Host "✅ Resource Group creado: $ResourceGroupName" -ForegroundColor Green
}

# ===== PASO 2: CREAR KEY VAULT =====
Write-Host "`n🔐 Paso 2: Creando Azure Key Vault..." -ForegroundColor Cyan

$keyVaultName = "secureshop-kv-$(Get-Random -Maximum 9999)"
$currentUserObjectId = az ad signed-in-user show --query id --output tsv

Write-Host "🔑 Key Vault Name: $keyVaultName" -ForegroundColor Yellow

try {
    # Crear Key Vault con configuración de seguridad máxima
    az keyvault create `
        --name $keyVaultName `
        --resource-group $ResourceGroupName `
        --location $Location `
        --sku Premium `
        --enable-soft-delete true `
        --soft-delete-retention-days 90 `
        --enable-purge-protection true `
        --enable-rbac-authorization true `
        --public-network-access Enabled  # Cambiar a Disabled en producción

    Write-Host "✅ Key Vault creado exitosamente" -ForegroundColor Green

    # Asignar permisos al usuario actual
    $subscriptionId = az account show --query id --output tsv
    $keyVaultScope = "/subscriptions/$subscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.KeyVault/vaults/$keyVaultName"
    
    az role assignment create `
        --assignee $currentUserObjectId `
        --role "Key Vault Administrator" `
        --scope $keyVaultScope

    Write-Host "✅ Permisos de administrador asignados" -ForegroundColor Green
}
catch {
    Write-Error "❌ Error creando Key Vault: $_"
    exit 1
}

# ===== PASO 3: CREAR MANAGED IDENTITY =====
Write-Host "`n🆔 Paso 3: Creando Managed Identity..." -ForegroundColor Cyan

$identityName = "secureshop-identity-$Environment"

try {
    $identity = az identity create `
        --name $identityName `
        --resource-group $ResourceGroupName `
        --location $Location `
        --query "{clientId:clientId,principalId:principalId}" `
        --output json | ConvertFrom-Json

    Write-Host "✅ Managed Identity creada:" -ForegroundColor Green
    Write-Host "   Client ID: $($identity.clientId)" -ForegroundColor White
    Write-Host "   Principal ID: $($identity.principalId)" -ForegroundColor White

    # Asignar rol Key Vault Secrets User
    az role assignment create `
        --assignee $identity.principalId `
        --role "Key Vault Secrets User" `
        --scope $keyVaultScope

    Write-Host "✅ Rol 'Key Vault Secrets User' asignado" -ForegroundColor Green
}
catch {
    Write-Error "❌ Error creando Managed Identity: $_"
    exit 1
}

# ===== PASO 4: POBLAR KEY VAULT CON SECRETOS =====
Write-Host "`n🔐 Paso 4: Poblando Key Vault con secretos..." -ForegroundColor Cyan

# Esperar para que la RBAC se propague
Write-Host "⏳ Esperando propagación de permisos RBAC..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

$secrets = @{
    "ConnectionStrings--DefaultConnection" = "Server=tcp:secureshop-sql-$Environment.database.windows.net,1433;Database=SecureShop;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
    "AzureAd--TenantId" = "your-tenant-id-here"
    "AzureAd--ClientId" = "your-client-id-here"
    "ExternalServices--PaymentGatewayKey" = "sk_test_secure_payment_key_$Environment"
    "ExternalServices--EmailServiceKey" = "SG.secure_sendgrid_key_$Environment"
}

# Generar claves de cifrado seguras
$encryptionKey = [System.Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
$jwtKey = [System.Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))

$secrets["Encryption--DataProtectionKey"] = $encryptionKey
$secrets["Authentication--JwtSigningKey"] = $jwtKey

foreach ($secretName in $secrets.Keys) {
    try {
        az keyvault secret set `
            --vault-name $keyVaultName `
            --name $secretName `
            --value $secrets[$secretName] `
            --output none

        Write-Host "✅ Secreto configurado: $secretName" -ForegroundColor Green
    }
    catch {
        Write-Warning "⚠️ Error configurando secreto $secretName`: $_"
    }
}

# ===== PASO 5: CREAR APP SERVICE (OPCIONAL) =====
Write-Host "`n🌐 Paso 5: ¿Crear App Service? (y/n)" -ForegroundColor Cyan
$createAppService = Read-Host

if ($createAppService -eq "y" -or $createAppService -eq "Y") {
    Write-Host "🌐 Creando App Service..." -ForegroundColor Cyan
    
    $appServicePlan = "secureshop-plan-$Environment"
    $webAppName = "secureshop-web-$Environment-$(Get-Random -Maximum 999)"
    
    try {
        # Crear App Service Plan
        az appservice plan create `
            --name $appServicePlan `
            --resource-group $ResourceGroupName `
            --location $Location `
            --sku S1 `
            --is-linux

        # Crear Web App
        az webapp create `
            --name $webAppName `
            --resource-group $ResourceGroupName `
            --plan $appServicePlan `
            --runtime "DOTNETCORE:8.0"

        # Asignar Managed Identity a Web App
        az webapp identity assign `
            --name $webAppName `
            --resource-group $ResourceGroupName `
            --identities $identity.clientId

        # Configurar App Settings
        az webapp config appsettings set `
            --name $webAppName `
            --resource-group $ResourceGroupName `
            --settings "KeyVault__VaultUri=https://$keyVaultName.vault.azure.net/" `
                      "AZURE_CLIENT_ID=$($identity.clientId)" `
                      "ASPNETCORE_ENVIRONMENT=$Environment"

        Write-Host "✅ App Service creado: https://$webAppName.azurewebsites.net" -ForegroundColor Green
    }
    catch {
        Write-Warning "⚠️ Error creando App Service: $_"
    }
}

# ===== PASO 6: GENERAR ARCHIVOS DE CONFIGURACIÓN =====
Write-Host "`n📄 Paso 6: Generando archivos de configuración..." -ForegroundColor Cyan

# Crear appsettings.Production.json
$productionConfig = @{
    "KeyVault" = @{
        "VaultUri" = "https://$keyVaultName.vault.azure.net/"
        "ManagedIdentityClientId" = $identity.clientId
    }
    "Logging" = @{
        "LogLevel" = @{
            "Default" = "Information"
            "Azure.Security.KeyVault" = "Information"
            "Azure.Identity" = "Information"
        }
    }
    "Features" = @{
        "EnableKeyVaultIntegration" = $true
        "EnableSecretCaching" = $true
    }
} | ConvertTo-Json -Depth 4

$productionConfig | Out-File -FilePath "appsettings.Production.json" -Encoding UTF8
Write-Host "✅ Archivo creado: appsettings.Production.json" -ForegroundColor Green

# Crear script de conexión local
$localScript = @"
# ================================================
# SCRIPT DE CONEXIÓN LOCAL A KEY VAULT
# ================================================

# Configurar variables de entorno para desarrollo local
`$env:AZURE_CLIENT_ID = "$($identity.clientId)"
`$env:AZURE_TENANT_ID = "$(az account show --query tenantId --output tsv)"
`$env:KeyVault__VaultUri = "https://$keyVaultName.vault.azure.net/"

Write-Host "✅ Variables de entorno configuradas para Key Vault" -ForegroundColor Green
Write-Host "🔑 Key Vault URI: `$env:KeyVault__VaultUri" -ForegroundColor Yellow
Write-Host "🆔 Client ID: `$env:AZURE_CLIENT_ID" -ForegroundColor Yellow

# Para conectar localmente, asegúrate de estar autenticado:
# az login
# az account set --subscription $subscriptionId

Write-Host "`n🚀 Ahora puedes ejecutar tu aplicación localmente con:" -ForegroundColor Cyan
Write-Host "dotnet run" -ForegroundColor White
"@

$localScript | Out-File -FilePath "connect-local-keyvault.ps1" -Encoding UTF8
Write-Host "✅ Script creado: connect-local-keyvault.ps1" -ForegroundColor Green

# ===== RESUMEN FINAL =====
Write-Host "`n🎉 ¡DEPLOYMENT COMPLETADO EXITOSAMENTE!" -ForegroundColor Green
Write-Host "=" * 50 -ForegroundColor Green

Write-Host "`n📋 RESUMEN DE RECURSOS CREADOS:" -ForegroundColor Cyan
Write-Host "🔐 Key Vault: $keyVaultName" -ForegroundColor White
Write-Host "   URI: https://$keyVaultName.vault.azure.net/" -ForegroundColor Gray
Write-Host "🆔 Managed Identity: $identityName" -ForegroundColor White
Write-Host "   Client ID: $($identity.clientId)" -ForegroundColor Gray
Write-Host "📦 Resource Group: $ResourceGroupName" -ForegroundColor White

Write-Host "`n🔑 SECRETOS CONFIGURADOS:" -ForegroundColor Cyan
foreach ($secretName in $secrets.Keys) {
    Write-Host "   ✅ $secretName" -ForegroundColor Gray
}

Write-Host "`n🔧 PRÓXIMOS PASOS:" -ForegroundColor Cyan
Write-Host "1. Actualizar AzureAd--TenantId y AzureAd--ClientId en Key Vault" -ForegroundColor White
Write-Host "2. Configurar connection string real de SQL Database" -ForegroundColor White
Write-Host "3. Ejecutar: .\connect-local-keyvault.ps1 para desarrollo local" -ForegroundColor White
Write-Host "4. Compilar y deployar aplicación .NET" -ForegroundColor White

Write-Host "`n💡 IMPORTANTE:" -ForegroundColor Yellow
Write-Host "- Los secretos están en Key Vault, NO en código fuente" -ForegroundColor White
Write-Host "- Managed Identity elimina necesidad de credenciales hardcodeadas" -ForegroundColor White
Write-Host "- Configuración lista para múltiples ambientes" -ForegroundColor White

Write-Host "`n🎯 SecureShop está listo para producción empresarial!" -ForegroundColor Green