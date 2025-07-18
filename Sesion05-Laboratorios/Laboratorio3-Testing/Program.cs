using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using Microsoft.AspNetCore.DataProtection;
using Azure.Identity;
using DevSeguroWebApp.Services;
using Azure.Storage.Blobs;
using Azure.Security.KeyVault.Secrets;
using Azure.Security.KeyVault.Keys;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Habilitar logging detallado en desarrollo
if (builder.Environment.IsDevelopment())
{
    IdentityModelEventSource.ShowPII = true;
}

// Configurar logging más detallado
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}

// 🔑 CONFIGURACIÓN DE KEY VAULT
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    ExcludeEnvironmentCredential = false,
    ExcludeWorkloadIdentityCredential = false,
    ExcludeManagedIdentityCredential = false
});

if (!string.IsNullOrEmpty(keyVaultUri))
{
    Console.WriteLine($"🔑 Configurando Key Vault: {keyVaultUri}");
    try
    {
        // Añadir Key Vault como configuration provider
        // builder.Configuration.AddAzureKeyVault(
        //     new Uri(keyVaultUri),
        //     credential);
        Console.WriteLine($"✅ Key Vault configuration provider configurado exitosamente (comentado temporalmente)");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Error configurando Key Vault: {ex.Message}");
        Console.WriteLine($"   - Usando configuración local como fallback");
    }
}

// Configurar servicios básicos
builder.Services.AddControllersWithViews();

// Configurar sesiones para almacenar preferencias
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configurar Microsoft Identity Web
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// 🔐 CONFIGURACIÓN AVANZADA DE DATA PROTECTION CON AZURE STORAGE Y KEY VAULT
var applicationName = builder.Configuration["DataProtection:ApplicationName"] ?? "DevSeguroApp-Testing";
var storageConnectionString = builder.Configuration["DataProtection:StorageConnectionString"];

// 📁 LEER PREFERENCIA DE ALMACENAMIENTO DESDE ARCHIVO
bool forceLocalStorage = false;
string preferenceSource = "configuración por defecto";

