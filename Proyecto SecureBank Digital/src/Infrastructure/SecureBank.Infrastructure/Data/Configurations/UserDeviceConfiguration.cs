using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureBank.Domain.Entities;

namespace SecureBank.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de Entity Framework para la entidad UserDevice
/// Implementa device fingerprinting y gestión de dispositivos confiables
/// </summary>
public class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
{
    public void Configure(EntityTypeBuilder<UserDevice> builder)
    {
        // Configuración de tabla
        builder.ToTable("user_devices", "securebank");
        builder.HasKey(ud => ud.Id);

        // Configuración de propiedades
        builder.Property(ud => ud.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(ud => ud.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(ud => ud.DeviceFingerprint)
            .HasColumnName("device_fingerprint")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(ud => ud.DeviceName)
            .HasColumnName("device_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(ud => ud.DeviceType)
            .HasColumnName("device_type")
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue("Unknown");

        builder.Property(ud => ud.OperatingSystem)
            .HasColumnName("operating_system")
            .HasMaxLength(100)
            .IsRequired()
            .HasDefaultValue("Unknown");

        builder.Property(ud => ud.Browser)
            .HasColumnName("browser")
            .HasMaxLength(100)
            .IsRequired()
            .HasDefaultValue("Unknown");

        builder.Property(ud => ud.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(ud => ud.IsTrusted)
            .HasColumnName("is_trusted")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ud => ud.FirstSeenAt)
            .HasColumnName("first_seen_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(ud => ud.LastSeenAt)
            .HasColumnName("last_seen_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(ud => ud.TrustedAt)
            .HasColumnName("trusted_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(ud => ud.RegistrationIpAddress)
            .HasColumnName("registration_ip_address")
            .HasMaxLength(45)
            .IsRequired();

        builder.Property(ud => ud.LastIpAddress)
            .HasColumnName("last_ip_address")
            .HasMaxLength(45)
            .IsRequired();

        builder.Property(ud => ud.Country)
            .HasColumnName("country")
            .HasMaxLength(100)
            .IsRequired()
            .HasDefaultValue("Unknown");

        builder.Property(ud => ud.City)
            .HasColumnName("city")
            .HasMaxLength(100)
            .IsRequired()
            .HasDefaultValue("Unknown");

        builder.Property(ud => ud.LoginCount)
            .HasColumnName("login_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(ud => ud.SuccessfulLoginCount)
            .HasColumnName("successful_login_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(ud => ud.LastSuccessfulLoginAt)
            .HasColumnName("last_successful_login_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(ud => ud.IsBlocked)
            .HasColumnName("is_blocked")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ud => ud.BlockedAt)
            .HasColumnName("blocked_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(ud => ud.BlockReason)
            .HasColumnName("block_reason")
            .HasMaxLength(500);

        builder.Property(ud => ud.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(ud => ud.RequiresRevalidation)
            .HasColumnName("requires_revalidation")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ud => ud.LastRevalidationAt)
            .HasColumnName("last_revalidation_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(ud => ud.AdditionalProperties)
            .HasColumnName("additional_properties")
            .HasColumnType("jsonb"); // PostgreSQL JSON para propiedades flexibles

        // Configuración de relaciones
        builder.HasOne(ud => ud.User)
            .WithMany(u => u.TrustedDevices)
            .HasForeignKey(ud => ud.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_UserDevices_Users");

        // Configuración de índices para consultas de seguridad
        builder.HasIndex(ud => new { ud.UserId, ud.DeviceFingerprint })
            .IsUnique()
            .HasDatabaseName("IX_UserDevices_User_Fingerprint_Unique");

        builder.HasIndex(ud => ud.DeviceFingerprint)
            .HasDatabaseName("IX_UserDevices_Fingerprint");

        builder.HasIndex(ud => new { ud.IsTrusted, ud.LastSeenAt })
            .HasDatabaseName("IX_UserDevices_Trusted_LastSeen");

        builder.HasIndex(ud => new { ud.UserId, ud.IsTrusted })
            .HasDatabaseName("IX_UserDevices_User_Trusted");

        builder.HasIndex(ud => new { ud.IsBlocked, ud.BlockedAt })
            .HasDatabaseName("IX_UserDevices_Blocked");

        builder.HasIndex(ud => ud.ExpiresAt)
            .HasDatabaseName("IX_UserDevices_ExpiresAt");

        builder.HasIndex(ud => new { ud.DeviceType, ud.OperatingSystem })
            .HasDatabaseName("IX_UserDevices_Type_OS");

        builder.HasIndex(ud => new { ud.Country, ud.City })
            .HasDatabaseName("IX_UserDevices_Location");

        builder.HasIndex(ud => ud.RequiresRevalidation)
            .HasDatabaseName("IX_UserDevices_RequiresRevalidation");

        builder.HasIndex(ud => ud.LastSeenAt)
            .HasDatabaseName("IX_UserDevices_LastSeenAt");

        // Configuración de constraints
        builder.HasCheckConstraint("CK_UserDevices_LoginCounts_Valid",
            "login_count >= 0 AND successful_login_count >= 0 AND successful_login_count <= login_count");

        builder.HasCheckConstraint("CK_UserDevices_DeviceType_Valid",
            "device_type IN ('Desktop', 'Mobile', 'Tablet', 'Unknown')");

        builder.HasCheckConstraint("CK_UserDevices_BlockLogic",
            "(is_blocked = false AND blocked_at IS NULL AND block_reason IS NULL) OR " +
            "(is_blocked = true AND blocked_at IS NOT NULL AND block_reason IS NOT NULL)");

        builder.HasCheckConstraint("CK_UserDevices_TrustLogic",
            "(is_trusted = false AND trusted_at IS NULL) OR (is_trusted = true AND trusted_at IS NOT NULL)");

        builder.HasCheckConstraint("CK_UserDevices_DeviceFingerprint_Length",
            "LENGTH(device_fingerprint) >= 10");

        builder.HasCheckConstraint("CK_UserDevices_LastSeenAfterFirstSeen",
            "last_seen_at >= first_seen_at");

        // Configuración de campos encriptados
        builder.Property(ud => ud.DeviceFingerprint)
            .HasAnnotation("Encrypted", true);

        // Configuración de computed columns para análisis
        builder.Property<double>("TrustScore")
            .HasComputedColumnSql("CASE " +
                "WHEN is_blocked THEN 0 " +
                "WHEN is_trusted AND login_count > 0 THEN (successful_login_count::float / login_count) * 100 " +
                "ELSE 50 END", stored: true)
            .HasColumnName("trust_score");

        builder.Property<bool>("IsExpired")
            .HasComputedColumnSql("expires_at IS NOT NULL AND expires_at <= CURRENT_TIMESTAMP", stored: true)
            .HasColumnName("is_expired");

        // Configuración de auditoría automática
        builder.Property<DateTime>("CreatedAt")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property<DateTime?>("UpdatedAt")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Configuración de soft delete
        builder.Property<bool>("IsDeleted")
            .HasDefaultValue(false);

        builder.Property<DateTime?>("DeletedAt");

        builder.HasQueryFilter(ud => !EF.Property<bool>(ud, "IsDeleted"));

        // Configuración de retención de datos (2 años para dispositivos)
        builder.HasAnnotation("RetentionPolicy", "2Years");

        // Configuración de triggers para limpieza automática
        builder.HasAnnotation("PostgreSQL:TriggerOnUpdate", "update_device_statistics");
        builder.HasAnnotation("PostgreSQL:TriggerOnInsert", "validate_device_limits");
    }
} 