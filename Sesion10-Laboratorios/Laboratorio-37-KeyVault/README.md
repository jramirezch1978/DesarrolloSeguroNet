# 🔐 LABORATORIO 37: CONFIGURACIÓN DE KEY VAULT PARA LA APLICACIÓN

## 🎯 Objetivo
Implementar Azure Key Vault como sistema centralizado de gestión de secretos, eliminando credenciales hardcodeadas y estableciendo un patrón de seguridad que escala desde desarrollo hasta producción empresarial.

## ⏱️ Duración: 25 minutos

## 🎭 Conceptos Fundamentales de Gestión de Secretos

### 🏛️ De Configuración Insegura a Vault Empresarial
> *"Key Vault transforma nuestra aplicación de una que almacena secretos en archivos de configuración (vulnerables a exposición en repositorios), a una que obtiene credenciales de forma segura usando identidades gestionadas. Es la diferencia entre tener las llaves de la empresa pegadas en la puerta versus un sistema de acceso biométrico."*

### 🔐 Managed Identity: El Santo Grial de la Autenticación

**Azure Managed Identity** es revolucionario porque:
- **Elimina credenciales**: No hay secretos que rotar, almacenar o exponer
- **Rotación automática**: Azure maneja la rotación de credenciales internamente
- **Auditoría completa**: Cada acceso queda registrado en Azure AD logs
- **Alcance limitado**: Permisos específicos solo para recursos necesarios

### 🎯 Arquitectura de Seguridad en Capas

#### **Hardware Security Modules (HSM)**
Key Vault Premium usa **HSMs certificados FIPS 140-2 Level 2**, el mismo nivel usado por:
- **Bancos centrales** para proteger reservas monetarias digitales
- **Autoridades de certificación** para firmar certificados raíz
- **Sistemas gubernamentales** para proteger información clasificada

#### **Separación de Responsabilidades**
```
Desarrollador Local → Managed Identity → Key Vault → Secretos
                   ↓
         No ve credenciales reales nunca
```

El desarrollador nunca maneja credenciales de producción. Sistema inmune a:
- **Credential leaks** en repositorios
- **Shoulder surfing** durante desarrollo
- **Compromiso de estaciones** de trabajo

## 🛡️ Casos de Seguridad Implementados

### **Caso 1: Protección contra Exposición en Repositorios**
**Problema**: En 2019, desarrolladores de Capital One subieron credenciales AWS a GitHub público
**Nuestra Solución**: Todas las credenciales están en Key Vault, imposible de exponer en código

### **Caso 2: Separación por Ambientes**
**Implementación**: 
- **Development Key Vault**: Secretos de prueba, acceso amplio para developers
- **Production Key Vault**: Secretos reales, acceso ultra-restringido
- **Staging Key Vault**: Datos similares a producción pero no críticos

### **Caso 3: Rotación Automática**
**Ventaja**: Si se sospecha compromiso, rotamos secretos centralmente sin tocar código

## 🎯 Pasos de Implementación

### Paso 1: Crear Key Vault con Azure CLI (8 minutos)

#### **Configuración de Variables Empresariales**
```powershell
# Variables de configuración
$resourceGroup = "SecureShop-RG-$(Get-Date -Format 'yyyyMMdd')"
$keyVaultName = "SecureShop-KV-$(Get-Random -Maximum 9999)"
$location = "East US"
$subscriptionId = (az account show --query id --output tsv)

Write-Host "🔧 Configuración:" -ForegroundColor Cyan
Write-Host "Resource Group: $resourceGroup" -ForegroundColor Yellow
Write-Host "Key Vault: $keyVaultName" -ForegroundColor Yellow
Write-Host "Location: $location" -ForegroundColor Yellow
```

#### **Crear Infraestructura Base**
```powershell
# Crear resource group
az group create --name $resourceGroup --location $location

# Crear Key Vault con configuración de seguridad máxima
az keyvault create `
    --name $keyVaultName `
    --resource-group $resourceGroup `
    --location $location `
    --sku Premium `
    --enable-soft-delete true `
    --soft-delete-retention-days 90 `
    --enable-purge-protection true `
    --enable-rbac-authorization true `
    --public-network-access Disabled

