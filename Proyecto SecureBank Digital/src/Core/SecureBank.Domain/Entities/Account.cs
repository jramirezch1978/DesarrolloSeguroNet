using SecureBank.Domain.Enums;
using SecureBank.Domain.ValueObjects;

namespace SecureBank.Domain.Entities;

/// <summary>
/// Entidad de cuenta bancaria en SecureBank Digital
/// Implementa validaciones de seguridad y límites por tipo de cuenta
/// </summary>
public class Account
{
    public Guid Id { get; private set; }
    public string AccountNumber { get; private set; } = string.Empty;
    public AccountType AccountType { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Balance { get; private set; }
    public decimal AvailableBalance { get; private set; }
    public string Currency { get; private set; } = "PEN"; // Soles peruanos por defecto
    public decimal DailyTransferLimit { get; private set; }
    public decimal MonthlyTransferLimit { get; private set; }
    public decimal OverdraftLimit { get; private set; }
    public decimal InterestRate { get; private set; }
    public decimal MaintenanceFee { get; private set; }
    public int FreeWithdrawalsPerMonth { get; private set; }
    public decimal WithdrawalFeeAfterLimit { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public string CreatedFromIpAddress { get; private set; } = string.Empty;
    public DateTime LastTransactionAt { get; private set; }
    public int WithdrawalsThisMonth { get; private set; }
    public decimal TransactionsToday { get; private set; }
    public decimal TransactionsThisMonth { get; private set; }
    public DateTime LastInterestCalculation { get; private set; }

    // Navegación
    public virtual User User { get; private set; } = null!;
    public virtual ICollection<Transaction> Transactions { get; private set; } = new List<Transaction>();

    // Constructor privado para EF Core
    private Account() { }

    /// <summary>
    /// Constructor para crear una nueva cuenta bancaria
    /// </summary>
    public Account(
        Guid userId,
        AccountType accountType,
        string createdFromIpAddress)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        AccountType = accountType;
        AccountNumber = GenerateAccountNumber();
        CreatedFromIpAddress = ValidateIpAddress(createdFromIpAddress);
        
        // Configurar características según el tipo de cuenta
        ConfigureAccountFeatures();
        
        Balance = 0;
        AvailableBalance = 0;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        LastTransactionAt = DateTime.UtcNow;
        LastInterestCalculation = DateTime.UtcNow;
        WithdrawalsThisMonth = 0;
        TransactionsToday = 0;
        TransactionsThisMonth = 0;
    }

    /// <summary>
    /// Debita un monto de la cuenta con validaciones de límites
    /// </summary>
    public void Debit(decimal amount, string description, TransactionType transactionType)
    {
        if (amount <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero", nameof(amount));

        if (!IsActive)
            throw new InvalidOperationException("No se puede debitar de una cuenta inactiva");

        // Verificar si hay fondos suficientes considerando el sobregiro
        var maxAmount = Balance + OverdraftLimit;
        if (amount > maxAmount)
            throw new InvalidOperationException($"Fondos insuficientes. Disponible: {maxAmount:C}");

        // Verificar límites diarios y mensuales para transferencias
        if (IsTransferTransaction(transactionType))
        {
            ValidateTransferLimits(amount);
        }

        // Actualizar balances
        Balance -= amount;
        AvailableBalance = Balance; // Simplificado, podría considerar retenciones
        
        // Actualizar contadores
        UpdateTransactionCounters(amount, transactionType);
        LastTransactionAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Acredita un monto a la cuenta
    /// </summary>
    public void Credit(decimal amount, string description, TransactionType transactionType)
    {
        if (amount <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero", nameof(amount));

        if (!IsActive)
            throw new InvalidOperationException("No se puede acreditar a una cuenta inactiva");

        // Actualizar balances
        Balance += amount;
        AvailableBalance = Balance; // Simplificado
        
        LastTransactionAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Bloquea fondos en la cuenta (para transferencias pendientes)
    /// </summary>
    public void HoldFunds(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero", nameof(amount));

        if (amount > AvailableBalance)
            throw new InvalidOperationException("No hay fondos suficientes para bloquear");

        AvailableBalance -= amount;
    }

    /// <summary>
    /// Libera fondos bloqueados
    /// </summary>
    public void ReleaseFunds(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero", nameof(amount));

        AvailableBalance += amount;
        
        // No permitir que el balance disponible sea mayor al balance real
        if (AvailableBalance > Balance)
            AvailableBalance = Balance;
    }

    /// <summary>
    /// Calcula y aplica intereses (para cuentas de ahorro y premium)
    /// </summary>
    public decimal CalculateAndApplyInterest()
    {
        if (InterestRate <= 0 || Balance <= 0)
            return 0;

        var daysSinceLastCalculation = (DateTime.UtcNow - LastInterestCalculation).Days;
        if (daysSinceLastCalculation <= 0)
            return 0;

        // Cálculo de interés diario
        var dailyRate = InterestRate / 365 / 100;
        var interestEarned = Balance * (decimal)dailyRate * daysSinceLastCalculation;

        if (interestEarned > 0)
        {
            Credit(interestEarned, $"Interés ganado - {daysSinceLastCalculation} días", TransactionType.Interest);
            LastInterestCalculation = DateTime.UtcNow;
        }

        return interestEarned;
    }

    /// <summary>
    /// Cobra la comisión de mantenimiento mensual
    /// </summary>
    public void ChargeMaintenanceFee()
    {
        if (MaintenanceFee <= 0)
            return;

        if (Balance >= MaintenanceFee)
        {
            Debit(MaintenanceFee, "Comisión de mantenimiento mensual", TransactionType.Fee);
        }
    }

    /// <summary>
    /// Actualiza los límites de transferencia (solo para cuentas empresariales)
    /// </summary>
    public void UpdateTransferLimits(decimal dailyLimit, decimal monthlyLimit)
    {
        if (AccountType != AccountType.Business)
            throw new InvalidOperationException("Solo las cuentas empresariales pueden tener límites personalizados");

        if (dailyLimit <= 0 || monthlyLimit <= 0)
            throw new ArgumentException("Los límites deben ser mayores a cero");

        if (dailyLimit > monthlyLimit)
            throw new ArgumentException("El límite diario no puede ser mayor al mensual");

        DailyTransferLimit = dailyLimit;
        MonthlyTransferLimit = monthlyLimit;
    }

    /// <summary>
    /// Cierra la cuenta
    /// </summary>
    public void CloseAccount()
    {
        if (Balance != 0)
            throw new InvalidOperationException("No se puede cerrar una cuenta con saldo diferente a cero");

        IsActive = false;
        ClosedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactiva la cuenta
    /// </summary>
    public void ReactivateAccount()
    {
        if (ClosedAt.HasValue)
        {
            IsActive = true;
            ClosedAt = null;
        }
    }

    /// <summary>
    /// Reinicia los contadores mensuales (llamado automáticamente el primer día del mes)
    /// </summary>
    public void ResetMonthlyCounters()
    {
        WithdrawalsThisMonth = 0;
        TransactionsThisMonth = 0;
    }

    /// <summary>
    /// Reinicia los contadores diarios (llamado automáticamente cada día)
    /// </summary>
    public void ResetDailyCounters()
    {
        TransactionsToday = 0;
    }

    /// <summary>
    /// Verifica si la cuenta puede realizar una transferencia por el monto especificado
    /// </summary>
    public bool CanTransfer(decimal amount)
    {
        return IsActive && 
               amount <= AvailableBalance + OverdraftLimit &&
               TransactionsToday + amount <= DailyTransferLimit &&
               TransactionsThisMonth + amount <= MonthlyTransferLimit;
    }

    // Métodos privados
    private void ConfigureAccountFeatures()
    {
        switch (AccountType)
        {
            case AccountType.Savings:
                // Cuenta de Ahorros: 2.5% anual, límite $2,000 diarios, sin comisión mantenimiento
                InterestRate = 2.5m;
                DailyTransferLimit = 2000m;
                MonthlyTransferLimit = 20000m;
                MaintenanceFee = 0m;
                FreeWithdrawalsPerMonth = 4;
                WithdrawalFeeAfterLimit = 2m;
                OverdraftLimit = 0m;
                break;

            case AccountType.Checking:
                // Cuenta Corriente: Sin intereses, sobregiro $500, límite $5,000 diarios, $8 mensual
                InterestRate = 0m;
                DailyTransferLimit = 5000m;
                MonthlyTransferLimit = 50000m;
                MaintenanceFee = 8m;
                FreeWithdrawalsPerMonth = 0;
                WithdrawalFeeAfterLimit = 2m;
                OverdraftLimit = 500m;
                break;

            case AccountType.Premium:
                // Cuenta Premium: 3.2% anual, límite $10,000 diarios, sin comisiones
                InterestRate = 3.2m;
                DailyTransferLimit = 10000m;
                MonthlyTransferLimit = 100000m;
                MaintenanceFee = 0m;
                FreeWithdrawalsPerMonth = int.MaxValue; // Ilimitado
                WithdrawalFeeAfterLimit = 0m;
                OverdraftLimit = 1000m;
                break;

            case AccountType.Business:
                // Cuenta Empresarial: Límites configurables, sin intereses base
                InterestRate = 0m;
                DailyTransferLimit = 50000m; // Por defecto, se puede cambiar
                MonthlyTransferLimit = 500000m; // Por defecto, se puede cambiar
                MaintenanceFee = 15m; // Comisión empresarial
                FreeWithdrawalsPerMonth = 10;
                WithdrawalFeeAfterLimit = 3m;
                OverdraftLimit = 5000m;
                break;

            default:
                throw new ArgumentException($"Tipo de cuenta no soportado: {AccountType}");
        }
    }

    private static string GenerateAccountNumber()
    {
        // Generar número de cuenta de 10 dígitos comenzando con 100 (código SecureBank)
        var random = new Random();
        var accountNumber = "100" + random.Next(1000000, 9999999).ToString();
        return accountNumber;
    }

    private static string ValidateIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("La dirección IP es obligatoria", nameof(ipAddress));

        return ipAddress.Trim();
    }

    private static bool IsTransferTransaction(TransactionType transactionType)
    {
        return transactionType == TransactionType.InternalTransfer ||
               transactionType == TransactionType.InterbankTransfer ||
               transactionType == TransactionType.ExpressTransfer ||
               transactionType == TransactionType.ScheduledTransfer;
    }

    private void ValidateTransferLimits(decimal amount)
    {
        if (TransactionsToday + amount > DailyTransferLimit)
        {
            throw new InvalidOperationException(
                $"Límite diario excedido. Límite: {DailyTransferLimit:C}, " +
                $"Usado hoy: {TransactionsToday:C}, Intentando: {amount:C}");
        }

        if (TransactionsThisMonth + amount > MonthlyTransferLimit)
        {
            throw new InvalidOperationException(
                $"Límite mensual excedido. Límite: {MonthlyTransferLimit:C}, " +
                $"Usado este mes: {TransactionsThisMonth:C}, Intentando: {amount:C}");
        }
    }

    private void UpdateTransactionCounters(decimal amount, TransactionType transactionType)
    {
        if (IsTransferTransaction(transactionType))
        {
            TransactionsToday += amount;
            TransactionsThisMonth += amount;
        }

        if (transactionType == TransactionType.Withdrawal)
        {
            WithdrawalsThisMonth++;
        }
    }
} 