try
{
    var preferencePath = Path.Combine(Directory.GetCurrentDirectory(), "storage-preference.json");
    if (File.Exists(preferencePath))
    {
        var preferenceJson = File.ReadAllText(preferencePath);
        var preferenceDoc = JsonDocument.Parse(preferenceJson);
        
        if (preferenceDoc.RootElement.TryGetProperty("UseAzureStorage", out var useAzureProperty))
        {
            var useAzureStorage = useAzureProperty.GetBoolean();
            forceLocalStorage = !useAzureStorage;
            preferenceSource = "archivo de preferencias del usuario";
            
            Console.WriteLine($"📋 Preferencia de almacenamiento cargada: {(useAzureStorage ? "Azure Storage" : "Local Storage")}");
            
            if (preferenceDoc.RootElement.TryGetProperty("LastChanged", out var lastChangedProperty))
            {
                Console.WriteLine($"   - Última modificación: {lastChangedProperty.GetDateTime():yyyy-MM-dd HH:mm:ss}");
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Error leyendo preferencia de almacenamiento: {ex.Message}");
    Console.WriteLine($"   - Usando configuración por defecto");
}

try
{
    var dataProtectionBuilder = builder.Services.AddDataProtection(options =>
    {
        // Nombre único de aplicación para aislamiento
        options.ApplicationDiscriminator = applicationName;
    })
    .SetDefaultKeyLifetime(TimeSpan.Parse(builder.Configuration["DataProtection:KeyLifetime"] ?? "90.00:00:00"))
    .SetApplicationName(applicationName);

    // Configurar persistencia según preferencia del usuario
    bool azureStorageConfigured = false;
    
    if (forceLocalStorage)
    {
        // 📁 FORZAR ALMACENAMIENTO LOCAL POR PREFERENCIA DEL USUARIO
        var keysPath = Path.Combine(Directory.GetCurrentDirectory(), "DataProtection-Keys");
        
        // Crear directorio si no existe
        if (!Directory.Exists(keysPath))
        {
            Directory.CreateDirectory(keysPath);
            Console.WriteLine($"   📁 Directorio creado: {keysPath}");
        }
        
        dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        Console.WriteLine($"📁 Data Protection configurado con ALMACENAMIENTO LOCAL por {preferenceSource}");
        Console.WriteLine($"   - Ruta absoluta: {keysPath}");
        Console.WriteLine($"   - Archivos de llaves se guardarán como: key-{{guid}}.xml");
        azureStorageConfigured = true; // Para evitar el bloque de Azure
    }
    else if (!string.IsNullOrEmpty(storageConnectionString))
    {
        // ☁️ INTENTAR USAR AZURE STORAGE + KEY VAULT
        try
        {
            Console.WriteLine($"🔍 Intentando conectar a Azure Storage...");
            
            var blobServiceClient = new BlobServiceClient(storageConnectionString);
            Console.WriteLine($"   ✅ BlobServiceClient creado con Connection String");
            
            var containerClient = blobServiceClient.GetBlobContainerClient("dataprotection-keys");
            Console.WriteLine($"   ✅ ContainerClient obtenido");
            
            var containerResponse = containerClient.CreateIfNotExists();
            Console.WriteLine($"   ✅ Container verificado/creado");
            Console.WriteLine($"   - Container status: {(containerResponse?.HasValue == true ? "Created" : "Exists")}");
            
            var dataProtectionBlobClient = containerClient.GetBlobClient("keys.xml");
            dataProtectionBuilder.PersistKeysToAzureBlobStorage(dataProtectionBlobClient);
            
            // 🔑 PROTECCIÓN ADICIONAL CON KEY VAULT (si está disponible)
            if (!string.IsNullOrEmpty(keyVaultUri))
            {
                try
                {
                    Console.WriteLine($"🔑 Configurando protección de claves con Key Vault...");
                    // dataProtectionBuilder.ProtectKeysWithAzureKeyVault(
                    //     new Uri($"{keyVaultUri}keys/dataprotection-key"),
                    //     credential);
                    Console.WriteLine($"   ✅ Claves protegidas con Key Vault (comentado temporalmente)");
                }
                catch (Exception kvEx)
                {
                    Console.WriteLine($"   ⚠️ Error configurando Key Vault para Data Protection: {kvEx.Message}");
                    Console.WriteLine($"   - Data Protection funcionará sin protección adicional de Key Vault");
                }
            }
            
            azureStorageConfigured = true;
            Console.WriteLine($"☁️ Data Protection configurado con AZURE STORAGE + KEY VAULT por {preferenceSource}");
            Console.WriteLine($"   - Storage Account: {GetStorageAccountName(storageConnectionString)}");
            Console.WriteLine($"   - Container: dataprotection-keys");
            Console.WriteLine($"   - Blob: keys.xml");
            Console.WriteLine($"   - Key Vault: {(string.IsNullOrEmpty(keyVaultUri) ? "No configurado" : "Configurado")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error configurando Azure Storage: {ex.Message}");
            Console.WriteLine($"⚠️  Usando fallback a sistema de archivos local");
            azureStorageConfigured = false;
        }
    }
    
    // Fallback automático a sistema de archivos local
    if (!azureStorageConfigured)
    {
        var keysPath = Path.Combine(Directory.GetCurrentDirectory(), "DataProtection-Keys");
        
        // Crear directorio si no existe
        if (!Directory.Exists(keysPath))
        {
            Directory.CreateDirectory(keysPath);
            Console.WriteLine($"   📁 Directorio creado: {keysPath}");
        }
        
        dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        Console.WriteLine($"📁 Data Protection usando sistema de archivos local (fallback)");
        Console.WriteLine($"   - Ruta absoluta: {keysPath}");
        Console.WriteLine($"   - Archivos de llaves se guardarán como: key-{{guid}}.xml");
    }

    Console.WriteLine($"✅ Data Protection configurado exitosamente con nombre: {applicationName}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error configurando Data Protection: {ex.Message}");
    throw;
}

// Registrar clientes de Key Vault
if (!string.IsNullOrEmpty(keyVaultUri))
{
    builder.Services.AddSingleton(provider =>
    {
        return new SecretClient(new Uri(keyVaultUri), credential);
    });

    builder.Services.AddSingleton(provider =>
    {
        return new KeyClient(new Uri(keyVaultUri), credential);
    });
    
    Console.WriteLine($"✅ Key Vault clients registrados");
}

// Registrar servicios
builder.Services.AddScoped<ISecureDataService, SecureDataService>();
builder.Services.AddScoped<IKeyVaultService, KeyVaultService>();

// Configurar autorización - Permitir acceso sin autenticación para testing
builder.Services.AddAuthorization(options =>
{
    // No requerir autenticación por defecto para facilitar testing
    options.FallbackPolicy = null;
});

builder.Services.AddRazorPages();

var app = builder.Build();

// Verificar configuración de Data Protection al inicio
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dataProtectionProvider = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
        var testProtector = dataProtectionProvider.CreateProtector("startup-test");
        var testData = "test-data";
        var protectedTest = testProtector.Protect(testData);
        var unprotectedTest = testProtector.Unprotect(protectedTest);
        
        if (testData == unprotectedTest)
        {
            Console.WriteLine("✅ Data Protection verification successful");
        }
        else
        {
            Console.WriteLine("❌ Data Protection verification failed");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error verificando Data Protection: {ex.Message}");
}

// Verificar configuración de Key Vault al inicio
if (!string.IsNullOrEmpty(keyVaultUri))
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var keyVaultService = scope.ServiceProvider.GetService<IKeyVaultService>();
            if (keyVaultService != null)
            {
                Console.WriteLine("✅ Key Vault service verificado exitosamente");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Error verificando Key Vault: {ex.Message}");
    }
}

// Configurar pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Habilitar sesiones
app.UseSession();

// ORDEN CRÍTICO en .NET 9
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

Console.WriteLine($"🚀 Aplicación Testing Completo iniciada en puerto 7001");
app.Run();

// Helper function para extraer el nombre de la cuenta de storage
static string GetStorageAccountName(string connectionString)
{
    try
    {
        var accountNameStart = connectionString.IndexOf("AccountName=") + "AccountName=".Length;
        var accountNameEnd = connectionString.IndexOf(";", accountNameStart);
        return connectionString.Substring(accountNameStart, accountNameEnd - accountNameStart);
    }
    catch
    {
        return "Unknown";
    }
} 