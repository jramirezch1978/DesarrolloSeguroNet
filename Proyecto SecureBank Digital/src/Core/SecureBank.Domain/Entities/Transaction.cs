using SecureBank.Domain.Enums;

namespace SecureBank.Domain.Entities;

/// <summary>
/// Entidad de transacción en SecureBank Digital
/// Implementa audit trail completo y validaciones de seguridad
/// </summary>
public class Transaction
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public string TransactionNumber { get; private set; } = string.Empty;
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "PEN";
    public string Description { get; private set; } = string.Empty;
    public string? Reference { get; private set; }
    public TransactionStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string CreatedFromIpAddress { get; private set; } = string.Empty;
    public string? DeviceFingerprint { get; private set; }
    public Guid? DestinationAccountId { get; private set; }
    public string? DestinationAccountNumber { get; private set; }
    public string? DestinationBankCode { get; private set; }
    public string? DestinationAccountHolderName { get; private set; }
    public decimal? Fee { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public string? FailureReason { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? ScheduledFor { get; private set; }
    public bool IsRecurring { get; private set; }
    public string? RecurringPattern { get; private set; }
    public Guid? ParentTransactionId { get; private set; }
    public RiskLevel RiskLevel { get; private set; }
    public string? FraudFlags { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? ApprovalNotes { get; private set; }

    // Navegación
    public virtual Account Account { get; private set; } = null!;
    public virtual Account? DestinationAccount { get; private set; }
    public virtual User? ApprovedByUser { get; private set; }
    public virtual Transaction? ParentTransaction { get; private set; }
    public virtual ICollection<Transaction> ChildTransactions { get; private set; } = new List<Transaction>();

    // Constructor privado para EF Core
    private Transaction() { }

    /// <summary>
    /// Constructor para crear una nueva transacción
    /// </summary>
    public Transaction(
        Guid accountId,
        TransactionType type,
        decimal amount,
        string description,
        string createdFromIpAddress,
        string? deviceFingerprint = null,
        string? reference = null,
        Guid? destinationAccountId = null,
        string? destinationAccountNumber = null,
        string? destinationBankCode = null,
        string? destinationAccountHolderName = null,
        decimal? fee = null,
        DateTime? scheduledFor = null,
        bool isRecurring = false,
        string? recurringPattern = null,
        Guid? parentTransactionId = null)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        TransactionNumber = GenerateTransactionNumber();
        Type = type;
        Amount = ValidateAmount(amount);
        Description = ValidateDescription(description);
        CreatedFromIpAddress = ValidateIpAddress(createdFromIpAddress);
        DeviceFingerprint = deviceFingerprint;
        Reference = reference;
        DestinationAccountId = destinationAccountId;
        DestinationAccountNumber = destinationAccountNumber;
        DestinationBankCode = destinationBankCode;
        DestinationAccountHolderName = destinationAccountHolderName;
        Fee = fee;
        ScheduledFor = scheduledFor;
        IsRecurring = isRecurring;
        RecurringPattern = recurringPattern;
        ParentTransactionId = parentTransactionId;

        // Estados iniciales
        Status = scheduledFor.HasValue ? TransactionStatus.Scheduled : TransactionStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        RetryCount = 0;
        RiskLevel = CalculateInitialRiskLevel();
    }

    /// <summary>
    /// Marca la transacción como en proceso
    /// </summary>
    public void StartProcessing()
    {
        if (Status != TransactionStatus.Pending && Status != TransactionStatus.Scheduled)
            throw new InvalidOperationException($"No se puede procesar una transacción en estado {Status}");

        Status = TransactionStatus.Processing;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Completa la transacción exitosamente
    /// </summary>
    public void Complete(decimal balanceAfter)
    {
        if (Status != TransactionStatus.Processing)
            throw new InvalidOperationException($"No se puede completar una transacción en estado {Status}");

        Status = TransactionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        BalanceAfter = balanceAfter;
        FailureReason = null;
    }

    /// <summary>
    /// Marca la transacción como fallida
    /// </summary>
    public void Fail(string reason)
    {
        if (Status == TransactionStatus.Completed)
            throw new InvalidOperationException("No se puede fallar una transacción completada");

        Status = TransactionStatus.Failed;
        FailureReason = ValidateFailureReason(reason);
    }

    /// <summary>
    /// Cancela la transacción
    /// </summary>
    public void Cancel(string? reason = null)
    {
        if (Status == TransactionStatus.Completed)
            throw new InvalidOperationException("No se puede cancelar una transacción completada");

        if (Status == TransactionStatus.Processing)
            throw new InvalidOperationException("No se puede cancelar una transacción en proceso");

        Status = TransactionStatus.Cancelled;
        FailureReason = reason ?? "Cancelada por el usuario";
    }

    /// <summary>
    /// Reintenta la transacción fallida
    /// </summary>
    public void Retry()
    {
        if (Status != TransactionStatus.Failed)
            throw new InvalidOperationException("Solo se pueden reintentar transacciones fallidas");

        if (RetryCount >= 3)
            throw new InvalidOperationException("Se ha excedido el número máximo de reintentos");

        Status = TransactionStatus.Pending;
        RetryCount++;
        FailureReason = null;
    }

    /// <summary>
    /// Requiere aprobación manual
    /// </summary>
    public void RequireApproval(string reason)
    {
        if (Status == TransactionStatus.Completed)
            throw new InvalidOperationException("No se puede requerir aprobación de una transacción completada");

        Status = TransactionStatus.PendingApproval;
        FraudFlags = reason;
    }

    /// <summary>
    /// Aprueba la transacción manualmente
    /// </summary>
    public void Approve(Guid approvedByUserId, string? notes = null)
    {
        if (Status != TransactionStatus.PendingApproval)
            throw new InvalidOperationException("Solo se pueden aprobar transacciones pendientes de aprobación");

        Status = TransactionStatus.Pending;
        ApprovedByUserId = approvedByUserId;
        ApprovedAt = DateTime.UtcNow;
        ApprovalNotes = notes;
        FraudFlags = null;
    }

    /// <summary>
    /// Rechaza la transacción manualmente
    /// </summary>
    public void Reject(Guid rejectedByUserId, string reason)
    {
        if (Status != TransactionStatus.PendingApproval)
            throw new InvalidOperationException("Solo se pueden rechazar transacciones pendientes de aprobación");

        Status = TransactionStatus.Rejected;
        ApprovedByUserId = rejectedByUserId;
        ApprovedAt = DateTime.UtcNow;
        FailureReason = $"Rechazada: {reason}";
    }

    /// <summary>
    /// Actualiza el nivel de riesgo de la transacción
    /// </summary>
    public void UpdateRiskLevel(RiskLevel newRiskLevel, string? flags = null)
    {
        RiskLevel = newRiskLevel;
        if (!string.IsNullOrWhiteSpace(flags))
        {
            FraudFlags = FraudFlags != null ? $"{FraudFlags}; {flags}" : flags;
        }
    }

    /// <summary>
    /// Verifica si la transacción está en un estado final
    /// </summary>
    public bool IsInFinalState()
    {
        return Status == TransactionStatus.Completed ||
               Status == TransactionStatus.Failed ||
               Status == TransactionStatus.Cancelled ||
               Status == TransactionStatus.Rejected;
    }

    /// <summary>
    /// Verifica si la transacción es de transferencia
    /// </summary>
    public bool IsTransfer()
    {
        return Type == TransactionType.InternalTransfer ||
               Type == TransactionType.InterbankTransfer ||
               Type == TransactionType.ExpressTransfer ||
               Type == TransactionType.ScheduledTransfer;
    }

    /// <summary>
    /// Verifica si la transacción es de pago de servicios
    /// </summary>
    public bool IsServicePayment()
    {
        return Type == TransactionType.BasicServicePayment ||
               Type == TransactionType.TaxPayment ||
               Type == TransactionType.InsurancePayment ||
               Type == TransactionType.EducationPayment;
    }

    /// <summary>
    /// Obtiene el tiempo transcurrido desde la creación
    /// </summary>
    public TimeSpan GetAge()
    {
        return DateTime.UtcNow - CreatedAt;
    }

    // Métodos privados
    private static decimal ValidateAmount(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero", nameof(amount));

        if (amount > 1000000) // Límite máximo de $1M por transacción
            throw new ArgumentException("El monto excede el límite máximo permitido", nameof(amount));

        return amount;
    }

    private static string ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripción es obligatoria", nameof(description));

        if (description.Length > 500)
            throw new ArgumentException("La descripción no puede tener más de 500 caracteres", nameof(description));

        return description.Trim();
    }

    private static string ValidateIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("La dirección IP es obligatoria", nameof(ipAddress));

        return ipAddress.Trim();
    }

    private static string ValidateFailureReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("La razón del fallo es obligatoria", nameof(reason));

        if (reason.Length > 1000)
            throw new ArgumentException("La razón del fallo no puede tener más de 1000 caracteres", nameof(reason));

        return reason.Trim();
    }

    private static string GenerateTransactionNumber()
    {
        // Generar número de transacción único: TXN + timestamp + random
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = new Random().Next(1000, 9999);
        return $"TXN{timestamp}{random}";
    }

    private RiskLevel CalculateInitialRiskLevel()
    {
        // Lógica básica para calcular el riesgo inicial
        if (Amount >= 10000)
            return RiskLevel.High;

        if (Amount >= 1000)
            return RiskLevel.Medium;

        return RiskLevel.Low;
    }
}

/// <summary>
/// Estados de una transacción
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Transacción creada y pendiente de procesamiento
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Transacción programada para una fecha futura
    /// </summary>
    Scheduled = 2,

    /// <summary>
    /// Transacción en proceso de ejecución
    /// </summary>
    Processing = 3,

    /// <summary>
    /// Transacción completada exitosamente
    /// </summary>
    Completed = 4,

    /// <summary>
    /// Transacción falló durante la ejecución
    /// </summary>
    Failed = 5,

    /// <summary>
    /// Transacción cancelada por el usuario
    /// </summary>
    Cancelled = 6,

    /// <summary>
    /// Transacción pendiente de aprobación manual
    /// </summary>
    PendingApproval = 7,

    /// <summary>
    /// Transacción rechazada por aprobador
    /// </summary>
    Rejected = 8
}

/// <summary>
/// Niveles de riesgo para transacciones
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// Riesgo bajo - procesamiento automático
    /// </summary>
    Low = 1,

    /// <summary>
    /// Riesgo medio - validaciones adicionales
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Riesgo alto - requiere aprobación manual
    /// </summary>
    High = 3,

    /// <summary>
    /// Riesgo crítico - bloquear automáticamente
    /// </summary>
    Critical = 4
} 