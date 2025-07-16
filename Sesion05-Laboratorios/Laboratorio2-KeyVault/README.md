# 🔑 Laboratorio 2: Integración Completa con Azure Key Vault

## 📋 Información del Laboratorio
- **Duración**: 30 minutos
- **Objetivo**: Implementar gestión completa de secretos con Azure Key Vault usando Managed Identity
- **Prerrequisitos**: Laboratorio 1 completado exitosamente

## 🎯 Objetivos Específicos
- Crear y configurar Azure Key Vault con RBAC
- Almacenar y gestionar secretos de aplicación
- Implementar Configuration Provider para Key Vault
- Crear servicio completo para operaciones de Key Vault
- Integrar Data Protection con Key Vault para protección de claves

## 🏗️ Paso 1: Crear Azure Key Vault (8 minutos)

### 🔐 Crear Key Vault desde Azure Portal

1. **Navegar al Portal**:
   - Abrir Azure Portal: https://portal.azure.com
   - Buscar "Key vaults" → Click "+ Create"

2. **Configuración Básica**:
   ```
   Resource group: rg-desarrollo-seguro-[SuNombre]
   Key vault name: kv-devsgro-[sunombre]-[numero]
   Region: East US
   Pricing tier: Standard (para laboratorio)
   ```

3. **Configuración de Acceso**:
   - Access configuration: **Azure role-based access control (RBAC)**
   - ✅ Esta opción habilita RBAC moderno en lugar de políticas legacy

4. **Crear el Key Vault**:
   - Click "Review + create"
   - Click "Create"
   - Esperar a que se complete el deployment

### 🔑 Configurar RBAC Permissions

1. **Asignar Rol Principal (Su Usuario)**:
   - Ir a Key Vault → Access control (IAM)
   - Click "+ Add" → "Add role assignment"
   - **Role**: `Key Vault Secrets Officer`
   - **Assign access to**: User, group, or service principal
   - **Members**: Su cuenta de usuario
   - Click "Review + assign"

2. **Asignar Rol al Grupo de Desarrollo**:
   - Click "+ Add" → "Add role assignment"
   - **Role**: `Key Vault Secrets User`
   - **Members**: `gu_desarrollo_seguro_aplicacion`
   - Click "Review + assign"

### 📋 Verificar Permisos
Debe tener estos roles asignados:
- **Key Vault Secrets Officer**: Para crear/modificar/eliminar secrets
- **Key Vault Secrets User**: Para leer secrets (grupo desarrollo)

## 🔒 Paso 2: Crear Secrets en Key Vault (7 minutos)

### 📝 Crear Secrets desde Azure Portal

Ir a: Key Vault → Objects → Secrets → "+ Generate/Import"

#### 🔐 Secret 1: Database Connection
```
Name: DatabaseConnectionString
Value: Server=localhost;Database=DevSeguroApp;Integrated Security=true;TrustServerCertificate=true;
```

#### 🔐 Secret 2: External API Key  
```
Name: ExternalApiKey
Value: sk-test-123456789abcdef-external-api-key
```

#### 🔐 Secret 3: Encryption Key
```
Name: EncryptionKey
Value: MyVerySecretEncryptionKey2024!
```

#### 🔐 Secret 4: SMTP Configuration
```
Name: SmtpPassword
Value: smtp-password-super-secret-123
```

### 📋 Lista de Secrets Creados

| Secret Name | Descripción | Uso |
|-------------|-------------|-----|
| `DatabaseConnectionString` | Conexión a base de datos | Entity Framework |
| `ExternalApiKey` | Clave API externa | Servicios terceros |
| `EncryptionKey` | Clave de encriptación | Datos sensibles |
| `SmtpPassword` | Password SMTP | Envío de emails |

### ✅ Verificar Secrets
- Cada secret debe aparecer en la lista
- Estado: **Enabled**
- No deben tener fecha de expiración (para laboratorio)

## ⚙️ Paso 3: Configurar Configuration Provider para Key Vault (8 minutos)

### 📄 Actualizar appsettings.json

