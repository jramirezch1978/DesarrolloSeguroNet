using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using SecureWebApp.Models;
using System.Security.Claims;
using System.Text.Json;

namespace SecureWebApp.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        var user = HttpContext.User;
        var model = new HomeViewModel
        {
            UserName = user.Identity?.Name ?? "Usuario desconocido",
            Email = user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("preferred_username")?.Value,
            IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
            Claims = user.Claims.Select(c => new ClaimInfo { Type = c.Type, Value = c.Value }).ToList(),
            DisplayName = user.FindFirst("name")?.Value ?? user.FindFirst(ClaimTypes.GivenName)?.Value
        };

        _logger.LogInformation("Usuario {UserName} accedió a la página principal", model.UserName);
        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public async Task<IActionResult> ApiTest()
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("SecureApiClient");
            
            // Obtener el token de acceso para llamar a la API
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(accessToken))
            {
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            }

            // Llamar a diferentes endpoints de la API
            var results = new List<ApiTestResult>();

            // Probar endpoint de salud (público)
            try
            {
                var healthResponse = await httpClient.GetAsync("/health");
                var healthContent = await healthResponse.Content.ReadAsStringAsync();
                results.Add(new ApiTestResult
                {
                    Endpoint = "/health",
                    StatusCode = (int)healthResponse.StatusCode,
                    Success = healthResponse.IsSuccessStatusCode,
                    Response = healthContent,
                    RequiresAuth = false
                });
            }
            catch (Exception ex)
            {
                results.Add(new ApiTestResult
                {
                    Endpoint = "/health",
                    StatusCode = 0,
                    Success = false,
                    Response = ex.Message,
                    RequiresAuth = false
                });
            }

            // Probar endpoint de datos seguros
            try
            {
                var secureResponse = await httpClient.GetAsync("/securedata");
                var secureContent = await secureResponse.Content.ReadAsStringAsync();
                results.Add(new ApiTestResult
                {
                    Endpoint = "/securedata",
                    StatusCode = (int)secureResponse.StatusCode,
                    Success = secureResponse.IsSuccessStatusCode,
                    Response = secureContent,
                    RequiresAuth = true
                });
            }
            catch (Exception ex)
            {
                results.Add(new ApiTestResult
                {
                    Endpoint = "/securedata",
                    StatusCode = 0,
                    Success = false,
                    Response = ex.Message,
                    RequiresAuth = true
                });
            }

            // Probar endpoint de información de usuario
            try
            {
                var userInfoResponse = await httpClient.GetAsync("/api/secure/user-info");
                var userInfoContent = await userInfoResponse.Content.ReadAsStringAsync();
                results.Add(new ApiTestResult
                {
                    Endpoint = "/api/secure/user-info",
                    StatusCode = (int)userInfoResponse.StatusCode,
                    Success = userInfoResponse.IsSuccessStatusCode,
                    Response = userInfoContent,
                    RequiresAuth = true
                });
            }
            catch (Exception ex)
            {
                results.Add(new ApiTestResult
                {
                    Endpoint = "/api/secure/user-info",
                    StatusCode = 0,
                    Success = false,
                    Response = ex.Message,
                    RequiresAuth = true
                });
            }

            var model = new ApiTestViewModel
            {
                Results = results,
                HasAccessToken = !string.IsNullOrEmpty(accessToken),
                ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "No configurado"
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al probar la API");
            var model = new ApiTestViewModel
            {
                Results = new List<ApiTestResult>
                {
                    new ApiTestResult
                    {
                        Endpoint = "Error general",
                        Success = false,
                        Response = ex.Message,
                        RequiresAuth = false
                    }
                },
                HasAccessToken = false,
                ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "No configurado"
            };
            return View(model);
        }
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
