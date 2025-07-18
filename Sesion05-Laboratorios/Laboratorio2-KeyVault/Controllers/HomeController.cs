using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DevSeguroWebApp.Services;

namespace DevSeguroWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ISecureDataService _secureDataService;
        private readonly IKeyVaultService _keyVaultService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ISecureDataService secureDataService,
            IKeyVaultService keyVaultService,
            ILogger<HomeController> logger)
        {
            _secureDataService = secureDataService;
            _keyVaultService = keyVaultService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            ViewBag.Title = "Laboratorio 2 - Azure Key Vault Integration";
            return View();
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
} 