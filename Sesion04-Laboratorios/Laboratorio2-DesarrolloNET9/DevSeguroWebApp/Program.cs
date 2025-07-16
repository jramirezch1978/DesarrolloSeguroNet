using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;

var builder = WebApplication.CreateBuilder(args);

// Habilitar logging detallado de Identity Model (solo en desarrollo)
if (builder.Environment.IsDevelopment())
{
    IdentityModelEventSource.ShowPII = true;
}

// Configurar servicios
builder.Services.AddControllersWithViews();

// Configurar Microsoft Identity Web (nueva forma en .NET 9)
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// Configurar autorización
builder.Services.AddAuthorization(options =>
{
    // Política por defecto: usuario debe estar autenticado
    options.FallbackPolicy = options.DefaultPolicy;
});

// Configurar Razor Pages (necesario para Microsoft.Identity.Web)
builder.Services.AddRazorPages();

var app = builder.Build();

// Configurar pipeline de la aplicación
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // HSTS por defecto en .NET 9
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ⚠️ ORDEN CRÍTICO en .NET 9
app.UseAuthentication();
app.UseAuthorization();

// Configurar rutas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Mapear Razor Pages (requerido por Microsoft.Identity.Web)
app.MapRazorPages();

app.Run();
