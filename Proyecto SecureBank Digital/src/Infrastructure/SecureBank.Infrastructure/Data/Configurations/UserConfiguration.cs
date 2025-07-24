using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureBank.Domain.Entities;
using SecureBank.Domain.Enums;

namespace SecureBank.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de Entity Framework para la entidad User
/// Implementa mapeo con PostgreSQL y configuraciones de seguridad
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Configuración de tabla
        builder.ToTable("users", "securebank");
        builder.HasKey(u => u.Id);

        // Configuración de propiedades
        builder.Property(u => u.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(u => u.DocumentNumber)
            .HasColumnName("document_number")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.DocumentType)
            .HasColumnName("document_type")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(u => u.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.HashedPin)
            .HasColumnName("hashed_pin")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.SecurityQuestion)
            .HasColumnName("security_question")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.SecurityAnswerHash)
            .HasColumnName("security_answer_hash")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(u => u.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(u => u.LastLoginAt)
            .HasColumnName("last_login_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(u => u.LastPasswordChangeAt)
            .HasColumnName("last_password_change_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(u => u.ProfileImageUrl)
            .HasColumnName("profile_image_url")
            .HasMaxLength(500);

        builder.Property(u => u.IsEmailVerified)
            .HasColumnName("is_email_verified")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.IsPhoneVerified)
            .HasColumnName("is_phone_verified")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.IsTwoFactorEnabled)
            .HasColumnName("is_two_factor_enabled")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.TwoFactorSecret)
            .HasColumnName("two_factor_secret")
            .HasMaxLength(100);

        builder.Property(u => u.FailedLoginAttempts)
            .HasColumnName("failed_login_attempts")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(u => u.AccountLockedUntil)
            .HasColumnName("account_locked_until")
            .HasColumnType("timestamp with time zone");

        builder.Property(u => u.CreatedFromIpAddress)
            .HasColumnName("created_from_ip_address")
            .HasMaxLength(45)
            .IsRequired();

        builder.Property(u => u.LastLoginIpAddress)
            .HasColumnName("last_login_ip_address")
            .HasMaxLength(45);

        builder.Property(u => u.DeviceFingerprint)
            .HasColumnName("device_fingerprint")
            .HasMaxLength(100);

        builder.Property(u => u.RequiresPasswordChange)
            .HasColumnName("requires_password_change")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.TermsAcceptedAt)
            .HasColumnName("terms_accepted_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(u => u.TermsAcceptedIpAddress)
            .HasColumnName("terms_accepted_ip_address")
            .HasMaxLength(45);

        // Configuración de value object Address
        builder.OwnsOne(u => u.Address, addressBuilder =>
        {
            addressBuilder.Property(a => a.Street)
                .HasColumnName("address_street")
                .HasMaxLength(100);

            addressBuilder.Property(a => a.Number)
                .HasColumnName("address_number")
                .HasMaxLength(10);

            addressBuilder.Property(a => a.Apartment)
                .HasColumnName("address_apartment")
                .HasMaxLength(20);

            addressBuilder.Property(a => a.District)
                .HasColumnName("address_district")
                .HasMaxLength(50);

            addressBuilder.Property(a => a.Province)
                .HasColumnName("address_province")
                .HasMaxLength(50);

            addressBuilder.Property(a => a.Department)
                .HasColumnName("address_department")
                .HasMaxLength(50);

            addressBuilder.Property(a => a.PostalCode)
                .HasColumnName("address_postal_code")
                .HasMaxLength(10);

            addressBuilder.Property(a => a.Country)
                .HasColumnName("address_country")
                .HasMaxLength(50)
                .HasDefaultValue("PERU");
        });

        // Configuración de relaciones
        builder.HasMany(u => u.Accounts)
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.TrustedDevices)
            .WithOne(d => d.User)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.AuditLogs)
            .WithOne(al => al.User)
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(u => u.LoginAttempts)
            .WithOne(la => la.User)
            .HasForeignKey(la => la.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configuración de índices únicos
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email_Unique");

        builder.HasIndex(u => new { u.DocumentType, u.DocumentNumber })
            .IsUnique()
            .HasDatabaseName("IX_Users_Document_Unique");

        builder.HasIndex(u => u.PhoneNumber)
            .IsUnique()
            .HasDatabaseName("IX_Users_Phone_Unique");

        // Índices adicionales para rendimiento
        builder.HasIndex(u => u.Status)
            .HasDatabaseName("IX_Users_Status");

        builder.HasIndex(u => u.Role)
            .HasDatabaseName("IX_Users_Role");

        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName("IX_Users_CreatedAt");

        builder.HasIndex(u => u.LastLoginAt)
            .HasDatabaseName("IX_Users_LastLoginAt");

        // Configuración de constraints y validaciones
        builder.HasCheckConstraint("CK_Users_Email_Format", 
            "email ~ '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}$'");

        builder.HasCheckConstraint("CK_Users_Phone_Format",
            "phone_number ~ '^\\+?[1-9]\\d{8,14}$'");

        builder.HasCheckConstraint("CK_Users_DocumentType_Valid",
            "document_type IN ('DNI', 'CE', 'PASSPORT', 'RUC')");

        builder.HasCheckConstraint("CK_Users_FailedAttempts_Range",
            "failed_login_attempts >= 0 AND failed_login_attempts <= 10");

        // Configuración de campos encriptados (marcadores para el interceptor)
        builder.Property(u => u.DocumentNumber)
            .HasAnnotation("Encrypted", true);

        builder.Property(u => u.PhoneNumber)
            .HasAnnotation("Encrypted", true);

        // Configuración de auditoría automática
        builder.Property<DateTime>("CreatedAt")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property<DateTime?>("UpdatedAt")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Configuración de soft delete (opcional)
        builder.Property<bool>("IsDeleted")
            .HasDefaultValue(false);

        builder.Property<DateTime?>("DeletedAt");

        builder.HasQueryFilter(u => !EF.Property<bool>(u, "IsDeleted"));
    }
} 