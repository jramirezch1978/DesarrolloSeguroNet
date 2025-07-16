using Microsoft.AspNetCore.Mvc;
using DevSeguroWebApp.Services;

namespace DevSeguroWebApp.Controllers
{
    public class DataProtectionTestController : Controller
    {
        private readonly ISecureDataService _secureDataService;
        private readonly ILogger<DataProtectionTestController> _logger;

        public DataProtectionTestController(
            ISecureDataService secureDataService,
            ILogger<DataProtectionTestController> logger)
        {
            _secureDataService = secureDataService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult TestProtection([FromBody] TestDataRequest request)
        {
            try
            {
                // Proteger datos
                var protectedData = _secureDataService.ProtectSensitiveData(request.Data, request.Purpose);
                
                // Desproteger datos para verificar
                var unprotectedData = _secureDataService.UnprotectSensitiveData<string>(protectedData, request.Purpose);
                
                return Json(new
                {
                    Success = true,
                    OriginalData = request.Data,
                    ProtectedData = protectedData,
                    UnprotectedData = unprotectedData,
                    ProtectedLength = protectedData.Length,
                    OriginalLength = request.Data.Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in data protection test");
                return Json(new { Success = false, Error = ex.Message });
            }
        }
    }

    public class TestDataRequest
    {
        public string Data { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
    }
} 