using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SecureShop.Data;

/// <summary>
/// Contexto de base de datos seguro que implementa principios de auditoría,
/// cifrado de datos sensibles y protección de integridad
/// </summary>
public class SecureDbContext : DbContext
{
    public SecureDbContext(DbContextOptions<SecureDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ===== CONFIGURACIÓN DE USUARIO =====
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AzureAdObjectId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            
            // Índice único para vinculación con Azure AD
            entity.HasIndex(e => e.AzureAdObjectId).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // ===== CONFIGURACIÓN DE PRODUCTO CON CIFRADO =====
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Price).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            
            // Campo cifrado para información sensible (costo)
            entity.Property(e => e.EncryptedCost).HasColumnType("varbinary(128)");
            
            // Relaciones con auditoría
            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== CONFIGURACIÓN DE PEDIDOS =====
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Pending");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            
            // Relaciones
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Orders)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== CONFIGURACIÓN DE ITEMS DE PEDIDO =====
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10,2)").IsRequired();
            
            // Relaciones
            entity.HasOne(e => e.Order)
                  .WithMany(o => o.OrderItems)
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== CONFIGURACIÓN DE ROLES DE USUARIO =====
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RoleName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AssignedAt).HasDefaultValueSql("GETDATE()");
            
            // Relaciones
            entity.HasOne(e => e.User)
                  .WithMany(u => u.UserRoles)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            // Índice compuesto para evitar roles duplicados
            entity.HasIndex(e => new { e.UserId, e.RoleName }).IsUnique();
        });

        // ===== CONFIGURACIÓN DE AUDITORÍA =====
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EntityType).HasMaxLength(50);
            entity.Property(e => e.EntityId).HasMaxLength(50);
            entity.Property(e => e.Changes).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Timestamp).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            
            // Índices para consultas de auditoría eficientes
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
        });

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Override de SaveChanges para implementar auditoría automática
    /// </summary>
    public override int SaveChanges()
    {
        // Implementar auditoría automática de cambios
        var auditEntries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || 
                       e.State == EntityState.Modified || 
                       e.State == EntityState.Deleted)
            .Where(e => !(e.Entity is AuditLog)) // No auditar logs de auditoría
            .Select(e => new AuditLog
            {
                EntityType = e.Entity.GetType().Name,
                EntityId = GetEntityId(e.Entity),
                Action = e.State.ToString(),
                Changes = GetChangesJson(e),
                Timestamp = DateTime.UtcNow,
                UserId = GetCurrentUserId() // Se implementará en el controlador
            }).ToList();

        // Agregar entradas de auditoría
        if (auditEntries.Any())
        {
            AuditLogs.AddRange(auditEntries);
        }

        // Implementar soft delete automático
        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChanges();
    }

    /// <summary>
    /// Obtiene el ID de la entidad para auditoría
    /// </summary>
    private string GetEntityId(object entity)
    {
        var idProperty = entity.GetType().GetProperty("Id");
        return idProperty?.GetValue(entity)?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// Serializa los cambios de la entidad para auditoría
    /// </summary>
    private string GetChangesJson(EntityEntry entry)
    {
        try
        {
            var changes = new Dictionary<string, object>();
            
            foreach (var property in entry.OriginalValues.Properties)
            {
                var originalValue = entry.OriginalValues[property];
                var currentValue = entry.CurrentValues[property];
                
                if (!Equals(originalValue, currentValue))
                {
                    changes[property.Name] = new 
                    { 
                        Original = originalValue, 
                        Current = currentValue 
                    };
                }
            }
            
            return JsonSerializer.Serialize(changes);
        }
        catch
        {
            return "Error serializing changes";
        }
    }

    /// <summary>
    /// Obtiene el ID del usuario actual (se implementará con HttpContext)
    /// </summary>
    private string GetCurrentUserId()
    {
        // TODO: Implementar con HttpContext.User.GetObjectId()
        return "System";
    }
}

// ===== INTERFACE PARA SOFT DELETE =====
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}