using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureShop.Security;
using System.Security.Claims;

namespace SecureShop.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    public IActionResult Dashboard()
    {
        var model = new DashboardViewModel
        {
            UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "N/A",
            Email = User.FindFirst(ClaimTypes.Email)?.Value ?? 
                   User.FindFirst("preferred_username")?.Value ?? "N/A",
            Roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList(),
            Department = User.FindFirst("department")?.Value ?? "N/A",
            Permissions = User.Claims.Where(c => c.Type == "permission").Select(c => c.Value).ToList(),
            StoreId = User.FindFirst("store_id")?.Value ?? "N/A"
        };

        return View(model);
    }

    [Authorize(Policy = "AdminOnly")]
    public IActionResult AdminPanel()
    {
        return View();
    }

    [Authorize(Policy = "CanManageProducts")]
    public IActionResult ManageProducts()
    {
        return View();
    }

    [Authorize(Policy = "CanViewReports")]
    public IActionResult Reports()
    {
        return View();
    }

    [Authorize(Policy = "CanApproveDiscounts")]
    public IActionResult ApproveDiscounts()
    {
        return View();
    }

    [Authorize(Policy = "CanEditOwnProfile")]
    public IActionResult EditProfile()
    {
        var currentUserId = User.FindFirst("oid")?.Value ?? 
                           User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        
        var profileResource = new UserProfileResource
        {
            OwnerId = currentUserId,
            Department = User.FindFirst("department")?.Value,
            Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "",
            DisplayName = User.Identity?.Name ?? ""
        };

        // Este recurso pasará la autorización porque el usuario es el propietario
        return View(profileResource);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}

public class DashboardViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public string Department { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
    public string StoreId { get; set; } = string.Empty;
}