using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevSeguroWebApp.Services;
using System.ComponentModel.DataAnnotations;

namespace DevSeguroWebApp.Controllers
{
    [Authorize]
    public class SecureDataController : Controller
    {
        private readonly ISecureDataService _secureDataService;
        private readonly IKeyVaultService _keyVaultService;
        private readonly ILogger<SecureDataController> _logger;

        public SecureDataController(
            ISecureDataService secureDataService,
            IKeyVaultService keyVaultService,
            ILogger<SecureDataController> logger)
        {
            _secureDataService = secureDataService;
            _keyVaultService = keyVaultService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ProtectData([Required] string data, [Required] string purpose)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Data and purpose are required");
            }

            try
            {
                var protectedData = _secureDataService.ProtectSensitiveData(data, purpose);
                return Json(new { 
                    success = true, 
                    protectedData = protectedData,
                    originalLength = data.Length,
                    protectedLength = protectedData.Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error protecting data");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UnprotectData([Required] string protectedData, [Required] string purpose)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Protected data and purpose are required");
            }

            try
            {
                var unprotectedData = _secureDataService.UnprotectSensitiveData<string>(protectedData, purpose);
                return Json(new { 
                    success = true, 
                    unprotectedData = unprotectedData 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unprotecting data");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetKeyVaultSecrets()
        {
            try
            {
                var secrets = await _keyVaultService.GetAllSecretsAsync();
                
                // Mask sensitive values for display
                var maskedSecrets = secrets.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Key.Contains("Password") || kvp.Key.Contains("Key") 
                        ? MaskSensitiveValue(kvp.Value) 
                        : kvp.Value
                );

                return Json(new { success = true, secrets = maskedSecrets });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Key Vault secrets");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateSecret([Required] string secretName, [Required] string secretValue)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Secret name and value are required");
            }

            try
            {
                await _keyVaultService.SetSecretAsync(secretName, secretValue);
                return Json(new { success = true, message = $"Secret '{secretName}' created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating secret: {SecretName}", secretName);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult TestConfiguration()
        {
            var config = new
            {
                DataProtectionConfigured = _secureDataService != null,
                KeyVaultConfigured = _keyVaultService != null,
                Environment = Environment.MachineName,
                Framework = Environment.Version.ToString(),
                ConfigurationSources = new[]
                {
                    "appsettings.json",
                    "Azure Key Vault",
                    "Environment Variables"
                }
            };

            return Json(config);
        }

        private static string MaskSensitiveValue(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= 8)
                return "***";
            
            return value.Substring(0, 4) + new string('*', Math.Min(value.Length - 8, 20)) + value.Substring(value.Length - 4);
        }
    }
} 