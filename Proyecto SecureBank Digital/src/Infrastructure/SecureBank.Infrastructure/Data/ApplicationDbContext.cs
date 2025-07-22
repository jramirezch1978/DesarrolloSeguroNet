using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using SecureBank.Application.Common.Interfaces;
using SecureBank.Domain.Entities;
using SecureBank.Domain.Enums;
using SecureBank.Infrastructure.Data.Configurations;

namespace SecureBank.Infrastructure.Data;

/// <summary>
/// Contexto de base de datos principal para SecureBank Digital
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IConfiguration configuration,
        ICurrentUserService currentUserService) : base(options)
    {
        _configuration = configuration;
        _currentUserService = currentUserService;
    }

    // DbSets con setter público para cumplir con la interfaz
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<LoginAttempt> LoginAttempts { get; set; } = null!;
    public DbSet<UserDevice> UserDevices { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configurar esquema por defecto
        modelBuilder.HasDefaultSchema("securebank");

        // Aplicar configuraciones
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new AccountConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new LoginAttemptConfiguration());
        modelBuilder.ApplyConfiguration(new UserDeviceConfiguration());

        // Configuraciones globales
        ConfigureGlobalSettings(modelBuilder);
        ConfigureIndexes(modelBuilder);
        SeedData(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    // Implementar el método BeginTransactionAsync con el tipo correcto
    public async Task<Application.Common.Interfaces.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var efTransaction = await Database.BeginTransactionAsync(cancellationToken);
        return new DbContextTransactionWrapper(efTransaction);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Aplicar encriptación antes de guardar (placeholder)
        await ApplyEncryptionBeforeSaving();

        // Aplicar auditoría automática
        await ApplyAuditingBeforeSaving();

        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ConfigureGlobalSettings(ModelBuilder modelBuilder)
    {
        // Configurar naming convention a snake_case
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(ToSnakeCase(entity.GetTableName() ?? entity.ClrType.Name));

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.GetColumnName()));
            }
        }

        // Configurar DateTime para usar UTC
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var dateTimeProperties = entityType.ClrType.GetProperties()
                .Where(p => p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?));

            foreach (var property in dateTimeProperties)
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(property.Name)
                    .HasConversion(new UtcDateTimeConverter());
            }
        }
    }

    private void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Índices adicionales para rendimiento
        // TODO: Agregar índices específicos una vez que las entidades estén completamente definidas
        
        // Índices básicos para User (usando propiedades que sabemos que existen)
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .HasDatabaseName("IX_Users_Email");

        // Índices básicos para Account
        modelBuilder.Entity<Account>()
            .HasIndex(a => a.UserId)
            .HasDatabaseName("IX_Accounts_UserId");

        // Índices básicos para Transaction
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.AccountId)
            .HasDatabaseName("IX_Transactions_AccountId");

        // Índices básicos para AuditLog
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.Timestamp)
            .HasDatabaseName("IX_AuditLogs_Timestamp");
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // TODO: Implementar seed data usando los constructores correctos de las entidades
        // Por ahora, omitimos el seed data para evitar errores de compilación
        // El seed data se puede agregar más tarde cuando se implemente la funcionalidad completa
        
        // Los datos iniciales se pueden crear mediante migraciones o scripts específicos
        // que respeten los constructores y propiedades de solo lectura de las entidades
    }

    private async Task ApplyEncryptionBeforeSaving()
    {
        // TODO: Implementar encriptación automática de campos marcados
        // Esta funcionalidad se implementará cuando el servicio de encriptación esté configurado
        await Task.CompletedTask;
    }

    private async Task ApplyAuditingBeforeSaving()
    {
        // TODO: Implementar auditoría automática usando los constructores correctos
        // Por ahora, omitimos la auditoría automática para evitar errores de compilación
        // La auditoría se implementará más tarde respetando los constructores de AuditLog
        
        await Task.CompletedTask;
    }

    private object? GetPrimaryKeyValue(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var keyProperty = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        return keyProperty?.CurrentValue;
    }

    private AuditAction GetAuditAction(EntityState state)
    {
        // Usar valores exactos del enum AuditAction
        return state switch
        {
            EntityState.Added => AuditAction.UserCreated,
            EntityState.Modified => AuditAction.ProfileUpdated,
            EntityState.Deleted => AuditAction.AccountClosed,
            _ => AuditAction.Login
        };
    }

    private static string ToSnakeCase(string name)
    {
        return string.Concat(name.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString())).ToLower();
    }
}

/// <summary>
/// Wrapper para IDbContextTransaction de Entity Framework
/// </summary>
public class DbContextTransactionWrapper : Application.Common.Interfaces.IDbContextTransaction
{
    private readonly Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction _efTransaction;

    public DbContextTransactionWrapper(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction efTransaction)
    {
        _efTransaction = efTransaction;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _efTransaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await _efTransaction.RollbackAsync(cancellationToken);
    }

    public void Dispose()
    {
        _efTransaction.Dispose();
    }
}

/// <summary>
/// Convertidor para DateTime UTC
/// </summary>
public class UtcDateTimeConverter : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter() : base(
        v => v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
} 