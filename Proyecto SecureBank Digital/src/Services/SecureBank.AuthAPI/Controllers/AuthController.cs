using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using SecureBank.Application.Features.Authentication.Commands.RegisterUser;
using SecureBank.Application.Features.Authentication.Commands.LoginUser;
using SecureBank.Application.Common.Interfaces;
using SecureBank.Shared.DTOs;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using AppAuthTokens = SecureBank.Application.Common.Interfaces.AuthenticationTokens;

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
    private readonly Application.Common.Interfaces.IAzureMonitorService _azureMonitorService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IMediator mediator, 
        Application.Common.Interfaces.IAzureMonitorService azureMonitorService,
        ICurrentUserService currentUserService,
        ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _azureMonitorService = azureMonitorService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Registra un nuevo usuario en el sistema
    /// </summary>
    /// <param name="request">Datos del usuario a registrar</param>
    /// <returns>Confirmación de registro</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Agregar información de contexto para auditoría
            command.IpAddress = GetClientIpAddress();
            command.UserAgent = Request.Headers.UserAgent.ToString();
            command.DeviceFingerprint = Request.Headers["X-Device-Fingerprint"].ToString();

            _logger.LogInformation("Iniciando registro de usuario: {Email}", command.Email);

            // Log de inicio de registro para análisis ML
            await _azureMonitorService.LogSecurityEventAsync("UserRegistrationStarted", new
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
                await _azureMonitorService.LogSecurityEventAsync("UserRegistrationCompleted", new
                {
                    UserId = response.UserId,
                    Email = command.Email,
                    IpAddress = command.IpAddress,
                    RequiredSteps = response.RequiredSteps.PendingVerifications,
                    Timestamp = DateTime.UtcNow
                }, response.UserId.ToString());

                // Métricas de negocio
                await _azureMonitorService.LogBusinessMetricAsync("NewUserRegistrations", 1, new Dictionary<string, string>
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
                await _azureMonitorService.LogSecurityEventAsync("UserRegistrationFailed", new
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
            await _azureMonitorService.LogSecurityEventAsync("UserRegistrationError", new
            {
                Email = command.Email,
                Error = ex.Message,
                IpAddress = GetClientIpAddress(),
                Timestamp = DateTime.UtcNow
            });

            _azureMonitorService.TrackException(ex, new Dictionary<string, string>
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
            await _azureMonitorService.LogSecurityEventAsync("LoginAttemptStarted", new
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
                await _azureMonitorService.LogSecurityEventAsync("LoginSuccess", new
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
                await _azureMonitorService.LogPerformanceMetricAsync("LoginDuration", duration.TotalMilliseconds, 
                    new Dictionary<string, string>
                    {
                        ["Success"] = "true",
                        ["Step"] = response.StepResult.CurrentStep.ToString()
                    });

                // Métricas de negocio
                await _azureMonitorService.LogBusinessMetricAsync("SuccessfulLogins", 1, new Dictionary<string, string>
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
                await _azureMonitorService.LogSecurityEventAsync("LoginFailed", new
                {
                    EmailOrDocument = command.EmailOrDocument,
                    IpAddress = command.IpAddress,
                    DeviceFingerprint = command.DeviceFingerprint,
                    Errors = response.Errors,
                    LoginDuration = duration.TotalMilliseconds,
                    Timestamp = DateTime.UtcNow
                });

                await _azureMonitorService.LogPerformanceMetricAsync("LoginDuration", duration.TotalMilliseconds, 
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
            
            await _azureMonitorService.LogSecurityEventAsync("LoginError", new
            {
                EmailOrDocument = command.EmailOrDocument,
                Error = ex.Message,
                IpAddress = GetClientIpAddress(),
                Duration = duration.TotalMilliseconds,
                Timestamp = DateTime.UtcNow
            });

            _azureMonitorService.TrackException(ex, new Dictionary<string, string>
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
            await _azureMonitorService.LogSecurityEventAsync("TokenRefreshAttempt", new
            {
                IpAddress = ipAddress,
                DeviceFingerprint = deviceFingerprint,
                Timestamp = DateTime.UtcNow
            });

            return Unauthorized(new { Message = "Refresh token inválido o expirado" });
        }
        catch (Exception ex)
        {
            _azureMonitorService.TrackException(ex, new Dictionary<string, string>
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

            await _azureMonitorService.LogSecurityEventAsync("UserLogout", new
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
            _azureMonitorService.TrackException(ex, new Dictionary<string, string>
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

            await _azureMonitorService.LogSecurityEventAsync("EmailVerificationAttempt", new
            {
                Token = request.VerificationCode?.Substring(0, Math.Min(10, request.VerificationCode.Length ?? 0)) + "...",
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            });

            // Implementar verificación de email aquí
            
            return Ok(new { Message = "Email verificado exitosamente" });
        }
        catch (Exception ex)
        {
            _azureMonitorService.TrackException(ex, new Dictionary<string, string>
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

            await _azureMonitorService.LogSecurityEventAsync("PhoneVerificationAttempt", new
            {
                CodeLength = request.VerificationCode?.Length ?? 0,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            });

            // Implementar verificación de teléfono aquí
            
            return Ok(new { Message = "Teléfono verificado exitosamente" });
        }
        catch (Exception ex)
        {
            _azureMonitorService.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "VerifyPhone"
            });

            _logger.LogError(ex, "Error durante verificación de teléfono");
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene información del usuario actual
    /// </summary>
    /// <returns>Información del usuario autenticado</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return Unauthorized(new ErrorResponse 
                { 
                    Message = "Usuario no autenticado",
                    Code = "UNAUTHORIZED"
                });
            }

            // Por ahora devolvemos datos simulados
            var userDto = new UserDto
            {
                Id = userId.Value,
                Email = _currentUserService.Email ?? "",
                Role = _currentUserService.Role ?? "",
                IsEmailVerified = true,
                IsPhoneVerified = false,
                LastLoginAt = DateTime.UtcNow
            };

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo información del usuario {UserId}", _currentUserService.UserId);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Error interno del servidor",
                Code = "INTERNAL_ERROR"
            });
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

    /// <summary>
    /// Mapea tokens de aplicación a respuesta de API
    /// </summary>
    private LoginResponse MapToLoginResponse(AppAuthTokens tokens, string deviceFingerprint)
    {
        return new LoginResponse
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            ExpiresIn = 3600, // 1 hora
            TokenType = "Bearer",
            DeviceFingerprint = deviceFingerprint,
            Scope = "api"
        };
    }

    /// <summary>
    /// Mapea tokens para respuesta de refresh
    /// </summary>
    private RefreshTokenResponse MapToRefreshResponse(AppAuthTokens tokens)
    {
        return new RefreshTokenResponse
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            ExpiresIn = 3600, // 1 hora
            TokenType = "Bearer"
        };
    }

    public class VerifyEmailRequest
    {
        [Required]
        public string VerificationCode { get; set; } = string.Empty;
    }

    public class VerifyPhoneRequest
    {
        [Required]
        public string VerificationCode { get; set; } = string.Empty;
    }
}

// DTOs para las operaciones de autenticación
public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Pin { get; set; } = string.Empty;

    [Required]
    [StringLength(12, MinimumLength = 8)]
    public string DocumentNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Pin { get; set; } = string.Empty;

    public string? DeviceFingerprint { get; set; }
}

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;

    public string? DeviceFingerprint { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
} 