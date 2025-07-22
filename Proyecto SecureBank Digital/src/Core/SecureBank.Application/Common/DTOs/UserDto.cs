using SecureBank.Domain.Enums;

namespace SecureBank.Application.Common.DTOs;

/// <summary>
/// DTO para información básica del usuario
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public bool IsTwoFactorEnabled { get; set; }
    public string? ProfileImageUrl { get; set; }
    public AddressDto? Address { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
    public bool IsActive => Status == UserStatus.Active;
    public bool IsVerified => IsEmailVerified && IsPhoneVerified;
}

/// <summary>
/// DTO detallado para el perfil del usuario
/// </summary>
public class UserProfileDto : UserDto
{
    public string DocumentNumber { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public DateTime? LastPasswordChangeAt { get; set; }
    public DateTime? TermsAcceptedAt { get; set; }
    public List<AccountSummaryDto> Accounts { get; set; } = new();
    public List<UserDeviceDto> TrustedDevices { get; set; } = new();
    public SecuritySettingsDto SecuritySettings { get; set; } = new();
}

/// <summary>
/// DTO para configuraciones de seguridad del usuario
/// </summary>
public class SecuritySettingsDto
{
    public bool IsTwoFactorEnabled { get; set; }
    public bool RequiresPasswordChange { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? AccountLockedUntil { get; set; }
    public string SecurityQuestion { get; set; } = string.Empty;
    public List<string> TrustedDevices { get; set; } = new();
    public List<RecentLoginDto> RecentLogins { get; set; } = new();
}

/// <summary>
/// DTO para direcciones
/// </summary>
public class AddressDto
{
    public string Street { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string? Apartment { get; set; }
    public string District { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = "PERU";

    public string FullAddress => $"{Street} {Number}{(string.IsNullOrWhiteSpace(Apartment) ? "" : " " + Apartment)}, {District}, {Province}, {Department}";
}

/// <summary>
/// DTO para dispositivos del usuario
/// </summary>
public class UserDeviceDto
{
    public Guid Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public bool IsTrusted { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public DateTime? TrustedAt { get; set; }
    public string LastIpAddress { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int LoginCount { get; set; }
    public int SuccessfulLoginCount { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public DateTime? ExpiresAt { get; set; }
    
    public double SuccessRate => LoginCount > 0 ? (double)SuccessfulLoginCount / LoginCount * 100 : 100;
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    public string Status => IsBlocked ? "Bloqueado" : IsTrusted ? "Confiable" : "No confiable";
}

/// <summary>
/// DTO para logins recientes
/// </summary>
public class RecentLoginDto
{
    public DateTime LoginAt { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    public bool IsTrustedDevice { get; set; }
    public string? FailureReason { get; set; }
}

/// <summary>
/// DTO para resumen de cuenta
/// </summary>
public class AccountSummaryDto
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public decimal Balance { get; set; }
    public decimal AvailableBalance { get; set; }
    public string Currency { get; set; } = "PEN";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public string AccountTypeName => AccountType switch
    {
        AccountType.Savings => "Cuenta de Ahorros",
        AccountType.Checking => "Cuenta Corriente", 
        AccountType.Premium => "Cuenta Premium",
        AccountType.Business => "Cuenta Empresarial",
        _ => "Desconocido"
    };
    
    public string FormattedBalance => $"{Balance:C} {Currency}";
} 