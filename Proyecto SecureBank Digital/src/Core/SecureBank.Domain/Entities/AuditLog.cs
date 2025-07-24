using SecureBank.Domain.Enums;

namespace SecureBank.Domain.Entities;

/// <summary>
/// Entidad de auditoría inmutable para SecureBank Digital
/// Implementa audit trail completo con hash chains para detectar modificaciones
/// </summary>
public class AuditLog
{
    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? AccountId { get; private set; }
    public Guid? TransactionId { get; private set; }
    public AuditAction Action { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public string? EntityId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string IpAddress { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;
    public string? DeviceFingerprint { get; private set; }
    public string? SessionId { get; private set; }
    public AuditLevel Level { get; private set; }
    public string? AdditionalData { get; private set; }
    public string Hash { get; private set; } = string.Empty;
    public string? PreviousHash { get; private set; }
    public long SequenceNumber { get; private set; }

    // Navegación
    public virtual User? User { get; private set; }
    public virtual Account? Account { get; private set; }
    public virtual Transaction? Transaction { get; private set; }

    // Constructor privado para EF Core
    private AuditLog() { }

    /// <summary>
    /// Constructor para crear un nuevo log de auditoría
    /// </summary>
    public AuditLog(
        AuditAction action,
        string entityType,
        string description,
        string ipAddress,
        string userAgent,
        AuditLevel level = AuditLevel.Information,
        Guid? userId = null,
        Guid? accountId = null,
        Guid? transactionId = null,
        string? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        string? deviceFingerprint = null,
        string? sessionId = null,
        string? additionalData = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        AccountId = accountId;
        TransactionId = transactionId;
        Action = action;
        EntityType = ValidateEntityType(entityType);
        EntityId = entityId;
        Description = ValidateDescription(description);
        OldValues = oldValues;
        NewValues = newValues;
        Timestamp = DateTime.UtcNow;
        IpAddress = ValidateIpAddress(ipAddress);
        UserAgent = ValidateUserAgent(userAgent);
        DeviceFingerprint = deviceFingerprint;
        SessionId = sessionId;
        Level = level;
        AdditionalData = additionalData;
        
        // El hash y número de secuencia se establecen cuando se persiste
        Hash = string.Empty;
        SequenceNumber = 0;
    }

    /// <summary>
    /// Establece el hash y número de secuencia para la cadena de bloques de auditoría
    /// </summary>
    public void SetHashAndSequence(long sequenceNumber, string? previousHash = null)
    {
        SequenceNumber = sequenceNumber;
        PreviousHash = previousHash;
        Hash = CalculateHash();
    }

    /// <summary>
    /// Verifica la integridad del hash
    /// </summary>
    public bool VerifyHash()
    {
        var calculatedHash = CalculateHash();
        return Hash == calculatedHash;
    }

    /// <summary>
    /// Verifica la cadena de hashes con el log anterior
    /// </summary>
    public bool VerifyChain(AuditLog? previousLog)
    {
        if (previousLog == null)
            return PreviousHash == null;
        
        return PreviousHash == previousLog.Hash;
    }

    /// <summary>
    /// Obtiene una representación resumida del log para reportes
    /// </summary>
    public string GetSummary()
    {
        var userInfo = UserId.HasValue ? $"Usuario: {UserId}" : "Sistema";
        return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Level} - {Action} en {EntityType}: {Description} ({userInfo} desde {IpAddress})";
    }

    /// <summary>
    /// Verifica si el log es crítico para seguridad
    /// </summary>
    public bool IsCriticalSecurityEvent()
    {
        return Level == AuditLevel.Critical || 
               Action == AuditAction.Login ||
               Action == AuditAction.LoginFailed ||
               Action == AuditAction.PasswordChanged ||
               Action == AuditAction.AccountLocked ||
               Action == AuditAction.PermissionEscalation ||
               Action == AuditAction.SecurityViolation;
    }

    /// <summary>
    /// Verifica si el log debe ser retenido por períodos extendidos
    /// </summary>
    public bool RequiresLongTermRetention()
    {
        return Level == AuditLevel.Critical ||
               Action == AuditAction.TransactionCreated ||
               Action == AuditAction.TransactionCompleted ||
               Action == AuditAction.AccountCreated ||
               Action == AuditAction.AccountClosed ||
               Action == AuditAction.ConfigurationChanged;
    }

    // Métodos privados
    private static string ValidateEntityType(string entityType)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("El tipo de entidad es obligatorio", nameof(entityType));

        if (entityType.Length > 100)
            throw new ArgumentException("El tipo de entidad no puede tener más de 100 caracteres", nameof(entityType));

        return entityType.Trim();
    }

    private static string ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripción es obligatoria", nameof(description));

        if (description.Length > 1000)
            throw new ArgumentException("La descripción no puede tener más de 1000 caracteres", nameof(description));

        return description.Trim();
    }

    private static string ValidateIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("La dirección IP es obligatoria", nameof(ipAddress));

        return ipAddress.Trim();
    }

    private static string ValidateUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "Unknown";

        return userAgent.Length > 500 ? userAgent.Substring(0, 500) : userAgent.Trim();
    }

    private string CalculateHash()
    {
        // Crear un hash SHA-256 de los datos críticos del log
        var data = $"{Id}|{Action}|{EntityType}|{EntityId}|{Description}|{Timestamp:O}|{IpAddress}|{SequenceNumber}|{PreviousHash}";
        
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hashBytes);
    }
}

/// <summary>
/// Acciones de auditoría
/// </summary>
public enum AuditAction
{
    // Autenticación y autorización
    Login = 1,
    LoginFailed = 2,
    Logout = 3,
    PasswordChanged = 4,
    TwoFactorEnabled = 5,
    TwoFactorDisabled = 6,
    AccountLocked = 7,
    AccountUnlocked = 8,
    PermissionGranted = 9,
    PermissionRevoked = 10,
    PermissionEscalation = 11,

    // Gestión de usuarios
    UserCreated = 20,
    UserUpdated = 21,
    UserDeleted = 22,
    UserStatusChanged = 23,
    ProfileUpdated = 24,

    // Gestión de cuentas
    AccountCreated = 30,
    AccountUpdated = 31,
    AccountClosed = 32,
    AccountReactivated = 33,
    BalanceChanged = 34,
    LimitsChanged = 35,

    // Transacciones
    TransactionCreated = 40,
    TransactionProcessed = 41,
    TransactionCompleted = 42,
    TransactionFailed = 43,
    TransactionCancelled = 44,
    TransactionApproved = 45,
    TransactionRejected = 46,

    // Configuración del sistema
    ConfigurationChanged = 50,
    SecurityPolicyUpdated = 51,
    SystemStarted = 52,
    SystemStopped = 53,
    MaintenanceStarted = 54,
    MaintenanceCompleted = 55,

    // Eventos de seguridad
    SecurityViolation = 60,
    SuspiciousActivity = 61,
    FraudDetected = 62,
    DataAccessed = 63,
    DataExported = 64,
    UnauthorizedAccess = 65,

    // Operaciones administrativas
    ReportGenerated = 70,
    BackupCreated = 71,
    BackupRestored = 72,
    DataPurged = 73,
    SystemHealthCheck = 74
}

/// <summary>
/// Niveles de auditoría
/// </summary>
public enum AuditLevel
{
    /// <summary>
    /// Información general
    /// </summary>
    Information = 1,

    /// <summary>
    /// Advertencia - eventos que requieren atención
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Error - eventos que causaron fallos
    /// </summary>
    Error = 3,

    /// <summary>
    /// Crítico - eventos de seguridad importantes
    /// </summary>
    Critical = 4
} 