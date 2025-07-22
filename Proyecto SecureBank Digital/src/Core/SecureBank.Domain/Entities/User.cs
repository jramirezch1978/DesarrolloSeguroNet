using SecureBank.Domain.Enums;
using SecureBank.Domain.ValueObjects;

namespace SecureBank.Domain.Entities;

/// <summary>
/// Entidad principal del usuario en SecureBank Digital
/// Implementa "Security First" con validaciones robustas
/// </summary>
public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string DocumentNumber { get; private set; } = string.Empty;
    public string DocumentType { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string HashedPin { get; private set; } = string.Empty;
    public string SecurityQuestion { get; private set; } = string.Empty;
    public string SecurityAnswerHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime? LastPasswordChangeAt { get; private set; }
    public string? ProfileImageUrl { get; private set; }
    public Address? Address { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public bool IsPhoneVerified { get; private set; }
    public bool IsTwoFactorEnabled { get; private set; }
    public string? TwoFactorSecret { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? AccountLockedUntil { get; private set; }
    public string CreatedFromIpAddress { get; private set; } = string.Empty;
    public string? LastLoginIpAddress { get; private set; }
    public string? DeviceFingerprint { get; private set; }
    public bool RequiresPasswordChange { get; private set; }
    public DateTime? TermsAcceptedAt { get; private set; }
    public string? TermsAcceptedIpAddress { get; private set; }

    // Colecciones de navegación
    public virtual ICollection<Account> Accounts { get; private set; } = new List<Account>();
    public virtual ICollection<UserDevice> TrustedDevices { get; private set; } = new List<UserDevice>();
    public virtual ICollection<AuditLog> AuditLogs { get; private set; } = new List<AuditLog>();
    public virtual ICollection<LoginAttempt> LoginAttempts { get; private set; } = new List<LoginAttempt>();

    // Constructor privado para EF Core
    private User() { }

    /// <summary>
    /// Constructor para crear un nuevo usuario con validaciones de seguridad
    /// </summary>
    public User(
        string email,
        string documentNumber,
        string documentType,
        string firstName,
        string lastName,
        string phoneNumber,
        string hashedPin,
        string securityQuestion,
        string securityAnswerHash,
        string createdFromIpAddress,
        Address? address = null)
    {
        Id = Guid.NewGuid();
        Email = ValidateEmail(email);
        DocumentNumber = ValidateDocumentNumber(documentNumber);
        DocumentType = ValidateDocumentType(documentType);
        FirstName = ValidateName(firstName, nameof(firstName));
        LastName = ValidateName(lastName, nameof(lastName));
        PhoneNumber = ValidatePhoneNumber(phoneNumber);
        HashedPin = ValidateHashedPin(hashedPin);
        SecurityQuestion = ValidateSecurityQuestion(securityQuestion);
        SecurityAnswerHash = ValidateSecurityAnswerHash(securityAnswerHash);
        CreatedFromIpAddress = ValidateIpAddress(createdFromIpAddress);
        
        Role = UserRole.Customer;
        Status = UserStatus.PendingVerification;
        CreatedAt = DateTime.UtcNow;
        Address = address;
        IsEmailVerified = false;
        IsPhoneVerified = false;
        IsTwoFactorEnabled = false;
        FailedLoginAttempts = 0;
        RequiresPasswordChange = false;
    }

    /// <summary>
    /// Verifica el email del usuario
    /// </summary>
    public void VerifyEmail()
    {
        IsEmailVerified = true;
        
        // Si tanto email como teléfono están verificados, activar cuenta
        if (IsPhoneVerified && Status == UserStatus.PendingVerification)
        {
            Status = UserStatus.Active;
        }
    }

    /// <summary>
    /// Verifica el teléfono del usuario
    /// </summary>
    public void VerifyPhone()
    {
        IsPhoneVerified = true;
        
        // Si tanto email como teléfono están verificados, activar cuenta
        if (IsEmailVerified && Status == UserStatus.PendingVerification)
        {
            Status = UserStatus.Active;
        }
    }

    /// <summary>
    /// Habilita la autenticación de dos factores
    /// </summary>
    public void EnableTwoFactor(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException("El secreto de 2FA no puede estar vacío", nameof(secret));

        TwoFactorSecret = secret;
        IsTwoFactorEnabled = true;
    }

    /// <summary>
    /// Deshabilita la autenticación de dos factores
    /// </summary>
    public void DisableTwoFactor()
    {
        TwoFactorSecret = null;
        IsTwoFactorEnabled = false;
    }

    /// <summary>
    /// Actualiza el PIN del usuario con validaciones de seguridad
    /// </summary>
    public void UpdatePin(string newHashedPin)
    {
        HashedPin = ValidateHashedPin(newHashedPin);
        LastPasswordChangeAt = DateTime.UtcNow;
        RequiresPasswordChange = false;
    }

    /// <summary>
    /// Registra un intento de login exitoso
    /// </summary>
    public void RecordSuccessfulLogin(string ipAddress, string deviceFingerprint)
    {
        LastLoginAt = DateTime.UtcNow;
        LastLoginIpAddress = ValidateIpAddress(ipAddress);
        DeviceFingerprint = deviceFingerprint;
        FailedLoginAttempts = 0;
        AccountLockedUntil = null;

        if (Status == UserStatus.Locked)
        {
            Status = UserStatus.Active;
        }
    }

    /// <summary>
    /// Registra un intento de login fallido
    /// </summary>
    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        
        // Bloquear cuenta después de 5 intentos fallidos
        if (FailedLoginAttempts >= 5)
        {
            Status = UserStatus.Locked;
            AccountLockedUntil = DateTime.UtcNow.AddMinutes(30);
        }
    }

    /// <summary>
    /// Bloquea la cuenta del usuario
    /// </summary>
    public void LockAccount(DateTime? lockUntil = null)
    {
        Status = UserStatus.Locked;
        AccountLockedUntil = lockUntil ?? DateTime.UtcNow.AddMinutes(30);
    }

    /// <summary>
    /// Desbloquea la cuenta del usuario
    /// </summary>
    public void UnlockAccount()
    {
        Status = UserStatus.Active;
        AccountLockedUntil = null;
        FailedLoginAttempts = 0;
    }

    /// <summary>
    /// Acepta los términos y condiciones
    /// </summary>
    public void AcceptTerms(string ipAddress)
    {
        TermsAcceptedAt = DateTime.UtcNow;
        TermsAcceptedIpAddress = ValidateIpAddress(ipAddress);
    }

    /// <summary>
    /// Verifica si la cuenta está bloqueada
    /// </summary>
    public bool IsAccountLocked()
    {
        return Status == UserStatus.Locked && 
               AccountLockedUntil.HasValue && 
               AccountLockedUntil.Value > DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza la dirección del usuario
    /// </summary>
    public void UpdateAddress(Address address)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
    }

    /// <summary>
    /// Cambia el rol del usuario (solo para administradores)
    /// </summary>
    public void ChangeRole(UserRole newRole)
    {
        Role = newRole;
    }

    // Métodos de validación privados
    private static string ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("El email es obligatorio", nameof(email));
        
        if (!email.Contains('@') || email.Length > 254)
            throw new ArgumentException("El email no es válido", nameof(email));
        
        return email.ToLowerInvariant().Trim();
    }

    private static string ValidateDocumentNumber(string documentNumber)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
            throw new ArgumentException("El número de documento es obligatorio", nameof(documentNumber));
        
        if (documentNumber.Length < 8 || documentNumber.Length > 20)
            throw new ArgumentException("El número de documento debe tener entre 8 y 20 caracteres", nameof(documentNumber));
        
        return documentNumber.Trim();
    }

    private static string ValidateDocumentType(string documentType)
    {
        var validTypes = new[] { "DNI", "CE", "PASSPORT", "RUC" };
        
        if (string.IsNullOrWhiteSpace(documentType))
            throw new ArgumentException("El tipo de documento es obligatorio", nameof(documentType));
        
        if (!validTypes.Contains(documentType.ToUpperInvariant()))
            throw new ArgumentException("Tipo de documento no válido", nameof(documentType));
        
        return documentType.ToUpperInvariant();
    }

    private static string ValidateName(string name, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"El {fieldName} es obligatorio", fieldName);
        
        if (name.Length < 2 || name.Length > 50)
            throw new ArgumentException($"El {fieldName} debe tener entre 2 y 50 caracteres", fieldName);
        
        return name.Trim();
    }

    private static string ValidatePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("El número de teléfono es obligatorio", nameof(phoneNumber));
        
        // Remover espacios y caracteres especiales para validación
        var cleanPhone = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        
        if (cleanPhone.Length < 9 || cleanPhone.Length > 15)
            throw new ArgumentException("El número de teléfono debe tener entre 9 y 15 dígitos", nameof(phoneNumber));
        
        return phoneNumber.Trim();
    }

    private static string ValidateHashedPin(string hashedPin)
    {
        if (string.IsNullOrWhiteSpace(hashedPin))
            throw new ArgumentException("El PIN hasheado es obligatorio", nameof(hashedPin));
        
        if (hashedPin.Length < 60) // BCrypt hash mínimo
            throw new ArgumentException("El PIN hasheado no es válido", nameof(hashedPin));
        
        return hashedPin;
    }

    private static string ValidateSecurityQuestion(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("La pregunta de seguridad es obligatoria", nameof(question));
        
        if (question.Length < 10 || question.Length > 200)
            throw new ArgumentException("La pregunta de seguridad debe tener entre 10 y 200 caracteres", nameof(question));
        
        return question.Trim();
    }

    private static string ValidateSecurityAnswerHash(string answerHash)
    {
        if (string.IsNullOrWhiteSpace(answerHash))
            throw new ArgumentException("La respuesta de seguridad hasheada es obligatoria", nameof(answerHash));
        
        if (answerHash.Length < 60) // BCrypt hash mínimo
            throw new ArgumentException("La respuesta de seguridad hasheada no es válida", nameof(answerHash));
        
        return answerHash;
    }

    private static string ValidateIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("La dirección IP es obligatoria", nameof(ipAddress));
        
        return ipAddress.Trim();
    }
} 