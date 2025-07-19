using Microsoft.AspNetCore.Mvc;
using DevSeguroWebApp.Services;
using DevSeguroWebApp.Models;

namespace DevSeguroWebApp.Controllers
{
    public class KeyVaultTestController : Controller
    {
        private readonly ISecureDataService _secureDataService;
        private readonly IKeyVaultService _keyVaultService;
        private readonly ILogger<KeyVaultTestController> _logger;
        private readonly IConfiguration _configuration;

        public KeyVaultTestController(
            ISecureDataService secureDataService,
            IKeyVaultService keyVaultService,
            ILogger<KeyVaultTestController> logger,
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
        public async Task<IActionResult> CreateSecret([FromBody] SecretRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Value))
                {
                    return Json(new { success = false, error = "Nombre y valor del secreto son requeridos" });
                }

                _logger.LogInformation("Creating secret: {SecretName}", request.Name);

                await _keyVaultService.SetSecretAsync(request.Name, request.Value);
                
                _logger.LogInformation("Secret created successfully: {SecretName}", request.Name);
                return Json(new
                {
                    success = true,
                    message = $"Secreto '{request.Name}' creado exitosamente",
                    secretName = request.Name,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating secret: {SecretName}", request?.Name);
                return Json(new { success = false, error = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSecret([FromBody] SecretGetRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Name))
                {
                    return Json(new { success = false, error = "Nombre del secreto es requerido" });
                }

                _logger.LogInformation("Retrieving secret: {SecretName}", request.Name);

                var secretValue = await _keyVaultService.GetSecretAsync(request.Name);
                
                if (!string.IsNullOrEmpty(secretValue))
                {
                    _logger.LogInformation("Secret retrieved successfully: {SecretName}", request.Name);
                    return Json(new
                    {
                        success = true,
                        secretName = request.Name,
                        secretValue = secretValue,
                        message = "Secreto recuperado exitosamente",
                        timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                    });
                }
                else
                {
                    _logger.LogWarning("Secret not found: {SecretName}", request.Name);
                    return Json(new { 
                        success = false, 
                        error = $"Secreto '{request.Name}' no encontrado en Key Vault" 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secret: {SecretName}", request?.Name);
                return Json(new { success = false, error = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListSecrets()
        {
            try
            {
                _logger.LogInformation("Listing all secrets from Key Vault");

                var secrets = await _keyVaultService.GetAllSecretsAsync();
                
                _logger.LogInformation("Found {SecretCount} secrets", secrets.Count);
                
                return Json(new
                {
                    success = true,
                    secrets = secrets,
                    count = secrets.Count,
                    message = $"Se encontraron {secrets.Count} secreto(s)",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing secrets");
                return Json(new { success = false, error = $"Error listando secretos: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSecret([FromBody] SecretGetRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Name))
                {
                    return Json(new { success = false, error = "Nombre del secreto es requerido" });
                }

                _logger.LogInformation("Checking if secret exists before deletion: {SecretName}", request.Name);

                var exists = await _keyVaultService.SecretExistsAsync(request.Name);
                
                if (exists)
                {
                    // No podemos eliminar secretos con esta implementación simple
                    _logger.LogWarning("Secret deletion not implemented in service: {SecretName}", request.Name);
                    return Json(new { 
                        success = false, 
                        error = "La eliminación de secretos no está implementada en el servicio actual" 
                    });
                }
                else
                {
                    _logger.LogWarning("Secret not found for deletion: {SecretName}", request.Name);
                    return Json(new { 
                        success = false, 
                        error = $"Secreto '{request.Name}' no encontrado" 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking secret for deletion: {SecretName}", request?.Name);
                return Json(new { success = false, error = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetKeyVaultInfo()
        {
            try
            {
                var keyVaultUri = _configuration["KeyVault:VaultUri"];
                
                if (string.IsNullOrEmpty(keyVaultUri))
                {
                    return Json(new { 
                        success = false, 
                        error = "Key Vault URI not configured" 
                    });
                }

                // Verificar conectividad
                var isConnected = await _keyVaultService.IsConnectedAsync();
                var keyVaultInfo = await _keyVaultService.GetKeyVaultInfoAsync();
                var secrets = await _keyVaultService.GetAllSecretsAsync();
                
                return Json(new
                {
                    success = true,
                    keyVaultUri = keyVaultUri,
                    isConnected = isConnected,
                    secretCount = secrets.Count,
                    keyVaultInfo = keyVaultInfo,
                    message = "Key Vault conectado correctamente",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Key Vault info");
                return Json(new { 
                    success = false, 
                    error = $"Error conectando con Key Vault: {ex.Message}",
                    keyVaultUri = _configuration["KeyVault:VaultUri"]
                });
            }
        }

        [HttpGet]
        public IActionResult Diagnostics()
        {
            try
            {
                var keyVaultUri = _configuration["KeyVault:VaultUri"];
                var hasKeyVaultConfig = !string.IsNullOrEmpty(keyVaultUri);
                var hasStorageConnection = !string.IsNullOrEmpty(_configuration["DataProtection:StorageConnectionString"]);

                return Json(new { 
                    success = true,
                    message = "Key Vault Test Controller funcionando correctamente",
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    applicationName = _configuration["DataProtection:ApplicationName"],
                    keyVaultUri = keyVaultUri,
                    hasKeyVaultConfig = hasKeyVaultConfig,
                    hasStorageConnection = hasStorageConnection,
                    integrationStatus = hasKeyVaultConfig && hasStorageConnection ? "Completa" : "Parcial"
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
    }
} 