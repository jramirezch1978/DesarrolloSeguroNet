namespace SecureBank.Domain.Enums;

/// <summary>
/// Tipos de cuenta bancaria en SecureBank Digital con características específicas
/// </summary>
public enum AccountType
{
    /// <summary>
    /// Cuenta de Ahorros: 2.5% anual, límite $2,000 diarios, sin comisión mantenimiento
    /// </summary>
    Savings = 1,

    /// <summary>
    /// Cuenta Corriente: Sin intereses, sobregiro $500, límite $5,000 diarios, $8 mensual
    /// </summary>
    Checking = 2,

    /// <summary>
    /// Cuenta Premium: 3.2% anual, límite $10,000 diarios, sin comisiones, productos exclusivos
    /// </summary>
    Premium = 3,

    /// <summary>
    /// Cuenta Empresarial: Multi-usuario, límites configurables, reportería avanzada, API access
    /// </summary>
    Business = 4
} 