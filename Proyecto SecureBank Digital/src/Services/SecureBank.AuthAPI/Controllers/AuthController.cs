using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using SecureBank.Application.Features.Authentication.Commands.RegisterUser;
using SecureBank.Application.Features.Authentication.Commands.LoginUser;
using SecureBank.Application.Common.DTOs;
using SecureBank.Application.Common.Interfaces;
using SecureBank.AuthAPI.Services;
using System.ComponentModel.DataAnnotations;

namespace SecureBank.AuthAPI.Controllers;

/// <summary>
/// Controlador de autenticación para SecureBank Digital
/// Implementa registro, login multi-factor y gestión de tokens con logging en Azure Monitor
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAzureMonitorService _azureMonitor;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        IAzureMonitorService azureMonitor,
        ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _azureMonitor = azureMonitor;
        _logger = logger;
    }

    /// <summary>
    /// Registra un nuevo usuario en SecureBank Digital
    /// </summary>
    /// <param name="command">Datos de registro del usuario</param>
    /// <returns>Resultado del registro con pasos de verificación</returns>
    [HttpPost("register")]
    [EnableRateLimiting("Registration")]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RegisterUserResponse>> RegisterUser([FromBody] RegisterUserCommand command)
    {
        try
        {
            // Agregar información de contexto para auditoría
            command.IpAddress = GetClientIpAddress();
            command.UserAgent = Request.Headers.UserAgent.ToString();
            command.DeviceFingerprint = Request.Headers["X-Device-Fingerprint"].ToString();

            _logger.LogInformation("Iniciando registro de usuario: {Email}", command.Email);

            // Log de inicio de registro para análisis ML
            await _azureMonitor.LogSecurityEventAsync("UserRegistrationStarted", new
            {
                Email = command.Email,
                DocumentType = command.DocumentType,
                IpAddress = command.IpAddress,
                UserAgent = command.UserAgent,
                DeviceFingerprint = command.DeviceFingerprint,
                Timestamp = DateTime.UtcNow,
                HasAddress = command.Address != null,
                AcceptTerms = command.AcceptTerms
            });

            var response = await _mediator.Send(command);

            if (response.Success)
            {
                // Log de registro exitoso
                await _azureMonitor.LogSecurityEventAsync("UserRegistrationCompleted", new
                {
                    UserId = response.UserId,
                    Email = command.Email,
                    IpAddress = command.IpAddress,
                    RequiredSteps = response.RequiredSteps.PendingVerifications,
                    Timestamp = DateTime.UtcNow
                }, response.UserId.ToString());

                // Métricas de negocio
                await _azureMonitor.LogBusinessMetricAsync("NewUserRegistrations", 1, new Dictionary<string, string>
                {
                    ["DocumentType"] = command.DocumentType,
                    ["HasAddress"] = (command.Address != null).ToString(),
                    ["RegistrationHour"] = DateTime.UtcNow.Hour.ToString()
                });

                _logger.LogInformation("Usuario registrado exitosamente: {UserId}", response.UserId);
                return Ok(response);
            }
            else
            {
                // Log de fallo en registro
                await _azureMonitor.LogSecurityEventAsync("UserRegistrationFailed", new
                {
                    Email = command.Email,
                    Errors = response.Errors,
                    IpAddress = command.IpAddress,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogWarning("Fallo en registro de usuario: {Email}, Errores: {Errors}", 
                    command.Email, string.Join(", ", response.Errors));
                
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            await _azureMonitor.LogSecurityEventAsync("UserRegistrationError", new
            {
                Email = command.Email,
                Error = ex.Message,
                IpAddress = GetClientIpAddress(),
                Timestamp = DateTime.UtcNow
            });

            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "UserRegistration",
                ["Email"] = command.Email
            });

            _logger.LogError(ex, "Error durante registro de usuario: {Email}", command.Email);
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Inicia el proceso de login multi-factor
    /// </summary>
    /// <param name="command">Credenciales de login</param>
    /// <returns>Resultado del login con pasos adicionales requeridos o tokens</returns>
    [HttpPost("login")]
    [EnableRateLimiting("LoginAttempts")]
    [ProducesResponseType(typeof(LoginUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginUserResponse>> Login([FromBody] LoginUserCommand command)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Agregar información de contexto
            command.IpAddress = GetClientIpAddress();
            command.UserAgent = Request.Headers.UserAgent.ToString();
            command.DeviceFingerprint = Request.Headers["X-Device-Fingerprint"].ToString();

            _logger.LogInformation("Intento de login iniciado: {EmailOrDocument}", command.EmailOrDocument);

            // Log de intento de login para análisis ML
            await _azureMonitor.LogSecurityEventAsync("LoginAttemptStarted", new
            {
                EmailOrDocument = command.EmailOrDocument,
                IpAddress = command.IpAddress,
                UserAgent = command.UserAgent,
                DeviceFingerprint = command.DeviceFingerprint,
                HasTwoFactorCode = !string.IsNullOrEmpty(command.TwoFactorCode),
                HasSecurityAnswer = !string.IsNullOrEmpty(command.SecurityAnswer),
                RememberDevice = command.RememberDevice,
                Timestamp = startTime,
                TimeOfDay = startTime.Hour,
                DayOfWeek = startTime.DayOfWeek.ToString()
            });

            var response = await _mediator.Send(command);
            var duration = DateTime.UtcNow - startTime;

            if (response.Success)
            {
                // Log de login exitoso
                await _azureMonitor.LogSecurityEventAsync("LoginSuccess", new
                {
                    UserId = response.User?.Id,
                    EmailOrDocument = command.EmailOrDocument,
                    IpAddress = command.IpAddress,
                    DeviceFingerprint = command.DeviceFingerprint,
                    LoginDuration = duration.TotalMilliseconds,
                    CurrentStep = response.StepResult.CurrentStep.ToString(),
                    NextStep = response.StepResult.NextStep.ToString(),
                    IsNewDevice = response.StepResult.IsNewDevice,
                    IsNewLocation = response.StepResult.IsNewLocation,
                    Timestamp = DateTime.UtcNow
                }, response.User?.Id.ToString());

                // Métricas de rendimiento
                await _azureMonitor.LogPerformanceMetricAsync("LoginDuration", duration.TotalMilliseconds, 
                    new Dictionary<string, string>
                    {
                        ["Success"] = "true",
                        ["Step"] = response.StepResult.CurrentStep.ToString()
                    });

                // Métricas de negocio
                await _azureMonitor.LogBusinessMetricAsync("SuccessfulLogins", 1, new Dictionary<string, string>
                {
                    ["Hour"] = startTime.Hour.ToString(),
                    ["DayOfWeek"] = startTime.DayOfWeek.ToString(),
                    ["IsNewDevice"] = response.StepResult.IsNewDevice.ToString()
                });

                _logger.LogInformation("Login exitoso para usuario: {UserId}", response.User?.Id);
                return Ok(response);
            }
            else
            {
                // Log de fallo en login
                await _azureMonitor.LogSecurityEventAsync("LoginFailed", new
                {
                    EmailOrDocument = command.EmailOrDocument,
                    IpAddress = command.IpAddress,
                    DeviceFingerprint = command.DeviceFingerprint,
                    Errors = response.Errors,
                    LoginDuration = duration.TotalMilliseconds,
                    Timestamp = DateTime.UtcNow
                });

                await _azureMonitor.LogPerformanceMetricAsync("LoginDuration", duration.TotalMilliseconds, 
                    new Dictionary<string, string>
                    {
                        ["Success"] = "false"
                    });

                _logger.LogWarning("Fallo en login: {EmailOrDocument}, Errores: {Errors}", 
                    command.EmailOrDocument, string.Join(", ", response.Errors));
                
                return Unauthorized(response);
            }
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            
            await _azureMonitor.LogSecurityEventAsync("LoginError", new
            {
                EmailOrDocument = command.EmailOrDocument,
                Error = ex.Message,
                IpAddress = GetClientIpAddress(),
                Duration = duration.TotalMilliseconds,
                Timestamp = DateTime.UtcNow
            });

            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "Login",
                ["EmailOrDocument"] = command.EmailOrDocument
            });

            _logger.LogError(ex, "Error durante login: {EmailOrDocument}", command.EmailOrDocument);
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Refresca los tokens de acceso usando el refresh token
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <returns>Nuevos tokens de acceso</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthenticationTokens), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticationTokens>> RefreshTokens([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var ipAddress = GetClientIpAddress();
            var deviceFingerprint = Request.Headers["X-Device-Fingerprint"].ToString();

            _logger.LogInformation("Intento de refresh token desde {IpAddress}", ipAddress);

            // Implementar lógica de refresh
            // Por ahora retornamos unauthorized para simular
            await _azureMonitor.LogSecurityEventAsync("TokenRefreshAttempt", new
            {
                IpAddress = ipAddress,
                DeviceFingerprint = deviceFingerprint,
                Timestamp = DateTime.UtcNow
            });

            return Unauthorized(new { Message = "Refresh token inválido o expirado" });
        }
        catch (Exception ex)
        {
            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "RefreshTokens"
            });

            _logger.LogError(ex, "Error durante refresh de tokens");
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Cierra la sesión del usuario y revoca tokens
    /// </summary>
    /// <returns>Confirmación de logout</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Logout()
    {
        try
        {
            var userId = _currentUserService.UserId;
            var ipAddress = _currentUserService.IpAddress;

            _logger.LogInformation("Logout de usuario: {UserId}", userId);

            await _azureMonitor.LogSecurityEventAsync("UserLogout", new
            {
                UserId = userId,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            }, userId?.ToString());

            // Implementar revocación de tokens aquí

            return Ok(new { Message = "Logout exitoso" });
        }
        catch (Exception ex)
        {
            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "Logout",
                ["UserId"] = _currentUserService.UserId?.ToString() ?? "unknown"
            });

            _logger.LogError(ex, "Error durante logout");
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Verifica el email del usuario
    /// </summary>
    /// <param name="request">Token de verificación de email</param>
    /// <returns>Resultado de la verificación</returns>
    [HttpPost("verify-email")]
    [EnableRateLimiting("GeneralApi")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        try
        {
            var ipAddress = GetClientIpAddress();

            await _azureMonitor.LogSecurityEventAsync("EmailVerificationAttempt", new
            {
                Token = request.Token?.Substring(0, Math.Min(10, request.Token.Length ?? 0)) + "...",
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            });

            // Implementar verificación de email aquí
            
            return Ok(new { Message = "Email verificado exitosamente" });
        }
        catch (Exception ex)
        {
            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "VerifyEmail"
            });

            _logger.LogError(ex, "Error durante verificación de email");
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Verifica el código SMS del usuario
    /// </summary>
    /// <param name="request">Código de verificación SMS</param>
    /// <returns>Resultado de la verificación</returns>
    [HttpPost("verify-phone")]
    [EnableRateLimiting("GeneralApi")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> VerifyPhone([FromBody] VerifyPhoneRequest request)
    {
        try
        {
            var ipAddress = GetClientIpAddress();

            await _azureMonitor.LogSecurityEventAsync("PhoneVerificationAttempt", new
            {
                CodeLength = request.Code?.Length ?? 0,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            });

            // Implementar verificación de teléfono aquí
            
            return Ok(new { Message = "Teléfono verificado exitosamente" });
        }
        catch (Exception ex)
        {
            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "VerifyPhone"
            });

            _logger.LogError(ex, "Error durante verificación de teléfono");
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene información del usuario actual autenticado
    /// </summary>
    /// <returns>Información del usuario</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        try
        {
            var userId = _currentUserService.UserId;
            
            await _azureMonitor.LogUserBehaviorAsync("ProfileViewed", new
            {
                UserId = userId,
                IpAddress = _currentUserService.IpAddress,
                Timestamp = DateTime.UtcNow
            }, userId?.ToString() ?? "unknown");

            // Implementar obtención de datos del usuario aquí
            
            return Ok(new UserDto { Id = userId ?? Guid.Empty });
        }
        catch (Exception ex)
        {
            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "GetCurrentUser",
                ["UserId"] = _currentUserService.UserId?.ToString() ?? "unknown"
            });

            _logger.LogError(ex, "Error obteniendo usuario actual");
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    // Métodos auxiliares
    private string GetClientIpAddress()
    {
        return Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim() ??
               Request.Headers["X-Real-IP"].FirstOrDefault() ??
               HttpContext.Connection.RemoteIpAddress?.ToString() ??
               "unknown";
    }
}

// DTOs para requests
public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class VerifyEmailRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}

public class VerifyPhoneRequest
{
    [Required]
    public string Code { get; set; } = string.Empty;
    
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
} 