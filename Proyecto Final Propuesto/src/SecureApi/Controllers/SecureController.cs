using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Security.Claims;

namespace SecureApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SecureController : ControllerBase
{
    private readonly ILogger<SecureController> _logger;
    private readonly IConfiguration _configuration;

    public SecureController(ILogger<SecureController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Obtiene información del usuario autenticado
    /// </summary>
    [HttpGet("user-info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetUserInfo()
    {
        try
        {
            var user = HttpContext.User;
            var userInfo = new
            {
                UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                Name = user.FindFirst(ClaimTypes.Name)?.Value ?? user.FindFirst("name")?.Value,
                Email = user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("preferred_username")?.Value,
                Roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray(),
                Claims = user.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToArray(),
                IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
                AuthenticationType = user.Identity?.AuthenticationType,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Información de usuario solicitada por {UserId}", userInfo.UserId);
            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener información del usuario");
            return StatusCode(500, "Error interno del servidor");
        }
    }

    /// <summary>
    /// Endpoint que requiere rol de administrador
    /// </summary>
    [HttpGet("admin-data")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetAdminData()
    {
        try
        {
            var adminData = new
            {
                Message = "Datos administrativos sensibles",
                SystemInfo = new
                {
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                    MachineName = Environment.MachineName,
                    ProcessorCount = Environment.ProcessorCount,
                    WorkingSet = Environment.WorkingSet,
                    Version = Environment.Version.ToString()
                },
                RequestInfo = new
                {
                    RequestId = HttpContext.TraceIdentifier,
                    RequestTime = DateTime.UtcNow,
                    ClientIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString()
                }
            };

            _logger.LogInformation("Datos administrativos accedidos por {UserId}", 
                HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            return Ok(adminData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener datos administrativos");
            return StatusCode(500, "Error interno del servidor");
        }
    }

    /// <summary>
    /// Endpoint para probar la configuración de Key Vault
    /// </summary>
    [HttpGet("keyvault-test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult TestKeyVault()
    {
        try
        {
            // Intentar leer una configuración que podría venir de Key Vault
            var testSecret = _configuration["TestSecret"] ?? "No configurado";
            var keyVaultUrl = _configuration["KeyVault:VaultUrl"];
            
            var result = new
            {
                Message = "Prueba de conectividad con Key Vault",
                KeyVaultConfigured = !string.IsNullOrEmpty(keyVaultUrl),
                KeyVaultUrl = string.IsNullOrEmpty(keyVaultUrl) ? "No configurado" : keyVaultUrl,
                TestSecretExists = testSecret != "No configurado",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Prueba de Key Vault ejecutada por {UserId}", 
                HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en la prueba de Key Vault");
            return StatusCode(500, "Error interno del servidor");
        }
    }

    /// <summary>
    /// Endpoint para verificar políticas de resiliencia
    /// </summary>
    [HttpGet("resilience-test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TestResilience([FromServices] IHttpClientFactory httpClientFactory)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("ResilientClient");
            
            // Simular llamada a API externa con políticas de resiliencia
            var testUrls = new[]
            {
                "https://httpbin.org/status/200",  // Éxito
                "https://httpbin.org/delay/1",     // Demora
                "https://httpbin.org/status/500"   // Error (para probar circuit breaker)
            };

            var results = new List<object>();

            foreach (var url in testUrls)
            {
                try
                {
                    var response = await httpClient.GetAsync(url);
                    results.Add(new
                    {
                        Url = url,
                        StatusCode = (int)response.StatusCode,
                        Success = response.IsSuccessStatusCode,
                        ResponseTime = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new
                    {
                        Url = url,
                        Error = ex.Message,
                        Success = false,
                        ResponseTime = DateTime.UtcNow
                    });
                }
            }

            _logger.LogInformation("Prueba de resiliencia ejecutada por {UserId}", 
                HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            return Ok(new
            {
                Message = "Prueba de políticas de resiliencia completada",
                Results = results,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en la prueba de resiliencia");
            return StatusCode(500, "Error interno del servidor");
        }
    }
} 