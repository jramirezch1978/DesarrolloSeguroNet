using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using SecureBank.Application.Features.Transactions.Commands.CreatePayment;
using SecureBank.Application.Features.Transactions.Queries.GetPaymentHistory;
using SecureBank.Application.Common.Interfaces;
using SecureBank.Domain.Enums;
using SecureBank.Shared.DTOs;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using DomainServiceType = SecureBank.Domain.Enums.ServiceType;

namespace SecureBank.TransactionAPI.Controllers;

/// <summary>
/// Controlador para pagos de servicios en SecureBank Digital
/// Maneja pagos de luz, agua, teléfono, cable, impuestos y otros servicios
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAzureMonitorService _azureMonitor;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        IAzureMonitorService azureMonitor,
        ILogger<PaymentsController> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _azureMonitor = azureMonitor;
        _logger = logger;
    }

    /// <summary>
    /// Procesa un pago de servicios
    /// </summary>
    /// <param name="command">Datos del pago</param>
    /// <returns>Resultado del pago con confirmación</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreatePaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<CreatePaymentResponse>> CreatePayment([FromBody] CreatePaymentCommand command)
    {
        var startTime = DateTime.UtcNow;
        var transactionId = Guid.NewGuid();
        
        try
        {
            // Validar autorización del usuario
            if (_currentUserService.UserId != command.UserId && !_currentUserService.IsInRole("Administrator"))
            {
                await _azureMonitor.LogSecurityEventAsync("UnauthorizedPaymentAttempt", new
                {
                    RequestedUserId = command.UserId,
                    ActualUserId = _currentUserService.UserId,
                    ServiceType = command.ServiceType.ToString(),
                    ServiceProvider = command.ServiceProviderName,
                    Amount = command.Amount,
                    IpAddress = _currentUserService.IpAddress,
                    DeviceFingerprint = _currentUserService.DeviceFingerprint,
                    Timestamp = startTime
                }, _currentUserService.UserId?.ToString());
                
                return Forbid("No tienes permisos para realizar pagos en nombre de otro usuario");
            }

            // Agregar información de contexto de auditoría
            command.IpAddress = _currentUserService.IpAddress ?? string.Empty;
            command.UserAgent = _currentUserService.UserAgent ?? string.Empty;
            command.DeviceFingerprint = _currentUserService.DeviceFingerprint ?? string.Empty;
            command.SessionId = _currentUserService.SessionId ?? string.Empty;

            _logger.LogInformation("Iniciando pago de {ServiceType}: {Amount} {Currency} a {ServiceProvider} para usuario {UserId}", 
                command.ServiceType, command.Amount, command.Currency, command.ServiceProviderName, command.UserId);

            // Análisis específico de fraude para pagos
            var paymentRiskAnalysis = await PerformPaymentRiskAnalysis(command, startTime);

            // Log detallado para Machine Learning específico de pagos
            await _azureMonitor.LogTransactionEventAsync("PaymentInitiated", new
            {
                TransactionId = transactionId,
                UserId = command.UserId,
                FromAccountId = command.FromAccountId,
                ServiceType = command.ServiceType.ToString(),
                ServiceProviderCode = command.ServiceProviderCode,
                ServiceProviderName = command.ServiceProviderName,
                ServiceNumber = MaskServiceNumber(command.ServiceNumber),
                Amount = command.Amount,
                Currency = command.Currency,
                BillingPeriod = command.BillingPeriod,
                InvoiceNumber = command.InvoiceNumber,
                DueDate = command.DueDate,
                IsScheduled = command.IsScheduled,
                IsRecurring = command.IsRecurring,
                IpAddress = command.IpAddress,
                DeviceFingerprint = command.DeviceFingerprint,
                UserAgent = command.UserAgent,
                SessionId = command.SessionId,
                
                // Análisis específico de pagos para ML
                IsNewServiceProvider = paymentRiskAnalysis.IsNewServiceProvider,
                PaymentFrequency = paymentRiskAnalysis.PaymentFrequency,
                AveragePaymentAmount = paymentRiskAnalysis.AveragePaymentAmount,
                DeviationFromAverage = paymentRiskAnalysis.DeviationFromAverage,
                DaysUntilDueDate = paymentRiskAnalysis.DaysUntilDueDate,
                IsLatePayment = paymentRiskAnalysis.IsLatePayment,
                IsHolidayPeriod = paymentRiskAnalysis.IsHolidayPeriod,
                ServiceCategoryRisk = paymentRiskAnalysis.ServiceCategoryRisk,
                
                // Datos de contexto temporal
                TimeOfDay = startTime.Hour,
                DayOfWeek = startTime.DayOfWeek.ToString(),
                IsWeekend = startTime.DayOfWeek == DayOfWeek.Saturday || startTime.DayOfWeek == DayOfWeek.Sunday,
                
                // Características específicas del pago
                HasInvoiceNumber = !string.IsNullOrEmpty(command.InvoiceNumber),
                HasDueDate = command.DueDate.HasValue,
                HasBillingPeriod = !string.IsNullOrEmpty(command.BillingPeriod),
                RecurrenceFrequency = command.RecurrenceConfig?.Frequency.ToString(),
                
                Timestamp = startTime
            }, command.UserId.ToString());

            // Validación específica de servicios
            var serviceValidation = await ValidateServiceProvider(command);
            if (!serviceValidation.IsValid)
            {
                await _azureMonitor.LogSecurityEventAsync("InvalidServiceProvider", new
                {
                    TransactionId = transactionId,
                    ServiceType = command.ServiceType.ToString(),
                    ServiceProviderCode = command.ServiceProviderCode,
                    ServiceNumber = MaskServiceNumber(command.ServiceNumber),
                    ValidationErrors = serviceValidation.Errors,
                    UserId = command.UserId,
                    IpAddress = command.IpAddress,
                    Timestamp = DateTime.UtcNow
                }, command.UserId.ToString());

                return BadRequest(new CreatePaymentResponse
                {
                    Success = false,
                    TransactionId = transactionId,
                    Errors = serviceValidation.Errors
                });
            }

            // Evaluación de riesgo específica para pagos
            var riskAssessment = await AssessPaymentRisk(command, paymentRiskAnalysis);
            
            if (riskAssessment.RiskLevel >= RiskLevel.High)
            {
                await _azureMonitor.LogFraudDetectionEventAsync("HighRiskPayment", new
                {
                    TransactionId = transactionId,
                    UserId = command.UserId,
                    ServiceType = command.ServiceType.ToString(),
                    ServiceProvider = command.ServiceProviderName,
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
                    _logger.LogWarning("Pago bloqueado por alto riesgo: {TransactionId}, Score: {FraudScore}", 
                        transactionId, riskAssessment.FraudScore);
                    
                    return BadRequest(new CreatePaymentResponse
                    {
                        Success = false,
                        TransactionId = transactionId,
                        Errors = ["Pago bloqueado por políticas de seguridad"],
                        SecurityAlerts = riskAssessment.SecurityAlerts
                    });
                }
            }

            // Procesar el pago
            var response = await _mediator.Send(command);
            var duration = DateTime.UtcNow - startTime;

            if (response.Success)
            {
                await _azureMonitor.LogTransactionEventAsync("PaymentCompleted", new
                {
                    TransactionId = response.TransactionId,
                    TransactionNumber = response.TransactionNumber,
                    UserId = command.UserId,
                    ServiceType = command.ServiceType.ToString(),
                    ServiceProvider = command.ServiceProviderName,
                    Amount = response.Amount,
                    Fee = response.Fee,
                    TotalAmount = response.TotalAmount,
                    Currency = response.Currency,
                    Status = response.Status.ToString(),
                    RiskLevel = response.RiskLevel.ToString(),
                    RequiresApproval = response.RequiresApproval,
                    ProcessingDuration = duration.TotalMilliseconds,
                    ConfirmationCode = response.ConfirmationCode,
                    IpAddress = command.IpAddress,
                    DeviceFingerprint = command.DeviceFingerprint,
                    Timestamp = DateTime.UtcNow
                }, command.UserId.ToString());

                // Métricas de negocio específicas de pagos
                await _azureMonitor.LogBusinessMetricAsync("PaymentsCompleted", 1, new Dictionary<string, string>
                {
                    ["ServiceType"] = command.ServiceType.ToString(),
                    ["ServiceCategory"] = GetServiceCategory(command.ServiceType),
                    ["Currency"] = response.Currency,
                    ["AmountRange"] = GetAmountRange(response.Amount),
                    ["RiskLevel"] = response.RiskLevel.ToString(),
                    ["RequiresApproval"] = response.RequiresApproval.ToString(),
                    ["IsScheduled"] = command.IsScheduled.ToString(),
                    ["IsRecurring"] = command.IsRecurring.ToString(),
                    ["HasDueDate"] = command.DueDate.HasValue.ToString(),
                    ["IsLatePayment"] = (command.DueDate.HasValue && command.DueDate < DateTime.Now).ToString(),
                    ["Hour"] = startTime.Hour.ToString(),
                    ["DayOfWeek"] = startTime.DayOfWeek.ToString()
                });

                // Métricas de performance
                await _azureMonitor.LogPerformanceMetricAsync("PaymentProcessingTime", duration.TotalMilliseconds,
                    new Dictionary<string, string>
                    {
                        ["ServiceType"] = command.ServiceType.ToString(),
                        ["RiskLevel"] = response.RiskLevel.ToString(),
                        ["RequiresApproval"] = response.RequiresApproval.ToString(),
                        ["Status"] = response.Status.ToString()
                    });

                _logger.LogInformation("Pago completado exitosamente: {TransactionId} - {ServiceType} por {Amount} {Currency}", 
                    response.TransactionId, command.ServiceType, response.Amount, response.Currency);

                return CreatedAtAction(nameof(GetPaymentStatus), 
                    new { transactionId = response.TransactionId }, response);
            }
            else
            {
                await _azureMonitor.LogTransactionEventAsync("PaymentFailed", new
                {
                    TransactionId = transactionId,
                    UserId = command.UserId,
                    ServiceType = command.ServiceType.ToString(),
                    ServiceProvider = command.ServiceProviderName,
                    Amount = command.Amount,
                    Currency = command.Currency,
                    Errors = response.Errors,
                    ProcessingDuration = duration.TotalMilliseconds,
                    IpAddress = command.IpAddress,
                    DeviceFingerprint = command.DeviceFingerprint,
                    Timestamp = DateTime.UtcNow
                }, command.UserId.ToString());

                _logger.LogWarning("Pago falló: {TransactionId}, Errores: {Errors}", 
                    transactionId, string.Join(", ", response.Errors));

                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            await _azureMonitor.LogSecurityEventAsync("PaymentProcessingError", new
            {
                TransactionId = transactionId,
                UserId = command.UserId,
                ServiceType = command.ServiceType.ToString(),
                Amount = command.Amount,
                Error = ex.Message,
                ProcessingDuration = (DateTime.UtcNow - startTime).TotalMilliseconds,
                IpAddress = command.IpAddress,
                DeviceFingerprint = command.DeviceFingerprint,
                Timestamp = DateTime.UtcNow
            }, command.UserId.ToString());

            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "CreatePayment",
                ["TransactionId"] = transactionId.ToString(),
                ["UserId"] = command.UserId.ToString(),
                ["ServiceType"] = command.ServiceType.ToString(),
                ["Amount"] = command.Amount.ToString()
            });

            _logger.LogError(ex, "Error procesando pago: {TransactionId}", transactionId);
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene el estado de un pago
    /// </summary>
    /// <param name="transactionId">ID de la transacción de pago</param>
    /// <returns>Estado actual del pago</returns>
    [HttpGet("{transactionId}/status")]
    [ProducesResponseType(typeof(PaymentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaymentStatusResponse>> GetPaymentStatus([FromRoute] Guid transactionId)
    {
        try
        {
            var userId = _currentUserService.UserId;
            
            _logger.LogInformation("Consultando estado de pago {TransactionId} para usuario {UserId}", 
                transactionId, userId);

            await _azureMonitor.LogUserBehaviorAsync("PaymentStatusInquiry", new
            {
                TransactionId = transactionId,
                UserId = userId,
                IpAddress = _currentUserService.IpAddress,
                DeviceFingerprint = _currentUserService.DeviceFingerprint,
                Timestamp = DateTime.UtcNow
            }, userId?.ToString() ?? "unknown");

            // Implementar obtención de estado aquí
            var response = new PaymentStatusResponse
            {
                TransactionId = transactionId,
                Status = TransactionStatus.Completed,
                LastUpdated = DateTime.UtcNow.AddMinutes(-2),
                ConfirmationCode = "CONF" + Random.Shared.Next(100000, 999999),
                ServiceProvider = "Luz del Sur"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "GetPaymentStatus",
                ["TransactionId"] = transactionId.ToString(),
                ["UserId"] = _currentUserService.UserId?.ToString() ?? "unknown"
            });

            _logger.LogError(ex, "Error consultando estado de pago {TransactionId}", transactionId);
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene los proveedores de servicios disponibles por tipo
    /// </summary>
    /// <param name="serviceType">Tipo de servicio</param>
    /// <returns>Lista de proveedores disponibles</returns>
    [HttpGet("providers/{serviceType}")]
    [ProducesResponseType(typeof(List<ServiceProvider>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<ServiceProvider>>> GetServiceProviders([FromRoute] ServiceType serviceType)
    {
        try
        {
            var userId = _currentUserService.UserId;

            await _azureMonitor.LogUserBehaviorAsync("ServiceProvidersViewed", new
            {
                ServiceType = serviceType.ToString(),
                UserId = userId,
                IpAddress = _currentUserService.IpAddress,
                Timestamp = DateTime.UtcNow
            }, userId?.ToString() ?? "unknown");

            // Obtener proveedores según el tipo de servicio
            var providers = await GetProvidersForServiceType(serviceType);

            return Ok(providers);
        }
        catch (Exception ex)
        {
            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "GetServiceProviders",
                ["ServiceType"] = serviceType.ToString()
            });

            _logger.LogError(ex, "Error obteniendo proveedores para {ServiceType}", serviceType);
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Programa un pago recurrente (débito automático)
    /// </summary>
    /// <param name="request">Configuración del débito automático</param>
    /// <returns>Confirmación del débito automático</returns>
    [HttpPost("recurring")]
    [ProducesResponseType(typeof(RecurringPaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RecurringPaymentResponse>> SetupRecurringPayment([FromBody] SetupRecurringPaymentRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;

            await _azureMonitor.LogUserBehaviorAsync("RecurringPaymentSetup", new
            {
                UserId = userId,
                ServiceType = request.ServiceType.ToString(),
                ServiceProvider = request.ServiceProviderCode,
                MaxAmount = request.MaxAmount,
                Frequency = request.Frequency.ToString(),
                IpAddress = _currentUserService.IpAddress,
                Timestamp = DateTime.UtcNow
            }, userId?.ToString() ?? "unknown");

            // Implementar configuración de débito automático
            var response = new RecurringPaymentResponse
            {
                Success = true,
                RecurringPaymentId = Guid.NewGuid(),
                Message = "Débito automático configurado exitosamente"
            };

            return CreatedAtAction(nameof(GetRecurringPayment), 
                new { recurringPaymentId = response.RecurringPaymentId }, response);
        }
        catch (Exception ex)
        {
            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "SetupRecurringPayment",
                ["UserId"] = _currentUserService.UserId?.ToString() ?? "unknown"
            });

            _logger.LogError(ex, "Error configurando débito automático");
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene información de un débito automático
    /// </summary>
    /// <param name="recurringPaymentId">ID del débito automático</param>
    /// <returns>Información del débito automático</returns>
    [HttpGet("recurring/{recurringPaymentId}")]
    [ProducesResponseType(typeof(RecurringPaymentInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecurringPaymentInfo>> GetRecurringPayment([FromRoute] Guid recurringPaymentId)
    {
        try
        {
            // Implementar obtención de información del débito automático
            var response = new RecurringPaymentInfo
            {
                RecurringPaymentId = recurringPaymentId,
                IsActive = true,
                NextPaymentDate = DateTime.UtcNow.AddMonths(1)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "GetRecurringPayment",
                ["RecurringPaymentId"] = recurringPaymentId.ToString()
            });

            _logger.LogError(ex, "Error obteniendo información de débito automático {RecurringPaymentId}", recurringPaymentId);
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    // Métodos privados auxiliares
    private async Task<PaymentRiskAnalysis> PerformPaymentRiskAnalysis(CreatePaymentCommand command, DateTime startTime)
    {
        // Análisis específico para pagos de servicios
        var analysis = new PaymentRiskAnalysis
        {
            IsNewServiceProvider = await IsNewServiceProvider(command.UserId, command.ServiceProviderCode),
            PaymentFrequency = await GetPaymentFrequency(command.UserId, command.ServiceProviderCode),
            AveragePaymentAmount = await GetAveragePaymentAmount(command.UserId, command.ServiceProviderCode),
            DeviationFromAverage = await CalculateDeviationFromAverage(command.UserId, command.ServiceProviderCode, command.Amount),
            DaysUntilDueDate = command.DueDate.HasValue ? (int)(command.DueDate.Value - DateTime.UtcNow).TotalDays : 0,
            IsLatePayment = command.DueDate.HasValue && command.DueDate < DateTime.UtcNow,
            IsHolidayPeriod = IsHolidayPeriod(startTime),
            ServiceCategoryRisk = GetServiceCategoryRisk(command.ServiceType)
        };

        return analysis;
    }

    private async Task<ServiceValidationResult> ValidateServiceProvider(CreatePaymentCommand command)
    {
        // Validar que el proveedor de servicios sea válido
        var isValidProvider = await IsValidServiceProvider(command.ServiceProviderCode, command.ServiceType);
        var isValidServiceNumber = await IsValidServiceNumber(command.ServiceNumber, command.ServiceType);

        var result = new ServiceValidationResult
        {
            IsValid = isValidProvider && isValidServiceNumber,
            Errors = new List<string>()
        };

        if (!isValidProvider)
            result.Errors.Add("Proveedor de servicio no válido");

        if (!isValidServiceNumber)
            result.Errors.Add("Número de servicio no válido");

        return result;
    }

    private async Task<RiskAssessment> AssessPaymentRisk(CreatePaymentCommand command, PaymentRiskAnalysis paymentAnalysis)
    {
        var riskScore = 0;
        var riskFactors = new List<string>();
        var securityAlerts = new List<SecurityAlert>();

        // Factores de riesgo específicos para pagos
        if (paymentAnalysis.IsNewServiceProvider)
        {
            riskScore += 2;
            riskFactors.Add("Nuevo proveedor de servicio");
        }

        if (paymentAnalysis.DeviationFromAverage > 100) // Más del 100% de variación
        {
            riskScore += 3;
            riskFactors.Add("Monto muy diferente al promedio");
            securityAlerts.Add(new SecurityAlert
            {
                Type = "AmountDeviation",
                Message = $"El monto es {paymentAnalysis.DeviationFromAverage:F0}% diferente al promedio",
                Severity = "Medium",
                Code = "PAY_001"
            });
        }

        if (paymentAnalysis.IsLatePayment && Math.Abs(paymentAnalysis.DaysUntilDueDate) > 30)
        {
            riskScore += 2;
            riskFactors.Add("Pago muy tardío");
        }

        if (command.Amount > 5000)
        {
            riskScore += 2;
            riskFactors.Add("Monto alto para pago de servicio");
        }

        if (paymentAnalysis.ServiceCategoryRisk >= 3)
        {
            riskScore += 1;
            riskFactors.Add("Categoría de servicio de riesgo medio-alto");
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
            ShouldBlock = riskLevel >= RiskLevel.VeryHigh && riskScore > 7,
            SecurityAlerts = securityAlerts
        };
    }

    private async Task<List<ServiceProvider>> GetProvidersForServiceType(ServiceType serviceType)
    {
        // Simular proveedores según el tipo de servicio (en producción vendría de base de datos)
        return serviceType switch
        {
            ServiceType.Electricity => new List<ServiceProvider>
            {
                new() { Code = "LDS", Name = "Luz del Sur", IsActive = true },
                new() { Code = "EDN", Name = "Edenor", IsActive = true },
                new() { Code = "ELP", Name = "Electronorte", IsActive = true }
            },
            ServiceType.Water => new List<ServiceProvider>
            {
                new() { Code = "SED", Name = "Sedapal", IsActive = true },
                new() { Code = "EPS", Name = "EPS Grau", IsActive = true }
            },
            ServiceType.Mobile => new List<ServiceProvider>
            {
                new() { Code = "MOV", Name = "Movistar", IsActive = true },
                new() { Code = "CLA", Name = "Claro", IsActive = true },
                new() { Code = "ENT", Name = "Entel", IsActive = true },
                new() { Code = "BIP", Name = "Bitel", IsActive = true }
            },
            ServiceType.Internet => new List<ServiceProvider>
            {
                new() { Code = "MOV", Name = "Movistar", IsActive = true },
                new() { Code = "CLA", Name = "Claro", IsActive = true },
                new() { Code = "OPT", Name = "Optical Networks", IsActive = true }
            },
            _ => await Task.FromResult(new List<ServiceProvider>())
        };
    }

    // Métodos auxiliares (implementaciones simuladas)
    private async Task<bool> IsNewServiceProvider(Guid userId, string providerCode) => await Task.FromResult(Random.Shared.Next(100) < 25);
    private async Task<string> GetPaymentFrequency(Guid userId, string providerCode) => await Task.FromResult("Monthly");
    private async Task<decimal> GetAveragePaymentAmount(Guid userId, string providerCode) => await Task.FromResult(Random.Shared.Next(80, 200));
    private async Task<double> CalculateDeviationFromAverage(Guid userId, string providerCode, decimal amount) => await Task.FromResult(Random.Shared.NextDouble() * 50);
    private async Task<bool> IsValidServiceProvider(string providerCode, ServiceType serviceType) => await Task.FromResult(!string.IsNullOrEmpty(providerCode));
    private async Task<bool> IsValidServiceNumber(string serviceNumber, ServiceType serviceType) => await Task.FromResult(serviceNumber.Length >= 6);

    private static bool IsHolidayPeriod(DateTime date)
    {
        // Verificar si es período de fiestas (Navidad, Año Nuevo, Fiestas Patrias en Perú)
        return (date.Month == 12 && date.Day >= 20) || 
               (date.Month == 1 && date.Day <= 10) ||
               (date.Month == 7 && date.Day >= 25 && date.Day <= 31);
    }

    private static int GetServiceCategoryRisk(ServiceType serviceType)
    {
        return serviceType switch
        {
            ServiceType.Electricity or ServiceType.Water or ServiceType.Gas => 1, // Servicios básicos - bajo riesgo
            ServiceType.Mobile or ServiceType.Internet or ServiceType.Cable => 2, // Telecomunicaciones - riesgo medio
            ServiceType.CreditCard or ServiceType.Loan => 4, // Financieros - alto riesgo
            ServiceType.Tax or ServiceType.Municipality => 3, // Públicos - riesgo medio-alto
            ServiceType.Gaming or ServiceType.Streaming => 3, // Entretenimiento - riesgo medio-alto
            _ => 2 // Otros - riesgo medio
        };
    }

    private static string GetServiceCategory(ServiceType serviceType)
    {
        return serviceType switch
        {
            ServiceType.Electricity or ServiceType.Water or ServiceType.Gas => "BasicServices",
            ServiceType.Mobile or ServiceType.Internet or ServiceType.Cable or ServiceType.Telephone => "Telecommunications",
            ServiceType.CreditCard or ServiceType.Loan or ServiceType.Insurance or ServiceType.Investment => "Financial",
            ServiceType.Municipality or ServiceType.Tax or ServiceType.Traffic or ServiceType.Education or ServiceType.Health => "Public",
            ServiceType.Streaming or ServiceType.Gaming or ServiceType.Subscription => "Entertainment",
            ServiceType.Transport or ServiceType.Parking or ServiceType.Toll => "Transportation",
            _ => "Other"
        };
    }

    private static string MaskServiceNumber(string serviceNumber)
    {
        if (string.IsNullOrEmpty(serviceNumber) || serviceNumber.Length < 4)
            return "***";
        
        return serviceNumber[..2] + "***" + serviceNumber[^2..];
    }

    private static string GetAmountRange(decimal amount)
    {
        return amount switch
        {
            < 50 => "0-50",
            < 100 => "50-100",
            < 200 => "100-200",
            < 500 => "200-500",
            < 1000 => "500-1000",
            _ => "1000+"
        };
    }

    private string GetServiceProvider(DomainServiceType serviceType)
    {
        return serviceType switch
        {
            DomainServiceType.Electricity => "Luz del Sur, Enel",
            DomainServiceType.Water => "Sedapal, Eps Grau",
            DomainServiceType.Gas => "Cálidda, Contugas", 
            DomainServiceType.Mobile => "Claro, Movistar, Entel, Bitel",
            DomainServiceType.Cable => "Movistar TV, Claro TV, DirecTV",
            DomainServiceType.Telephone => "Telefónica, Claro",
            DomainServiceType.CreditCard => "Visa, MasterCard, Amex, Diners",
            DomainServiceType.Insurance => "Rímac, Pacífico, La Positiva",
            DomainServiceType.Investment => "Fondos Mutuos, AFP",
            DomainServiceType.Municipality => "Municipalidades",
            DomainServiceType.Tax => "SUNAT, SAT",
            DomainServiceType.Traffic => "Policía Nacional",
            DomainServiceType.Education => "Universidades, Colegios",
            DomainServiceType.Health => "EsSalud, Clínicas",
            DomainServiceType.Subscription => "Netflix, Spotify, Prime",
            DomainServiceType.Transport => "ATU, Metropolitano",
            _ => "Proveedor General"
        };
    }
}

// DTOs específicos para pagos
public class PaymentStatusResponse
{
    public Guid TransactionId { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime LastUpdated { get; set; }
    public string? ConfirmationCode { get; set; }
    public string ServiceProvider { get; set; } = string.Empty;
    public string? StatusDescription { get; set; }
}

public class ServiceProvider
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? LogoUrl { get; set; }
    public List<string> SupportedCurrencies { get; set; } = new() { "PEN" };
}

public class SetupRecurringPaymentRequest
{
    [Required]
    public ServiceType ServiceType { get; set; }
    
    [Required]
    public string ServiceProviderCode { get; set; } = string.Empty;
    
    [Required]
    public string ServiceNumber { get; set; } = string.Empty;
    
    [Required]
    public Guid FromAccountId { get; set; }
    
    [Required]
    public RecurrenceFrequency Frequency { get; set; }
    
    [Range(1, 31)]
    public int DayOfMonth { get; set; } = 1;
    
    [Range(0.01, 10000)]
    public decimal MaxAmount { get; set; }
    
    public DateTime? EndDate { get; set; }
}

public class RecurringPaymentResponse
{
    public bool Success { get; set; }
    public Guid RecurringPaymentId { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}

public class RecurringPaymentInfo
{
    public Guid RecurringPaymentId { get; set; }
    public bool IsActive { get; set; }
    public DateTime NextPaymentDate { get; set; }
    public string ServiceProvider { get; set; } = string.Empty;
    public decimal MaxAmount { get; set; }
    public RecurrenceFrequency Frequency { get; set; }
}

public class PaymentRiskAnalysis
{
    public bool IsNewServiceProvider { get; set; }
    public string PaymentFrequency { get; set; } = string.Empty;
    public decimal AveragePaymentAmount { get; set; }
    public double DeviationFromAverage { get; set; }
    public int DaysUntilDueDate { get; set; }
    public bool IsLatePayment { get; set; }
    public bool IsHolidayPeriod { get; set; }
    public int ServiceCategoryRisk { get; set; }
}

public class ServiceValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
} 