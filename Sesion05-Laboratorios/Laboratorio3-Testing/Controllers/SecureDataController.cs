using Microsoft.AspNetCore.Mvc;
using DevSeguroWebApp.Services;
using System.ComponentModel.DataAnnotations;

namespace DevSeguroWebApp.Controllers
{
    public class SecureDataController : Controller
    {
        private readonly ISecureDataService _secureDataService;
        private readonly IKeyVaultService _keyVaultService;
        private readonly ILogger<SecureDataController> _logger;
        private readonly IConfiguration _configuration;

        public SecureDataController(
            ISecureDataService secureDataService,
            IKeyVaultService keyVaultService,
            ILogger<SecureDataController> logger,
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
                    var preferencePath = Path.Combine(Directory.GetCurrentDirectory(), "storage-preference.json");
                    var preference = new { 
                        UseAzureStorage = request.UseAzureStorage, 
                        LastChanged = DateTime.UtcNow,
                        ChangedBy = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                    };
                    
                    var jsonOptions = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                    System.IO.File.WriteAllText(preferencePath, System.Text.Json.JsonSerializer.Serialize(preference, jsonOptions));
                    _logger.LogInformation("Storage preference saved to file: {Preference}", request.UseAzureStorage ? "Azure" : "Local");
                }
                catch (Exception fileEx)
                {
                    _logger.LogWarning("Could not save preference to file: {Error}", fileEx.Message);
                }

                var storageType = request.UseAzureStorage ? "Azure Storage + Key Vault" : "Local File System";
                var description = request.UseAzureStorage 
                    ? "Usando Azure Blob Storage + Key Vault para testing completo" 
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
        public IActionResult TestProtection([FromBody] TestDataRequest request)
        {
            try
            {
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
        public async Task<IActionResult> GetKeyVaultSecrets()
        {
            try
            {
                var secrets = await _keyVaultService.GetAllSecretsAsync();
                
                return Json(new { success = true, secrets = secrets });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Key Vault secrets");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateSecret([FromBody] CreateSecretRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.SecretName) || string.IsNullOrWhiteSpace(request.SecretValue))
                {
                    return Json(new { success = false, error = "Nombre y valor del secret son requeridos" });
                }

                await _keyVaultService.SetSecretAsync(request.SecretName, request.SecretValue);
                return Json(new { success = true, message = $"Secret '{request.SecretName}' creado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating secret: {SecretName}", request?.SecretName);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckKeyVaultStatus()
        {
            try
            {
                var isConnected = await _keyVaultService.IsConnectedAsync();
                
                if (!isConnected)
                {
                    return Json(new { 
                        success = false, 
                        error = "No se puede conectar a Key Vault",
                        connected = false
                    });
                }

                var keyVaultInfo = await _keyVaultService.GetKeyVaultInfoAsync();
                var secrets = await _keyVaultService.GetAllSecretsAsync();
                
                return Json(new {
                    success = true,
                    connected = true,
                    info = keyVaultInfo,
                    secretCount = secrets.Count,
                    secrets = secrets,
                    message = $"Key Vault conectado exitosamente con {secrets.Count} secret(s)"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Key Vault status");
                return Json(new { 
                    success = false, 
                    error = ex.Message,
                    connected = false
                });
            }
        }

        [HttpGet]
        public IActionResult TestConfiguration()
        {
            try
            {
                var keyVaultUri = _configuration["KeyVault:VaultUri"];
                var hasStorageConnection = !string.IsNullOrEmpty(_configuration["DataProtection:StorageConnectionString"]);
                
                return Json(new { 
                    success = true,
                    message = "Controlador funcionando correctamente",
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    applicationName = _configuration["DataProtection:ApplicationName"],
                    hasStorageConnection = hasStorageConnection,
                    hasKeyVault = !string.IsNullOrEmpty(keyVaultUri),
                    keyVaultUri = keyVaultUri,
                    dataProtectionConfigured = _secureDataService != null,
                    keyVaultConfigured = _keyVaultService != null,
                    framework = ".NET 9",
                    port = "7001",
                    laboratory = "Testing Completo"
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

        [HttpGet]
        public IActionResult Diagnostics()
        {
            return TestConfiguration();
        }

        // Cross-purpose testing para demostrar aislamiento de protectores
        [HttpPost]
        public IActionResult TestCrossDecryption([FromBody] CrossDecryptRequest request)
        {
            try
            {
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

    public class CreateSecretRequest
    {
        public string SecretName { get; set; } = string.Empty;
        public string SecretValue { get; set; } = string.Empty;
    }

    public class CrossDecryptRequest
    {
        public string ProtectedData { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
    }
} 