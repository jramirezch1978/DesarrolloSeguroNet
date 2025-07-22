namespace SecureBank.Domain.Enums;

/// <summary>
/// Tipos de créditos disponibles en SecureBank Digital
/// </summary>
public enum CreditType
{
    /// <summary>
    /// Crédito personal para gastos varios
    /// </summary>
    Personal = 1,

    /// <summary>
    /// Crédito hipotecario para compra de vivienda
    /// </summary>
    Mortgage = 2,

    /// <summary>
    /// Crédito vehicular para compra de auto
    /// </summary>
    Vehicle = 3,

    /// <summary>
    /// Crédito para pequeñas y medianas empresas
    /// </summary>
    Business = 4,

    /// <summary>
    /// Crédito educativo para estudios
    /// </summary>
    Education = 5,

    /// <summary>
    /// Línea de crédito revolvente
    /// </summary>
    CreditLine = 6,

    /// <summary>
    /// Crédito de consumo general
    /// </summary>
    Consumer = 7
} 