namespace SecureBank.Domain.Enums;

/// <summary>
/// Frecuencias de recurrencia para transacciones automáticas
/// </summary>
public enum RecurrenceFrequency
{
    /// <summary>
    /// Diario
    /// </summary>
    Daily = 1,

    /// <summary>
    /// Semanal
    /// </summary>
    Weekly = 2,

    /// <summary>
    /// Quincenal (cada 2 semanas)
    /// </summary>
    Biweekly = 3,

    /// <summary>
    /// Mensual
    /// </summary>
    Monthly = 4,

    /// <summary>
    /// Bimestral (cada 2 meses)
    /// </summary>
    Bimonthly = 5,

    /// <summary>
    /// Trimestral (cada 3 meses)
    /// </summary>
    Quarterly = 6,

    /// <summary>
    /// Semestral (cada 6 meses)
    /// </summary>
    Semiannual = 7,

    /// <summary>
    /// Anual
    /// </summary>
    Annual = 8,

    /// <summary>
    /// Personalizado (intervalo definido por el usuario)
    /// </summary>
    Custom = 99
} 