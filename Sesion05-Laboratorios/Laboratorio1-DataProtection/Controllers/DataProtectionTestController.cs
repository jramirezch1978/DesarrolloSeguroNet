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

        public IActionResult CrossProtection()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ChangeStorageType([FromBody] StorageChangeRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Json(new { success = false, error = "Solicitud inválida" });
                }

                // Almacenar preferencia en sesión (simulación)
                HttpContext.Session.SetString("StorageType", request.UseAzureStorage ? "Azure" : "Local");
                
                var storageType = request.UseAzureStorage ? "Azure Storage" : "Local File System";
                var description = request.UseAzureStorage 
                    ? "Usando Azure Blob Storage para persistencia enterprise" 
                    : "Usando sistema de archivos local para desarrollo";

                _logger.LogInformation("Storage type changed to: {StorageType}", storageType);

                return Json(new
                {
                    success = true,
                    storageType = storageType,
                    description = description,
                    isAzure = request.UseAzureStorage,
                    message = $"Configuración cambiada a {storageType}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing storage type");
                return Json(new { success = false, error = $"Error al cambiar configuración: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult TestCrossDecryption([FromBody] CrossDecryptRequest request)
        {
            try
            {
                // Validaciones de entrada
                if (request == null)
                {
                    return Json(new { success = false, error = "Solicitud inválida" });
                }

                if (string.IsNullOrWhiteSpace(request.ProtectedData))
                {
                    return Json(new { success = false, error = "Los datos protegidos no pueden estar vacíos" });
                }

                if (string.IsNullOrWhiteSpace(request.Purpose))
                {
                    return Json(new { success = false, error = "El propósito es requerido" });
                }

                _logger.LogInformation("Attempting cross-decryption with purpose: {Purpose}", request.Purpose);

                // Intentar desproteger con el propósito especificado
                var unprotectedData = _secureDataService.UnprotectSensitiveData<string>(request.ProtectedData, request.Purpose);
                
                _logger.LogInformation("Cross-decryption successful with purpose: {Purpose}", request.Purpose);

                return Json(new
                {
                    success = true,
                    unprotectedData = unprotectedData,
                    purpose = request.Purpose,
                    message = "Desencriptación exitosa"
                });
            }
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                _logger.LogWarning("Cryptographic error (expected for cross-purpose): {Error}", ex.Message);
                return Json(new { 
                    success = false, 
                    error = "Error criptográfico: Los datos fueron encriptados con un propósito diferente",
                    technicalError = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in cross-decryption test");
                return Json(new { success = false, error = $"Error inesperado: {ex.Message}" });
            }
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
                    return Json(new { success = false, error = "Datos de entrada vacíos" });
                }

                if (string.IsNullOrWhiteSpace(request.Data))
                {
                    _logger.LogWarning("Data is null or empty");
                    return Json(new { success = false, error = "Los datos a proteger no pueden estar vacíos" });
                }

                if (string.IsNullOrWhiteSpace(request.Purpose))
                {
                    _logger.LogWarning("Purpose is null or empty");
                    return Json(new { success = false, error = "El propósito de protección es requerido" });
                }

                _logger.LogInformation("Testing protection for data length: {DataLength}, purpose: {Purpose}", 
                    request.Data.Length, request.Purpose);

                // Proteger datos
                var protectedData = _secureDataService.ProtectSensitiveData(request.Data, request.Purpose);
                
                if (string.IsNullOrEmpty(protectedData))
                {
                    _logger.LogError("Protected data is null or empty");
                    return Json(new { success = false, error = "Error: Los datos protegidos están vacíos" });
                }

                // Desproteger datos para verificar
                var unprotectedData = _secureDataService.UnprotectSensitiveData<string>(protectedData, request.Purpose);
                
                if (string.IsNullOrEmpty(unprotectedData))
                {
                    _logger.LogError("Unprotected data is null or empty");
                    return Json(new { success = false, error = "Error: Los datos desprotegidos están vacíos" });
                }

                // Verificar integridad
                if (unprotectedData != request.Data)
                {
                    _logger.LogError("Data integrity check failed. Original: {Original}, Unprotected: {Unprotected}", 
                        request.Data, unprotectedData);
                    return Json(new { success = false, error = "Error: Falló la verificación de integridad de datos" });
                }

                _logger.LogInformation("Data protection test successful");

                return Json(new
                {
                    success = true,
                    originalData = request.Data,
                    protectedData = protectedData,
                    unprotectedData = unprotectedData,
                    protectedLength = protectedData.Length,
                    originalLength = request.Data.Length,
                    purpose = request.Purpose,
                    testTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Argument error in data protection test");
                return Json(new { success = false, error = $"Error de parámetros: {ex.Message}" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation in data protection test");
                return Json(new { success = false, error = $"Error de operación: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in data protection test");
                return Json(new { success = false, error = $"Error inesperado: {ex.Message}" });
            }
        }
    }

    public class TestDataRequest
    {
        public string Data { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
    }

    public class StorageChangeRequest
    {
        public bool UseAzureStorage { get; set; }
    }

    public class CrossDecryptRequest
    {
        public string ProtectedData { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
    }
} 