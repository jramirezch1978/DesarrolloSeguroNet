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
    // .PersistKeysToAzureBlobStorage(
    //     builder.Configuration["DataProtection:StorageConnectionString"],
    //     "dataprotection-keys",
    //     "keys.xml")
    // .ProtectKeysWithAzureKeyVault(
    //     new Uri($"{keyVaultUri}keys/dataprotection-key"),
    //     credential)
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