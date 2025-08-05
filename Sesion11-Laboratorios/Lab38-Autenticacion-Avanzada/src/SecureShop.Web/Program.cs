using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using SecureShop.Security;

var builder = WebApplication.CreateBuilder(args);

// ===== CONFIGURACIÓN DE AUTENTICACIÓN AZURE AD =====
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// ===== CONFIGURACIÓN DE AUTORIZACIÓN AVANZADA =====

// Registrar servicios de autorización personalizados
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IClaimsTransformation, SecureClaimsTransformation>();
builder.Services.AddScoped<IAuthorizationHandler, OwnershipAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// Configurar políticas de autorización granulares
builder.Services.AddAuthorization(options =>
{
    // Políticas básicas por rol
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
        
    options.AddPolicy("ManagerOrAdmin", policy =>
        policy.RequireRole("Manager", "Admin"));
        
    options.AddPolicy("CustomerAccess", policy =>
        policy.RequireRole("Customer", "Manager", "Admin"));

    // Políticas basadas en permisos específicos
    options.AddPolicy("CanManageProducts", policy =>
        policy.Requirements.Add(new PermissionRequirement("products.manage")));
        
    options.AddPolicy("CanViewReports", policy =>
        policy.Requirements.Add(new PermissionRequirement("reports.view")));
        
    options.AddPolicy("CanManageUsers", policy =>
        policy.Requirements.Add(new PermissionRequirement("users.manage")));

    // Políticas combinadas (rol + departamento)
    options.AddPolicy("CanManageInventory", policy =>
        policy.RequireAssertion(context =>
            (context.User.IsInRole("Manager") || context.User.IsInRole("Admin")) &&
            (context.User.HasClaim("department", "Inventory") || context.User.IsInRole("Admin"))));

    // Políticas basadas en recursos
    options.AddPolicy("CanEditOwnProfile", policy =>
        policy.Requirements.Add(new OwnershipRequirement()));

    // Políticas con lógica compleja
    options.AddPolicy("CanApproveDiscounts", policy =>
        policy.RequireAssertion(context =>
        {
            // Solo managers y admins pueden aprobar descuentos
            if (!context.User.IsInRole("Manager") && !context.User.IsInRole("Admin"))
                return false;
                
            // Managers solo pueden aprobar en su tienda
            if (context.User.IsInRole("Manager") && !context.User.IsInRole("Admin"))
            {
                var userStore = context.User.FindFirst("store_id")?.Value;
                return !string.IsNullOrEmpty(userStore);
            }
            
            // Admins pueden aprobar en cualquier tienda
            return true;
        }));
});

// ===== CONFIGURACIÓN DE SERVICIOS ADICIONALES =====
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();
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