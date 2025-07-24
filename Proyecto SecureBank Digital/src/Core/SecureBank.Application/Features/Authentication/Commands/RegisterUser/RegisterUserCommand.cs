using MediatR;
using SecureBank.Application.Common.DTOs;
using SecureBank.Domain.ValueObjects;

namespace SecureBank.Application.Features.Authentication.Commands.RegisterUser;

/// <summary>
/// Command para registrar un nuevo usuario en SecureBank Digital
/// Implementa proceso de registro multi-paso con validaciones de seguridad
/// </summary>
public class RegisterUserCommand : IRequest<RegisterUserResponse>
{
    public string Email { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
    public string ConfirmPin { get; set; } = string.Empty;
    public string SecurityQuestion { get; set; } = string.Empty;
    public string SecurityAnswer { get; set; } = string.Empty;
    public AddressDto? Address { get; set; }
    public bool AcceptTerms { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string? DeviceFingerprint { get; set; }
    public string? ReferralCode { get; set; }
}

/// <summary>
/// Respuesta del comando de registro
/// </summary>
public class RegisterUserResponse
{
    public bool Success { get; set; }
    public Guid UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public RegisterUserSteps RequiredSteps { get; set; } = new();
    public string? EmailVerificationToken { get; set; }
    public string? PhoneVerificationCode { get; set; }
    public DateTime TokenExpiresAt { get; set; }
}

/// <summary>
/// Pasos requeridos para completar el registro
/// </summary>
public class RegisterUserSteps
{
    public bool RequiresEmailVerification { get; set; } = true;
    public bool RequiresPhoneVerification { get; set; } = true;
    public bool RequiresDocumentVerification { get; set; } = true;
    public bool RequiresBiometricSetup { get; set; } = false;
    public bool RequiresTwoFactorSetup { get; set; } = false;
    public List<string> PendingVerifications { get; set; } = new();
} 