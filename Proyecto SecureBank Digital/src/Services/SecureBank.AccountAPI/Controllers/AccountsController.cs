using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;
using SecureBank.Application.Features.Accounts.Commands.CreateAccount;
using SecureBank.Application.Features.Accounts.Queries.GetAccountBalance;
using SecureBank.Application.Common.Interfaces;
using SecureBank.AuthAPI.Services;
using SecureBank.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SecureBank.AccountAPI.Controllers;

/// <summary>
/// Controlador para gestión de cuentas bancarias en SecureBank Digital
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[Produces("application/json")]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAzureMonitorService _azureMonitor;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        IAzureMonitorService azureMonitor,
        ILogger<AccountsController> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _azureMonitor = azureMonitor;
        _logger = logger;
    }

    /// <summary>
    /// Crea una nueva cuenta bancaria para el usuario autenticado
    /// </summary>
    /// <param name="command">Información de la cuenta a crear</param>
    /// <returns>Detalles de la cuenta creada</returns>
    [HttpPost]
    [EnableRateLimiting("GeneralApi")]
    [ProducesResponseType(typeof(CreateAccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CreateAccountResponse>> CreateAccount([FromBody] CreateAccountCommand command)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Validar que el usuario pueda crear cuentas
            if (_currentUserService.UserId != command.UserId && !_currentUserService.IsInRole("Administrator"))
            {
                await _azureMonitor.LogSecurityEventAsync("UnauthorizedAccountCreation", new
                {
                    RequestedUserId = command.UserId,
                    ActualUserId = _currentUserService.UserId,
                    IpAddress = _currentUserService.IpAddress,
                    Timestamp = DateTime.UtcNow
                }, _currentUserService.UserId?.ToString());
                
                return Forbid("No tienes permisos para crear cuentas para otro usuario");
            }

            // Agregar información de auditoría
            command.IpAddress = _currentUserService.IpAddress ?? string.Empty;
            command.UserAgent = _currentUserService.UserAgent ?? string.Empty;
            command.DeviceFingerprint = _currentUserService.DeviceFingerprint ?? string.Empty;

            _logger.LogInformation("Iniciando creación de cuenta: {AccountType} para usuario {UserId}", 
                command.AccountType, command.UserId);

            // Log de inicio de creación de cuenta
            await _azureMonitor.LogUserBehaviorAsync("AccountCreationStarted", new
            {
                UserId = command.UserId,
                AccountType = command.AccountType.ToString(),
                Currency = command.Currency,
                InitialDeposit = command.InitialDeposit,
                IpAddress = command.IpAddress,
                DeviceFingerprint = command.DeviceFingerprint,
                Timestamp = startTime
            }, command.UserId.ToString());

            var response = await _mediator.Send(command);
            var duration = DateTime.UtcNow - startTime;

            if (response.Success)
            {
                // Log de creación exitosa
                await _azureMonitor.LogUserBehaviorAsync("AccountCreated", new
                {
                    AccountId = response.AccountId,
                    AccountNumber = response.AccountNumber,
                    AccountType = response.AccountType.ToString(),
                    Currency = response.Currency,
                    InitialBalance = response.Balance,
                    DailyLimit = response.DailyTransferLimit,
                    MonthlyLimit = response.MonthlyTransferLimit,
                    InterestRate = response.InterestRate,
                    Duration = duration.TotalMilliseconds,
                    IpAddress = command.IpAddress,
                    Timestamp = DateTime.UtcNow
                }, command.UserId.ToString());

                // Métricas de negocio
                await _azureMonitor.LogBusinessMetricAsync("AccountsCreated", 1, new Dictionary<string, string>
                {
                    ["AccountType"] = command.AccountType.ToString(),
                    ["Currency"] = command.Currency,
                    ["HasInitialDeposit"] = (command.InitialDeposit > 0).ToString(),
                    ["CreationHour"] = startTime.Hour.ToString()
                });

                _logger.LogInformation("Cuenta creada exitosamente: {AccountId} para usuario {UserId}", 
                    response.AccountId, command.UserId);

                return CreatedAtAction(nameof(GetAccountBalance), 
                    new { accountId = response.AccountId }, response);
            }
            else
            {
                // Log de fallo en creación
                await _azureMonitor.LogUserBehaviorAsync("AccountCreationFailed", new
                {
                    UserId = command.UserId,
                    AccountType = command.AccountType.ToString(),
                    Errors = response.Errors,
                    Duration = duration.TotalMilliseconds,
                    IpAddress = command.IpAddress,
                    Timestamp = DateTime.UtcNow
                }, command.UserId.ToString());

                _logger.LogWarning("Fallo en creación de cuenta para usuario {UserId}: {Errors}", 
                    command.UserId, string.Join(", ", response.Errors));

                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            await _azureMonitor.LogSecurityEventAsync("AccountCreationError", new
            {
                UserId = command.UserId,
                AccountType = command.AccountType.ToString(),
                Error = ex.Message,
                Duration = (DateTime.UtcNow - startTime).TotalMilliseconds,
                IpAddress = _currentUserService.IpAddress,
                Timestamp = DateTime.UtcNow
            }, command.UserId.ToString());

            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "CreateAccount",
                ["UserId"] = command.UserId.ToString(),
                ["AccountType"] = command.AccountType.ToString()
            });

            _logger.LogError(ex, "Error creando cuenta para usuario {UserId}", command.UserId);
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene el saldo y detalles de una cuenta específica
    /// </summary>
    /// <param name="accountId">ID de la cuenta</param>
    /// <param name="includeTransactions">Incluir transacciones recientes</param>
    /// <param name="transactionCount">Número de transacciones a incluir</param>
    /// <returns>Información detallada de la cuenta</returns>
    [HttpGet("{accountId}/balance")]
    [EnableRateLimiting("GeneralApi")]
    [ProducesResponseType(typeof(GetAccountBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetAccountBalanceResponse>> GetAccountBalance(
        [FromRoute] Guid accountId,
        [FromQuery] bool includeTransactions = false,
        [FromQuery] int transactionCount = 10)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            var query = new GetAccountBalanceQuery
            {
                AccountIdentifier = accountId.ToString(),
                UserId = _currentUserService.UserId ?? Guid.Empty,
                IncludeRecentTransactions = includeTransactions,
                TransactionCount = Math.Min(transactionCount, 50), // Máximo 50
                IncludeLimits = true,
                IncludeProjections = true,
                IpAddress = _currentUserService.IpAddress ?? string.Empty,
                UserAgent = _currentUserService.UserAgent ?? string.Empty,
                DeviceFingerprint = _currentUserService.DeviceFingerprint ?? string.Empty
            };

            _logger.LogInformation("Consultando saldo de cuenta {AccountId} para usuario {UserId}", 
                accountId, _currentUserService.UserId);

            // Log de consulta de saldo
            await _azureMonitor.LogUserBehaviorAsync("BalanceInquiry", new
            {
                AccountId = accountId,
                UserId = _currentUserService.UserId,
                IncludeTransactions = includeTransactions,
                TransactionCount = transactionCount,
                IpAddress = query.IpAddress,
                DeviceFingerprint = query.DeviceFingerprint,
                Timestamp = startTime,
                TimeOfDay = startTime.Hour,
                DayOfWeek = startTime.DayOfWeek.ToString()
            }, _currentUserService.UserId?.ToString());

            var response = await _mediator.Send(query);
            var duration = DateTime.UtcNow - startTime;

            if (response.Success && response.Account != null)
            {
                // Log de consulta exitosa
                await _azureMonitor.LogUserBehaviorAsync("BalanceRetrieved", new
                {
                    AccountId = accountId,
                    UserId = _currentUserService.UserId,
                    AccountType = response.Account.AccountType.ToString(),
                    Currency = response.Account.Currency,
                    HasSufficientBalance = response.Account.AvailableBalance > 0,
                    TransactionsIncluded = response.RecentTransactions.Count,
                    Duration = duration.TotalMilliseconds,
                    IpAddress = query.IpAddress,
                    Timestamp = DateTime.UtcNow
                }, _currentUserService.UserId?.ToString());

                // Métricas de performance
                await _azureMonitor.LogPerformanceMetricAsync("BalanceInquiryDuration", duration.TotalMilliseconds,
                    new Dictionary<string, string>
                    {
                        ["IncludeTransactions"] = includeTransactions.ToString(),
                        ["TransactionCount"] = transactionCount.ToString()
                    });

                _logger.LogInformation("Saldo obtenido exitosamente para cuenta {AccountId}", accountId);
                return Ok(response);
            }
            else
            {
                await _azureMonitor.LogUserBehaviorAsync("BalanceInquiryFailed", new
                {
                    AccountId = accountId,
                    UserId = _currentUserService.UserId,
                    Errors = response.Errors,
                    Duration = duration.TotalMilliseconds,
                    IpAddress = query.IpAddress,
                    Timestamp = DateTime.UtcNow
                }, _currentUserService.UserId?.ToString());

                _logger.LogWarning("No se pudo obtener saldo para cuenta {AccountId}: {Errors}", 
                    accountId, string.Join(", ", response.Errors));

                return NotFound(response);
            }
        }
        catch (Exception ex)
        {
            await _azureMonitor.LogSecurityEventAsync("BalanceInquiryError", new
            {
                AccountId = accountId,
                UserId = _currentUserService.UserId,
                Error = ex.Message,
                Duration = (DateTime.UtcNow - startTime).TotalMilliseconds,
                IpAddress = _currentUserService.IpAddress,
                Timestamp = DateTime.UtcNow
            }, _currentUserService.UserId?.ToString());

            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "GetAccountBalance",
                ["AccountId"] = accountId.ToString(),
                ["UserId"] = _currentUserService.UserId?.ToString() ?? "unknown"
            });

            _logger.LogError(ex, "Error consultando saldo de cuenta {AccountId}", accountId);
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene todas las cuentas del usuario autenticado
    /// </summary>
    /// <returns>Lista de cuentas del usuario</returns>
    [HttpGet]
    [EnableRateLimiting("GeneralApi")]
    [ProducesResponseType(typeof(List<AccountBalanceInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<AccountBalanceInfo>>> GetUserAccounts()
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            _logger.LogInformation("Obteniendo todas las cuentas para usuario {UserId}", userId);

            await _azureMonitor.LogUserBehaviorAsync("AccountsListViewed", new
            {
                UserId = userId,
                IpAddress = _currentUserService.IpAddress,
                DeviceFingerprint = _currentUserService.DeviceFingerprint,
                Timestamp = DateTime.UtcNow
            }, userId.ToString());

            // Implementar lógica para obtener todas las cuentas del usuario
            // Por ahora retornamos una lista vacía
            var accounts = new List<AccountBalanceInfo>();

            await _azureMonitor.LogUserBehaviorAsync("AccountsListRetrieved", new
            {
                UserId = userId,
                AccountCount = accounts.Count,
                IpAddress = _currentUserService.IpAddress,
                Timestamp = DateTime.UtcNow
            }, userId.ToString());

            return Ok(accounts);
        }
        catch (Exception ex)
        {
            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "GetUserAccounts",
                ["UserId"] = _currentUserService.UserId?.ToString() ?? "unknown"
            });

            _logger.LogError(ex, "Error obteniendo cuentas del usuario");
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Actualiza los límites de transferencia de una cuenta
    /// </summary>
    /// <param name="accountId">ID de la cuenta</param>
    /// <param name="request">Nuevos límites</param>
    /// <returns>Confirmación de actualización</returns>
    [HttpPut("{accountId}/limits")]
    [EnableRateLimiting("GeneralApi")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateAccountLimits(
        [FromRoute] Guid accountId,
        [FromBody] UpdateLimitsRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;
            
            _logger.LogInformation("Actualizando límites para cuenta {AccountId}", accountId);

            await _azureMonitor.LogUserBehaviorAsync("AccountLimitsUpdateAttempt", new
            {
                AccountId = accountId,
                UserId = userId,
                NewDailyLimit = request.DailyLimit,
                NewMonthlyLimit = request.MonthlyLimit,
                IpAddress = _currentUserService.IpAddress,
                Timestamp = DateTime.UtcNow
            }, userId?.ToString());

            // Implementar lógica de actualización de límites aquí

            await _azureMonitor.LogUserBehaviorAsync("AccountLimitsUpdated", new
            {
                AccountId = accountId,
                UserId = userId,
                UpdatedDailyLimit = request.DailyLimit,
                UpdatedMonthlyLimit = request.MonthlyLimit,
                IpAddress = _currentUserService.IpAddress,
                Timestamp = DateTime.UtcNow
            }, userId?.ToString());

            return Ok(new { Message = "Límites actualizados exitosamente" });
        }
        catch (Exception ex)
        {
            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "UpdateAccountLimits",
                ["AccountId"] = accountId.ToString()
            });

            _logger.LogError(ex, "Error actualizando límites de cuenta {AccountId}", accountId);
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Cierra una cuenta bancaria
    /// </summary>
    /// <param name="accountId">ID de la cuenta a cerrar</param>
    /// <param name="request">Motivo del cierre</param>
    /// <returns>Confirmación del cierre</returns>
    [HttpDelete("{accountId}")]
    [EnableRateLimiting("GeneralApi")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> CloseAccount(
        [FromRoute] Guid accountId,
        [FromBody] CloseAccountRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;
            
            _logger.LogInformation("Iniciando cierre de cuenta {AccountId}", accountId);

            await _azureMonitor.LogUserBehaviorAsync("AccountClosureAttempt", new
            {
                AccountId = accountId,
                UserId = userId,
                Reason = request.Reason,
                IpAddress = _currentUserService.IpAddress,
                Timestamp = DateTime.UtcNow
            }, userId?.ToString());

            // Implementar lógica de cierre de cuenta aquí

            await _azureMonitor.LogUserBehaviorAsync("AccountClosed", new
            {
                AccountId = accountId,
                UserId = userId,
                Reason = request.Reason,
                IpAddress = _currentUserService.IpAddress,
                Timestamp = DateTime.UtcNow
            }, userId?.ToString());

            return Ok(new { Message = "Cuenta cerrada exitosamente" });
        }
        catch (Exception ex)
        {
            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "CloseAccount",
                ["AccountId"] = accountId.ToString()
            });

            _logger.LogError(ex, "Error cerrando cuenta {AccountId}", accountId);
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }
}

// DTOs para requests
public class UpdateLimitsRequest
{
    [Range(0, 50000)]
    public decimal DailyLimit { get; set; }

    [Range(0, 500000)]
    public decimal MonthlyLimit { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class CloseAccountRequest
{
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    public bool TransferRemainingBalance { get; set; }
    
    public Guid? DestinationAccountId { get; set; }
} 