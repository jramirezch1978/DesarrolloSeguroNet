using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureBank.Domain.Entities;

namespace SecureBank.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de Entity Framework para la entidad Account
/// Implementa mapeo con PostgreSQL para cuentas bancarias
/// </summary>
public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        // Configuración de tabla
        builder.ToTable("accounts", "securebank");
        builder.HasKey(a => a.Id);

        // Configuración de propiedades
        builder.Property(a => a.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(a => a.AccountNumber)
            .HasColumnName("account_number")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.AccountType)
            .HasColumnName("account_type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(a => a.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(a => a.Balance)
            .HasColumnName("balance")
            .HasColumnType("decimal(18,2)")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.AvailableBalance)
            .HasColumnName("available_balance")
            .HasColumnType("decimal(18,2)")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired()
            .HasDefaultValue("PEN");

        builder.Property(a => a.DailyTransferLimit)
            .HasColumnName("daily_transfer_limit")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(a => a.MonthlyTransferLimit)
            .HasColumnName("monthly_transfer_limit")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(a => a.OverdraftLimit)
            .HasColumnName("overdraft_limit")
            .HasColumnType("decimal(18,2)")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.InterestRate)
            .HasColumnName("interest_rate")
            .HasColumnType("decimal(5,2)")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.MaintenanceFee)
            .HasColumnName("maintenance_fee")
            .HasColumnType("decimal(10,2)")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.FreeWithdrawalsPerMonth)
            .HasColumnName("free_withdrawals_per_month")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.WithdrawalFeeAfterLimit)
            .HasColumnName("withdrawal_fee_after_limit")
            .HasColumnType("decimal(10,2)")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(a => a.ClosedAt)
            .HasColumnName("closed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(a => a.CreatedFromIpAddress)
            .HasColumnName("created_from_ip_address")
            .HasMaxLength(45)
            .IsRequired();

        builder.Property(a => a.LastTransactionAt)
            .HasColumnName("last_transaction_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(a => a.WithdrawalsThisMonth)
            .HasColumnName("withdrawals_this_month")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.TransactionsToday)
            .HasColumnName("transactions_today")
            .HasColumnType("decimal(18,2)")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.TransactionsThisMonth)
            .HasColumnName("transactions_this_month")
            .HasColumnType("decimal(18,2)")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.LastInterestCalculation)
            .HasColumnName("last_interest_calculation")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Configuración de relaciones
        builder.HasOne(a => a.User)
            .WithMany(u => u.Accounts)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_Accounts_Users");

        builder.HasMany(a => a.Transactions)
            .WithOne(t => t.Account)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_Transactions_Accounts");

        // Configuración de índices
        builder.HasIndex(a => a.AccountNumber)
            .IsUnique()
            .HasDatabaseName("IX_Accounts_Number_Unique");

        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_Accounts_UserId");

        builder.HasIndex(a => new { a.UserId, a.AccountType })
            .HasDatabaseName("IX_Accounts_User_Type");

        builder.HasIndex(a => a.IsActive)
            .HasDatabaseName("IX_Accounts_IsActive");

        builder.HasIndex(a => a.CreatedAt)
            .HasDatabaseName("IX_Accounts_CreatedAt");

        builder.HasIndex(a => a.Balance)
            .HasDatabaseName("IX_Accounts_Balance");

        // Configuración de constraints
        builder.HasCheckConstraint("CK_Accounts_Balance_NonNegative_WithOverdraft",
            "balance >= -overdraft_limit");

        builder.HasCheckConstraint("CK_Accounts_AvailableBalance_Valid",
            "available_balance <= balance");

        builder.HasCheckConstraint("CK_Accounts_Currency_Valid",
            "currency IN ('PEN', 'USD', 'EUR')");

        builder.HasCheckConstraint("CK_Accounts_Limits_Valid",
            "daily_transfer_limit > 0 AND monthly_transfer_limit >= daily_transfer_limit");

        builder.HasCheckConstraint("CK_Accounts_InterestRate_Range",
            "interest_rate >= 0 AND interest_rate <= 50");

        builder.HasCheckConstraint("CK_Accounts_Fees_NonNegative",
            "maintenance_fee >= 0 AND withdrawal_fee_after_limit >= 0");

        builder.HasCheckConstraint("CK_Accounts_AccountNumber_Format",
            "account_number ~ '^[0-9]{10}$'");

        // Configuración de auditoría
        builder.Property<DateTime>("CreatedAt")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property<DateTime?>("UpdatedAt")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Configuración de soft delete
        builder.Property<bool>("IsDeleted")
            .HasDefaultValue(false);

        builder.Property<DateTime?>("DeletedAt");

        builder.HasQueryFilter(a => !EF.Property<bool>(a, "IsDeleted"));

        // Configuración de encriptación para datos sensibles
        builder.Property(a => a.Balance)
            .HasAnnotation("Encrypted", true);

        builder.Property(a => a.AvailableBalance)
            .HasAnnotation("Encrypted", true);

        // Configuración para diferentes tipos de cuenta
        builder.HasDiscriminator<int>("account_type")
            .HasValue<Account>((int)Domain.Enums.AccountType.Savings)
            .HasValue<Account>((int)Domain.Enums.AccountType.Checking)
            .HasValue<Account>((int)Domain.Enums.AccountType.Premium)
            .HasValue<Account>((int)Domain.Enums.AccountType.Business);
    }
} 