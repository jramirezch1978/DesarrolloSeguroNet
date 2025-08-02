using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using SecureShop.Data;
using System.Security.Claims;

namespace SecureShop.Controllers;

/// <summary>
/// Controlador principal que demuestra autenticación y autorización con Azure AD
/// Implementa diferentes niveles de acceso basados en roles
/// </summary>
[Authorize] // Requiere autenticación para todas las acciones
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SecureDbContext _context;

    public HomeController(ILogger<HomeController> logger, SecureDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Página principal - accesible sin autenticación
    /// </summary>
    [AllowAnonymous]
    public IActionResult Index()
    {
        // Verificar si el usuario está autenticado
        if (User.Identity?.IsAuthenticated == true)
        {
            var userEmail = User.GetDisplayName();
            _logger.LogInformation("👤 Usuario autenticado visitó la página principal: {Email}", userEmail);
            
            ViewBag.IsAuthenticated = true;
            ViewBag.UserName = userEmail;
        }
        else
        {
            ViewBag.IsAuthenticated = false;
        }

        return View();
    }

    /// <summary>
    /// Dashboard - requiere autenticación
    /// Muestra información del usuario autenticado y sus roles
    /// </summary>
    [Authorize]
    public IActionResult Dashboard()
    {
        // ===== EXTRACCIÓN SEGURA DE INFORMACIÓN DEL USUARIO =====
        var userObjectId = User.GetObjectId();
        var userName = User.GetDisplayName();
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? 
                       User.FindFirst("preferred_username")?.Value;
        
        // Obtener roles desde claims
        var userRoles = User.Claims
            .Where(c => c.Type == "roles" || c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        // Obtener información adicional de Azure AD
        var givenName = User.FindFirst("given_name")?.Value;
        var familyName = User.FindFirst("family_name")?.Value;
        var tenantId = User.FindFirst("tid")?.Value;

        // ===== PREPARAR MODELO DE VISTA =====
        var model = new DashboardViewModel
        {
            ObjectId = userObjectId ?? "Unknown",
            DisplayName = userName ?? "Unknown User",
            Email = userEmail ?? "No email",
            GivenName = givenName,
            FamilyName = familyName,
            TenantId = tenantId,
            Roles = userRoles,
            LastLogin = DateTime.UtcNow, // Se puede obtener de base de datos
            Claims = User.Claims.ToDictionary(c => c.Type, c => c.Value)
        };

        // ===== LOGGING DE AUDITORÍA =====
        _logger.LogInformation("🎛️ Usuario {UserName} ({ObjectId}) accedió al dashboard con roles: {Roles}", 
            userName, userObjectId, string.Join(", ", userRoles));

        return View(model);
    }

    /// <summary>
    /// Panel de administración - solo para administradores
    /// </summary>
    [Authorize(Policy = "AdminOnly")]
    public IActionResult AdminPanel()
    {
        var userEmail = User.GetDisplayName();
        _logger.LogInformation("🔐 Admin {Email} accedió al panel de administración", userEmail);

        // Obtener estadísticas para admins
        var model = new AdminPanelViewModel
        {
            TotalUsers = _context.Users.Count(),
            ActiveUsers = _context.Users.Count(u => u.IsActive),
            TotalProducts = _context.Products.Count(),
            RecentLogins = _context.AuditLogs
                .Where(al => al.Action == "LOGIN")
                .OrderByDescending(al => al.Timestamp)
                .Take(10)
                .ToList()
        };

        return View(model);
    }

    /// <summary>
    /// Reportes - para managers y administradores
    /// </summary>
    [Authorize(Policy = "ManagerOrAdmin")]
    public IActionResult Reports()
    {
        var userEmail = User.GetDisplayName();
        var userRoles = User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        _logger.LogInformation("📊 Usuario {Email} con roles {Roles} accedió a reportes", 
            userEmail, string.Join(", ", userRoles));

        return View();
    }

    /// <summary>
    /// Perfil de usuario - información personal del usuario autenticado
    /// </summary>
    [Authorize]
    public IActionResult Profile()
    {
        var userObjectId = User.GetObjectId();
        
        // Buscar información del usuario en base de datos local
        var localUser = _context.Users
            .FirstOrDefault(u => u.AzureAdObjectId == userObjectId);

        var model = new UserProfileViewModel
        {
            AzureAdInfo = new DashboardViewModel
            {
                ObjectId = userObjectId ?? "Unknown",
                DisplayName = User.GetDisplayName() ?? "Unknown",
                Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "No email",
                GivenName = User.FindFirst("given_name")?.Value,
                FamilyName = User.FindFirst("family_name")?.Value,
                Roles = User.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList()
            },
            LocalUserInfo = localUser
        };

        return View(model);
    }

    /// <summary>
    /// Cerrar sesión
    /// </summary>
    [Authorize]
    public IActionResult Logout()
    {
        var userEmail = User.GetDisplayName();
        _logger.LogInformation("🚪 Usuario {Email} cerrando sesión", userEmail);

        // Registrar logout en auditoría
        try
        {
            var auditLog = new AuditLog
            {
                UserId = User.GetObjectId() ?? "Unknown",
                Action = "LOGOUT",
                EntityType = "Authentication",
                Timestamp = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
            };

            _context.AuditLogs.Add(auditLog);
            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error registrando logout: {Error}", ex.Message);
        }

        return SignOut();
    }

    /// <summary>
    /// Página de error
    /// </summary>
    [AllowAnonymous]
    public IActionResult Error()
    {
        return View();
    }

    /// <summary>
    /// Página de acceso denegado
    /// </summary>
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        var userEmail = User.GetDisplayName();
        var attemptedAction = HttpContext.Request.Path;
        
        _logger.LogWarning("🚫 Acceso denegado para usuario {Email} a ruta {Path}", 
            userEmail, attemptedAction);

        return View();
    }
}

// ===== MODELOS DE VISTA =====

/// <summary>
/// Modelo para el dashboard principal
/// </summary>
public class DashboardViewModel
{
    public string ObjectId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public string? TenantId { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime LastLogin { get; set; }
    public Dictionary<string, string> Claims { get; set; } = new();
}

/// <summary>
/// Modelo para panel de administración
/// </summary>
public class AdminPanelViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalProducts { get; set; }
    public List<AuditLog> RecentLogins { get; set; } = new();
}

/// <summary>
/// Modelo para perfil de usuario
/// </summary>
public class UserProfileViewModel
{
    public DashboardViewModel AzureAdInfo { get; set; } = new();
    public User? LocalUserInfo { get; set; }
}