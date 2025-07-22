namespace SecureBank.Domain.Entities;

/// <summary>
/// Entidad para dispositivos confiables de usuario en SecureBank Digital
/// Implementa device fingerprinting y gestión de confianza
/// </summary>
public class UserDevice
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string DeviceFingerprint { get; private set; } = string.Empty;
    public string DeviceName { get; private set; } = string.Empty;
    public string DeviceType { get; private set; } = string.Empty;
    public string OperatingSystem { get; private set; } = string.Empty;
    public string Browser { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;
    public bool IsTrusted { get; private set; }
    public DateTime FirstSeenAt { get; private set; }
    public DateTime LastSeenAt { get; private set; }
    public DateTime? TrustedAt { get; private set; }
    public string RegistrationIpAddress { get; private set; } = string.Empty;
    public string LastIpAddress { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public int LoginCount { get; private set; }
    public int SuccessfulLoginCount { get; private set; }
    public DateTime? LastSuccessfulLoginAt { get; private set; }
    public bool IsBlocked { get; private set; }
    public DateTime? BlockedAt { get; private set; }
    public string? BlockReason { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool RequiresRevalidation { get; private set; }
    public DateTime? LastRevalidationAt { get; private set; }
    public string? AdditionalProperties { get; private set; }

    // Navegación
    public virtual User User { get; private set; } = null!;

    // Constructor privado para EF Core
    private UserDevice() { }

    /// <summary>
    /// Constructor para registrar un nuevo dispositivo
    /// </summary>
    public UserDevice(
        Guid userId,
        string deviceFingerprint,
        string deviceName,
        string deviceType,
        string operatingSystem,
        string browser,
        string userAgent,
        string registrationIpAddress,
        string country = "Unknown",
        string city = "Unknown",
        string? additionalProperties = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        DeviceFingerprint = ValidateDeviceFingerprint(deviceFingerprint);
        DeviceName = ValidateDeviceName(deviceName);
        DeviceType = ValidateDeviceType(deviceType);
        OperatingSystem = ValidateOperatingSystem(operatingSystem);
        Browser = ValidateBrowser(browser);
        UserAgent = ValidateUserAgent(userAgent);
        RegistrationIpAddress = ValidateIpAddress(registrationIpAddress);
        LastIpAddress = RegistrationIpAddress;
        Country = ValidateLocation(country);
        City = ValidateLocation(city);
        AdditionalProperties = additionalProperties;

        // Estados iniciales
        IsTrusted = false;
        FirstSeenAt = DateTime.UtcNow;
        LastSeenAt = DateTime.UtcNow;
        LoginCount = 0;
        SuccessfulLoginCount = 0;
        IsBlocked = false;
        RequiresRevalidation = false;
        
        // Los dispositivos móviles expiran en 90 días, desktop en 180 días
        ExpiresAt = DateTime.UtcNow.AddDays(DeviceType.ToLowerInvariant().Contains("mobile") ? 90 : 180);
    }

    /// <summary>
    /// Marca el dispositivo como confiable
    /// </summary>
    public void MarkAsTrusted()
    {
        if (IsBlocked)
            throw new InvalidOperationException("No se puede confiar en un dispositivo bloqueado");

        IsTrusted = true;
        TrustedAt = DateTime.UtcNow;
        RequiresRevalidation = false;
    }

    /// <summary>
    /// Revoca la confianza del dispositivo
    /// </summary>
    public void RevokeTrust(string reason)
    {
        IsTrusted = false;
        TrustedAt = null;
        RequiresRevalidation = true;
        
        // Agregar razón a las propiedades adicionales
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var newProperty = $"TrustRevoked_{timestamp}: {reason}";
        AdditionalProperties = AdditionalProperties != null 
            ? $"{AdditionalProperties}; {newProperty}" 
            : newProperty;
    }

    /// <summary>
    /// Registra un uso del dispositivo
    /// </summary>
    public void RecordUsage(string ipAddress, string country, string city, bool isSuccessful = true)
    {
        LastSeenAt = DateTime.UtcNow;
        LastIpAddress = ValidateIpAddress(ipAddress);
        Country = ValidateLocation(country);
        City = ValidateLocation(city);
        LoginCount++;

        if (isSuccessful)
        {
            SuccessfulLoginCount++;
            LastSuccessfulLoginAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Bloquea el dispositivo
    /// </summary>
    public void Block(string reason)
    {
        IsBlocked = true;
        BlockedAt = DateTime.UtcNow;
        BlockReason = ValidateBlockReason(reason);
        
        // Si estaba confiable, revocar la confianza
        if (IsTrusted)
        {
            RevokeTrust($"Dispositivo bloqueado: {reason}");
        }
    }

    /// <summary>
    /// Desbloquea el dispositivo
    /// </summary>
    public void Unblock()
    {
        IsBlocked = false;
        BlockedAt = null;
        BlockReason = null;
        RequiresRevalidation = true;
    }

    /// <summary>
    /// Extiende la fecha de expiración
    /// </summary>
    public void ExtendExpiration(int days = 180)
    {
        if (days <= 0)
            throw new ArgumentException("Los días deben ser mayor a cero", nameof(days));

        ExpiresAt = DateTime.UtcNow.AddDays(days);
    }

    /// <summary>
    /// Marca que requiere revalidación
    /// </summary>
    public void RequireRevalidation(string reason)
    {
        RequiresRevalidation = true;
        
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var newProperty = $"RevalidationRequired_{timestamp}: {reason}";
        AdditionalProperties = AdditionalProperties != null 
            ? $"{AdditionalProperties}; {newProperty}" 
            : newProperty;
    }

    /// <summary>
    /// Completa la revalidación
    /// </summary>
    public void CompleteRevalidation()
    {
        RequiresRevalidation = false;
        LastRevalidationAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Verifica si el dispositivo está activo y válido
    /// </summary>
    public bool IsActiveAndValid()
    {
        return !IsBlocked && 
               !IsExpired() && 
               !RequiresRevalidation;
    }

    /// <summary>
    /// Verifica si el dispositivo ha expirado
    /// </summary>
    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    }

    /// <summary>
    /// Verifica si el dispositivo tiene buena reputación
    /// </summary>
    public bool HasGoodReputation()
    {
        if (LoginCount == 0)
            return true;

        var successRate = (double)SuccessfulLoginCount / LoginCount;
        return successRate >= 0.8; // Al menos 80% de intentos exitosos
    }

    /// <summary>
    /// Calcula el score de confianza del dispositivo
    /// </summary>
    public double CalculateTrustScore()
    {
        var score = 0.0;

        // Factores positivos
        if (IsTrusted) score += 40;
        if (HasGoodReputation()) score += 20;
        if (SuccessfulLoginCount >= 5) score += 15;
        if ((DateTime.UtcNow - FirstSeenAt).TotalDays >= 30) score += 15;

        // Factores negativos
        if (IsBlocked) score -= 50;
        if (RequiresRevalidation) score -= 20;
        if (IsExpired()) score -= 30;

        return Math.Max(0, Math.Min(100, score));
    }

    /// <summary>
    /// Obtiene un resumen del dispositivo
    /// </summary>
    public string GetSummary()
    {
        var status = IsBlocked ? "BLOCKED" : IsTrusted ? "TRUSTED" : "UNTRUSTED";
        var usage = $"{SuccessfulLoginCount}/{LoginCount} successful logins";
        
        return $"{DeviceName} ({DeviceType}) - {status} - {usage} - Last seen: {LastSeenAt:yyyy-MM-dd}";
    }

    /// <summary>
    /// Actualiza las propiedades del dispositivo si han cambiado
    /// </summary>
    public void UpdateProperties(
        string? newUserAgent = null,
        string? newBrowser = null,
        string? newOperatingSystem = null)
    {
        var changes = new List<string>();

        if (!string.IsNullOrWhiteSpace(newUserAgent) && newUserAgent != UserAgent)
        {
            changes.Add($"UserAgent: {UserAgent} -> {newUserAgent}");
            UserAgent = ValidateUserAgent(newUserAgent);
        }

        if (!string.IsNullOrWhiteSpace(newBrowser) && newBrowser != Browser)
        {
            changes.Add($"Browser: {Browser} -> {newBrowser}");
            Browser = ValidateBrowser(newBrowser);
        }

        if (!string.IsNullOrWhiteSpace(newOperatingSystem) && newOperatingSystem != OperatingSystem)
        {
            changes.Add($"OS: {OperatingSystem} -> {newOperatingSystem}");
            OperatingSystem = ValidateOperatingSystem(newOperatingSystem);
        }

        if (changes.Any())
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            var changeLog = $"PropertiesUpdated_{timestamp}: {string.Join(", ", changes)}";
            AdditionalProperties = AdditionalProperties != null 
                ? $"{AdditionalProperties}; {changeLog}" 
                : changeLog;
        }
    }

    // Métodos de validación privados
    private static string ValidateDeviceFingerprint(string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
            throw new ArgumentException("El fingerprint del dispositivo es obligatorio", nameof(fingerprint));

        if (fingerprint.Length < 10)
            throw new ArgumentException("El fingerprint del dispositivo debe tener al menos 10 caracteres", nameof(fingerprint));

        return fingerprint.Trim();
    }

    private static string ValidateDeviceName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del dispositivo es obligatorio", nameof(name));

        return name.Length > 100 ? name.Substring(0, 100) : name.Trim();
    }

    private static string ValidateDeviceType(string type)
    {
        var validTypes = new[] { "Desktop", "Mobile", "Tablet", "Unknown" };
        
        if (string.IsNullOrWhiteSpace(type))
            return "Unknown";

        var normalizedType = type.Trim();
        return validTypes.Contains(normalizedType) ? normalizedType : "Unknown";
    }

    private static string ValidateOperatingSystem(string os)
    {
        if (string.IsNullOrWhiteSpace(os))
            return "Unknown";

        return os.Length > 100 ? os.Substring(0, 100) : os.Trim();
    }

    private static string ValidateBrowser(string browser)
    {
        if (string.IsNullOrWhiteSpace(browser))
            return "Unknown";

        return browser.Length > 100 ? browser.Substring(0, 100) : browser.Trim();
    }

    private static string ValidateUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "Unknown";

        return userAgent.Length > 500 ? userAgent.Substring(0, 500) : userAgent.Trim();
    }

    private static string ValidateIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("La dirección IP es obligatoria", nameof(ipAddress));

        return ipAddress.Trim();
    }

    private static string ValidateLocation(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return "Unknown";

        return location.Length > 100 ? location.Substring(0, 100) : location.Trim();
    }

    private static string ValidateBlockReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("La razón del bloqueo es obligatoria", nameof(reason));

        return reason.Length > 500 ? reason.Substring(0, 500) : reason.Trim();
    }
} 