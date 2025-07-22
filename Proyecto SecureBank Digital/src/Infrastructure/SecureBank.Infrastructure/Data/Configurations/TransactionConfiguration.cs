using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureBank.Domain.Entities;

namespace SecureBank.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de Entity Framework para la entidad Transaction
/// Implementa mapeo con PostgreSQL para transacciones bancarias
/// </summary>
public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        // Configuración de tabla
        builder.ToTable("transactions", "securebank");
        builder.HasKey(t => t.Id);

        // Configuración de propiedades básicas
        builder.Property(t => t.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(t => t.AccountId)
            .HasColumnName("account_id")
            .IsRequired();

        builder.Property(t => t.TransactionNumber)
            .HasColumnName("transaction_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Type)
            .HasColumnName("type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasColumnName("amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(t => t.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired()
            .HasDefaultValue("PEN");

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.Reference)
            .HasColumnName("reference")
            .HasMaxLength(100);

        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        // Configuración de fechas
        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(t => t.ProcessedAt)
            .HasColumnName("processed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(t => t.CompletedAt)
            .HasColumnName("completed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(t => t.ScheduledFor)
            .HasColumnName("scheduled_for")
            .HasColumnType("timestamp with time zone");

        // Configuración de seguridad y auditoría
        builder.Property(t => t.CreatedFromIpAddress)
            .HasColumnName("created_from_ip_address")
            .HasMaxLength(45)
            .IsRequired();

        builder.Property(t => t.DeviceFingerprint)
            .HasColumnName("device_fingerprint")
            .HasMaxLength(100);

        // Configuración de datos de destino
        builder.Property(t => t.DestinationAccountId)
            .HasColumnName("destination_account_id");

        builder.Property(t => t.DestinationAccountNumber)
            .HasColumnName("destination_account_number")
            .HasMaxLength(20);

        builder.Property(t => t.DestinationBankCode)
            .HasColumnName("destination_bank_code")
            .HasMaxLength(10);

        builder.Property(t => t.DestinationAccountHolderName)
            .HasColumnName("destination_account_holder_name")
            .HasMaxLength(100);

        // Configuración de montos y comisiones
        builder.Property(t => t.Fee)
            .HasColumnName("fee")
            .HasColumnType("decimal(10,2)");

        builder.Property(t => t.BalanceAfter)
            .HasColumnName("balance_after")
            .HasColumnType("decimal(18,2)")
            .IsRequired()
            .HasDefaultValue(0);

        // Configuración de errores y reintentos
        builder.Property(t => t.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(1000);

        builder.Property(t => t.RetryCount)
            .HasColumnName("retry_count")
            .IsRequired()
            .HasDefaultValue(0);

        // Configuración de transacciones recurrentes
        builder.Property(t => t.IsRecurring)
            .HasColumnName("is_recurring")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.RecurringPattern)
            .HasColumnName("recurring_pattern")
            .HasMaxLength(100);

        builder.Property(t => t.ParentTransactionId)
            .HasColumnName("parent_transaction_id");

        // Configuración de riesgo y fraude
        builder.Property(t => t.RiskLevel)
            .HasColumnName("risk_level")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.FraudFlags)
            .HasColumnName("fraud_flags")
            .HasMaxLength(500);

        // Configuración de aprobación
        builder.Property(t => t.ApprovedByUserId)
            .HasColumnName("approved_by_user_id");

        builder.Property(t => t.ApprovedAt)
            .HasColumnName("approved_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(t => t.ApprovalNotes)
            .HasColumnName("approval_notes")
            .HasMaxLength(1000);

        // Configuración de relaciones
        builder.HasOne(t => t.Account)
            .WithMany(a => a.Transactions)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_Transactions_Accounts");

        builder.HasOne(t => t.DestinationAccount)
            .WithMany()
            .HasForeignKey(t => t.DestinationAccountId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("FK_Transactions_DestinationAccounts");

        builder.HasOne(t => t.ApprovedByUser)
            .WithMany()
            .HasForeignKey(t => t.ApprovedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("FK_Transactions_ApprovedByUser");

        builder.HasOne(t => t.ParentTransaction)
            .WithMany(t => t.ChildTransactions)
            .HasForeignKey(t => t.ParentTransactionId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("FK_Transactions_ParentTransaction");

        // Configuración de índices
        builder.HasIndex(t => t.TransactionNumber)
            .IsUnique()
            .HasDatabaseName("IX_Transactions_Number_Unique");

        builder.HasIndex(t => t.AccountId)
            .HasDatabaseName("IX_Transactions_AccountId");

        builder.HasIndex(t => new { t.AccountId, t.CreatedAt })
            .HasDatabaseName("IX_Transactions_Account_Date");

        builder.HasIndex(t => new { t.Type, t.Status })
            .HasDatabaseName("IX_Transactions_Type_Status");

        builder.HasIndex(t => new { t.Status, t.CreatedAt })
            .HasDatabaseName("IX_Transactions_Status_Date");

        builder.HasIndex(t => t.DestinationAccountId)
            .HasDatabaseName("IX_Transactions_DestinationAccountId");

        builder.HasIndex(t => t.RiskLevel)
            .HasDatabaseName("IX_Transactions_RiskLevel");

        builder.HasIndex(t => t.CreatedFromIpAddress)
            .HasDatabaseName("IX_Transactions_IpAddress");

        builder.HasIndex(t => t.ScheduledFor)
            .HasDatabaseName("IX_Transactions_ScheduledFor");

        builder.HasIndex(t => t.ParentTransactionId)
            .HasDatabaseName("IX_Transactions_ParentId");

        // Configuración de constraints
        builder.HasCheckConstraint("CK_Transactions_Amount_Positive",
            "amount > 0");

        builder.HasCheckConstraint("CK_Transactions_Fee_NonNegative",
            "fee IS NULL OR fee >= 0");

        builder.HasCheckConstraint("CK_Transactions_RetryCount_Range",
            "retry_count >= 0 AND retry_count <= 5");

        builder.HasCheckConstraint("CK_Transactions_Currency_Valid",
            "currency IN ('PEN', 'USD', 'EUR')");

        builder.HasCheckConstraint("CK_Transactions_Status_Valid",
            "status IN (1, 2, 3, 4, 5, 6, 7, 8)");

        builder.HasCheckConstraint("CK_Transactions_Type_Valid",
            "type BETWEEN 1 AND 99");

        builder.HasCheckConstraint("CK_Transactions_RiskLevel_Valid",
            "risk_level BETWEEN 1 AND 4");

        builder.HasCheckConstraint("CK_Transactions_TransactionNumber_Format",
            "transaction_number ~ '^TXN[0-9]+$'");

        // Configuración temporal para fechas
        builder.HasCheckConstraint("CK_Transactions_ProcessedAfterCreated",
            "processed_at IS NULL OR processed_at >= created_at");

        builder.HasCheckConstraint("CK_Transactions_CompletedAfterProcessed",
            "completed_at IS NULL OR processed_at IS NULL OR completed_at >= processed_at");

        // Configuración de auditoría
        builder.Property<DateTime>("CreatedAt")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property<DateTime?>("UpdatedAt")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Configuración de soft delete
        builder.Property<bool>("IsDeleted")
            .HasDefaultValue(false);

        builder.Property<DateTime?>("DeletedAt");

        builder.HasQueryFilter(t => !EF.Property<bool>(t, "IsDeleted"));

        // Configuración de encriptación para datos sensibles
        builder.Property(t => t.Amount)
            .HasAnnotation("Encrypted", true);

        builder.Property(t => t.BalanceAfter)
            .HasAnnotation("Encrypted", true);

        builder.Property(t => t.DestinationAccountNumber)
            .HasAnnotation("Encrypted", true);

        // Configuración de particionamiento por fecha (para PostgreSQL)
        builder.HasAnnotation("PostgreSQL:PartitionKey", "created_at");
        builder.HasAnnotation("PostgreSQL:PartitionScheme", "RANGE");
    }
} 