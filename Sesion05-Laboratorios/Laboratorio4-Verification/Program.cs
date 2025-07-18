using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.DataProtection;
using Azure.Identity;
using DevSeguroWebApp.Services;
using Azure.Storage.Blobs;
using Azure.Security.KeyVault.Secrets;
using Azure.Security.KeyVault.Keys;

var builder = WebApplication.CreateBuilder(args);

// Configurar servicios básicos
builder.Services.AddControllersWithViews();

// Configurar sesiones
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

// Configurar Data Protection básico
var applicationName = builder.Configuration["DataProtection:ApplicationName"] ?? "DevSeguroApp-Verification";

builder.Services.AddDataProtection(options =>
{
    options.ApplicationDiscriminator = applicationName;
})
.SetDefaultKeyLifetime(TimeSpan.Parse(builder.Configuration["DataProtection:KeyLifetime"] ?? "90.00:00:00"))
.SetApplicationName(applicationName);

// Configurar Key Vault (opcional)
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
if (!string.IsNullOrEmpty(keyVaultUri))
{
    var credential = new DefaultAzureCredential();
    
    builder.Services.AddSingleton(provider => new SecretClient(new Uri(keyVaultUri), credential));
    builder.Services.AddSingleton(provider => new KeyClient(new Uri(keyVaultUri), credential));
}

// Registrar servicios
builder.Services.AddScoped<ISecureDataService, SecureDataService>();
builder.Services.AddScoped<IKeyVaultService, KeyVaultService>();

// Configurar autorización
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = null;
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
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Verification}/{action=Index}/{id?}");

app.MapRazorPages();

Console.WriteLine($"🚀 Aplicación de Verificación iniciada en puerto 7001");
app.Run();
