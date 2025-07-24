using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureBank.Domain.Entities;

namespace SecureBank.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de Entity Framework para la entidad LoginAttempt
/// Implementa tracking de intentos de login para detección de fraude
/// </summary>
public class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        // Configuración de tabla
        builder.ToTable("login_attempts", "securebank");
        builder.HasKey(la => la.Id);

        // Configuración de propiedades
        builder.Property(la => la.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(la => la.UserId)
            .HasColumnName("user_id");

        builder.Property(la => la.Email)
            .HasColumnName("email")
            .HasMaxLength(254);

        builder.Property(la => la.DocumentNumber)
            .HasColumnName("document_number")
            .HasMaxLength(20);

        builder.Property(la => la.IsSuccessful)
            .HasColumnName("is_successful")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(la => la.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(500)
            .IsRequired()
            .HasDefaultValue("");

        builder.Property(la => la.AttemptedAt)
            .HasColumnName("attempted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(la => la.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45)
            .IsRequired();

        builder.Property(la => la.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(la => la.DeviceFingerprint)
            .HasColumnName("device_fingerprint")
            .HasMaxLength(100);

        builder.Property(la => la.SessionId)
            .HasColumnName("session_id")
            .HasMaxLength(100);

        builder.Property(la => la.Country)
            .HasColumnName("country")
            .HasMaxLength(100)
            .IsRequired()
            .HasDefaultValue("Unknown");

        builder.Property(la => la.City)
            .HasColumnName("city")
            .HasMaxLength(100)
            .IsRequired()
            .HasDefaultValue("Unknown");

        builder.Property(la => la.IsTrustedDevice)
            .HasColumnName("is_trusted_device")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(la => la.RequiredTwoFactor)
            .HasColumnName("required_two_factor")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(la => la.TwoFactorPassed)
            .HasColumnName("two_factor_passed")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(la => la.TwoFactorMethod)
            .HasColumnName("two_factor_method")
            .HasMaxLength(20);

        builder.Property(la => la.Duration)
            .HasColumnName("duration")
            .HasColumnType("interval")
            .IsRequired()
            .HasDefaultValue(TimeSpan.Zero);

        builder.Property(la => la.IsBlocked)
            .HasColumnName("is_blocked")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(la => la.BlockReason)
            .HasColumnName("block_reason")
            .HasMaxLength(500);

        builder.Property(la => la.RiskScore)
            .HasColumnName("risk_score")
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(1); // Low

        builder.Property(la => la.RiskFactors)
            .HasColumnName("risk_factors")
            .HasMaxLength(1000);

        // Configuración de relaciones
        builder.HasOne(la => la.User)
            .WithMany(u => u.LoginAttempts)
            .HasForeignKey(la => la.UserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("FK_LoginAttempts_Users");

        // Configuración de índices para consultas de seguridad
        builder.HasIndex(la => new { la.IpAddress, la.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_IP_Date");

        builder.HasIndex(la => new { la.UserId, la.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_User_Date");

        builder.HasIndex(la => new { la.Email, la.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_Email_Date");

        builder.HasIndex(la => new { la.DocumentNumber, la.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_Document_Date");

        builder.HasIndex(la => new { la.IsSuccessful, la.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_Success_Date");

        builder.HasIndex(la => new { la.RiskScore, la.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_Risk_Date");

        builder.HasIndex(la => la.DeviceFingerprint)
            .HasDatabaseName("IX_LoginAttempts_DeviceFingerprint");

        builder.HasIndex(la => new { la.Country, la.City })
            .HasDatabaseName("IX_LoginAttempts_Location");

        builder.HasIndex(la => la.IsBlocked)
            .HasDatabaseName("IX_LoginAttempts_Blocked");

        builder.HasIndex(la => la.AttemptedAt)
            .HasDatabaseName("IX_LoginAttempts_AttemptedAt");

        // Configuración de constraints
        builder.HasCheckConstraint("CK_LoginAttempts_RiskScore_Valid",
            "risk_score BETWEEN 1 AND 4");

        builder.HasCheckConstraint("CK_LoginAttempts_TwoFactorLogic",
            "(required_two_factor = false) OR (required_two_factor = true AND two_factor_method IS NOT NULL)");

        builder.HasCheckConstraint("CK_LoginAttempts_BlockLogic",
            "(is_blocked = false AND block_reason IS NULL) OR (is_blocked = true AND block_reason IS NOT NULL)");

        builder.HasCheckConstraint("CK_LoginAttempts_Duration_NonNegative",
            "duration >= INTERVAL '0 seconds'");

        // Configuración de campos encriptados para PII
        builder.Property(la => la.Email)
            .HasAnnotation("Encrypted", true);

        builder.Property(la => la.DocumentNumber)
            .HasAnnotation("Encrypted", true);

        // Configuración de particionamiento por fecha para rendimiento
        builder.HasAnnotation("PostgreSQL:PartitionKey", "attempted_at");
        builder.HasAnnotation("PostgreSQL:PartitionScheme", "RANGE");

        // Configuración de retención de datos (6 meses para login attempts)
        builder.HasAnnotation("RetentionPolicy", "6Months");

        // Configuración de auditoría automática
        builder.Property<DateTime>("CreatedAt")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // No hay soft delete para login attempts (se mantienen por seguridad)
        
        // Configuración de triggers para análisis de fraude en tiempo real
        builder.HasAnnotation("PostgreSQL:TriggerOnInsert", "analyze_login_attempt_patterns");
    }
} 