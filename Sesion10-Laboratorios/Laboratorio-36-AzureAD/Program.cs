// ========================================
// LABORATORIO 36: AZURE AD AUTHENTICATION
// ========================================
// Este archivo demuestra la configuración completa de autenticación Azure AD
// Integra con el Program.cs del Laboratorio 35

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.EntityFrameworkCore;
using SecureShop.Data;

var builder = WebApplication.CreateBuilder(args);

// ===== CONFIGURACIÓN DE AZURE AD =====
// Configurar Microsoft Identity Web (Azure AD)
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        
        // ===== EVENTOS DE AUDITORÍA DE AUTENTICACIÓN =====
        options.Events = new OpenIdConnectEvents
        {
            // Evento cuando token es validado exitosamente
            OnTokenValidated = async context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                
                var userObjectId = context.Principal?.GetObjectId();
                var userEmail = context.Principal?.GetDisplayName();
                
                logger.LogInformation("✅ Usuario autenticado: {Email} (ID: {ObjectId})", 
                    userEmail, userObjectId);
                
                // ===== SINCRONIZACIÓN CON BASE DE DATOS LOCAL =====
                try
                {
                    var dbContext = context.HttpContext.RequestServices
                        .GetRequiredService<SecureDbContext>();
                    
                    // Buscar usuario en base de datos local
                    var existingUser = await dbContext.Users
                        .FirstOrDefaultAsync(u => u.AzureAdObjectId == userObjectId);
                    
                    if (existingUser == null)
                    {
                        // Crear nuevo usuario local vinculado a Azure AD
                        var newUser = new User
                        {
                            AzureAdObjectId = userObjectId!,
                            Email = userEmail ?? "unknown@example.com",
                            FirstName = context.Principal?.GetClaim("given_name"),
                            LastName = context.Principal?.GetClaim("family_name"),
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            LastLoginAt = DateTime.UtcNow
                        };
                        
                        dbContext.Users.Add(newUser);
                        
                        // Asignar rol por defecto
                        var defaultRole = new UserRole
                        {
                            UserId = newUser.Id,
                            RoleName = "Customer",
                            IsActive = true,
                            AssignedAt = DateTime.UtcNow
                        };
                        
                        dbContext.UserRoles.Add(defaultRole);
                        await dbContext.SaveChangesAsync();
                        
                        logger.LogInformation("👤 Nuevo usuario creado: {Email}", userEmail);
                    }
                    else
                    {
                        // Actualizar último login
                        existingUser.LastLoginAt = DateTime.UtcNow;
                        await dbContext.SaveChangesAsync();
                        
                        logger.LogInformation("🔄 Usuario existente actualizado: {Email}", userEmail);
                    }
                    
                    // ===== AGREGANDO CLAIMS DE ROLES =====
                    // Obtener roles del usuario desde nuestra base de datos
                    var userRoles = await dbContext.UserRoles
                        .Where(ur => ur.UserId == existingUser!.Id && ur.IsActive)
                        .Select(ur => ur.RoleName)
                        .ToListAsync();
                    
                    // Agregar claims de roles a la identidad
                    var identity = context.Principal?.Identity as ClaimsIdentity;
                    if (identity != null)
                    {
                        foreach (var role in userRoles)
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, role));
                            logger.LogDebug("🎭 Rol agregado a claims: {Role}", role);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ Error en sincronización de usuario: {Error}", ex.Message);
                    // No fallar autenticación por errores de sincronización
                }
            },
            
            // Evento cuando falla la autenticación
            OnAuthenticationFailed = async context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                
                logger.LogError("❌ Fallo de autenticación: {Error} | {Description}", 
                    context.Exception?.Message, context.Exception?.InnerException?.Message);
                
                // Registrar intento de autenticación fallido para auditoría
                try
                {
                    var dbContext = context.HttpContext.RequestServices
                        .GetRequiredService<SecureDbContext>();
                    
                    var auditLog = new AuditLog
                    {
                        UserId = "UNKNOWN",
                        Action = "AUTHENTICATION_FAILED",
                        EntityType = "Authentication",
                        Changes = $"Error: {context.Exception?.Message}",
                        Timestamp = DateTime.UtcNow,
                        IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = context.HttpContext.Request.Headers["User-Agent"].ToString()
                    };
                    
                    dbContext.AuditLogs.Add(auditLog);
                    await dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ Error registrando fallo de autenticación: {Error}", ex.Message);
                }
            },
            
            // Evento antes de redirigir a Azure AD
            OnRedirectToIdentityProvider = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                
                logger.LogInformation("🔄 Redirigiendo a Azure AD para autenticación");
                return Task.CompletedTask;
            },
            
            // Evento cuando usuario cierra sesión
            OnRedirectToIdentityProviderForSignOut = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                
                var userEmail = context.HttpContext.User?.GetDisplayName();
                logger.LogInformation("🚪 Usuario cerrando sesión: {Email}", userEmail);
                
                return Task.CompletedTask;
            }
        };
        
        // ===== CONFIGURACIÓN ADICIONAL DE SEGURIDAD =====
        options.GetClaimsFromUserInfoEndpoint = true; // Obtener claims adicionales
        options.SaveTokens = false; // No almacenar tokens por seguridad
        options.UseTokenLifetime = true; // Respetar expiración de tokens
        
        // Configurar scopes mínimos necesarios
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
    });

