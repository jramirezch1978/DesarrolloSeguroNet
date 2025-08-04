using System.ComponentModel.DataAnnotations;

namespace SecureShop.Data;

/// <summary>
/// Modelo de usuario con vinculación segura a Azure AD
/// Implementa principios de auditoría y trazabilidad
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// ID único de Azure AD - no puede ser falsificado
    /// Actúa como vinculación segura entre nuestro sistema y Azure AD
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string AzureAdObjectId { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [MaxLength(100)] //<script>alert('jramirez@npssac.com.pe')</script>
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? FirstName { get; set; }
    
    [MaxLength(50)]
    public string? LastName { get; set; }
    
    /// <summary>
    /// Timestamps de auditoría automática
    /// </summary>
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>
    /// Soft delete para preservar auditoría
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    // ===== RELACIONES =====
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

/// <summary>
/// Modelo de producto con cifrado de datos sensibles
/// Demuestra separación entre datos públicos y confidenciales
/// </summary>
public class Product : ISoftDeletable
{
    public int Id { get; set; }

    /// <summary>
    /// Validación robusta con whitelist de caracteres permitidos
    /// </summary>
    [Required]
    [MaxLength(100)]
    [RegularExpression(@"^[a-zA-Z0-9\s\-\.]+$",
        ErrorMessage = "El nombre contiene caracteres no válidos")]
    //<script>alert('jramirez@npssac.com.pe')</script>
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Precio público - sin cifrado
    /// </summary>
    [Required]
    [Range(0.01, 999999.99, ErrorMessage = "El precio debe estar entre 0.01 y 999,999.99")]
    public decimal Price { get; set; }
    
    /// <summary>
    /// Costo cifrado - información competitiva sensible
    /// Solo personal autorizado puede descifrar estos datos
    /// </summary>
    public byte[]? EncryptedCost { get; set; }
    
    /// <summary>
    /// Auditoría automática de creación
    /// </summary>
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    
    /// <summary>
    /// Soft delete implementation
    /// </summary>
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // ===== RELACIONES =====
    public User CreatedByUser { get; set; } = null!;
}

/// <summary>
/// Modelo de pedido con trazabilidad completa
/// Implementa auditoría de transacciones financieras
/// </summary>
public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Monto total con validación de rango
    /// </summary>
    [Required]
    [Range(0.01, 999999.99)]
    public decimal TotalAmount { get; set; }
    
    /// <summary>
    /// Estados controlados para workflow de procesamiento
    /// </summary>
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Cancelled
    
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    
    // ===== RELACIONES =====
    public User User { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

/// <summary>
/// Item de pedido con integridad referencial
/// </summary>
public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid OrderId { get; set; }
    
    [Required]
    public int ProductId { get; set; }
    
    [Required]
    [Range(1, 1000)]
    public int Quantity { get; set; }
    
    /// <summary>
    /// Precio unitario al momento de la compra
    /// Preserva el precio histórico aunque el producto cambie de precio
    /// </summary>
    [Required]
    [Range(0.01, 999999.99)]
    public decimal UnitPrice { get; set; }
    
    // ===== RELACIONES =====
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

/// <summary>
/// Roles de usuario con autorización granular
/// Implementa principio de menor privilegio
/// </summary>
public class UserRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Nombres de roles estandarizados
    /// Customer, Manager, Admin, ProductManager, StoreManager
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string RoleName { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    public DateTime AssignedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    
    // ===== RELACIONES =====
    public User User { get; set; } = null!;
}

/// <summary>
/// Log de auditoría para investigaciones forenses
/// Captura quién hizo qué, cuándo, desde dónde
/// </summary>
public class AuditLog
{
    public long Id { get; set; }
    
    /// <summary>
    /// ID del usuario que ejecutó la acción
    /// Vinculado al sistema de identidad para trazabilidad
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Acción realizada: CREATE, UPDATE, DELETE, VIEW (para datos sensibles)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// Tipo de entidad afectada (User, Product, Order, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? EntityType { get; set; }
    
    /// <summary>
    /// ID específico del objeto afectado
    /// </summary>
    [MaxLength(50)]
    public string? EntityId { get; set; }
    
    /// <summary>
    /// JSON con los cambios realizados
    /// Permite investigación forense detallada
    /// </summary>
    public string? Changes { get; set; }
    
    /// <summary>
    /// Timestamp UTC inmutable para correlación global
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Información forense para detectar acceso no autorizado
    /// </summary>
    [MaxLength(45)] // IPv6 compatible
    public string? IpAddress { get; set; }
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
}

/// <summary>
/// Modelo para validación de entrada de productos
/// Implementa validación en múltiples capas
/// </summary>
public class ProductCreateModel
{
    /// <summary>
    /// Validación múltiple para máxima seguridad:
    /// - Required: Campo obligatorio
    /// - StringLength: Control de longitud bidireccional  
    /// - RegularExpression: Whitelist approach - solo caracteres seguros
    /// </summary>
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, MinimumLength = 3, 
        ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-\.]+$", 
        ErrorMessage = "El nombre contiene caracteres no válidos")]
    public string Name { get; set; } = string.Empty;

    public void setName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre no puede estar vacío");
        }
        if (name.Length < 3 || name.Length > 100)
        {
            throw new ArgumentOutOfRangeException("El nombre debe tener entre 3 y 100 caracteres");
        }
        if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z0-9\s\-\.]+$"))
        {
            throw new FormatException("El nombre contiene caracteres no válidos");
        }
        Name = name;
    }

    /// <summary>
    /// Descripción opcional con límite de caracteres
    /// </summary>
    [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
    public string? Description { get; set; }

    /// <summary>
    /// Precio con validación de rango para prevenir valores negativos
    /// o astronómicos que podrían causar problemas
    /// </summary>
    [Required(ErrorMessage = "El precio es requerido")]
    [Range(0.01, 999999.99, ErrorMessage = "El precio debe estar entre 0.01 y 999,999.99")]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    /// <summary>
    /// Costo confidencial - solo para roles autorizados
    /// </summary>
    [Range(0.01, 999999.99, ErrorMessage = "El costo debe estar entre 0.01 y 999,999.99")]
    [DataType(DataType.Currency)]
    public decimal? Cost { get; set; }
}

/// <summary>
/// Validador personalizado para prevenir inyección de scripts
/// Implementa detección específica de patrones de ataque
/// </summary>
public class NoScriptInjectionAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is string str)
        {
            // Detectar intentos de inyección de script comunes
            var dangerousPatterns = new[]
            {
                "<script", "javascript:", "vbscript:", "onload=", "onerror=", 
                "onclick=", "onmouseover=", "eval(", "expression("
            };
            
            return !dangerousPatterns.Any(pattern => 
                str.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }
        return true;
    }
}