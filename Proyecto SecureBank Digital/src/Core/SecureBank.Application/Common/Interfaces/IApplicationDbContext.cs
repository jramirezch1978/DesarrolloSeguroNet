using Microsoft.EntityFrameworkCore;
using SecureBank.Domain.Entities;

namespace SecureBank.Application.Common.Interfaces;

/// <summary>
/// Interfaz para el contexto de base de datos de la aplicación
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>
    /// Conjunto de entidades de usuarios
    /// </summary>
    DbSet<User> Users { get; set; }
    DbSet<Account> Accounts { get; set; }
    DbSet<Transaction> Transactions { get; set; }

    /// <summary>
    /// Auditoría y logs
    /// </summary>
    DbSet<AuditLog> AuditLogs { get; set; }
    DbSet<LoginAttempt> LoginAttempts { get; set; }
    DbSet<UserDevice> UserDevices { get; set; }

    /// <summary>
    /// Guarda los cambios de forma asíncrona
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
public interface IDbContextTransaction : IDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
} 