Añadir configuración de Key Vault:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "[SU-TENANT-ID-AQUÍ]",
    "ClientId": "[SU-CLIENT-ID-AQUÍ]",
    "ClientSecret": "[SU-CLIENT-SECRET-AQUÍ]",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  },
  "KeyVault": {
    "VaultUri": "https://kv-devsgro-[sunombre]-[numero].vault.azure.net/"
  },
  "DataProtection": {
    "ApplicationName": "DevSeguroApp-[SuNombre]",
    "StorageConnectionString": "[SU-STORAGE-CONNECTION-STRING]",
    "KeyLifetime": "90.00:00:00"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.AspNetCore.DataProtection": "Debug",
      "Azure.Security.KeyVault": "Information"
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://localhost:7001"
      }
    }
  }
}
```

### 🔧 Actualizar Program.cs para Key Vault

Configuración completa de Program.cs con Key Vault:

```csharp
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using Microsoft.AspNetCore.DataProtection;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Security.KeyVault.Keys;
using DevSeguroWebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// 🔑 CONFIGURACIÓN DE KEY VAULT
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
if (!string.IsNullOrEmpty(keyVaultUri))
{
    // Usar DefaultAzureCredential para desarrollo local
    var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
        // Para desarrollo local, usar Azure CLI credentials
        ExcludeEnvironmentCredential = false,
        ExcludeWorkloadIdentityCredential = false,
        ExcludeManagedIdentityCredential = false
    });

    // Añadir Key Vault como configuration provider
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        credential);
}

// Habilitar logging detallado en desarrollo
if (builder.Environment.IsDevelopment())
{
    IdentityModelEventSource.ShowPII = true;
}

// Configurar servicios básicos
builder.Services.AddControllersWithViews();

// Configurar Microsoft Identity Web
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// 🔐 CONFIGURACIÓN AVANZADA DE DATA PROTECTION CON KEY VAULT
if (!string.IsNullOrEmpty(keyVaultUri))
{
    var credential = new DefaultAzureCredential();
    
    builder.Services.AddDataProtection(options =>
    {
        options.ApplicationDiscriminator = builder.Configuration["DataProtection:ApplicationName"];
    })
    .SetDefaultKeyLifetime(TimeSpan.Parse(builder.Configuration["DataProtection:KeyLifetime"]))
    .PersistKeysToAzureBlobStorage(
        builder.Configuration["DataProtection:StorageConnectionString"],
        "dataprotection-keys",
        "keys.xml")
    .ProtectKeysWithAzureKeyVault(
        new Uri($"{keyVaultUri}keys/dataprotection-key"),
        credential)
    .SetApplicationName(builder.Configuration["DataProtection:ApplicationName"]);
}

// Registrar clientes de Key Vault
if (!string.IsNullOrEmpty(keyVaultUri))
{
    builder.Services.AddSingleton(provider =>
    {
        var credential = new DefaultAzureCredential();
        return new SecretClient(new Uri(keyVaultUri), credential);
    });

    builder.Services.AddSingleton(provider =>
    {
        var credential = new DefaultAzureCredential();
        return new KeyClient(new Uri(keyVaultUri), credential);
    });
}

// Registrar servicios
builder.Services.AddScoped<ISecureDataService, SecureDataService>();
builder.Services.AddScoped<IKeyVaultService, KeyVaultService>();

// Configurar autorización
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddRazorPages();

var app = builder.Build();

// Configurar pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
```

### 🔍 Puntos Clave de la Configuración

1. **DefaultAzureCredential**: Maneja múltiples métodos de autenticación
2. **AddAzureKeyVault**: Integra secrets como configuration
3. **ProtectKeysWithAzureKeyVault**: Protege claves de Data Protection
4. **SecretClient/KeyClient**: Clientes para operaciones directas

## 🛠️ Paso 4: Crear Servicio de Key Vault (7 minutos)

### 📄 Crear IKeyVaultService Interface

Crear archivo `Services/IKeyVaultService.cs`:

```csharp
namespace DevSeguroWebApp.Services
{
    public interface IKeyVaultService
    {
        Task<string> GetSecretAsync(string secretName);
        Task SetSecretAsync(string secretName, string secretValue);
        Task<string> EncryptDataAsync(string keyName, string plaintext);
        Task<string> DecryptDataAsync(string keyName, string ciphertext);
        Task<Dictionary<string, string>> GetAllSecretsAsync();
        Task<bool> SecretExistsAsync(string secretName);
    }
}
```

### 📄 Crear KeyVaultService Implementation

Crear archivo `Services/KeyVaultService.cs`:

```csharp
using Azure.Security.KeyVault.Secrets;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Identity;
using System.Text;

namespace DevSeguroWebApp.Services
{
    public class KeyVaultService : IKeyVaultService
    {
        private readonly SecretClient _secretClient;
        private readonly KeyClient _keyClient;
        private readonly ILogger<KeyVaultService> _logger;

        public KeyVaultService(
            SecretClient secretClient,
            KeyClient keyClient,
            ILogger<KeyVaultService> logger)
        {
            _secretClient = secretClient;
            _keyClient = keyClient;
            _logger = logger;
        }

