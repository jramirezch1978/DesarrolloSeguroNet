using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureBank.Domain.Entities;

namespace SecureBank.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de Entity Framework para la entidad AuditLog
/// Implementa audit trail inmutable con hash chains para detectar modificaciones
/// </summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        // Configuración de tabla
        builder.ToTable("audit_logs", "securebank");
        builder.HasKey(al => al.Id);

        // Configuración de propiedades
        builder.Property(al => al.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(al => al.UserId)
            .HasColumnName("user_id");

        builder.Property(al => al.AccountId)
            .HasColumnName("account_id");

        builder.Property(al => al.TransactionId)
            .HasColumnName("transaction_id");

        builder.Property(al => al.Action)
            .HasColumnName("action")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(al => al.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(al => al.EntityId)
            .HasColumnName("entity_id")
            .HasMaxLength(50);

        builder.Property(al => al.Description)
            .HasColumnName("description")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(al => al.OldValues)
            .HasColumnName("old_values")
            .HasColumnType("jsonb"); // PostgreSQL JSON para eficiencia

        builder.Property(al => al.NewValues)
            .HasColumnName("new_values")
            .HasColumnType("jsonb"); // PostgreSQL JSON para eficiencia

        builder.Property(al => al.Timestamp)
            .HasColumnName("timestamp")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(al => al.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45)
            .IsRequired();

        builder.Property(al => al.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(al => al.DeviceFingerprint)
            .HasColumnName("device_fingerprint")
            .HasMaxLength(100);

        builder.Property(al => al.SessionId)
            .HasColumnName("session_id")
            .HasMaxLength(100);

        builder.Property(al => al.Level)
            .HasColumnName("level")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(al => al.AdditionalData)
            .HasColumnName("additional_data")
            .HasColumnType("jsonb"); // PostgreSQL JSON para datos adicionales

        // Configuración de hash chain para inmutabilidad
        builder.Property(al => al.Hash)
            .HasColumnName("hash")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(al => al.PreviousHash)
            .HasColumnName("previous_hash")
            .HasMaxLength(100);

        builder.Property(al => al.SequenceNumber)
            .HasColumnName("sequence_number")
            .IsRequired()
            .ValueGeneratedOnAdd(); // Auto-incremento para secuencia inmutable

        // Configuración de relaciones
        builder.HasOne(al => al.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("FK_AuditLogs_Users");

        builder.HasOne(al => al.Account)
            .WithMany()
            .HasForeignKey(al => al.AccountId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("FK_AuditLogs_Accounts");

        builder.HasOne(al => al.Transaction)
            .WithMany()
            .HasForeignKey(al => al.TransactionId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("FK_AuditLogs_Transactions");

        // Configuración de índices para consultas eficientes
        builder.HasIndex(al => al.SequenceNumber)
            .IsUnique()
            .HasDatabaseName("IX_AuditLogs_Sequence_Unique");

        builder.HasIndex(al => new { al.UserId, al.Timestamp })
            .HasDatabaseName("IX_AuditLogs_User_Timestamp");

        builder.HasIndex(al => new { al.Action, al.Level, al.Timestamp })
            .HasDatabaseName("IX_AuditLogs_Action_Level_Timestamp");

        builder.HasIndex(al => new { al.EntityType, al.EntityId, al.Timestamp })
            .HasDatabaseName("IX_AuditLogs_Entity_Timestamp");

        builder.HasIndex(al => al.IpAddress)
            .HasDatabaseName("IX_AuditLogs_IpAddress");

        builder.HasIndex(al => al.Timestamp)
            .HasDatabaseName("IX_AuditLogs_Timestamp");

        builder.HasIndex(al => al.Level)
            .HasDatabaseName("IX_AuditLogs_Level");

        builder.HasIndex(al => al.Hash)
            .HasDatabaseName("IX_AuditLogs_Hash");

        // Configuración de constraints
        builder.HasCheckConstraint("CK_AuditLogs_Action_Valid",
            "action BETWEEN 1 AND 99");

        builder.HasCheckConstraint("CK_AuditLogs_Level_Valid",
            "level BETWEEN 1 AND 4");

        builder.HasCheckConstraint("CK_AuditLogs_Hash_NotEmpty",
            "hash IS NOT NULL AND LENGTH(hash) > 0");

        builder.HasCheckConstraint("CK_AuditLogs_SequenceNumber_Positive",
            "sequence_number > 0");

        // Configuración de inmutabilidad (solo inserción, no actualización ni eliminación)
        builder.Property<bool>("IsReadOnly")
            .HasDefaultValue(true);

        // Configuración de particionamiento por fecha para rendimiento
        builder.HasAnnotation("PostgreSQL:PartitionKey", "timestamp");
        builder.HasAnnotation("PostgreSQL:PartitionScheme", "RANGE");

        // Configuración para retención de datos según el prompt
        builder.HasAnnotation("RetentionPolicy", "7Years_Security_10Years_Transactional");

        // No permitir soft delete en logs de auditoría (inmutabilidad)
        // Los logs de auditoría NUNCA se eliminan físicamente

        // Configuración de triggers para integridad
        builder.HasAnnotation("PostgreSQL:TriggerOnInsert", "audit_log_hash_validation");
        builder.HasAnnotation("PostgreSQL:TriggerOnUpdate", "prevent_audit_log_modification");
        builder.HasAnnotation("PostgreSQL:TriggerOnDelete", "prevent_audit_log_deletion");
    }
} 