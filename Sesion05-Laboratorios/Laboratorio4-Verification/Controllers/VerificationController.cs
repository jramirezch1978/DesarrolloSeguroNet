using Microsoft.AspNetCore.Mvc;
using DevSeguroWebApp.Services;

namespace DevSeguroWebApp.Controllers
{
    public class VerificationController : Controller
    {
        private readonly ISecureDataService _secureDataService;
        private readonly IKeyVaultService _keyVaultService;
        private readonly ILogger<VerificationController> _logger;
        private readonly IConfiguration _configuration;

        public VerificationController(
            ISecureDataService secureDataService,
            IKeyVaultService keyVaultService,
            ILogger<VerificationController> logger,
            IConfiguration configuration)
        {
            _secureDataService = secureDataService;
            _keyVaultService = keyVaultService;
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Status()
        {
            try
            {
                var applicationName = _configuration["DataProtection:ApplicationName"];
                var hasStorageConnection = !string.IsNullOrEmpty(_configuration["DataProtection:StorageConnectionString"]);
                var keyVaultUri = _configuration["KeyVault:VaultUri"];
                
                return Json(new { 
                    success = true,
                    message = "Sistema de verificación funcionando correctamente",
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    applicationName = applicationName,
                    hasStorageConnection = hasStorageConnection,
                    hasKeyVault = !string.IsNullOrEmpty(keyVaultUri),
                    keyVaultUri = keyVaultUri,
                    dataProtectionConfigured = _secureDataService != null,
                    keyVaultConfigured = _keyVaultService != null,
                    framework = ".NET 9",
                    port = "7001",
                    laboratory = "Verificación Completa"
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    error = ex.Message 
                });
            }
        }

        [HttpPost]
        public IActionResult VerifySystem([FromBody] VerificationRequest request)
        {
            try
            {
                var testData = $"Verificación del sistema - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
                var purpose = "UserData.Verification.v1";
                
                // Test Data Protection
                var protectedData = _secureDataService.ProtectSensitiveData(testData, purpose);
                var unprotectedData = _secureDataService.UnprotectSensitiveData<string>(protectedData, purpose);
                
                var dataProtectionWorking = testData == unprotectedData;
                
                return Json(new
                {
                    success = true,
                    dataProtectionWorking = dataProtectionWorking,
                    testTimestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    message = dataProtectionWorking ? "Sistema verificado exitosamente" : "Sistema con problemas",
                    originalData = testData,
                    protectedData = protectedData,
                    unprotectedData = unprotectedData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando sistema");
                return Json(new { 
                    success = false, 
                    error = ex.Message,
                    dataProtectionWorking = false
                });
            }
        }
    }

    public class VerificationRequest
    {
        public string TestType { get; set; } = "full";
    }
} 