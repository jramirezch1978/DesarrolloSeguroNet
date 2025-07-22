namespace SecureBank.Domain.Enums;

/// <summary>
/// Tipos de transacción en SecureBank Digital
/// </summary>
public enum TransactionType
{
    // Transferencias
    /// <summary>
    /// Transferencia interna entre cuentas del mismo banco
    /// </summary>
    InternalTransfer = 1,

    /// <summary>
    /// Transferencia a otros bancos
    /// </summary>
    InterbankTransfer = 2,

    /// <summary>
    /// Transferencia inmediata con comisión adicional
    /// </summary>
    ExpressTransfer = 3,

    /// <summary>
    /// Transferencia programada para fecha futura
    /// </summary>
    ScheduledTransfer = 4,

    // Pagos de Servicios
    /// <summary>
    /// Pago de servicios básicos (luz, agua, gas, etc.)
    /// </summary>
    BasicServicePayment = 10,

    /// <summary>
    /// Pago de impuestos y tributos
    /// </summary>
    TaxPayment = 11,

    /// <summary>
    /// Pago de seguros y pensiones
    /// </summary>
    InsurancePayment = 12,

    /// <summary>
    /// Pago de educación y entretenimiento
    /// </summary>
    EducationPayment = 13,

    // Operaciones de Cuenta
    /// <summary>
    /// Depósito en cuenta
    /// </summary>
    Deposit = 20,

    /// <summary>
    /// Retiro de cuenta
    /// </summary>
    Withdrawal = 21,

    /// <summary>
    /// Comisión cobrada por el banco
    /// </summary>
    Fee = 22,

    /// <summary>
    /// Interés ganado en cuenta de ahorros
    /// </summary>
    Interest = 23,

    // Productos Financieros
    /// <summary>
    /// Desembolso de préstamo
    /// </summary>
    LoanDisbursement = 30,

    /// <summary>
    /// Pago de cuota de préstamo
    /// </summary>
    LoanPayment = 31,

    /// <summary>
    /// Inversión en depósito a plazo
    /// </summary>
    FixedDeposit = 32,

    /// <summary>
    /// Redención de depósito a plazo
    /// </summary>
    FixedDepositRedemption = 33,

    /// <summary>
    /// Inversión en fondo mutuo
    /// </summary>
    MutualFundInvestment = 34,

    /// <summary>
    /// Rescate de fondo mutuo
    /// </summary>
    MutualFundRedemption = 35,

    // Trading
    /// <summary>
    /// Compra de acciones
    /// </summary>
    StockPurchase = 40,

    /// <summary>
    /// Venta de acciones
    /// </summary>
    StockSale = 41
} 