// ===== CONFIGURACIÓN DE AUTORIZACIÓN =====
builder.Services.AddAuthorization(options =>
{
    // Política básica: solo usuarios autenticados
    options.AddPolicy("AuthenticatedUsers", policy => 
        policy.RequireAuthenticatedUser());
    
    // Políticas por roles específicos
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("ManagerOrAdmin", policy => 
        policy.RequireRole("Manager", "Admin"));
    
    options.AddPolicy("CustomerAccess", policy => 
        policy.RequireRole("Customer", "Manager", "Admin"));
    
    // Política avanzada para gestión de productos
    options.AddPolicy("CanManageProducts", policy =>
        policy.RequireAssertion(context =>
        {
            // Admin puede hacer todo
            if (context.User.IsInRole("Admin"))
                return true;
            
            // ProductManager puede gestionar productos
            if (context.User.IsInRole("ProductManager"))
                return true;
            
            // StoreManager solo si tiene claim de tienda específica
            if (context.User.IsInRole("StoreManager"))
            {
                return context.User.HasClaim("StoreId", "current-store");
            }
            
            return false;
        }));
    
    // Política para reportes con múltiples condiciones
    options.AddPolicy("CanViewReports", policy =>
        policy.RequireAssertion(context =>
        {
            // Múltiples formas de acceder a reportes
            return context.User.IsInRole("Admin") ||
                   context.User.IsInRole("Manager") ||
                   context.User.HasClaim("Permission", "ViewReports") ||
                   context.User.HasClaim("Department", "Finance");
        }));
});

// ===== CONFIGURACIÓN DE SERVICIOS ADICIONALES =====
builder.Services.AddControllersWithViews();

// Configurar Entity Framework (se integra con Laboratorio 35)
builder.Services.AddDbContext<SecureDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseInMemoryDatabase("SecureShopAzureAD");
    }
    else
    {
        // En producción usará cadena de conexión de Key Vault
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseSqlServer(connectionString);
    }
});

var app = builder.Build();

// ===== CONFIGURACIÓN DEL PIPELINE =====
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ===== AUTENTICACIÓN Y AUTORIZACIÓN =====
app.UseAuthentication(); // Debe ir antes de UseAuthorization
app.UseAuthorization();

// ===== RUTAS =====
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ===== INICIALIZACIÓN DE DATOS DE DESARROLLO =====
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<SecureDbContext>();
        await context.Database.EnsureCreatedAsync();
        
        // Crear datos de prueba si no existen
        if (!context.Users.Any())
        {
            var testUsers = new[]
            {
                new User 
                { 
                    AzureAdObjectId = "admin-test-001",
                    Email = "admin@contoso.onmicrosoft.com",
                    FirstName = "Admin",
                    LastName = "User",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User 
                { 
                    AzureAdObjectId = "manager-test-001",
                    Email = "manager@contoso.onmicrosoft.com",
                    FirstName = "Manager",
                    LastName = "User",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };
            
            context.Users.AddRange(testUsers);
            
            // Asignar roles
            var roles = new[]
            {
                new UserRole { UserId = testUsers[0].Id, RoleName = "Admin", AssignedAt = DateTime.UtcNow },
                new UserRole { UserId = testUsers[0].Id, RoleName = "Manager", AssignedAt = DateTime.UtcNow },
                new UserRole { UserId = testUsers[1].Id, RoleName = "Manager", AssignedAt = DateTime.UtcNow }
            };
            
            context.UserRoles.AddRange(roles);
            await context.SaveChangesAsync();
        }
    }
}

// ===== LOGGING DE INICIO =====
app.Logger.LogInformation("🚀 SecureShop con Azure AD iniciado");
app.Logger.LogInformation("🔐 Autenticación: Azure AD OAuth 2.0/OpenID Connect");
app.Logger.LogInformation("🎭 Autorización: Claims-based con roles locales");
app.Logger.LogInformation("🔍 Auditoría: Eventos de autenticación completos");

app.Run();

// ===== EXTENSIONES AUXILIARES =====
public static class ClaimsExtensions
{
    public static string? GetClaim(this ClaimsPrincipal principal, string claimType)
    {
        return principal?.FindFirst(claimType)?.Value;
    }
}

/// <summary>
/// Ejemplo de servicio para gestión de usuarios
/// Puede expandirse para lógica de negocio más compleja
/// </summary>
public interface IUserService
{
    Task<User?> GetUserByAzureIdAsync(string azureObjectId);
    Task<List<string>> GetUserRolesAsync(Guid userId);
    Task AssignRoleAsync(Guid userId, string roleName);
    Task RevokeRoleAsync(Guid userId, string roleName);
}

/// <summary>
/// Implementación del servicio de usuario con auditoría
/// </summary>
public class UserService : IUserService
{
    private readonly SecureDbContext _context;
    private readonly ILogger<UserService> _logger;
    
    public UserService(SecureDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<User?> GetUserByAzureIdAsync(string azureObjectId)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.AzureAdObjectId == azureObjectId);
    }
    
    public async Task<List<string>> GetUserRolesAsync(Guid userId)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId && ur.IsActive)
            .Select(ur => ur.RoleName)
            .ToListAsync();
    }
    
    public async Task AssignRoleAsync(Guid userId, string roleName)
    {
        var existingRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleName == roleName);
        
        if (existingRole == null)
        {
            var newRole = new UserRole
            {
                UserId = userId,
                RoleName = roleName,
                IsActive = true,
                AssignedAt = DateTime.UtcNow
            };
            
            _context.UserRoles.Add(newRole);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("✅ Rol {Role} asignado a usuario {UserId}", roleName, userId);
        }
        else if (!existingRole.IsActive)
        {
            existingRole.IsActive = true;
            existingRole.AssignedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("🔄 Rol {Role} reactivado para usuario {UserId}", roleName, userId);
        }
    }
    
    public async Task RevokeRoleAsync(Guid userId, string roleName)
    {
        var role = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleName == roleName && ur.IsActive);
        
        if (role != null)
        {
            role.IsActive = false;
            role.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            _logger.LogWarning("⚠️ Rol {Role} revocado para usuario {UserId}", roleName, userId);
        }
    }
}