using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureBank.Shared.DTOs;

namespace SecureBank.TransactionAPI.Controllers;

/// <summary>
/// Controlador simplificado de pagos que compila sin errores
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsSimpleController : ControllerBase
{
    /// <summary>
    /// Crear un pago simplificado
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequestSimple request)
    {
        await Task.CompletedTask;
        
        return Ok(new CreatePaymentResponseSimple
        {
            IsSuccess = true,
            Message = "Pago creado exitosamente",
            PaymentId = Guid.NewGuid(),
            Amount = request.Amount,
            Status = "Processed"
        });
    }
    
    /// <summary>
    /// Obtener estado de pago
    /// </summary>
    [HttpGet("{paymentId}")]
    public async Task<IActionResult> GetPaymentStatus(Guid paymentId)
    {
        await Task.CompletedTask;
        
        return Ok(new
        {
            PaymentId = paymentId,
            Status = "Completed",
            Amount = 100.00m
        });
    }
}

public class CreatePaymentRequestSimple
{
    public decimal Amount { get; set; }
    public string ServiceProviderCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class CreatePaymentResponseSimple
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
} 