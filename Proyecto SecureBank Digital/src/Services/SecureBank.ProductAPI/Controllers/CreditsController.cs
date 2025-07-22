using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;
using SecureBank.Application.Features.Products.Commands.ApplyForCredit;
using SecureBank.Application.Common.Interfaces;
using SecureBank.AuthAPI.Services;
using Microsoft.ML;
using System.ComponentModel.DataAnnotations;

namespace SecureBank.ProductAPI.Controllers;

/// <summary>
/// Controlador para productos crediticios en SecureBank Digital
/// Implementa scoring crediticio con Machine Learning y análisis de riesgo
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[Produces("application/json")]
public class CreditsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAzureMonitorService _azureMonitor;
    private readonly ILogger<CreditsController> _logger;
    private readonly MLContext _mlContext;

    public CreditsController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        IAzureMonitorService azureMonitor,
        ILogger<CreditsController> logger,
        MLContext mlContext)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _azureMonitor = azureMonitor;
        _logger = logger;
        _mlContext = mlContext;
    }

    /// <summary>
    /// Solicita un nuevo crédito
    /// </summary>
    /// <param name="command">Datos de la solicitud de crédito</param>
    /// <returns>Resultado de la evaluación crediticia</returns>
    [HttpPost("apply")]
    [EnableRateLimiting("CreditApplicationLimits")]
    [ProducesResponseType(typeof(ApplyForCreditResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApplyForCreditResponse>> ApplyForCredit([FromBody] ApplyForCreditCommand command)
    {
        var startTime = DateTime.UtcNow;
        var applicationId = Guid.NewGuid();
        
        try
        {
            // Validar autorización del usuario
            if (_currentUserService.UserId != command.UserId && !_currentUserService.IsInRole("Administrator"))
            {
                await _azureMonitor.LogSecurityEventAsync("UnauthorizedCreditApplication", new
                {
                    RequestedUserId = command.UserId,
                    ActualUserId = _currentUserService.UserId,
                    CreditType = command.CreditType.ToString(),
                    RequestedAmount = command.RequestedAmount,
                    IpAddress = _currentUserService.IpAddress,
                    DeviceFingerprint = _currentUserService.DeviceFingerprint,
                    Timestamp = startTime
                }, _currentUserService.UserId?.ToString());
                
                return Forbid("No tienes permisos para solicitar créditos en nombre de otro usuario");
            }

            // Agregar información de contexto de auditoría
            command.IpAddress = _currentUserService.IpAddress ?? string.Empty;
            command.UserAgent = _currentUserService.UserAgent ?? string.Empty;
            command.DeviceFingerprint = _currentUserService.DeviceFingerprint ?? string.Empty;
            command.SessionId = _currentUserService.SessionId ?? string.Empty;

            _logger.LogInformation("Iniciando solicitud de crédito {CreditType}: {Amount} {Currency} para usuario {UserId}", 
                command.CreditType, command.RequestedAmount, command.Currency, command.UserId);

            // Análisis crediticio con Machine Learning
            var creditAnalysis = await PerformCreditAnalysis(command, startTime);

            // Log detallado para Machine Learning y análisis crediticio
            await _azureMonitor.LogTransactionEventAsync("CreditApplicationStarted", new
            {
                ApplicationId = applicationId,
                UserId = command.UserId,
                CreditType = command.CreditType.ToString(),
                RequestedAmount = command.RequestedAmount,
                RequestedTermMonths = command.RequestedTermMonths,
                Currency = command.Currency,
                Purpose = command.Purpose,
                
                // Información laboral para ML
                EmploymentType = command.EmploymentInfo.EmploymentType,
                YearsInCompany = command.EmploymentInfo.YearsInCompany,
                TotalWorkExperience = command.EmploymentInfo.TotalWorkExperience,
                Industry = command.EmploymentInfo.Industry,
                
                // Información financiera para ML
                MonthlyNetIncome = command.IncomeInfo.MonthlyNetIncome,
                MonthlyGrossIncome = command.IncomeInfo.MonthlyGrossIncome,
                AdditionalIncome = command.IncomeInfo.AdditionalIncome,
                HasVariableIncome = command.IncomeInfo.HasVariableIncome,
                
                // Gastos y capacidad de pago
                TotalMonthlyExpenses = CalculateTotalExpenses(command.ExpenseInfo),
                DebtToIncomeRatio = CalculateDebtToIncomeRatio(command.IncomeInfo, command.ExpenseInfo),
                
                // Patrimonio
                TotalAssets = command.AssetInfo?.RealEstateValue + command.AssetInfo?.VehicleValue + 
                             command.AssetInfo?.BankDeposits + command.AssetInfo?.Investments + 
                             command.AssetInfo?.OtherAssets ?? 0,
                TotalDebts = command.AssetInfo?.TotalDebts ?? 0,
                
                // Referencias
                PersonalReferencesCount = command.PersonalReferences.Count,
                CommercialReferencesCount = command.CommercialReferences.Count,
                DocumentsCount = command.Documents.Count,
                
                // Scoring ML
                CreditScore = creditAnalysis.CreditScore,
                RiskLevel = creditAnalysis.RiskLevel,
                DefaultProbability = creditAnalysis.DefaultProbability,
                RecommendedAmount = creditAnalysis.RecommendedAmount,
                RecommendedInterestRate = creditAnalysis.RecommendedInterestRate,
                
                // Contexto
                IpAddress = command.IpAddress,
                DeviceFingerprint = command.DeviceFingerprint,
                UserAgent = command.UserAgent,
                SessionId = command.SessionId,
                TimeOfDay = startTime.Hour,
                DayOfWeek = startTime.DayOfWeek.ToString(),
                
                Timestamp = startTime
            }, command.UserId.ToString());

            // Evaluación de políticas crediticias
            var policyEvaluation = await EvaluateCreditPolicies(command, creditAnalysis);
            
            if (!policyEvaluation.MeetsBasicRequirements)
            {
                await _azureMonitor.LogBusinessMetricAsync("CreditApplicationRejected", 1, new Dictionary<string, string>
                {
                    ["CreditType"] = command.CreditType.ToString(),
                    ["RejectionReason"] = "PolicyViolation",
                    ["RequestedAmount"] = GetAmountRange(command.RequestedAmount),
                    ["CreditScore"] = creditAnalysis.CreditScore.ToString(),
                    ["RiskLevel"] = creditAnalysis.RiskLevel
                });

                _logger.LogWarning("Solicitud de crédito rechazada por políticas: {ApplicationId}", applicationId);
                
                return BadRequest(new ApplyForCreditResponse
                {
                    Success = false,
                    ApplicationId = applicationId,
                    Status = CreditApplicationStatus.Rejected,
                    Errors = policyEvaluation.PolicyViolations,
                    Decision = new CreditDecision
                    {
                        DecisionType = "Rechazado",
                        CreditScore = creditAnalysis.CreditScore,
                        RiskLevel = creditAnalysis.RiskLevel,
                        NegativeFactors = policyEvaluation.PolicyViolations,
                        RecommendationsToImprove = policyEvaluation.ImprovementRecommendations
                    }
                });
            }

            // Procesar la solicitud
            var response = await _mediator.Send(command);
            var duration = DateTime.UtcNow - startTime;

            if (response.Success)
            {
                await _azureMonitor.LogTransactionEventAsync("CreditApplicationProcessed", new
                {
                    ApplicationId = response.ApplicationId,
                    ApplicationNumber = response.ApplicationNumber,
                    UserId = command.UserId,
                    CreditType = command.CreditType.ToString(),
                    RequestedAmount = command.RequestedAmount,
                    Status = response.Status.ToString(),
                    DecisionType = response.Decision.DecisionType,
                    CreditScore = response.Decision.CreditScore,
                    RiskLevel = response.Decision.RiskLevel,
                    ApprovedAmount = response.PreApprovedOffer?.ApprovedAmount ?? 0,
                    InterestRate = response.PreApprovedOffer?.InterestRate ?? 0,
                    ProcessingDuration = duration.TotalMilliseconds,
                    RequiredDocumentsCount = response.RequiredDocuments.Count,
                    IpAddress = command.IpAddress,
                    DeviceFingerprint = command.DeviceFingerprint,
                    Timestamp = DateTime.UtcNow
                }, command.UserId.ToString());

                // Métricas de negocio
                await _azureMonitor.LogBusinessMetricAsync("CreditApplicationsProcessed", 1, new Dictionary<string, string>
                {
                    ["CreditType"] = command.CreditType.ToString(),
                    ["Status"] = response.Status.ToString(),
                    ["DecisionType"] = response.Decision.DecisionType,
                    ["RequestedAmountRange"] = GetAmountRange(command.RequestedAmount),
                    ["ApprovedAmountRange"] = response.PreApprovedOffer != null ? GetAmountRange(response.PreApprovedOffer.ApprovedAmount) : "None",
                    ["CreditScoreRange"] = GetCreditScoreRange(response.Decision.CreditScore),
                    ["RiskLevel"] = response.Decision.RiskLevel,
                    ["EmploymentType"] = command.EmploymentInfo.EmploymentType,
                    ["Industry"] = command.EmploymentInfo.Industry,
                    ["Currency"] = command.Currency,
                    ["Hour"] = startTime.Hour.ToString(),
                    ["DayOfWeek"] = startTime.DayOfWeek.ToString()
                });

                // Métricas de performance del scoring
                await _azureMonitor.LogPerformanceMetricAsync("CreditScoringTime", duration.TotalMilliseconds,
                    new Dictionary<string, string>
                    {
                        ["CreditType"] = command.CreditType.ToString(),
                        ["ComplexityLevel"] = GetApplicationComplexity(command),
                        ["DocumentsCount"] = command.Documents.Count.ToString()
                    });

                _logger.LogInformation("Solicitud de crédito procesada: {ApplicationId} - {Status} con score {CreditScore}", 
                    response.ApplicationId, response.Status, response.Decision.CreditScore);

                return CreatedAtAction(nameof(GetCreditApplication), 
                    new { applicationId = response.ApplicationId }, response);
            }
            else
            {
                await _azureMonitor.LogTransactionEventAsync("CreditApplicationFailed", new
                {
                    ApplicationId = applicationId,
                    UserId = command.UserId,
                    CreditType = command.CreditType.ToString(),
                    RequestedAmount = command.RequestedAmount,
                    Errors = response.Errors,
                    ProcessingDuration = duration.TotalMilliseconds,
                    IpAddress = command.IpAddress,
                    DeviceFingerprint = command.DeviceFingerprint,
                    Timestamp = DateTime.UtcNow
                }, command.UserId.ToString());

                _logger.LogWarning("Solicitud de crédito falló: {ApplicationId}, Errores: {Errors}", 
                    applicationId, string.Join(", ", response.Errors));

                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            await _azureMonitor.LogSecurityEventAsync("CreditApplicationError", new
            {
                ApplicationId = applicationId,
                UserId = command.UserId,
                CreditType = command.CreditType.ToString(),
                RequestedAmount = command.RequestedAmount,
                Error = ex.Message,
                ProcessingDuration = (DateTime.UtcNow - startTime).TotalMilliseconds,
                IpAddress = command.IpAddress,
                DeviceFingerprint = command.DeviceFingerprint,
                Timestamp = DateTime.UtcNow
            }, command.UserId.ToString());

            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "ApplyForCredit",
                ["ApplicationId"] = applicationId.ToString(),
                ["UserId"] = command.UserId.ToString(),
                ["CreditType"] = command.CreditType.ToString(),
                ["RequestedAmount"] = command.RequestedAmount.ToString()
            });

            _logger.LogError(ex, "Error procesando solicitud de crédito: {ApplicationId}", applicationId);
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene el estado de una solicitud de crédito
    /// </summary>
    /// <param name="applicationId">ID de la solicitud</param>
    /// <returns>Estado actual de la solicitud</returns>
    [HttpGet("{applicationId}")]
    [EnableRateLimiting("GeneralApi")]
    [ProducesResponseType(typeof(CreditApplicationDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreditApplicationDetails>> GetCreditApplication([FromRoute] Guid applicationId)
    {
        try
        {
            var userId = _currentUserService.UserId;
            
            _logger.LogInformation("Consultando solicitud de crédito {ApplicationId} para usuario {UserId}", 
                applicationId, userId);

            await _azureMonitor.LogUserBehaviorAsync("CreditApplicationViewed", new
            {
                ApplicationId = applicationId,
                UserId = userId,
                IpAddress = _currentUserService.IpAddress,
                DeviceFingerprint = _currentUserService.DeviceFingerprint,
                Timestamp = DateTime.UtcNow
            }, userId?.ToString() ?? "unknown");

            // Implementar obtención de detalles aquí
            var response = new CreditApplicationDetails
            {
                ApplicationId = applicationId,
                Status = CreditApplicationStatus.UnderReview,
                LastUpdated = DateTime.UtcNow.AddHours(-2),
                EstimatedDecisionDate = DateTime.UtcNow.AddDays(3)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "GetCreditApplication",
                ["ApplicationId"] = applicationId.ToString(),
                ["UserId"] = _currentUserService.UserId?.ToString() ?? "unknown"
            });

            _logger.LogError(ex, "Error consultando solicitud de crédito {ApplicationId}", applicationId);
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene las ofertas de crédito disponibles para el usuario
    /// </summary>
    /// <param name="creditType">Tipo de crédito (opcional)</param>
    /// <param name="amount">Monto aproximado (opcional)</param>
    /// <returns>Ofertas disponibles personalizadas</returns>
    [HttpGet("offers")]
    [EnableRateLimiting("GeneralApi")]
    [ProducesResponseType(typeof(List<CreditOfferPreview>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<CreditOfferPreview>>> GetAvailableOffers(
        [FromQuery] CreditType? creditType = null,
        [FromQuery] decimal? amount = null)
    {
        try
        {
            var userId = _currentUserService.UserId;

            await _azureMonitor.LogUserBehaviorAsync("CreditOffersViewed", new
            {
                UserId = userId,
                CreditType = creditType?.ToString(),
                Amount = amount,
                IpAddress = _currentUserService.IpAddress,
                DeviceFingerprint = _currentUserService.DeviceFingerprint,
                Timestamp = DateTime.UtcNow
            }, userId?.ToString() ?? "unknown");

            // Obtener ofertas personalizadas basadas en el perfil del usuario
            var offers = await GetPersonalizedOffers(userId, creditType, amount);

            return Ok(offers);
        }
        catch (Exception ex)
        {
            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "GetAvailableOffers",
                ["UserId"] = _currentUserService.UserId?.ToString() ?? "unknown"
            });

            _logger.LogError(ex, "Error obteniendo ofertas de crédito");
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Simula un crédito con diferentes parámetros
    /// </summary>
    /// <param name="request">Parámetros de simulación</param>
    /// <returns>Simulación detallada del crédito</returns>
    [HttpPost("simulate")]
    [EnableRateLimiting("GeneralApi")]
    [ProducesResponseType(typeof(CreditSimulationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreditSimulationResult>> SimulateCredit([FromBody] CreditSimulationRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;

            await _azureMonitor.LogUserBehaviorAsync("CreditSimulation", new
            {
                UserId = userId,
                CreditType = request.CreditType.ToString(),
                Amount = request.Amount,
                TermMonths = request.TermMonths,
                IpAddress = _currentUserService.IpAddress,
                Timestamp = DateTime.UtcNow
            }, userId?.ToString() ?? "unknown");

            // Realizar simulación financiera
            var simulation = await PerformCreditSimulation(request, userId);

            return Ok(simulation);
        }
        catch (Exception ex)
        {
            _azureMonitor.TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = "SimulateCredit",
                ["UserId"] = _currentUserService.UserId?.ToString() ?? "unknown"
            });

            _logger.LogError(ex, "Error simulando crédito");
            return StatusCode(500, new { Message = "Error interno del servidor" });
        }
    }

    // Métodos privados auxiliares
    private async Task<CreditAnalysisResult> PerformCreditAnalysis(ApplyForCreditCommand command, DateTime startTime)
    {
        // Implementar scoring crediticio con Machine Learning
        var features = ExtractCreditFeatures(command);
        var creditScore = await CalculateCreditScore(features);
        var riskLevel = DetermineRiskLevel(creditScore);
        var defaultProbability = await CalculateDefaultProbability(features);
        
        var analysis = new CreditAnalysisResult
        {
            CreditScore = creditScore,
            RiskLevel = riskLevel,
            DefaultProbability = defaultProbability,
            RecommendedAmount = CalculateRecommendedAmount(command.RequestedAmount, creditScore, command.IncomeInfo),
            RecommendedInterestRate = CalculateInterestRate(creditScore, command.CreditType),
            AnalysisFactors = ExtractAnalysisFactors(features)
        };

        return analysis;
    }

    private async Task<PolicyEvaluationResult> EvaluateCreditPolicies(ApplyForCreditCommand command, CreditAnalysisResult analysis)
    {
        var violations = new List<string>();
        var recommendations = new List<string>();

        // Políticas básicas
        var debtToIncomeRatio = CalculateDebtToIncomeRatio(command.IncomeInfo, command.ExpenseInfo);
        if (debtToIncomeRatio > 0.4m) // Máximo 40% de endeudamiento
        {
            violations.Add("La relación deuda/ingreso excede el límite permitido (40%)");
            recommendations.Add("Reduzca sus gastos fijos o considere un monto menor");
        }

        if (command.IncomeInfo.MonthlyNetIncome < 1000) // Ingreso mínimo
        {
            violations.Add("Los ingresos no cumplen con el mínimo requerido");
            recommendations.Add("Se requiere un ingreso mínimo de S/ 1,000");
        }

        if (analysis.CreditScore < 300) // Score mínimo
        {
            violations.Add("El score crediticio está por debajo del mínimo aceptable");
            recommendations.Add("Mejore su historial crediticio antes de volver a aplicar");
        }

        if (command.EmploymentInfo.YearsInCompany < 1 && command.EmploymentInfo.EmploymentType == "Dependiente")
        {
            violations.Add("Se requiere al menos 1 año de antigüedad laboral");
            recommendations.Add("Espere a cumplir el tiempo mínimo de antigüedad");
        }

        return new PolicyEvaluationResult
        {
            MeetsBasicRequirements = violations.Count == 0,
            PolicyViolations = violations,
            ImprovementRecommendations = recommendations
        };
    }

    private CreditFeatures ExtractCreditFeatures(ApplyForCreditCommand command)
    {
        return new CreditFeatures
        {
            MonthlyIncome = (float)command.IncomeInfo.MonthlyNetIncome,
            TotalExpenses = (float)CalculateTotalExpenses(command.ExpenseInfo),
            DebtToIncomeRatio = (float)CalculateDebtToIncomeRatio(command.IncomeInfo, command.ExpenseInfo),
            YearsInCompany = command.EmploymentInfo.YearsInCompany,
            TotalWorkExperience = command.EmploymentInfo.TotalWorkExperience,
            RequestedAmount = (float)command.RequestedAmount,
            RequestedTermMonths = command.RequestedTermMonths,
            HasVariableIncome = command.IncomeInfo.HasVariableIncome ? 1f : 0f,
            TotalAssets = (float)(command.AssetInfo?.RealEstateValue + command.AssetInfo?.VehicleValue + 
                                 command.AssetInfo?.BankDeposits + command.AssetInfo?.Investments + 
                                 command.AssetInfo?.OtherAssets ?? 0),
            PersonalReferencesCount = command.PersonalReferences.Count,
            CommercialReferencesCount = command.CommercialReferences.Count,
            CreditTypeNumeric = (float)command.CreditType
        };
    }

    private async Task<int> CalculateCreditScore(CreditFeatures features)
    {
        // Implementar modelo ML real aquí
        // Por ahora simulamos el cálculo
        var baseScore = 500;
        
        // Factores positivos
        if (features.MonthlyIncome > 5000) baseScore += 100;
        else if (features.MonthlyIncome > 3000) baseScore += 50;
        
        if (features.DebtToIncomeRatio < 0.2f) baseScore += 80;
        else if (features.DebtToIncomeRatio < 0.3f) baseScore += 40;
        
        if (features.YearsInCompany >= 5) baseScore += 60;
        else if (features.YearsInCompany >= 2) baseScore += 30;
        
        if (features.TotalAssets > 100000) baseScore += 120;
        else if (features.TotalAssets > 50000) baseScore += 60;
        
        // Factores negativos
        if (features.HasVariableIncome > 0) baseScore -= 30;
        if (features.DebtToIncomeRatio > 0.4f) baseScore -= 100;
        
        return await Task.FromResult(Math.Max(300, Math.Min(850, baseScore)));
    }

    private string DetermineRiskLevel(int creditScore)
    {
        return creditScore switch
        {
            >= 750 => "Muy Bajo",
            >= 650 => "Bajo",
            >= 550 => "Medio",
            >= 450 => "Alto",
            _ => "Muy Alto"
        };
    }

    private async Task<double> CalculateDefaultProbability(CreditFeatures features)
    {
        // Implementar modelo predictivo ML aquí
        var probability = 0.05; // Base 5%
        
        if (features.DebtToIncomeRatio > 0.4f) probability += 0.15;
        if (features.MonthlyIncome < 2000) probability += 0.10;
        if (features.YearsInCompany < 1) probability += 0.08;
        if (features.HasVariableIncome > 0) probability += 0.05;
        
        return await Task.FromResult(Math.Min(0.95, probability));
    }

    // Métodos auxiliares de cálculo
    private decimal CalculateTotalExpenses(ExpenseInfo expenses)
    {
        return expenses.HousingExpenses + expenses.FoodExpenses + expenses.TransportationExpenses +
               expenses.UtilitiesExpenses + expenses.EducationExpenses + expenses.HealthExpenses +
               expenses.DebtPayments + expenses.OtherExpenses;
    }

    private decimal CalculateDebtToIncomeRatio(IncomeInfo income, ExpenseInfo expenses)
    {
        if (income.MonthlyNetIncome == 0) return 1;
        return expenses.DebtPayments / income.MonthlyNetIncome;
    }

    private decimal CalculateRecommendedAmount(decimal requestedAmount, int creditScore, IncomeInfo income)
    {
        var maxAmount = income.MonthlyNetIncome * 36; // 36 meses de ingreso
        var scoreMultiplier = creditScore / 850m;
        var recommendedMax = maxAmount * scoreMultiplier;
        
        return Math.Min(requestedAmount, recommendedMax);
    }

    private decimal CalculateInterestRate(int creditScore, CreditType creditType)
    {
        var baseRate = creditType switch
        {
            CreditType.Personal => 18m,
            CreditType.Vehicle => 14m,
            CreditType.Mortgage => 8m,
            CreditType.Business => 16m,
            CreditType.Education => 12m,
            _ => 15m
        };

        var scoreAdjustment = (850 - creditScore) / 100m;
        return Math.Max(5m, baseRate + scoreAdjustment);
    }

    private List<string> ExtractAnalysisFactors(CreditFeatures features)
    {
        var factors = new List<string>();
        
        if (features.MonthlyIncome > 5000) factors.Add("Ingresos altos");
        if (features.DebtToIncomeRatio < 0.2f) factors.Add("Bajo nivel de endeudamiento");
        if (features.YearsInCompany >= 5) factors.Add("Estabilidad laboral");
        if (features.TotalAssets > 100000) factors.Add("Patrimonio significativo");
        
        return factors;
    }

    private async Task<List<CreditOfferPreview>> GetPersonalizedOffers(Guid? userId, CreditType? creditType, decimal? amount)
    {
        // Implementar lógica de ofertas personalizadas
        return await Task.FromResult(new List<CreditOfferPreview>
        {
            new()
            {
                CreditType = CreditType.Personal,
                MaxAmount = 50000,
                MinInterestRate = 15.5m,
                MaxTermMonths = 72,
                Title = "Crédito Personal Exclusivo",
                Benefits = new() { "Sin comisiones", "Aprobación en 24 horas", "Seguro de desgravamen gratuito" }
            }
        });
    }

    private async Task<CreditSimulationResult> PerformCreditSimulation(CreditSimulationRequest request, Guid? userId)
    {
        // Implementar simulación financiera detallada
        var monthlyPayment = CalculateMonthlyPayment(request.Amount, request.InterestRate, request.TermMonths);
        var totalToPay = monthlyPayment * request.TermMonths;
        var totalInterest = totalToPay - request.Amount;

        return await Task.FromResult(new CreditSimulationResult
        {
            RequestedAmount = request.Amount,
            TermMonths = request.TermMonths,
            InterestRate = request.InterestRate,
            MonthlyPayment = monthlyPayment,
            TotalToPay = totalToPay,
            TotalInterest = totalInterest,
            PaymentSchedule = GeneratePaymentSchedule(request.Amount, request.InterestRate, request.TermMonths)
        });
    }

    private decimal CalculateMonthlyPayment(decimal principal, decimal annualRate, int termMonths)
    {
        var monthlyRate = annualRate / 100 / 12;
        var factor = (decimal)Math.Pow((double)(1 + monthlyRate), termMonths);
        return principal * monthlyRate * factor / (factor - 1);
    }

    private List<PaymentScheduleItem> GeneratePaymentSchedule(decimal principal, decimal annualRate, int termMonths)
    {
        var schedule = new List<PaymentScheduleItem>();
        var monthlyPayment = CalculateMonthlyPayment(principal, annualRate, termMonths);
        var monthlyRate = annualRate / 100 / 12;
        var balance = principal;

        for (int month = 1; month <= termMonths; month++)
        {
            var interestPayment = balance * monthlyRate;
            var principalPayment = monthlyPayment - interestPayment;
            balance -= principalPayment;

            schedule.Add(new PaymentScheduleItem
            {
                PaymentNumber = month,
                PaymentDate = DateTime.UtcNow.AddMonths(month),
                PaymentAmount = monthlyPayment,
                PrincipalAmount = principalPayment,
                InterestAmount = interestPayment,
                RemainingBalance = Math.Max(0, balance)
            });
        }

        return schedule;
    }

    private static string GetAmountRange(decimal amount)
    {
        return amount switch
        {
            < 5000 => "0-5K",
            < 10000 => "5K-10K",
            < 25000 => "10K-25K",
            < 50000 => "25K-50K",
            < 100000 => "50K-100K",
            _ => "100K+"
        };
    }

    private static string GetCreditScoreRange(int score)
    {
        return score switch
        {
            < 400 => "Poor",
            < 550 => "Fair",
            < 650 => "Good",
            < 750 => "VeryGood",
            _ => "Excellent"
        };
    }

    private static string GetApplicationComplexity(ApplyForCreditCommand command)
    {
        var complexityScore = 0;
        
        if (command.RequestedAmount > 100000) complexityScore += 2;
        if (command.CreditType == CreditType.Business || command.CreditType == CreditType.Mortgage) complexityScore += 2;
        if (command.IncomeInfo.HasVariableIncome) complexityScore += 1;
        if (command.Documents.Count > 10) complexityScore += 1;
        if (command.CommercialReferences.Count > 2) complexityScore += 1;
        
        return complexityScore switch
        {
            <= 2 => "Low",
            <= 4 => "Medium",
            _ => "High"
        };
    }
}

// DTOs y clases auxiliares
public class CreditAnalysisResult
{
    public int CreditScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public double DefaultProbability { get; set; }
    public decimal RecommendedAmount { get; set; }
    public decimal RecommendedInterestRate { get; set; }
    public List<string> AnalysisFactors { get; set; } = new();
}

public class PolicyEvaluationResult
{
    public bool MeetsBasicRequirements { get; set; }
    public List<string> PolicyViolations { get; set; } = new();
    public List<string> ImprovementRecommendations { get; set; } = new();
}

public class CreditFeatures
{
    public float MonthlyIncome { get; set; }
    public float TotalExpenses { get; set; }
    public float DebtToIncomeRatio { get; set; }
    public int YearsInCompany { get; set; }
    public int TotalWorkExperience { get; set; }
    public float RequestedAmount { get; set; }
    public int RequestedTermMonths { get; set; }
    public float HasVariableIncome { get; set; }
    public float TotalAssets { get; set; }
    public int PersonalReferencesCount { get; set; }
    public int CommercialReferencesCount { get; set; }
    public float CreditTypeNumeric { get; set; }
}

public class CreditApplicationDetails
{
    public Guid ApplicationId { get; set; }
    public CreditApplicationStatus Status { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime? EstimatedDecisionDate { get; set; }
    public string? StatusDescription { get; set; }
    public int ProgressPercentage { get; set; }
}

public class CreditOfferPreview
{
    public CreditType CreditType { get; set; }
    public decimal MaxAmount { get; set; }
    public decimal MinInterestRate { get; set; }
    public int MaxTermMonths { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Benefits { get; set; } = new();
}

public class CreditSimulationRequest
{
    [Required]
    public CreditType CreditType { get; set; }
    
    [Required]
    [Range(1000, 500000)]
    public decimal Amount { get; set; }
    
    [Required]
    [Range(6, 360)]
    public int TermMonths { get; set; }
    
    [Range(5, 50)]
    public decimal InterestRate { get; set; } = 15m;
}

public class CreditSimulationResult
{
    public decimal RequestedAmount { get; set; }
    public int TermMonths { get; set; }
    public decimal InterestRate { get; set; }
    public decimal MonthlyPayment { get; set; }
    public decimal TotalToPay { get; set; }
    public decimal TotalInterest { get; set; }
    public List<PaymentScheduleItem> PaymentSchedule { get; set; } = new();
}

public class PaymentScheduleItem
{
    public int PaymentNumber { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal PaymentAmount { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal RemainingBalance { get; set; }
} 