Write-Host "✅ Key Vault creado con protección máxima" -ForegroundColor Green
```

**¿Por qué estos parámetros específicos?**

**--sku Premium**: HSM respaldado, cumple regulaciones bancarias
**--enable-soft-delete**: Protege contra eliminación accidental
**--enable-purge-protection**: Previene eliminación maliciosa permanente
**--enable-rbac-authorization**: Control granular usando Azure AD
**--public-network-access Disabled**: Solo acceso desde redes autorizadas

#### **Configurar Managed Identity**
```powershell
# Crear Managed Identity para la aplicación
$identityName = "SecureShop-Identity"
$identity = az identity create `
    --name $identityName `
    --resource-group $resourceGroup `
    --query "{clientId:clientId,principalId:principalId}" `
    --output json | ConvertFrom-Json

Write-Host "🆔 Managed Identity creada:" -ForegroundColor Green
Write-Host "Client ID: $($identity.clientId)" -ForegroundColor Yellow
Write-Host "Principal ID: $($identity.principalId)" -ForegroundColor Yellow

# Asignar permisos de Key Vault Secrets User
az role assignment create `
    --assignee $identity.principalId `
    --role "Key Vault Secrets User" `
    --scope "/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.KeyVault/vaults/$keyVaultName"

Write-Host "✅ Permisos asignados: Key Vault Secrets User" -ForegroundColor Green
```

### Paso 2: Poblar Key Vault con Secretos (5 minutos)

#### **Secretos de Conexión de Base de Datos**
```powershell
# Cadena de conexión SQL Server
$sqlConnection = "Server=tcp:secureshop-sql.database.windows.net,1433;Database=SecureShop;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az keyvault secret set `
    --vault-name $keyVaultName `
    --name "ConnectionStrings--DefaultConnection" `
    --value $sqlConnection

# Configuración de Azure AD
az keyvault secret set `
    --vault-name $keyVaultName `
    --name "AzureAd--TenantId" `
    --value "your-tenant-id-here"

az keyvault secret set `
    --vault-name $keyVaultName `
    --name "AzureAd--ClientId" `
    --value "your-client-id-here"

