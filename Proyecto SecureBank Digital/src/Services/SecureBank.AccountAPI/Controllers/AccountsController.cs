using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using SecureBank.Application.Features.Accounts.Commands.CreateAccount;
using SecureBank.Application.Features.Accounts.Queries.GetAccountBalance;
using SecureBank.Application.Common.Interfaces;
using SecureBank.Shared.DTOs;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using SecureBank.Domain.Enums;

namespace SecureBank.AccountAPI.Controllers;

/// <summary>
/// Controlador para gestión de cuentas bancarias
/// Maneja operaciones CRUD y consultas de cuentas
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<AccountsController> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el balance de una cuenta específica
    /// </summary>
    /// <param name="accountId">ID de la cuenta</param>
    /// <returns>Balance actual de la cuenta</returns>
    [HttpGet("{accountId}/balance")]
    [ProducesResponseType(typeof(AccountBalanceResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetAccountBalance(Guid accountId)
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

            // Verificar que el usuario tenga acceso a la cuenta
            if (!_currentUserService.HasAccessToAccount(accountId))
            {
                return Forbid();
            }

            var query = new GetAccountBalanceQuery { AccountId = accountId, UserId = userId.Value };
            var response = await _mediator.Send(query);

            if (response.IsSuccess)
            {
                return Ok(response);
            }

            return BadRequest(new ErrorResponse 
            { 
                Message = response.Message ?? "Error al obtener el balance",
                Code = "BALANCE_ERROR"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account balance for account {AccountId}", accountId);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Error interno del servidor",
                Code = "INTERNAL_ERROR"
            });
        }
    }

    /// <summary>
    /// Obtiene los detalles completos de una cuenta
    /// </summary>
    /// <param name="accountId">ID de la cuenta</param>
    /// <returns>Detalles completos de la cuenta</returns>
    [HttpGet("{accountId}")]
    [ProducesResponseType(typeof(AccountDetailsResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetAccountDetails(Guid accountId)
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

            // Verificar acceso a la cuenta
            if (!_currentUserService.HasAccessToAccount(accountId))
            {
                return Forbid();
            }

            // Por ahora devolvemos datos simulados hasta implementar el query
            var response = new AccountDetailsResponse
            {
                AccountId = accountId,
                AccountNumber = "ACC-" + accountId.ToString("N")[..8].ToUpper(),
                AccountType = "Savings",
                Balance = 10000.00m,
                Currency = "PEN",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                LastTransactionDate = DateTime.UtcNow.AddDays(-2)
            };

            // Log successful retrieval
            // await _azureMonitorService.LogSecurityEventAsync(new SecurityEvent
            // {
            //     UserId = userId.Value,
            //     EventType = "AccountDetailsRetrieved",
            //     IsSuccessful = true,
            //     Details = $"Account details retrieved for account {accountId}",
            //     Timestamp = DateTime.UtcNow,
            //     RiskLevel = RiskLevel.Low
            // });

            await Task.CompletedTask; // Fix CS1998

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account details for account {AccountId}", accountId);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Error interno del servidor",
                Code = "INTERNAL_ERROR"
            });
        }
    }

    /// <summary>
    /// Crea una nueva cuenta bancaria
    /// </summary>
    /// <param name="request">Datos para crear la cuenta</param>
    /// <returns>Detalles de la cuenta creada</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Shared.DTOs.CreateAccountResponse), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
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

            var command = new CreateAccountCommand
            {
                UserId = userId.Value,
                AccountType = request.AccountType,
                InitialDeposit = request.InitialDeposit,
                Currency = request.Currency ?? "PEN"
            };

            var response = await _mediator.Send(command);

            if (response.IsSuccess)
            {
                return CreatedAtAction(
                    nameof(GetAccountDetails), 
                    new { accountId = response.AccountId }, 
                    new Shared.DTOs.CreateAccountResponse
                    {
                        IsSuccess = response.IsSuccess,
                        Message = response.Message,
                        AccountId = response.AccountId,
                        AccountNumber = "ACC-" + response.AccountId.ToString("N")[..8].ToUpper(),
                        AccountType = command.AccountType.ToString(),
                        InitialBalance = command.InitialDeposit,
                        Currency = command.Currency,
                        CreatedAt = DateTime.UtcNow
                    });
            }

            return BadRequest(new ErrorResponse 
            { 
                Message = response.Message ?? "Error al crear la cuenta",
                Code = "CREATE_ACCOUNT_ERROR"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Error interno del servidor",
                Code = "INTERNAL_ERROR"
            });
        }
    }

    /// <summary>
    /// Actualiza los límites de transferencia de una cuenta
    /// </summary>
    /// <param name="accountId">ID de la cuenta</param>
    /// <param name="request">Nuevos límites</param>
    /// <returns>Confirmación de actualización</returns>
    [HttpPut("{accountId}/limits")]
    [ProducesResponseType(typeof(UpdateLimitsResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> UpdateTransferLimits(Guid accountId, [FromBody] UpdateLimitsRequest request)
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

            // Verificar acceso a la cuenta
            if (!_currentUserService.HasAccessToAccount(accountId))
            {
                return Forbid();
            }

            // Por ahora simulamos la respuesta
            var response = new UpdateLimitsResponse
            {
                AccountId = accountId,
                DailyLimit = request.DailyLimit,
                MonthlyLimit = request.MonthlyLimit,
                UpdatedAt = DateTime.UtcNow,
                Message = "Límites actualizados exitosamente"
            };

            await Task.CompletedTask; // Fix CS1998

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating limits for account {AccountId}", accountId);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Error interno del servidor",
                Code = "INTERNAL_ERROR"
            });
        }
    }

    /// <summary>
    /// Cierra una cuenta bancaria
    /// </summary>
    /// <param name="accountId">ID de la cuenta a cerrar</param>
    /// <param name="request">Motivo del cierre</param>
    /// <returns>Confirmación del cierre</returns>
    [HttpDelete("{accountId}")]
    [ProducesResponseType(typeof(CloseAccountResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> CloseAccount(Guid accountId, [FromBody] CloseAccountRequest request)
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

            // Verificar acceso a la cuenta
            if (!_currentUserService.HasAccessToAccount(accountId))
            {
                return Forbid();
            }

            // Por ahora simulamos la respuesta
            var response = new CloseAccountResponse
            {
                AccountId = accountId,
                ClosedAt = DateTime.UtcNow,
                Reason = request.Reason,
                Message = "Cuenta cerrada exitosamente"
            };

            await Task.CompletedTask; // Fix CS1998

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing account {AccountId}", accountId);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Error interno del servidor",
                Code = "INTERNAL_ERROR"
            });
        }
    }
}

// DTOs para las operaciones de cuentas
public class CreateAccountRequest
{
    [Required]
    public AccountType AccountType { get; set; }
    
    [Required]
    [System.ComponentModel.DataAnnotations.Range(0.01, 1000000)]
    public decimal InitialDeposit { get; set; }
    
    [StringLength(3)]
    public string? Currency { get; set; }
}

public class UpdateLimitsRequest
{
    [Required]
    [System.ComponentModel.DataAnnotations.Range(100, 50000)]
    public decimal DailyLimit { get; set; }
    
    [Required]
    [System.ComponentModel.DataAnnotations.Range(1000, 200000)]
    public decimal MonthlyLimit { get; set; }
}

public class CloseAccountRequest
{
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
} 