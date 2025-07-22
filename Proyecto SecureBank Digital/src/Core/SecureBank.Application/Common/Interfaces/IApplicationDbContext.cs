using Microsoft.EntityFrameworkCore;
using SecureBank.Domain.Entities;

namespace SecureBank.Application.Common.Interfaces;

/// <summary>
/// Interfaz del contexto de base de datos para SecureBank Digital
/// Define las entidades disponibles para la capa Application
/// </summary>
public interface IApplicationDbContext
{
    // Entidades principales
    DbSet<User> Users { get; }
    DbSet<Account> Accounts { get; }
    DbSet<Transaction> Transactions { get; }
    
    // Entidades de auditoría y seguridad
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<LoginAttempt> LoginAttempts { get; }
    DbSet<UserDevice> UserDevices { get; }

    /// <summary>
    /// Guarda los cambios en la base de datos de forma asíncrona
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Inicia una transacción de base de datos
    /// </summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interfaz para transacciones de base de datos
/// </summary>
public interface IDbContextTransaction : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Confirma la transacción
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deshace la transacción
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
} 