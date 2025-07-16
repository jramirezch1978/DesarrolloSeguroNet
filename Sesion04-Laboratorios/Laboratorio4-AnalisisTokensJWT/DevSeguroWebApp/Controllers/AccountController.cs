using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Security.Claims;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace DevSeguroWebApp.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        [AllowAnonymous]
        public IActionResult SignIn()
        {
            var redirectUrl = Url.Action(nameof(HomeController.Index), "Home");
            return Challenge(
                new AuthenticationProperties { RedirectUri = redirectUrl },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet]
        [Authorize]
        public new IActionResult SignOut()
        {
            return SignOut(
                new AuthenticationProperties 
                { 
                    RedirectUri = Url.Action(nameof(SignedOut), "Account") 
                },
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SignedOut()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult Profile()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult TokenInfo()
        {
            // Para la vista principal que cargará la información via JavaScript
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetTokenInfo()
        {
            try
            {
                // Obtener información del usuario
                var userInfo = new
                {
                    Name = User.Identity?.Name,
                    IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                    AuthenticationType = User.Identity?.AuthenticationType,
                    Claims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList()
                };

                // Intentar obtener tokens desde el contexto
                var accessToken = await HttpContext.GetTokenAsync("access_token");
                var idToken = await HttpContext.GetTokenAsync("id_token");
                var refreshToken = await HttpContext.GetTokenAsync("refresh_token");

                var tokenInfo = new
                {
                    AccessTokenPresent = !string.IsNullOrEmpty(accessToken),
                    IdTokenPresent = !string.IsNullOrEmpty(idToken),
                    RefreshTokenPresent = !string.IsNullOrEmpty(refreshToken)
                };

                return Json(new
                {
                    User = userInfo,
                    Tokens = tokenInfo
                });
            }
            catch (Exception ex)
            {
                return Json(new 
                { 
                    Error = "Error al obtener información de tokens", 
                    Details = ex.Message 
                });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetFullIdToken()
        {
            try
            {
                var accessToken = await HttpContext.GetTokenAsync("access_token");
                var idToken = await HttpContext.GetTokenAsync("id_token");
                var refreshToken = await HttpContext.GetTokenAsync("refresh_token");
                
                return Json(new
                {
                    accessToken = accessToken,
                    idToken = idToken,
                    refreshToken = refreshToken,
                    hasAccessToken = !string.IsNullOrEmpty(accessToken),
                    hasIdToken = !string.IsNullOrEmpty(idToken),
                    hasRefreshToken = !string.IsNullOrEmpty(refreshToken),
                    tokenInfo = "Tokens JWT reales obtenidos según especificación del laboratorio",
                    authenticationScheme = User.Identity?.AuthenticationType,
                    isAuthenticated = User.Identity?.IsAuthenticated ?? false,
                    user = new
                    {
                        name = User.Identity?.Name,
                        claims = User.Claims.Select(c => new { type = c.Type, value = c.Value }).ToList(),
                        isAuthenticated = User.Identity?.IsAuthenticated ?? false,
                        authenticationType = User.Identity?.AuthenticationType
                    },
                    tokens = new
                    {
                        accessTokenPresent = !string.IsNullOrEmpty(accessToken),
                        idTokenPresent = !string.IsNullOrEmpty(idToken),
                        refreshTokenPresent = !string.IsNullOrEmpty(refreshToken)
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    error = "Error al obtener tokens", 
                    details = ex.Message,
                    note = "Verificar configuración de SaveTokens en Program.cs"
                });
            }
        }

        [HttpGet]
        [Authorize]
        public IActionResult TestAuthentication()
        {
            var authenticationInfo = new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                UserName = User.Identity?.Name,
                AuthenticationType = User.Identity?.AuthenticationType,
                ClaimsCount = User.Claims.Count(),
                ServerTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                UserAgent = Request.Headers["User-Agent"].ToString(),
                RemoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                RequestScheme = Request.Scheme,
                RequestHost = Request.Host.Value,
                RequestPath = Request.Path.Value,
                HasSessionId = !string.IsNullOrEmpty(HttpContext.Session.Id),
                SessionId = HttpContext.Session.Id,
                CookieCount = Request.Cookies.Count,
                HeaderCount = Request.Headers.Count
            };

            return View(authenticationInfo);
        }

        [HttpGet]
        [Authorize]
        public IActionResult ClaimsAnalysis()
        {
            var claimsAnalysis = new
            {
                TotalClaims = User.Claims.Count(),
                ClaimsByType = User.Claims.GroupBy(c => c.Type).Select(g => new { Type = g.Key, Count = g.Count() }),
                UniqueClaims = User.Claims.Select(c => c.Type).Distinct().Count(),
                SecurityClaims = User.Claims.Where(c => c.Type.Contains("security") || c.Type.Contains("auth")).Select(c => new { Type = c.Type, Value = c.Value }).ToList(),
                PersonalClaims = User.Claims.Where(c => c.Type.Contains("name") || c.Type.Contains("email") || c.Type.Contains("given") || c.Type.Contains("family")).Select(c => new { Type = c.Type, Value = c.Value }).ToList(),
                RoleClaims = User.Claims.Where(c => c.Type.Contains("role") || c.Type.Contains("group")).Select(c => new { Type = c.Type, Value = c.Value }).ToList(),
                AzureAdClaims = User.Claims.Where(c => c.Type.StartsWith("http://schemas.microsoft.com/") || c.Type.StartsWith("http://schemas.xmlsoap.org/")).Select(c => new { Type = c.Type, Value = c.Value }).ToList(),
                CustomClaims = User.Claims.Where(c => !c.Type.StartsWith("http://")).Select(c => new { Type = c.Type, Value = c.Value }).ToList()
            };

            return View(claimsAnalysis);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
} 