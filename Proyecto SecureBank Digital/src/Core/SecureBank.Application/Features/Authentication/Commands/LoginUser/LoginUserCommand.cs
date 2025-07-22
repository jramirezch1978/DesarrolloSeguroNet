using MediatR;
using SecureBank.Application.Common.DTOs;

namespace SecureBank.Application.Features.Authentication.Commands.LoginUser;

/// <summary>
/// Command para login de usuario en SecureBank Digital
/// Implementa proceso de login multi-factor con validaciones de seguridad
/// </summary>
public class LoginUserCommand : IRequest<LoginUserResponse>
{
    public string EmailOrDocument { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
    public string? TwoFactorCode { get; set; }
    public string? SecurityAnswer { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string? DeviceFingerprint { get; set; }
    public bool RememberDevice { get; set; } = false;
    public string? CaptchaToken { get; set; }
}

/// <summary>
/// Respuesta del comando de login
/// </summary>
public class LoginUserResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public UserDto? User { get; set; }
    public AuthenticationTokens? Tokens { get; set; }
    public LoginStepResult StepResult { get; set; } = new();
}

/// <summary>
/// Tokens de autenticación
/// </summary>
public class AuthenticationTokens
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public List<string> Scopes { get; set; } = new();
}

/// <summary>
/// Resultado del paso de login actual
/// </summary>
public class LoginStepResult
{
    public LoginStep CurrentStep { get; set; } = LoginStep.Credentials;
    public LoginStep NextStep { get; set; } = LoginStep.Complete;
    public bool RequiresTwoFactor { get; set; }
    public bool RequiresSecurityQuestion { get; set; }
    public bool RequiresCaptcha { get; set; }
    public bool RequiresDeviceTrust { get; set; }
    public bool IsNewDevice { get; set; }
    public bool IsNewLocation { get; set; }
    public string? SecurityQuestion { get; set; }
    public List<TwoFactorMethod> AvailableTwoFactorMethods { get; set; } = new();
    public string? PhoneNumberMasked { get; set; }
    public string? EmailMasked { get; set; }
    public DateTime? ChallengeExpiresAt { get; set; }
    public int RemainingAttempts { get; set; }
    public TimeSpan? LockoutDuration { get; set; }
}

/// <summary>
/// Pasos del proceso de login
/// </summary>
public enum LoginStep
{
    /// <summary>
    /// Paso 1: Validación de credenciales básicas (email/documento + PIN)
    /// </summary>
    Credentials = 1,

    /// <summary>
    /// Paso 2: Validación CAPTCHA (después de intentos fallidos)
    /// </summary>
    Captcha = 2,

    /// <summary>
    /// Paso 3: Autenticación de dos factores
    /// </summary>
    TwoFactor = 3,

    /// <summary>
    /// Paso 4: Pregunta de seguridad (dispositivos nuevos/ubicaciones sospechosas)
    /// </summary>
    SecurityQuestion = 4,

    /// <summary>
    /// Paso 5: Verificación biométrica (para operaciones de alto valor)
    /// </summary>
    Biometric = 5,

    /// <summary>
    /// Paso 6: Confirmación del dispositivo
    /// </summary>
    DeviceTrust = 6,

    /// <summary>
    /// Login completado exitosamente
    /// </summary>
    Complete = 99
}

/// <summary>
/// Métodos de autenticación de dos factores disponibles
/// </summary>
public class TwoFactorMethod
{
    public string Type { get; set; } = string.Empty; // SMS, TOTP, Email
    public string Display { get; set; } = string.Empty; // "SMS a ***-***-1234"
    public bool IsDefault { get; set; }
    public bool IsAvailable { get; set; } = true;
}

/// <summary>
/// Información adicional del resultado de login
/// </summary>
public class LoginResultInfo
{
    public bool IsFirstLogin { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginLocation { get; set; }
    public bool RequiresPasswordChange { get; set; }
    public bool RequiresProfileUpdate { get; set; }
    public List<SecurityAlert> SecurityAlerts { get; set; } = new();
    public int DaysSinceLastLogin { get; set; }
    public bool HasPendingTransactions { get; set; }
    public List<string> ImportantNotifications { get; set; } = new();
}

/// <summary>
/// Alertas de seguridad para mostrar al usuario
/// </summary>
public class SecurityAlert
{
    public string Type { get; set; } = string.Empty; // NewDevice, NewLocation, SuspiciousActivity
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Severity { get; set; } = "Info"; // Info, Warning, Critical
    public bool RequiresAction { get; set; }
} 