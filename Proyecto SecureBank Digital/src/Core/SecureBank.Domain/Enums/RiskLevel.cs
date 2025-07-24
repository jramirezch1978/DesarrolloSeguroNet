namespace SecureBank.Domain.Enums;

/// <summary>
/// Niveles de riesgo para transacciones y operaciones
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// Riesgo muy bajo - transacciones rutinarias
    /// </summary>
    VeryLow = 1,

    /// <summary>
    /// Riesgo bajo - transacciones normales
    /// </summary>
    Low = 2,

    /// <summary>
    /// Riesgo medio - transacciones que requieren atención
    /// </summary>
    Medium = 3,

    /// <summary>
    /// Riesgo alto - transacciones sospechosas
    /// </summary>
    High = 4,

    /// <summary>
    /// Riesgo crítico - posible fraude
    /// </summary>
    Critical = 5
} 