        public async Task<string> GetSecretAsync(string secretName)
        {
            try
            {
                var secret = await _secretClient.GetSecretAsync(secretName);
                _logger.LogInformation("Successfully retrieved secret: {SecretName}", secretName);
                return secret.Value.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secret: {SecretName}", secretName);
                throw;
            }
        }

        public async Task SetSecretAsync(string secretName, string secretValue)
        {
            try
            {
                await _secretClient.SetSecretAsync(secretName, secretValue);
                _logger.LogInformation("Successfully set secret: {SecretName}", secretName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting secret: {SecretName}", secretName);
                throw;
            }
        }

        public async Task<string> EncryptDataAsync(string keyName, string plaintext)
        {
            try
            {
                var key = await _keyClient.GetKeyAsync(keyName);
                var cryptoClient = new CryptographyClient(key.Value.Id, new DefaultAzureCredential());
                
                var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
                var encryptResult = await cryptoClient.EncryptAsync(EncryptionAlgorithm.RsaOaep, plaintextBytes);
                
                var encryptedBase64 = Convert.ToBase64String(encryptResult.Ciphertext);
                _logger.LogInformation("Successfully encrypted data using key: {KeyName}", keyName);
                
                return encryptedBase64;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting data with key: {KeyName}", keyName);
                throw;
            }
        }

        public async Task<string> DecryptDataAsync(string keyName, string ciphertext)
        {
            try
            {
                var key = await _keyClient.GetKeyAsync(keyName);
                var cryptoClient = new CryptographyClient(key.Value.Id, new DefaultAzureCredential());
                
                var ciphertextBytes = Convert.FromBase64String(ciphertext);
                var decryptResult = await cryptoClient.DecryptAsync(EncryptionAlgorithm.RsaOaep, ciphertextBytes);
                
                var plaintext = Encoding.UTF8.GetString(decryptResult.Plaintext);
                _logger.LogInformation("Successfully decrypted data using key: {KeyName}", keyName);
                
                return plaintext;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting data with key: {KeyName}", keyName);
                throw;
            }
        }

        public async Task<Dictionary<string, string>> GetAllSecretsAsync()
        {
            try
            {
                var secrets = new Dictionary<string, string>();
                
                await foreach (var secretProperty in _secretClient.GetPropertiesOfSecretsAsync())
                {
                    if (secretProperty.Enabled == true)
                    {
                        try
                        {
                            var secret = await _secretClient.GetSecretAsync(secretProperty.Name);
                            secrets[secretProperty.Name] = secret.Value.Value;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not retrieve secret: {SecretName}", secretProperty.Name);
                            secrets[secretProperty.Name] = "[Error retrieving secret]";
                        }
                    }
                }
                
                _logger.LogInformation("Retrieved {Count} secrets from Key Vault", secrets.Count);
                return secrets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all secrets from Key Vault");
                throw;
            }
        }

        public async Task<bool> SecretExistsAsync(string secretName)
        {
            try
            {
                await _secretClient.GetSecretAsync(secretName);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
```

## 🧪 Configurar Azure CLI para Desarrollo Local

### 🔐 Autenticación con Azure CLI

Para que funcione la autenticación local con Key Vault:

1. **Instalar Azure CLI** (si no está instalado):
   ```bash
   # Con chocolatey (Windows)
   choco install azure-cli -y
   
   # O descargar desde: https://aka.ms/installazurecliwindows
   ```

2. **Autenticarse con Azure CLI**:
   ```bash
   az login
   ```

3. **Seleccionar suscripción correcta**:
   ```bash
   # Listar suscripciones disponibles
   az account list --output table
   
   # Seleccionar suscripción (si tiene múltiples)
   az account set --subscription [SUBSCRIPTION-ID]
   
   # Verificar login
   az account show
   ```

4. **Verificar acceso a Key Vault**:
   ```bash
   az keyvault secret list --vault-name kv-devsgro-[sunombre]-[numero]
   ```

### 🔍 Troubleshooting de Autenticación

| Error | Solución |
|-------|----------|
| "Please run 'az login'" | Ejecutar `az login` |
| "Access denied" | Verificar permisos RBAC en Key Vault |
| "Vault not found" | Verificar nombre del Key Vault |
| "Invalid subscription" | `az account set --subscription [id]` |

## 🧪 Paso 5: Testing de Key Vault Integration

### 📄 Crear Controller de Testing

Crear archivo `Controllers/KeyVaultTestController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using DevSeguroWebApp.Services;

namespace DevSeguroWebApp.Controllers
{
    public class KeyVaultTestController : Controller
    {
        private readonly IKeyVaultService _keyVaultService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<KeyVaultTestController> _logger;

        public KeyVaultTestController(
            IKeyVaultService keyVaultService,
            IConfiguration configuration,
            ILogger<KeyVaultTestController> logger)
        {
            _keyVaultService = keyVaultService;
            _configuration = configuration;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSecrets()
        {
            try
            {
                var secrets = await _keyVaultService.GetAllSecretsAsync();
                
                // Mask sensitive values for display
                var maskedSecrets = secrets.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Key.Contains("Password") || kvp.Key.Contains("Key") 
                        ? MaskSensitiveValue(kvp.Value) 
                        : kvp.Value
                );

                return Json(new { success = true, secrets = maskedSecrets });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Key Vault secrets");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateSecret([FromBody] CreateSecretRequest request)
        {
            try
            {
                await _keyVaultService.SetSecretAsync(request.SecretName, request.SecretValue);
                return Json(new { success = true, message = $"Secret '{request.SecretName}' created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating secret: {SecretName}", request.SecretName);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult TestConfiguration()
        {
            var config = new
            {
                KeyVaultConfigured = !string.IsNullOrEmpty(_configuration["KeyVault:VaultUri"]),
                KeyVaultUri = _configuration["KeyVault:VaultUri"],
                Environment = Environment.MachineName,
                Framework = Environment.Version.ToString(),
                
                // Test secrets from Key Vault
                DatabaseConnectionFromKV = _configuration["DatabaseConnectionString"],
                ExternalApiKeyFromKV = _configuration["ExternalApiKey"]
            };

            return Json(config);
        }

        private static string MaskSensitiveValue(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= 8)
                return "***";
            
            return value.Substring(0, 4) + new string('*', Math.Min(value.Length - 8, 20)) + value.Substring(value.Length - 4);
        }
    }

    public class CreateSecretRequest
    {
        public string SecretName { get; set; } = string.Empty;
        public string SecretValue { get; set; } = string.Empty;
    }
}
```

## ✅ Checklist de Completación

Marcar cuando esté completado:

- [ ] ✅ Azure Key Vault creado y configurado
- [ ] ✅ RBAC configurado correctamente
- [ ] ✅ 4 secrets iniciales creados
- [ ] ✅ appsettings.json actualizado con Key Vault URI
- [ ] ✅ Program.cs configurado con Key Vault integration
- [ ] ✅ IKeyVaultService interface creada
- [ ] ✅ KeyVaultService implementación creada
- [ ] ✅ Azure CLI autenticado
- [ ] ✅ Verificación de acceso a Key Vault exitosa
- [ ] ✅ Controller de testing creado
- [ ] ✅ Data Protection integrada con Key Vault
- [ ] ✅ Configuration Provider funcionando
- [ ] ✅ Testing básico exitoso

## 🚨 Troubleshooting Común

### Error: "Could not load Azure.Identity"
**Solución**:
```bash
dotnet clean
dotnet restore
dotnet build
```

### Error: "Access denied to Key Vault"
**Solución**:
1. Verificar que Azure CLI esté autenticado: `az login`
2. Confirmar permisos RBAC en Key Vault
3. Verificar que el grupo `gu_desarrollo_seguro_aplicacion` tiene acceso

### Error: "Vault not found"
**Solución**:
- Verificar URL en appsettings.json
- Confirmar que el Key Vault existe
- Verificar que la suscripción es correcta

### Error: "DataProtection key creation failed"
**Solución**:
1. Verificar permisos en Key Vault para crear keys
2. El key se crea automáticamente en primer uso
3. Revisar logs para detalles específicos

## 🎯 Resultado Esperado

Al completar este laboratorio, debe tener:

1. **Key Vault Funcionando**:
   - Secrets almacenados y accesibles
   - RBAC configurado correctamente
   - Configuration provider integrado

2. **Servicio de Key Vault**:
   - Operaciones CRUD de secrets
   - Logging detallado
   - Manejo de errores robusto

3. **Data Protection Integrado**:
   - Claves protegidas con Key Vault
   - Persistencia en Azure Storage
   - Configuración enterprise-ready

4. **Testing Funcional**:
   - Controller de testing operativo
   - Verificación de configuración
   - Acceso a secrets confirmado

## ➡️ Próximo Paso

Una vez completado exitosamente este laboratorio, proceder con:
**[Laboratorio 3: Implementación de Vistas Avanzadas y Testing](../Laboratorio3-Testing/)**

---
⚡ **Nota Importante**: La clave de Data Protection se creará automáticamente en Key Vault al primer uso de la aplicación. 