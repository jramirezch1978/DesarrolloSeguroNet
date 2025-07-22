using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SecureBank.Application.Common.Interfaces;
using SecureBank.Domain.Entities;
using SecureBank.Infrastructure.Data.Configurations;

namespace SecureBank.Infrastructure.Data;

/// <summary>
/// Contexto de Entity Framework para SecureBank Digital
/// Implementa acceso a datos con PostgreSQL y configuraciones de seguridad
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Entidades principales
    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    
    // Entidades de auditoría y seguridad
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();

    /// <summary>
    /// Inicia una transacción de base de datos
    /// </summary>
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await Database.BeginTransactionAsync(cancellationToken);
        return new DbContextTransactionWrapper(transaction);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar configuraciones de entidades
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new AccountConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new LoginAttemptConfiguration());
        modelBuilder.ApplyConfiguration(new UserDeviceConfiguration());

        // Configuraciones globales
        ConfigureGlobalSettings(modelBuilder);
        
        // Configurar índices para rendimiento y seguridad
        ConfigureIndexes(modelBuilder);
        
        // Configurar seeds de datos iniciales
        ConfigureSeedData(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Configuración por defecto para desarrollo
            optionsBuilder.UseNpgsql("Server=localhost;Database=SecureBankDigital;User Id=postgres;Password=123456;")
                         .EnableSensitiveDataLogging(false) // Seguridad: no loggear datos sensibles
                         .EnableDetailedErrors(false);       // Seguridad: no exponer detalles en producción
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Actualizar timestamps automáticamente
        UpdateTimestamps();
        
        // Aplicar encriptación a datos sensibles antes de guardar
        await ApplyEncryptionAsync(cancellationToken);
        
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ConfigureGlobalSettings(ModelBuilder modelBuilder)
    {
        // Configurar esquema por defecto
        modelBuilder.HasDefaultSchema("securebank");

        // Configurar convenciones de nombres
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Convertir nombres de tabla a snake_case
            entity.SetTableName(entity.GetTableName()?.ToSnakeCase());
            
            // Convertir nombres de columna a snake_case
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.GetColumnName()?.ToSnakeCase());
            }
        }

        // Configurar UTC para todas las fechas
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(new UtcDateTimeConverter());
                }
            }
        }
    }

    private void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Índices para User
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email_Unique");

        modelBuilder.Entity<User>()
            .HasIndex(u => new { u.DocumentType, u.DocumentNumber })
            .IsUnique()
            .HasDatabaseName("IX_Users_Document_Unique");

        modelBuilder.Entity<User>()
            .HasIndex(u => u.PhoneNumber)
            .IsUnique()
            .HasDatabaseName("IX_Users_Phone_Unique");

        // Índices para Account
        modelBuilder.Entity<Account>()
            .HasIndex(a => a.AccountNumber)
            .IsUnique()
            .HasDatabaseName("IX_Accounts_Number_Unique");

        modelBuilder.Entity<Account>()
            .HasIndex(a => new { a.UserId, a.AccountType })
            .HasDatabaseName("IX_Accounts_User_Type");

        // Índices para Transaction
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.TransactionNumber)
            .IsUnique()
            .HasDatabaseName("IX_Transactions_Number_Unique");

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => new { t.AccountId, t.CreatedAt })
            .HasDatabaseName("IX_Transactions_Account_Date");

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => new { t.Type, t.Status, t.CreatedAt })
            .HasDatabaseName("IX_Transactions_Type_Status_Date");

        // Índices para AuditLog
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => new { a.UserId, a.Timestamp })
            .HasDatabaseName("IX_AuditLogs_User_Timestamp");

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => new { a.Action, a.Level, a.Timestamp })
            .HasDatabaseName("IX_AuditLogs_Action_Level_Timestamp");

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.SequenceNumber)
            .IsUnique()
            .HasDatabaseName("IX_AuditLogs_Sequence_Unique");

        // Índices para LoginAttempt
        modelBuilder.Entity<LoginAttempt>()
            .HasIndex(l => new { l.IpAddress, l.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_IP_Date");

        modelBuilder.Entity<LoginAttempt>()
            .HasIndex(l => new { l.UserId, l.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_User_Date");

        // Índices para UserDevice
        modelBuilder.Entity<UserDevice>()
            .HasIndex(d => new { d.UserId, d.DeviceFingerprint })
            .IsUnique()
            .HasDatabaseName("IX_UserDevices_User_Fingerprint_Unique");

        modelBuilder.Entity<UserDevice>()
            .HasIndex(d => new { d.IsTrusted, d.LastSeenAt })
            .HasDatabaseName("IX_UserDevices_Trusted_LastSeen");
    }

    private void ConfigureSeedData(ModelBuilder modelBuilder)
    {
        // Seed data para administrador inicial
        var adminUserId = new Guid("A0000000-0000-0000-0000-000000000001");
        var adminAccountId = new Guid("B0000000-0000-0000-0000-000000000001");

        modelBuilder.Entity<User>().HasData(new
        {
            Id = adminUserId,
            Email = "admin@securebankdigital.pe",
            DocumentNumber = "12345678",
            DocumentType = "DNI",
            FirstName = "Administrador",
            LastName = "SecureBank",
            PhoneNumber = "+51987654321",
            HashedPin = "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj3XOE.i/.U2", // PIN: 123456
            SecurityQuestion = "¿Cuál es el nombre del banco?",
            SecurityAnswerHash = "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj3XOE.i/.U2", // Respuesta: SecureBank
            Role = Domain.Enums.UserRole.Administrator,
            Status = Domain.Enums.UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedFromIpAddress = "127.0.0.1",
            IsEmailVerified = true,
            IsPhoneVerified = true,
            IsTwoFactorEnabled = false,
            FailedLoginAttempts = 0,
            RequiresPasswordChange = false,
            TermsAcceptedAt = DateTime.UtcNow,
            TermsAcceptedIpAddress = "127.0.0.1"
        });

        modelBuilder.Entity<Account>().HasData(new
        {
            Id = adminAccountId,
            AccountNumber = "1000000001",
            AccountType = Domain.Enums.AccountType.Premium,
            UserId = adminUserId,
            Balance = 0m,
            AvailableBalance = 0m,
            Currency = "PEN",
            DailyTransferLimit = 100000m,
            MonthlyTransferLimit = 1000000m,
            OverdraftLimit = 10000m,
            InterestRate = 3.2m,
            MaintenanceFee = 0m,
            FreeWithdrawalsPerMonth = int.MaxValue,
            WithdrawalFeeAfterLimit = 0m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedFromIpAddress = "127.0.0.1",
            LastTransactionAt = DateTime.UtcNow,
            WithdrawalsThisMonth = 0,
            TransactionsToday = 0m,
            TransactionsThisMonth = 0m,
            LastInterestCalculation = DateTime.UtcNow
        });
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is User user && entry.State == EntityState.Modified)
            {
                // No actualizar CreatedAt para entidades existentes
                entry.Property(nameof(User.CreatedAt)).IsModified = false;
            }
            
            if (entry.Entity is Account account && entry.State == EntityState.Modified)
            {
                entry.Property(nameof(Account.CreatedAt)).IsModified = false;
            }
            
            if (entry.Entity is Transaction transaction && entry.State == EntityState.Modified)
            {
                entry.Property(nameof(Transaction.CreatedAt)).IsModified = false;
            }
        }
    }

    private async Task ApplyEncryptionAsync(CancellationToken cancellationToken)
    {
        // Aquí se aplicaría encriptación a datos sensibles antes de persistir
        // Por ahora es un placeholder para la funcionalidad futura
        await Task.CompletedTask;
    }
}

/// <summary>
/// Wrapper para transacciones de base de datos
/// </summary>
public class DbContextTransactionWrapper : IDbContextTransaction
{
    private readonly IDbContextTransaction _transaction;

    public DbContextTransactionWrapper(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _transaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await _transaction.RollbackAsync(cancellationToken);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
            await _transaction.DisposeAsync();
    }
}

/// <summary>
/// Converter para manejar fechas en UTC
/// </summary>
public class UtcDateTimeConverter : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter() : base(
        v => v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}

/// <summary>
/// Extensión para convertir a snake_case
/// </summary>
public static class StringExtensions
{
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return string.Concat(
            input.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString())
        ).ToLowerInvariant();
    }
} 