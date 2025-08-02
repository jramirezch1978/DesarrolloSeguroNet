using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using SecureShop.Data;

var builder = WebApplication.CreateBuilder(args);

// ===== CONFIGURACIÓN DE SERVICIOS DE SEGURIDAD =====

// Configurar Entity Framework (temporal con InMemory para desarrollo)
// En producción se configurará SQL Server con Key Vault
builder.Services.AddDbContext<SecureDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseInMemoryDatabase("SecureShopDev");
    }
    else
    {
        // En producción usará cadena de conexión de Key Vault
        options.UseInMemoryDatabase("SecureShopProd"); // Temporal
    }
});

// Configurar Data Protection para protección de cookies y tokens
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("./keys"))
    .SetApplicationName("SecureShop")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90)); // Rotación de claves cada 90 días

// Configurar servicios MVC con filtros de seguridad globales
builder.Services.AddControllersWithViews(options =>
{
    // Requerir HTTPS globalmente - no hay tráfico no encriptado nunca
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.RequireHttpsAttribute());
    
    // Agregar filtro de validación automática del modelo
    options.ModelValidatorProviders.Clear();
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(x => "Valor inválido");
    options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(x => "Debe ser un número");
});

// Configurar políticas de autorización granulares
builder.Services.AddAuthorization(options =>
{
    // Políticas básicas por rol (principio de menor privilegio)
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("ManagerOrAdmin", policy => 
        policy.RequireRole("Manager", "Admin"));
    
    options.AddPolicy("CustomerAccess", policy => 
        policy.RequireRole("Customer", "Manager", "Admin"));
    
    // Políticas más granulares para gestión de productos
    options.AddPolicy("CanManageProducts", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("Admin") ||
            context.User.IsInRole("ProductManager") ||
            (context.User.IsInRole("StoreManager") && 
             context.User.HasClaim("StoreId", "current-store"))));
    
    // Política para visualización de reportes
    options.AddPolicy("CanViewReports", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("Admin") ||
            context.User.IsInRole("Manager") ||
            context.User.HasClaim("Permission", "ViewReports")));
});

// Configurar CORS de forma segura (lista blanca de orígenes)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:7000", "https://localhost:5001")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Necesario para cookies de autenticación
    });
});

// Configurar redirección HTTPS obligatoria
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
    options.HttpsPort = 7000; // Puerto HTTPS estándar para desarrollo
});

// TODO: Configurar rate limiting en versión futura (requiere .NET 8+)
// builder.Services.AddRateLimiter(...);

// Configurar límites de tamaño de request para prevenir DoS
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB máximo
    options.ValueLengthLimit = 4096; // 4KB por valor
    options.KeyLengthLimit = 2048; // 2KB por clave
});

var app = builder.Build();

// ===== CONFIGURACIÓN DEL PIPELINE DE SEGURIDAD =====
// El orden de middleware es CRÍTICO para seguridad

// 1. Headers de seguridad (primera línea de defensa)
app.Use(async (context, next) =>
{
    // Headers de seguridad fundamentales
    context.Response.Headers["X-Frame-Options"] = "DENY"; // Previene clickjacking
    context.Response.Headers["X-Content-Type-Options"] = "nosniff"; // Previene MIME sniffing
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block"; // Activa protección XSS del navegador
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin"; // Control de referrer
    context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none"; // Bloquea políticas crossdomain
    
    // Content Security Policy (CSP) - Sistema inmunológico contra código malicioso
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self'; " + // Solo contenido de nuestro dominio por defecto
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " + // Scripts limitados a fuentes confiables
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " + // Estilos de fuentes seguras
        "img-src 'self' data: https:; " + // Imágenes de fuentes seguras
        "font-src 'self' https://cdn.jsdelivr.net; " + // Fuentes de CDN confiable
        "connect-src 'self'; " + // Conexiones AJAX solo a nuestro dominio
        "frame-ancestors 'none'"; // No permitir ser embebido en frames

    await next();
});

// 2. Middleware de desarrollo/producción
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // En producción: manejo de errores seguro sin exposición de información
    app.UseExceptionHandler("/Home/Error");
    
    // HTTP Strict Transport Security (HSTS)
    app.UseHsts();
}

// 3. Forzar HTTPS en todos los ambientes (crítico)
app.UseHttpsRedirection();

// 4. TODO: Rate limiting (requiere .NET 8+)
// app.UseRateLimiter();

// 5. Servir archivos estáticos con headers de seguridad
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Headers adicionales para archivos estáticos
        ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=31536000"; // Cache 1 año
    }
});

// 6. Enrutamiento
app.UseRouting();

// 7. CORS (después de routing, antes de autenticación)
app.UseCors();

// 8. Autenticación (se configurará en Lab 36)
app.UseAuthentication();

// 9. Autorización (después de autenticación)
app.UseAuthorization();

// 10. Configurar rutas con valores por defecto seguros
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); // Rutas por defecto

// Endpoint de health check para monitoreo
app.MapGet("/health", () => 
{
    return Results.Ok(new { 
        Status = "Healthy", 
        Timestamp = DateTime.UtcNow,
        Environment = app.Environment.EnvironmentName,
        Version = "1.0.0"
    });
}).AllowAnonymous();

// ===== INICIALIZACIÓN DE DATOS DE DESARROLLO =====
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<SecureDbContext>();
        
        // Asegurar que la base de datos existe
        await context.Database.EnsureCreatedAsync();
        
        // Seed data para desarrollo (solo si no hay datos)
        if (!context.Users.Any())
        {
            var adminUser = new User
            {
                AzureAdObjectId = "dev-admin-001",
                Email = "admin@secureshop.local",
                FirstName = "Admin",
                LastName = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            
            context.Users.Add(adminUser);
            
            // Agregar rol de administrador
            context.UserRoles.Add(new UserRole
            {
                UserId = adminUser.Id,
                RoleName = "Admin",
                IsActive = true,
                AssignedAt = DateTime.UtcNow
            });
            
            // Productos de ejemplo
            var sampleProducts = new[]
            {
                new Product 
                { 
                    Name = "Laptop Segura", 
                    Description = "Laptop con cifrado de hardware", 
                    Price = 1299.99m,
                    CreatedBy = adminUser.Id,
                    CreatedAt = DateTime.UtcNow
                },
                new Product 
                { 
                    Name = "Mouse Inalámbrico", 
                    Description = "Mouse ergonómico con conectividad segura", 
                    Price = 49.99m,
                    CreatedBy = adminUser.Id,
                    CreatedAt = DateTime.UtcNow
                }
            };
            
            context.Products.AddRange(sampleProducts);
            await context.SaveChangesAsync();
        }
    }
}

// Logging de inicio seguro (sin exponer información sensible)
app.Logger.LogInformation("SecureShop iniciado en ambiente: {Environment}", 
    app.Environment.EnvironmentName);
app.Logger.LogInformation("HTTPS forzado: {IsHttpsEnabled}", true);
app.Logger.LogInformation("Headers de seguridad: Habilitados");

app.Run();

/// <summary>
/// Clase para configuración de extensiones del programa
/// </summary>
public static class SecurityExtensions
{
    /// <summary>
    /// Configura headers de seguridad adicionales
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            await next();
        });
    }
}