using Microsoft.AspNetCore.Mvc;
using DevSeguroWebApp.Services;
using System.Text.Json; // 👈 AGREGAR ESTA LÍNEA

namespace DevSeguroWebApp.Controllers
{
    public class DataProtectionTestController : Controller
    {
        private readonly ISecureDataService _secureDataService;
        private readonly ILogger<DataProtectionTestController> _logger;
        private readonly IConfiguration _configuration;

        public DataProtectionTestController(
            ISecureDataService secureDataService,
            ILogger<DataProtectionTestController> logger,
            IConfiguration configuration)
        {
            _secureDataService = secureDataService;
            _logger = logger;
            _configuration = configuration;
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

        // Almacenar preferencia en sesión
        HttpContext.Session.SetString("StorageType", request.UseAzureStorage ? "Azure" : "Local");
        
        // 📁 GUARDAR PREFERENCIA EN ARCHIVO PERSISTENTE
        try
        {
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "storage-preference.json");
            var preference = new { 
                UseAzureStorage = request.UseAzureStorage, 
                LastChanged = DateTime.UtcNow,
                ChangedBy = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
            };
            System.IO.File.WriteAllText(configPath, System.Text.Json.JsonSerializer.Serialize(preference, new JsonSerializerOptions { WriteIndented = true }));
            _logger.LogInformation("Storage preference saved to file: {Preference}", request.UseAzureStorage ? "Azure" : "Local");
        }
        catch (Exception fileEx)
        {
            _logger.LogWarning("Could not save preference to file: {Error}", fileEx.Message);
        }

        var storageType = request.UseAzureStorage ? "Azure Storage" : "Local File System";
        var description = request.UseAzureStorage 
            ? "Usando Azure Blob Storage para persistencia enterprise" 
            : "Usando sistema de archivos local para desarrollo";

        _logger.LogInformation("Storage type preference changed to: {StorageType}", storageType);

        return Json(new
        {
            success = true,
            storageType = storageType,
            description = description,
            isAzure = request.UseAzureStorage,
            message = $"Preferencia guardada: {storageType}",
            requiresRestart = true,
            note = "⚠️ La aplicación debe reiniciarse para aplicar este cambio en Data Protection.",
            instruction = "Presiona Ctrl+C en la consola y ejecuta 'dotnet run' nuevamente."
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

        [HttpGet]
        public async Task<IActionResult> CheckAzureStorage()
        {
            try
            {
                var connectionString = _configuration["DataProtection:StorageConnectionString"];
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    return Json(new { 
                        success = false, 
                        error = "No hay connection string configurado",
                        storageType = "Local"
                    });
                }

                var blobServiceClient = new Azure.Storage.Blobs.BlobServiceClient(connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient("dataprotection-keys");
                
                // Verificar si el container existe
                var containerExists = await containerClient.ExistsAsync();
                
                if (!containerExists)
                {
                    return Json(new {
                        success = true,
                        containerExists = false,
                        message = "El container 'dataprotection-keys' aún no existe. Se creará al proteger el primer dato.",
                        storageAccount = GetStorageAccountName(connectionString),
                        containerName = "dataprotection-keys"
                    });
                }

                // Verificar si el blob de claves existe
                var blobClient = containerClient.GetBlobClient("keys.xml");
                var blobExists = await blobClient.ExistsAsync();
                
                if (!blobExists)
                {
                    return Json(new {
                        success = true,
                        containerExists = true,
                        blobExists = false,
                        message = "El container existe pero aún no hay claves. Protege algún dato para generar las claves.",
                        storageAccount = GetStorageAccountName(connectionString),
                        containerName = "dataprotection-keys",
                        blobName = "keys.xml"
                    });
                }

                // Obtener información del blob
                var properties = await blobClient.GetPropertiesAsync();
                var content = await blobClient.DownloadContentAsync();
                var xmlContent = content.Value.Content.ToString();
                
                // Contar claves en el XML
                var keyCount = 0;
                try
                {
                    var doc = System.Xml.Linq.XDocument.Parse(xmlContent);
                    keyCount = doc.Root?.Elements().Count() ?? 0;
                }
                catch
                {
                    keyCount = 0;
                }

                return Json(new {
                    success = true,
                    containerExists = true,
                    blobExists = true,
                    storageAccount = GetStorageAccountName(connectionString),
                    containerName = "dataprotection-keys",
                    blobName = "keys.xml",
                    lastModified = properties.Value.LastModified.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    sizeBytes = properties.Value.ContentLength,
                    keyCount = keyCount,
                    message = $"Azure Storage activo con {keyCount} clave(s) almacenada(s)",
                    xmlPreview = xmlContent.Length > 200 ? xmlContent.Substring(0, 200) + "..." : xmlContent
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Azure Storage status");
                return Json(new { 
                    success = false, 
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

        [HttpGet]
        public IActionResult Diagnostics()
        {
            try
            {
                return Json(new { 
                    success = true,
                    message = "Controlador funcionando correctamente",
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    applicationName = _configuration["DataProtection:ApplicationName"],
                    hasStorageConnection = !string.IsNullOrEmpty(_configuration["DataProtection:StorageConnectionString"])
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

        private static string GetStorageAccountName(string connectionString)
        {
            try
            {
                var accountNameStart = connectionString.IndexOf("AccountName=") + "AccountName=".Length;
                var accountNameEnd = connectionString.IndexOf(";", accountNameStart);
                return connectionString.Substring(accountNameStart, accountNameEnd - accountNameStart);
            }
            catch
            {
                return "Unknown";
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