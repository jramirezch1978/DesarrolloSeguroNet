namespace SecureBank.Domain.Enums;

/// <summary>
/// Roles de usuario en SecureBank Digital con niveles de autorización específicos
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Cliente Regular: Consultas, transferencias hasta $1,000, pagos básicos
    /// </summary>
    Customer = 1,

    /// <summary>
    /// Cliente Premium: Límites elevados, productos de inversión, asesoría
    /// </summary>
    CustomerPremium = 2,

    /// <summary>
    /// Cliente Empresarial: Múltiples usuarios, aprobaciones jerárquicas, reportes
    /// </summary>
    CustomerBusiness = 3,

    /// <summary>
    /// Operador de Soporte: Solo lectura de cuentas, no acceso a transacciones
    /// </summary>
    SupportOperator = 10,

    /// <summary>
    /// Gerente: Aprobación de transacciones sospechosas, gestión de límites
    /// </summary>
    Manager = 20,

    /// <summary>
    /// Auditor de Seguridad: Acceso completo a logs, métricas de seguridad
    /// </summary>
    SecurityAuditor = 30,

    /// <summary>
    /// Administrador: Gestión completa del sistema
    /// </summary>
    Administrator = 40
} 