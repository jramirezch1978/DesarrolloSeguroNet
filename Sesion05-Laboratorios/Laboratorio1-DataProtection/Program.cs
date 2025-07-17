using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using Microsoft.AspNetCore.DataProtection;
using Azure.Identity;
using DevSeguroWebApp.Services;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.DataProtection.Repositories;

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

// 🔐 CONFIGURACIÓN AVANZADA DE DATA PROTECTION CON AZURE STORAGE
var applicationName = builder.Configuration["DataProtection:ApplicationName"] ?? "DevSeguroApp-Default";
var storageConnectionString = builder.Configuration["DataProtection:StorageConnectionString"];

try
{
    var dataProtectionBuilder = builder.Services.AddDataProtection(options =>
    {
        // Nombre único de aplicación para aislamiento
        options.ApplicationDiscriminator = applicationName;
    })
    .SetDefaultKeyLifetime(TimeSpan.Parse(builder.Configuration["DataProtection:KeyLifetime"] ?? "90.00:00:00"))
    .SetApplicationName(applicationName);

    // Configurar persistencia según disponibilidad de Azure Storage
    if (!string.IsNullOrEmpty(storageConnectionString))
    {
        try
        {
            // CONFIGURACIÓN AZURE BLOB STORAGE (implementación personalizada)
            var blobServiceClient = new BlobServiceClient(storageConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient("dataprotection-keys");
            
            // Crear container si no existe (esto se ejecuta al inicio)
            containerClient.CreateIfNotExists();
            
            var blobClient = containerClient.GetBlobClient("keys.xml");
            
            // Registrar nuestro repositorio personalizado
            builder.Services.AddSingleton<IXmlRepository>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<AzureBlobXmlRepository>>();
                return new AzureBlobXmlRepository(blobClient, logger);
            });
                
            Console.WriteLine($"✅ Data Protection configurado con Azure Blob Storage");
            Console.WriteLine($"   - Storage Account: {GetStorageAccountName(storageConnectionString)}");
            Console.WriteLine($"   - Container: dataprotection-keys");
            Console.WriteLine($"   - Blob: keys.xml");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error configurando Azure Storage: {ex.Message}");
            Console.WriteLine($"⚠️  Usando fallback a sistema de archivos local");
            
            // FALLBACK: Sistema de archivos local
            var keysPath = Path.Combine(Directory.GetCurrentDirectory(), "DataProtection-Keys");
            dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }
    }
    else
    {
        // FALLBACK: Sistema de archivos local
        var keysPath = Path.Combine(Directory.GetCurrentDirectory(), "DataProtection-Keys");
        dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        Console.WriteLine($"⚠️  Data Protection usando sistema de archivos local: {keysPath}");
        Console.WriteLine($"   - Para producción, configure DataProtection:StorageConnectionString");
    }

    Console.WriteLine($"✅ Data Protection configurado exitosamente con nombre: {applicationName}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error configurando Data Protection: {ex.Message}");
    throw;
}

// Registrar servicio de protección de datos
builder.Services.AddScoped<ISecureDataService, SecureDataService>();

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

Console.WriteLine($"🚀 Aplicación iniciada en puerto 7001");
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