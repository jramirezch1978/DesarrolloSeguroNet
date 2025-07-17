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

// Configurar servicios básicos
builder.Services.AddControllersWithViews();

// Configurar Microsoft Identity Web
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// 🔐 CONFIGURACIÓN AVANZADA DE DATA PROTECTION
builder.Services.AddDataProtection(options =>
{
    // Nombre único de aplicación para aislamiento
    options.ApplicationDiscriminator = builder.Configuration["DataProtection:ApplicationName"];
})
.SetDefaultKeyLifetime(TimeSpan.Parse(builder.Configuration["DataProtection:KeyLifetime"] ?? "90"))
.SetApplicationName(builder.Configuration["DataProtection:ApplicationName"]);

//Registrar servicio de protección de datos
builder.Services.AddScoped<ISecureDataService, SecureDataService>();

// Configurar autorización - Permitir acceso sin autenticación para testing
builder.Services.AddAuthorization(options =>
{
    // No requerir autenticación por defecto para facilitar testing
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

// ORDEN CRÍTICO en .NET 9
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run(); 