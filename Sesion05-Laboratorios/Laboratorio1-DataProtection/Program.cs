using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using Microsoft.AspNetCore.DataProtection;
using Azure.Identity;
using DevSeguroWebApp.Services;

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

// Configurar Microsoft Identity Web
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// 🔐 CONFIGURACIÓN AVANZADA DE DATA PROTECTION
var applicationName = builder.Configuration["DataProtection:ApplicationName"] ?? "DevSeguroApp-Default";

try
{
    builder.Services.AddDataProtection(options =>
    {
        // Nombre único de aplicación para aislamiento
        options.ApplicationDiscriminator = applicationName;
    })
    .SetDefaultKeyLifetime(TimeSpan.Parse(builder.Configuration["DataProtection:KeyLifetime"] ?? "90.00:00:00"))
    .SetApplicationName(applicationName)
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "DataProtection-Keys")));

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

// ORDEN CRÍTICO en .NET 9
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

Console.WriteLine($"🚀 Aplicación iniciada en puerto 7001");
app.Run(); 