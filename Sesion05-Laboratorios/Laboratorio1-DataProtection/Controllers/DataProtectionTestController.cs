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
                // Validaciones de entrada
                if (request == null)
                {
                    _logger.LogWarning("Request is null");
                    return Json(new { Success = false, Error = "Datos de entrada vacíos" });
                }

                if (string.IsNullOrWhiteSpace(request.Data))
                {
                    _logger.LogWarning("Data is null or empty");
                    return Json(new { Success = false, Error = "Los datos a proteger no pueden estar vacíos" });
                }

                if (string.IsNullOrWhiteSpace(request.Purpose))
                {
                    _logger.LogWarning("Purpose is null or empty");
                    return Json(new { Success = false, Error = "El propósito de protección es requerido" });
                }

                _logger.LogInformation("Testing protection for data length: {DataLength}, purpose: {Purpose}", 
                    request.Data.Length, request.Purpose);

                // Proteger datos
                var protectedData = _secureDataService.ProtectSensitiveData(request.Data, request.Purpose);
                
                if (string.IsNullOrEmpty(protectedData))
                {
                    _logger.LogError("Protected data is null or empty");
                    return Json(new { Success = false, Error = "Error: Los datos protegidos están vacíos" });
                }

                // Desproteger datos para verificar
                var unprotectedData = _secureDataService.UnprotectSensitiveData<string>(protectedData, request.Purpose);
                
                if (string.IsNullOrEmpty(unprotectedData))
                {
                    _logger.LogError("Unprotected data is null or empty");
                    return Json(new { Success = false, Error = "Error: Los datos desprotegidos están vacíos" });
                }

                // Verificar integridad
                if (unprotectedData != request.Data)
                {
                    _logger.LogError("Data integrity check failed. Original: {Original}, Unprotected: {Unprotected}", 
                        request.Data, unprotectedData);
                    return Json(new { Success = false, Error = "Error: Falló la verificación de integridad de datos" });
                }

                _logger.LogInformation("Data protection test successful");

                return Json(new
                {
                    Success = true,
                    OriginalData = request.Data,
                    ProtectedData = protectedData,
                    UnprotectedData = unprotectedData,
                    ProtectedLength = protectedData.Length,
                    OriginalLength = request.Data.Length,
                    Purpose = request.Purpose,
                    TestTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Argument error in data protection test");
                return Json(new { Success = false, Error = $"Error de parámetros: {ex.Message}" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation in data protection test");
                return Json(new { Success = false, Error = $"Error de operación: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in data protection test");
                return Json(new { Success = false, Error = $"Error inesperado: {ex.Message}" });
            }
        }
    }

    public class TestDataRequest
    {
        public string Data { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
    }
} 