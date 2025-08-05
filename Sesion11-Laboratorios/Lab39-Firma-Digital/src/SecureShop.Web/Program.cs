using Microsoft.Identity.Web;
using SecureShop.Security;

var builder = WebApplication.CreateBuilder(args);

// ===== CONFIGURACIÓN DE AUTENTICACIÓN AZURE AD =====
builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// ===== CONFIGURACIÓN DE SERVICIOS DE FIRMA DIGITAL =====
builder.Services.AddScoped<IDigitalSignatureService, KeyVaultDigitalSignatureService>();

// ===== CONFIGURACIÓN DE AUTORIZACIÓN =====
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
        
    options.AddPolicy("ManagerOrAdmin", policy =>
        policy.RequireRole("Manager", "Admin"));
        
    options.AddPolicy("CustomerAccess", policy =>
        policy.RequireRole("Customer", "Manager", "Admin"));
});

// ===== CONFIGURACIÓN DE SERVICIOS ADICIONALES =====
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// ===== CONFIGURACIÓN DEL PIPELINE =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Middleware de autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

// Configuración de rutas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();