# Claves de cifrado para datos sensibles
$encryptionKey = [System.Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
az keyvault secret set `
    --vault-name $keyVaultName `
    --name "Encryption--DataProtectionKey" `
    --value $encryptionKey

Write-Host "🔐 Secretos almacenados en Key Vault" -ForegroundColor Green
```

#### **Secretos de Servicios Externos**
```powershell
# API Keys para servicios de terceros
az keyvault secret set `
    --vault-name $keyVaultName `
    --name "ExternalServices--PaymentGatewayKey" `
    --value "sk_test_secure_payment_key_here"

az keyvault secret set `
    --vault-name $keyVaultName `
    --name "ExternalServices--EmailServiceKey" `
    --value "SG.secure_sendgrid_key_here"

# JWT Signing Key
$jwtKey = [System.Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
az keyvault secret set `
    --vault-name $keyVaultName `
    --name "Authentication--JwtSigningKey" `
    --value $jwtKey

Write-Host "🔑 Secretos de servicios configurados" -ForegroundColor Green
```

### Paso 3: Integrar Key Vault en .NET (12 minutos)

#### **Configuración de Program.cs con Key Vault**
```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration.AzureKeyVault;

var builder = WebApplication.CreateBuilder(args);

// ===== CONFIGURACIÓN DE KEY VAULT =====
if (!builder.Environment.IsDevelopment())
{
    // En producción: usar Managed Identity
    var keyVaultEndpoint = builder.Configuration["KeyVault:VaultUri"];
    
    if (!string.IsNullOrEmpty(keyVaultEndpoint))
    {
        // Configurar Azure Key Vault como provider de configuración
        builder.Configuration.AddAzureKeyVault(
            vaultUri: new Uri(keyVaultEndpoint),
            credential: new DefaultAzureCredential(),
            configurePrefixKeyNormalization: true);
        
        builder.Services.AddSingleton<SecretClient>(provider =>
        {
            return new SecretClient(
                vaultUri: new Uri(keyVaultEndpoint),
                credential: new DefaultAzureCredential());
        });
        
        builder.Logging.AddConsole();
        builder.Services.AddSingleton<ISecretService, KeyVaultSecretService>();
        
        Console.WriteLine("✅ Key Vault configurado con Managed Identity");
    }
}
else
{
    // En desarrollo: usar secretos locales o emulador
    builder.Services.AddSingleton<ISecretService, LocalSecretService>();
    Console.WriteLine("🔧 Usando configuración local para desarrollo");
}

// ===== CONFIGURACIÓN DE BASE DE DATOS CON KEY VAULT =====
builder.Services.AddDbContext<SecureDbContext>(options =>
{
    string connectionString;
    
    if (builder.Environment.IsDevelopment())
    {
        connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=(localdb)\\mssqllocaldb;Database=SecureShopDev;Trusted_Connection=true;";
    }
    else
    {
        // En producción: obtener de Key Vault
        connectionString = builder.Configuration["ConnectionStrings--DefaultConnection"]
            ?? throw new InvalidOperationException("Connection string not found in Key Vault");
    }
    
    options.UseSqlServer(connectionString);
});
```

#### **Servicio de Gestión de Secretos**
```csharp
/// <summary>
/// Interfaz para gestión abstracta de secretos
/// Permite alternar entre Key Vault y configuración local
/// </summary>
public interface ISecretService
{
    Task<string?> GetSecretAsync(string secretName);
    Task<Dictionary<string, string>> GetSecretsAsync(string[] secretNames);
    Task SetSecretAsync(string secretName, string secretValue);
}

/// <summary>
/// Implementación con Azure Key Vault
/// </summary>
public class KeyVaultSecretService : ISecretService
{
    private readonly SecretClient _secretClient;
    private readonly ILogger<KeyVaultSecretService> _logger;
    private readonly IMemoryCache _cache;

    public KeyVaultSecretService(
        SecretClient secretClient, 
        ILogger<KeyVaultSecretService> logger,
        IMemoryCache cache)
    {
        _secretClient = secretClient;
        _logger = logger;
        _cache = cache;
    }

    public async Task<string?> GetSecretAsync(string secretName)
    {
        try
        {
            // Verificar cache primero (reduce latencia)
            if (_cache.TryGetValue($"secret:{secretName}", out string? cachedValue))
            {
                return cachedValue;
            }

            // Obtener de Key Vault
            var response = await _secretClient.GetSecretAsync(secretName);
            var secretValue = response.Value.Value;

            // Cachear por 5 minutos para performance
            _cache.Set($"secret:{secretName}", secretValue, TimeSpan.FromMinutes(5));

            _logger.LogInformation("🔐 Secreto obtenido de Key Vault: {SecretName}", secretName);
            return secretValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error obteniendo secreto {SecretName}: {Error}", 
                secretName, ex.Message);
            return null;
        }
    }

    public async Task<Dictionary<string, string>> GetSecretsAsync(string[] secretNames)
    {
        var secrets = new Dictionary<string, string>();
        var tasks = secretNames.Select(async name =>
        {
            var value = await GetSecretAsync(name);
            if (value != null)
            {
                secrets[name] = value;
            }
        });

        await Task.WhenAll(tasks);
        return secrets;
    }

    public async Task SetSecretAsync(string secretName, string secretValue)
    {
        try
        {
            await _secretClient.SetSecretAsync(secretName, secretValue);
            
            // Invalidar cache
            _cache.Remove($"secret:{secretName}");
            
            _logger.LogInformation("✅ Secreto actualizado en Key Vault: {SecretName}", secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error actualizando secreto {SecretName}: {Error}", 
                secretName, ex.Message);
            throw;
        }
    }
}

