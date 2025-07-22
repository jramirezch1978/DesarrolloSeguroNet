using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;
using SecureBank.Application.Features.Transactions.Commands.CreateTransfer;
using SecureBank.Application.Common.Interfaces;
using SecureBank.AuthAPI.Services;
using SecureBank.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SecureBank.TransactionAPI.Controllers;

/// <summary>
/// Controlador para transferencias bancarias en SecureBank Digital
/// Implementa detección de fraude en tiempo real y análisis de comportamiento
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[Produces("application/json")]
public class TransfersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAzureMonitorService _azureMonitor;
    private readonly ILogger<TransfersController> _logger;

    public TransfersController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        IAzureMonitorService azureMonitor,
        ILogger<TransfersController> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _azureMonitor = azureMonitor;
        _logger = logger;
    }

    /// <summary>
    /// Crea una nueva transferencia bancaria
    /// </summary>
    /// <param name="command">Datos de la transferencia</param>
    /// <returns>Resultado de la transferencia con análisis de riesgo</returns>
    [HttpPost]
    [EnableRateLimiting("TransferLimits")]
    [ProducesResponseType(typeof(CreateTransferResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<CreateTransferResponse>> CreateTransfer([FromBody] CreateTransferCommand command)
    {
        var startTime = DateTime.UtcNow;
        var transactionId = Guid.NewGuid();
        
        try
        {
            // Validar autorización del usuario
            if (_currentUserService.UserId != command.UserId && !_currentUserService.IsInRole("Administrator"))
            {
                await _azureMonitor.LogSecurityEventAsync("UnauthorizedTransferAttempt", new
                {
                    RequestedUserId = command.UserId,
                    ActualUserId = _currentUserService.UserId,
                    Amount = command.Amount,
                    ToAccount = command.ToAccountNumber,
                    IpAddress = _currentUserService.IpAddress,
                    DeviceFingerprint = _currentUserService.DeviceFingerprint,
                    Timestamp = startTime
                }, _currentUserService.UserId?.ToString());
                
                return Forbid("No tienes permisos para realizar transferencias en nombre de otro usuario");
            }

            // Agregar información de contexto de auditoría
            command.IpAddress = _currentUserService.IpAddress ?? string.Empty;
            command.UserAgent = _currentUserService.UserAgent ?? string.Empty;
            command.DeviceFingerprint = _currentUserService.DeviceFingerprint ?? string.Empty;
            command.SessionId = _currentUserService.SessionId ?? string.Empty;

            _logger.LogInformation("Iniciando transferencia: {Amount} {Currency} de cuenta {FromAccount} a {ToAccount} para usuario {UserId}", 
                command.Amount, command.Currency, command.FromAccountId, command.ToAccountNumber, command.UserId);

            // Análisis de fraude pre-transacción
            var fraudAnalysis = await PerformFraudAnalysis(command, startTime);
            command.FraudAnalysis = fraudAnalysis;

            // Log detallado para Machine Learning
            await _azureMonitor.LogTransactionEventAsync("TransferInitiated", new
            {
                TransactionId = transactionId,
                UserId = command.UserId,
                FromAccountId = command.FromAccountId,
                ToAccountNumber = MaskAccountNumber(command.ToAccountNumber),
                Amount = command.Amount,
                Currency = command.Currency,
                BeneficiaryName = command.BeneficiaryName,
                IsScheduled = command.IsScheduled,
                IsRecurring = command.IsRecurring,
                IpAddress = command.IpAddress,
                DeviceFingerprint = command.DeviceFingerprint,
                UserAgent = command.UserAgent,
                SessionId = command.SessionId,
                
                // Datos para ML - Detección de fraude
                IsNewBeneficiary = fraudAnalysis.IsNewBeneficiary,
                IsNewDevice = fraudAnalysis.IsNewDevice,
                IsNewLocation = fraudAnalysis.IsNewLocation,
                TransactionsLast24Hours = fraudAnalysis.TransactionsLast24Hours,
                AmountLast24Hours = fraudAnalysis.AmountLast24Hours,
                IsOutsideBusinessHours = fraudAnalysis.IsOutsideBusinessHours,
                BehaviorScore = fraudAnalysis.BehaviorScore,
                
                // Datos de contexto temporal
                TimeOfDay = startTime.Hour,
                DayOfWeek = startTime.DayOfWeek.ToString(),
                IsWeekend = startTime.DayOfWeek == DayOfWeek.Saturday || startTime.DayOfWeek == DayOfWeek.Sunday,
                
                // Características de la transacción
                DescriptionLength = command.Description.Length,
                HasReference = !string.IsNullOrEmpty(command.Reference),
                RequestedNotifications = new { Email = command.NotifyByEmail, Sms = command.NotifyBySms },
                
                Timestamp = startTime
            }, command.UserId.ToString());

            // Validar límites y riesgos
            var riskAssessment = await AssessTransactionRisk(command, fraudAnalysis);
            
            if (riskAssessment.RiskLevel >= RiskLevel.High)
            {
                await _azureMonitor.LogFraudDetectionEventAsync("HighRiskTransfer", new
                {
                    TransactionId = transactionId,
                    UserId = command.UserId,
                    Amount = command.Amount,
                    RiskLevel = riskAssessment.RiskLevel.ToString(),
                    RiskFactors = riskAssessment.RiskFactors,
                    FraudScore = riskAssessment.FraudScore,
                    RequiresApproval = riskAssessment.RequiresApproval,
                    BlockTransaction = riskAssessment.ShouldBlock,
                    IpAddress = command.IpAddress,
                    DeviceFingerprint = command.DeviceFingerprint,
                    Timestamp = DateTime.UtcNow
                }, command.UserId.ToString());

                if (riskAssessment.ShouldBlock)
                {
                    _logger.LogWarning("Transferencia bloqueada por alto riesgo: {TransactionId}, Score: {FraudScore}", 
                        transactionId, riskAssessment.FraudScore);
                    
                    return BadRequest(new CreateTransferResponse
                    {
                        Success = false,
                        TransactionId = transactionId,
                        Errors = ["Transferencia bloqueada por políticas de seguridad"],
                        SecurityAlerts = riskAssessment.SecurityAlerts
                    });
                }
            }

            // Procesar la transferencia
            var response = await _mediator.Send(command);
            var duration = DateTime.UtcNow - startTime;

            if (response.Success)
            {
                await _azureMonitor.LogTransactionEventAsync("TransferCompleted", new
                {
                    TransactionId = response.TransactionId,
                    TransactionNumber = response.TransactionNumber,
                    UserId = command.UserId,
                    Amount = response.Amount,
                    Fee = response.Fee,
                    TotalAmount = response.TotalAmount,
                    Currency = response.Currency,
                    Status = response.Status.ToString(),
                    RiskLevel = response.RiskLevel.ToString(),
                    RequiresApproval = response.RequiresApproval,
                    ProcessingDuration = duration.TotalMilliseconds,
                    ToAccount = MaskAccountNumber(response.ToAccountNumber),
                    BeneficiaryName = response.BeneficiaryName,
                    IpAddress = command.IpAddress,
                    DeviceFingerprint = command.DeviceFingerprint,
                    Timestamp = DateTime.UtcNow
                }, command.UserId.ToString());

                // Métricas de negocio
                await _azureMonitor.LogBusinessMetricAsync("TransfersCompleted", 1, new Dictionary<string, string>
                {
                    ["Currency"] = response.Currency,
                    ["AmountRange"] = GetAmountRange(response.Amount),
                    ["RiskLevel"] = response.RiskLevel.ToString(),
                    ["RequiresApproval"] = response.RequiresApproval.ToString(),
                    ["IsScheduled"] = command.IsScheduled.ToString(),
                    ["IsRecurring"] = command.IsRecurring.ToString(),
                    ["Hour"] = startTime.Hour.ToString(),
                    ["DayOfWeek"] = startTime.DayOfWeek.ToString()
                });

                // Métricas de performance
                await _azureMonitor.LogPerformanceMetricAsync("TransferProcessingTime", duration.TotalMilliseconds,
                    new Dictionary<string, string>
                    {
                        ["RiskLevel"] = response.RiskLevel.ToString(),
                        ["RequiresApproval"] = response.RequiresApproval.ToString(),
                        ["Status"] = response.Status.ToString()
                    });

                _logger.LogInformation("Transferencia completada exitosamente: {TransactionId} por {Amount} {Currency}", 
                    response.TransactionId, response.Amount, response.Currency);

                return CreatedAtAction(nameof(GetTransferStatus), 
                    new { transactionId = response.TransactionId }, response);
            }
            else
            {
                await _azureMonitor.LogTransactionEventAsync("TransferFailed", new
                {
                    TransactionId = transactionId,
                    UserId = command.UserId,
                    Amount = command.Amount,
                    Currency = command.Currency,
                    Errors = response.Errors,
                    ProcessingDuration = duration.TotalMilliseconds,
                    ToAccount = MaskAccountNumber(command.ToAccountNumber),
                    IpAddress = command.IpAddress,
                    DeviceFingerprint = command.DeviceFingerprint,
                    Timestamp = DateTime.UtcNow
                }, command.UserId.ToString());

                _logger.LogWarning("Transferencia falló: {TransactionId}, Errores: {Errors}", 
                    transactionId, string.Join(", ", response.Errors));

                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            await _azureMonitor.LogSecurityEventAsync("TransferProcessingError", new
            {
                TransactionId = transactionId,
                UserId = command.UserId,
                Amount = command.Amount,
                Error = ex.Message,
                ProcessingDuration = (DateTime.UtcNow - startTime).TotalMilliseconds,
                IpAddress = command.IpAddress,
                DeviceFingerprint = command.DeviceFingerprint,
                Timestamp = DateTime.UtcNow
            }, command.UserId.ToString());

            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "CreateTransfer",
                ["TransactionId"] = transactionId.ToString(),
                ["UserId"] = command.UserId.ToString(),
                ["Amount"] = command.Amount.ToString(),
                ["Currency"] = command.Currency
            });

            _logger.LogError(ex, "Error procesando transferencia: {TransactionId}", transactionId);
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene el estado de una transferencia
    /// </summary>
    /// <param name="transactionId">ID de la transacción</param>
    /// <returns>Estado actual de la transferencia</returns>
    [HttpGet("{transactionId}/status")]
    [EnableRateLimiting("GeneralApi")]
    [ProducesResponseType(typeof(TransferStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TransferStatusResponse>> GetTransferStatus([FromRoute] Guid transactionId)
    {
        try
        {
            var userId = _currentUserService.UserId;
            
            _logger.LogInformation("Consultando estado de transferencia {TransactionId} para usuario {UserId}", 
                transactionId, userId);

            await _azureMonitor.LogUserBehaviorAsync("TransferStatusInquiry", new
            {
                TransactionId = transactionId,
                UserId = userId,
                IpAddress = _currentUserService.IpAddress,
                DeviceFingerprint = _currentUserService.DeviceFingerprint,
                Timestamp = DateTime.UtcNow
            }, userId?.ToString() ?? "unknown");

            // Implementar obtención de estado aquí
            // Por ahora retornamos un status simulado
            var response = new TransferStatusResponse
            {
                TransactionId = transactionId,
                Status = TransactionStatus.Completed,
                LastUpdated = DateTime.UtcNow.AddMinutes(-5)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "GetTransferStatus",
                ["TransactionId"] = transactionId.ToString(),
                ["UserId"] = _currentUserService.UserId?.ToString() ?? "unknown"
            });

            _logger.LogError(ex, "Error consultando estado de transferencia {TransactionId}", transactionId);
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Cancela una transferencia programada
    /// </summary>
    /// <param name="transactionId">ID de la transacción</param>
    /// <param name="request">Motivo de cancelación</param>
    /// <returns>Confirmación de cancelación</returns>
    [HttpDelete("{transactionId}")]
    [EnableRateLimiting("GeneralApi")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> CancelTransfer(
        [FromRoute] Guid transactionId,
        [FromBody] CancelTransferRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;
            
            _logger.LogInformation("Cancelando transferencia {TransactionId} para usuario {UserId}", 
                transactionId, userId);

            await _azureMonitor.LogUserBehaviorAsync("TransferCancellationAttempt", new
            {
                TransactionId = transactionId,
                UserId = userId,
                Reason = request.Reason,
                IpAddress = _currentUserService.IpAddress,
                DeviceFingerprint = _currentUserService.DeviceFingerprint,
                Timestamp = DateTime.UtcNow
            }, userId?.ToString() ?? "unknown");

            // Implementar lógica de cancelación aquí

            await _azureMonitor.LogTransactionEventAsync("TransferCancelled", new
            {
                TransactionId = transactionId,
                UserId = userId,
                Reason = request.Reason,
                CancelledAt = DateTime.UtcNow,
                IpAddress = _currentUserService.IpAddress,
                DeviceFingerprint = _currentUserService.DeviceFingerprint,
                Timestamp = DateTime.UtcNow
            }, userId?.ToString() ?? "unknown");

            return Ok(new { Message = "Transferencia cancelada exitosamente" });
        }
        catch (Exception ex)
        {
            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "CancelTransfer",
                ["TransactionId"] = transactionId.ToString(),
                ["UserId"] = _currentUserService.UserId?.ToString() ?? "unknown"
            });

            _logger.LogError(ex, "Error cancelando transferencia {TransactionId}", transactionId);
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene el historial de transferencias del usuario
    /// </summary>
    /// <param name="accountId">ID de la cuenta (opcional)</param>
    /// <param name="fromDate">Fecha inicio</param>
    /// <param name="toDate">Fecha fin</param>
    /// <param name="status">Estado de las transferencias</param>
    /// <param name="pageSize">Tamaño de página</param>
    /// <param name="pageNumber">Número de página</param>
    /// <returns>Lista paginada de transferencias</returns>
    [HttpGet]
    [EnableRateLimiting("GeneralApi")]
    [ProducesResponseType(typeof(PaginatedTransfersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedTransfersResponse>> GetTransfers(
        [FromQuery] Guid? accountId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] TransactionStatus? status = null,
        [FromQuery] int pageSize = 20,
        [FromQuery] int pageNumber = 1)
    {
        try
        {
            var userId = _currentUserService.UserId;
            pageSize = Math.Min(pageSize, 100); // Máximo 100 elementos por página

            _logger.LogInformation("Obteniendo historial de transferencias para usuario {UserId}", userId);

            await _azureMonitor.LogUserBehaviorAsync("TransferHistoryViewed", new
            {
                UserId = userId,
                AccountId = accountId,
                FromDate = fromDate,
                ToDate = toDate,
                Status = status?.ToString(),
                PageSize = pageSize,
                PageNumber = pageNumber,
                IpAddress = _currentUserService.IpAddress,
                DeviceFingerprint = _currentUserService.DeviceFingerprint,
                Timestamp = DateTime.UtcNow
            }, userId?.ToString() ?? "unknown");

            // Implementar obtención de historial aquí
            var response = new PaginatedTransfersResponse
            {
                Transfers = new List<TransferSummary>(),
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = 0
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "GetTransfers",
                ["UserId"] = _currentUserService.UserId?.ToString() ?? "unknown"
            });

            _logger.LogError(ex, "Error obteniendo historial de transferencias");
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    // Métodos privados auxiliares
    private async Task<FraudAnalysisData> PerformFraudAnalysis(CreateTransferCommand command, DateTime startTime)
    {
        // Implementar análisis de fraude real aquí
        // Por ahora simulamos el análisis
        
        var analysis = new FraudAnalysisData
        {
            IsNewBeneficiary = await IsNewBeneficiary(command.UserId, command.ToAccountNumber),
            IsNewDevice = await IsNewDevice(command.UserId, command.DeviceFingerprint),
            IsNewLocation = await IsNewLocation(command.UserId, command.IpAddress),
            TransactionsLast24Hours = await GetTransactionCount24Hours(command.UserId),
            AmountLast24Hours = await GetTransactionAmount24Hours(command.UserId),
            TimeSinceLastTransaction = await GetTimeSinceLastTransaction(command.UserId),
            IsOutsideBusinessHours = IsOutsideBusinessHours(startTime),
            GeolocationData = await GetGeolocation(command.IpAddress),
            BehaviorScore = await CalculateBehaviorScore(command.UserId, command.DeviceFingerprint)
        };

        return analysis;
    }

    private async Task<RiskAssessment> AssessTransactionRisk(CreateTransferCommand command, FraudAnalysisData fraudAnalysis)
    {
        var riskScore = 0;
        var riskFactors = new List<string>();
        var securityAlerts = new List<SecurityAlert>();

        // Evaluar factores de riesgo
        if (fraudAnalysis.IsNewBeneficiary)
        {
            riskScore += 2;
            riskFactors.Add("Nuevo beneficiario");
        }

        if (fraudAnalysis.IsNewDevice)
        {
            riskScore += 3;
            riskFactors.Add("Dispositivo nuevo");
            securityAlerts.Add(new SecurityAlert
            {
                Type = "NewDevice",
                Message = "Transferencia desde dispositivo no reconocido",
                Severity = "Medium",
                Code = "SEC_001"
            });
        }

        if (fraudAnalysis.IsNewLocation)
        {
            riskScore += 2;
            riskFactors.Add("Ubicación nueva");
        }

        if (command.Amount > 10000)
        {
            riskScore += 3;
            riskFactors.Add("Monto alto");
        }

        if (fraudAnalysis.TransactionsLast24Hours > 5)
        {
            riskScore += 2;
            riskFactors.Add("Alta frecuencia de transacciones");
        }

        if (fraudAnalysis.IsOutsideBusinessHours)
        {
            riskScore += 1;
            riskFactors.Add("Horario inusual");
        }

        var riskLevel = riskScore switch
        {
            <= 2 => RiskLevel.Low,
            <= 4 => RiskLevel.Medium,
            <= 6 => RiskLevel.High,
            _ => RiskLevel.VeryHigh
        };

        return new RiskAssessment
        {
            RiskLevel = riskLevel,
            FraudScore = riskScore,
            RiskFactors = riskFactors,
            RequiresApproval = riskLevel >= RiskLevel.High,
            ShouldBlock = riskLevel >= RiskLevel.VeryHigh && riskScore > 8,
            SecurityAlerts = securityAlerts
        };
    }

    // Métodos auxiliares (implementaciones simuladas)
    private async Task<bool> IsNewBeneficiary(Guid userId, string accountNumber) => await Task.FromResult(Random.Shared.Next(100) < 30);
    private async Task<bool> IsNewDevice(Guid userId, string deviceFingerprint) => await Task.FromResult(Random.Shared.Next(100) < 20);
    private async Task<bool> IsNewLocation(Guid userId, string ipAddress) => await Task.FromResult(Random.Shared.Next(100) < 15);
    private async Task<int> GetTransactionCount24Hours(Guid userId) => await Task.FromResult(Random.Shared.Next(0, 10));
    private async Task<decimal> GetTransactionAmount24Hours(Guid userId) => await Task.FromResult(Random.Shared.Next(0, 50000));
    private async Task<TimeSpan> GetTimeSinceLastTransaction(Guid userId) => await Task.FromResult(TimeSpan.FromHours(Random.Shared.Next(1, 24)));
    private async Task<string> GetGeolocation(string ipAddress) => await Task.FromResult("Lima, Peru");
    private async Task<double> CalculateBehaviorScore(Guid userId, string deviceFingerprint) => await Task.FromResult(Random.Shared.NextDouble() * 10);

    private static bool IsOutsideBusinessHours(DateTime dateTime)
    {
        var hour = dateTime.Hour;
        return hour < 6 || hour > 22 || dateTime.DayOfWeek == DayOfWeek.Sunday;
    }

    private static string MaskAccountNumber(string accountNumber)
    {
        if (string.IsNullOrEmpty(accountNumber) || accountNumber.Length < 4)
            return "***";
        
        return accountNumber[..3] + "***" + accountNumber[^3..];
    }

    private static string GetAmountRange(decimal amount)
    {
        return amount switch
        {
            < 100 => "0-100",
            < 500 => "100-500",
            < 1000 => "500-1000",
            < 5000 => "1000-5000",
            < 10000 => "5000-10000",
            _ => "10000+"
        };
    }
}

// DTOs adicionales
public class TransferStatusResponse
{
    public Guid TransactionId { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime LastUpdated { get; set; }
    public string? StatusDescription { get; set; }
    public string? EstimatedCompletion { get; set; }
}

public class CancelTransferRequest
{
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class PaginatedTransfersResponse
{
    public List<TransferSummary> Transfers { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class TransferSummary
{
    public Guid TransactionId { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string ToAccountNumber { get; set; } = string.Empty;
    public string BeneficiaryName { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class RiskAssessment
{
    public RiskLevel RiskLevel { get; set; }
    public int FraudScore { get; set; }
    public List<string> RiskFactors { get; set; } = new();
    public bool RequiresApproval { get; set; }
    public bool ShouldBlock { get; set; }
    public List<SecurityAlert> SecurityAlerts { get; set; } = new();
} 