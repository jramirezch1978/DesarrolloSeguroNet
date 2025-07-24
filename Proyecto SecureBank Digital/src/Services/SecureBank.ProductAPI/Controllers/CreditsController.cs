using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using SecureBank.Application.Common.Interfaces;
using SecureBank.Application.Features.Products.Commands.ApplyForCredit;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using Microsoft.ML;
using DomainCreditType = SecureBank.Domain.Enums.CreditType;
using DomainCreditApplicationStatus = SecureBank.Domain.Enums.CreditApplicationStatus;
using SharedApplyForCreditResponse = SecureBank.Shared.DTOs.ApplyForCreditResponse;
using SharedIncomeInfo = SecureBank.Shared.DTOs.IncomeInfo;
using SharedExpenseInfo = SecureBank.Shared.DTOs.ExpenseInfo;

namespace SecureBank.ProductAPI.Controllers;

/// <summary>
/// Controlador para gestión de productos crediticios
/// Maneja solicitudes de crédito con ML y análisis de riesgo
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
public class CreditsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreditsController> _logger;
    private readonly IAzureMonitorService _azureMonitorService;
    private readonly MLContext _mlContext;

    public CreditsController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<CreditsController> logger,
        IAzureMonitorService azureMonitorService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
        _azureMonitorService = azureMonitorService;
        _mlContext = new MLContext(seed: 0);
    }

    /// <summary>
    /// Solicita un crédito con análisis ML integrado
    /// </summary>
    /// <param name="request">Datos de la solicitud de crédito</param>
    /// <returns>Respuesta de la evaluación crediticia</returns>
    [HttpPost("apply")]
    [ProducesResponseType(typeof(SharedApplyForCreditResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> ApplyForCredit([FromBody] ApplyForCreditRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return Unauthorized(new ErrorResponse 
                { 
                    Message = "Usuario no autenticado",
                    Code = "UNAUTHORIZED"
                });
            }

            // Por ahora simulamos la respuesta hasta completar la implementación
            var response = new SharedApplyForCreditResponse
            {
                IsSuccess = true,
                Message = "Solicitud de crédito procesada exitosamente",
                ApplicationId = Guid.NewGuid(),
                ApplicationNumber = "APP-" + DateTime.Now.Ticks.ToString()[..8],
                CreditType = request.CreditType.ToString(),
                RequestedAmount = request.RequestedAmount,
                Status = "UnderReview",
                ApplicationDate = DateTime.UtcNow,
                NextSteps = "Su solicitud está siendo evaluada. Le contactaremos en 24-48 horas."
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing credit application for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Error interno del servidor",
                Code = "INTERNAL_ERROR"
            });
        }
    }

    /// <summary>
    /// Obtiene el historial de solicitudes de crédito
    /// </summary>
    /// <returns>Lista de solicitudes de crédito</returns>
    [HttpGet("applications")]
    [ProducesResponseType(typeof(List<SharedApplyForCreditResponse>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> GetCreditApplications()
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return Unauthorized(new ErrorResponse 
                { 
                    Message = "Usuario no autenticado",
                    Code = "UNAUTHORIZED"
                });
            }

            // Simulamos algunas solicitudes
            var applications = new List<SharedApplyForCreditResponse>
            {
                new SharedApplyForCreditResponse
                {
                    IsSuccess = true,
                    ApplicationId = Guid.NewGuid(),
                    ApplicationNumber = "APP-20241201",
                    CreditType = "Personal",
                    RequestedAmount = 10000,
                    Status = "Approved",
                    ApplicationDate = DateTime.UtcNow.AddDays(-10)
                }
            };

            return Ok(applications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting credit applications for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Error interno del servidor",
                Code = "INTERNAL_ERROR"
            });
        }
    }
}

// DTOs simplificados para la API
public class ApplyForCreditRequest
{
    [Required]
    public DomainCreditType CreditType { get; set; }

    [Required]
    [Range(1000, 500000)]
    public decimal RequestedAmount { get; set; }

    [Required]
    [Range(6, 360)]
    public int TermInMonths { get; set; }

    [Required]
    [StringLength(500)]
    public string Purpose { get; set; } = string.Empty;

    [Required]
    public SharedIncomeInfo IncomeInfo { get; set; } = new();

    [Required]
    public SharedExpenseInfo ExpenseInfo { get; set; } = new();
}

public class ErrorResponse
{
    public bool IsSuccess { get; set; } = false;
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
} 