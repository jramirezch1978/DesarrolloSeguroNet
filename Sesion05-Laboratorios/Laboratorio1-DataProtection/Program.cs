using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using Microsoft.AspNetCore.DataProtection;
using Azure.Identity;
using DevSeguroWebApp.Services;
using Azure.Storage.Blobs;

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
    bool azureStorageConfigured = false;
    
    if (!string.IsNullOrEmpty(storageConnectionString))
    {
        try
        {
            Console.WriteLine($"🔍 Intentando conectar a Azure Storage...");
            
            BlobServiceClient blobServiceClient;
            
            // OPCIÓN 1: Connection String (actual)
            try 
            {
                blobServiceClient = new BlobServiceClient(storageConnectionString);
                Console.WriteLine($"   ✅ BlobServiceClient creado con Connection String");
            }
            catch (Exception csEx)
            {
                Console.WriteLine($"   ❌ Error con Connection String: {csEx.Message}");
                
                // OPCIÓN 2: Azure AD Authentication (fallback)
                Console.WriteLine($"   🔄 Intentando con Azure AD Authentication...");
                var storageAccountName = GetStorageAccountName(storageConnectionString);
                var blobUri = new Uri($"https://{storageAccountName}.blob.core.windows.net");
                
                // Usar DefaultAzureCredential (incluye Azure CLI, Visual Studio, etc.)
                blobServiceClient = new BlobServiceClient(blobUri, new Azure.Identity.DefaultAzureCredential());
                Console.WriteLine($"   ✅ BlobServiceClient creado con Azure AD");
            }
            
            var containerClient = blobServiceClient.GetBlobContainerClient("dataprotection-keys");
            Console.WriteLine($"   ✅ ContainerClient obtenido");
            
            // Crear container si no existe
            var containerResponse = containerClient.CreateIfNotExists();
            Console.WriteLine($"   ✅ Container verificado/creado");
            Console.WriteLine($"   - Container status: {(containerResponse?.HasValue == true ? "Created" : "Exists")}");
            
            // Validar conectividad básica
            try 
            {
                var blobClient = containerClient.GetBlobClient("keys.xml");
                var exists = blobClient.Exists();
                Console.WriteLine($"   ✅ Prueba de conectividad exitosa - Blob exists: {exists.Value}");
                
                // Listar blobs existentes
                var blobs = containerClient.GetBlobs();
                Console.WriteLine($"   ℹ️  Blobs existentes en container: {blobs.Count()}");
            }
            catch (Exception testEx)
            {
                Console.WriteLine($"   ❌ Error en prueba de conectividad: {testEx.Message}");
                Console.WriteLine($"   - Tipo: {testEx.GetType().Name}");
                throw;
            }
            
            // ✅ USAR EL MÉTODO OFICIAL DE ASP.NET CORE - ESTA ES LA LÍNEA CLAVE
            var dataProtectionBlobClient = containerClient.GetBlobClient("keys.xml");
            dataProtectionBuilder.PersistKeysToAzureBlobStorage(dataProtectionBlobClient);
            
            azureStorageConfigured = true;
            
            Console.WriteLine($"✅ Data Protection configurado con Azure Blob Storage (método oficial)");
            Console.WriteLine($"   - Storage Account: {GetStorageAccountName(storageConnectionString)}");
            Console.WriteLine($"   - Container: dataprotection-keys");
            Console.WriteLine($"   - Blob: keys.xml");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error configurando Azure Storage: {ex.Message}");
            Console.WriteLine($"   - Tipo de error: {ex.GetType().Name}");
            Console.WriteLine($"   - Stack trace: {ex.StackTrace?.Split('\n')[0]}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   - Inner Exception: {ex.InnerException.Message}");
                Console.WriteLine($"   - Inner Exception Type: {ex.InnerException.GetType().Name}");
            }
            Console.WriteLine($"⚠️  Usando fallback a sistema de archivos local");
            azureStorageConfigured = false;
        }
    }
    
    // Si Azure Storage no se configuró, usar sistema de archivos local
    if (!azureStorageConfigured)
    {
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