/// <summary>
/// Implementación para desarrollo local
/// </summary>
public class LocalSecretService : ISecretService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LocalSecretService> _logger;

    public LocalSecretService(IConfiguration configuration, ILogger<LocalSecretService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<string?> GetSecretAsync(string secretName)
    {
        var value = _configuration[secretName.Replace("--", ":")];
        _logger.LogDebug("🔧 Secreto obtenido de configuración local: {SecretName}", secretName);
        return Task.FromResult(value);
    }

    public async Task<Dictionary<string, string>> GetSecretsAsync(string[] secretNames)
    {
        var secrets = new Dictionary<string, string>();
        foreach (var name in secretNames)
        {
            var value = await GetSecretAsync(name);
            if (value != null)
            {
                secrets[name] = value;
            }
        }
        return secrets;
    }

    public Task SetSecretAsync(string secretName, string secretValue)
    {
        _logger.LogWarning("⚠️ SetSecret no implementado en servicio local");
        return Task.CompletedTask;
    }
}
```

## 📋 Configuración de Deployment

### **appsettings.Production.json**
```json
{
  "KeyVault": {
    "VaultUri": "https://secureshop-kv-1234.vault.azure.net/"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Azure.Security.KeyVault": "Information"
    }
  }
}
```

### **Dockerfile con Managed Identity**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Instalar Azure CLI para Managed Identity
RUN apt-get update && apt-get install -y curl gnupg
RUN curl -sL https://aka.ms/InstallAzureCLIDeb | bash

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SecureShop.Web/SecureShop.Web.csproj", "SecureShop.Web/"]
RUN dotnet restore "SecureShop.Web/SecureShop.Web.csproj"

COPY . .
WORKDIR "/src/SecureShop.Web"
RUN dotnet build "SecureShop.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SecureShop.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Configurar Managed Identity endpoint
ENV AZURE_CLIENT_ID=your-managed-identity-client-id

ENTRYPOINT ["dotnet", "SecureShop.Web.dll"]
```

## 📋 Entregables del Laboratorio

Al completar este laboratorio tendrás:

- [ ] Azure Key Vault configurado con HSM Premium
- [ ] Managed Identity configurada con permisos mínimos
- [ ] Secretos organizados por categorías (DB, Auth, External)
- [ ] Integración .NET con fallback a configuración local
- [ ] Servicio abstracto de gestión de secretos
- [ ] Cache de secretos para optimización
- [ ] Configuración lista para múltiples ambientes
- [ ] Pipeline de deployment con Managed Identity

## 🔍 Casos de Estudio de Seguridad

### **Marriott (2018)**
- **Problema**: Credenciales de base de datos expuestas permitieron acceso a 500M registros
- **Nuestra Protección**: Credenciales en Key Vault con rotación automática
- **Resultado**: Imposible acceso directo a credenciales desde aplicación comprometida

### **SolarWinds (2020)**
- **Problema**: Secretos hardcodeados en código permitieron propagación lateral
- **Nuestra Protección**: Zero-secret application design con Managed Identity
- **Resultado**: Aplicación comprometida no expone credenciales a otros sistemas

## 💡 Valor Profesional Generado

**Portfolio Evidence**: Sistema de gestión de secretos de nivel empresarial  
**Skills Advancement**: Azure Key Vault, Managed Identity, HSM, secretos rotation  
**Security Mindset**: Zero-trust architecture con eliminación de credenciales  
**Enterprise Integration**: Patrones usados por organizaciones Fortune 500  

## 🔗 Integración Completa

La implementación de Key Vault completa nuestro stack de seguridad:
- **Lab 34**: Arquitectura segura por diseño ✅
- **Lab 35**: Base de aplicación con cifrado ✅  
- **Lab 36**: Autenticación empresarial con Azure AD ✅
- **Lab 37**: Gestión centralizada de secretos ✅

---

> **💡 Mindset Clave**: En un mundo de amenazas persistentes avanzadas, la gestión de secretos no es opcional - es la diferencia entre ser una empresa que protege datos versus una que aparece en titulares de violaciones de seguridad. Key Vault + Managed Identity representa el estado del arte en eliminación de superficies de ataque.