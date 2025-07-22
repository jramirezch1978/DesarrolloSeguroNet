using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SecurityAutomation.Models;
using SecurityAutomation.Services;
using System.Net;
using System.Text.Json;

namespace SecurityAutomation.Functions;

public class ThreatAnalysisFunction
{
    private readonly ILogger<ThreatAnalysisFunction> _logger;
    private readonly IThreatAnalysisService _threatAnalysisService;
    private readonly IThreatIntelligenceService _threatIntelligenceService;
    private readonly IMLThreatDetectionService _mlThreatDetectionService;

    public ThreatAnalysisFunction(
        ILogger<ThreatAnalysisFunction> logger,
        IThreatAnalysisService threatAnalysisService,
        IThreatIntelligenceService threatIntelligenceService,
        IMLThreatDetectionService mlThreatDetectionService)
    {
        _logger = logger;
        _threatAnalysisService = threatAnalysisService;
        _threatIntelligenceService = threatIntelligenceService;
        _mlThreatDetectionService = mlThreatDetectionService;
    }

    [Function("ThreatAnalysis")]
    public async Task<HttpResponseData> AnalyzeThreat(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("🔍 Iniciando análisis de amenaza...");

        try
        {
            // Leer el evento de seguridad del request
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var securityEvent = JsonSerializer.Deserialize<SecurityEvent>(requestBody);

            if (securityEvent == null)
            {
                _logger.LogError("❌ No se pudo deserializar el evento de seguridad");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid security event format");
                return badResponse;
            }

            _logger.LogInformation($"📋 Analizando evento: {securityEvent.EventType} desde {securityEvent.SourceIP}");

            // Realizar análisis completo de amenaza
            var analysisResult = await _threatAnalysisService.AnalyzeThreatAsync(securityEvent);

            // Enriquecer con threat intelligence
            if (!string.IsNullOrEmpty(securityEvent.SourceIP))
            {
                analysisResult.ThreatIntelligence = await _threatIntelligenceService.GetThreatIntelligenceAsync(securityEvent.SourceIP);
                analysisResult.GeoLocation = await _threatIntelligenceService.GetGeoLocationAsync(securityEvent.SourceIP);
                analysisResult.HistoricalActivity = await _threatIntelligenceService.GetHistoricalActivityAsync(securityEvent.SourceIP);
            }

            // Aplicar ML si está habilitado
            if (await _mlThreatDetectionService.IsMLEnabledAsync())
            {
                analysisResult.MLPrediction = await _mlThreatDetectionService.PredictThreatAsync(securityEvent);
                
                // Ajustar risk score basado en predicción ML
                if (analysisResult.MLPrediction.ThreatProbability > 0.8)
                {
                    analysisResult.RiskScore = Math.Max(analysisResult.RiskScore, analysisResult.MLPrediction.ThreatProbability * 100);
                }
            }

            // Log resultado
            _logger.LogInformation($"✅ Análisis completado - Risk Score: {analysisResult.RiskScore:F1}, Categoría: {analysisResult.ThreatCategory}");

            if (analysisResult.RiskScore > 70)
            {
                _logger.LogWarning($"🚨 AMENAZA DE ALTO RIESGO DETECTADA: {securityEvent.SourceIP} - Score: {analysisResult.RiskScore:F1}");
            }

            // Crear respuesta
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            
            var jsonResponse = JsonSerializer.Serialize(analysisResult, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            await response.WriteStringAsync(jsonResponse);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error durante análisis de amenaza");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error analyzing threat: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("AnalyzeDDoSSeverity")]
    public async Task<HttpResponseData> AnalyzeDDoSSeverity(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("🛡️ Analizando severidad de ataque DDoS...");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var ddosEvent = JsonSerializer.Deserialize<DDoSAttackEvent>(requestBody);

            if (ddosEvent == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid DDoS event format");
                return badResponse;
            }

            // Análisis específico para DDoS
            var severity = await _threatAnalysisService.AnalyzeDDoSSeverityAsync(ddosEvent);
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            
            var result = new
            {
                EventId = ddosEvent.Id,
                PublicIP = ddosEvent.PublicIPAddress,
                Magnitude = ddosEvent.AttackMagnitude,
                Severity = severity.Severity,
                RecommendedActions = severity.RecommendedActions,
                EstimatedDuration = severity.EstimatedDurationMinutes,
                BusinessImpact = severity.BusinessImpact,
                AutoMitigationEnabled = severity.AutoMitigationEnabled
            };
            
            await response.WriteStringAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            
            _logger.LogInformation($"✅ Análisis DDoS completado - Severidad: {severity.Severity}, Magnitud: {ddosEvent.AttackMagnitude} Gbps");
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error analizando severidad DDoS");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error analyzing DDoS severity: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("BulkThreatAnalysis")]
    public async Task<HttpResponseData> BulkAnalyzeThreat(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("📊 Iniciando análisis bulk de amenazas...");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var securityEvents = JsonSerializer.Deserialize<SecurityEvent[]>(requestBody);

            if (securityEvents == null || securityEvents.Length == 0)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("No security events provided");
                return badResponse;
            }

            _logger.LogInformation($"📋 Procesando {securityEvents.Length} eventos de seguridad...");

            // Procesar eventos en paralelo con límite de concurrencia
            var semaphore = new SemaphoreSlim(5); // Máximo 5 análisis concurrentes
            var analysisResults = new List<ThreatAnalysisResult>();

            var tasks = securityEvents.Select(async securityEvent =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await _threatAnalysisService.AnalyzeThreatAsync(securityEvent);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);
            analysisResults.AddRange(results);

            // Generar estadísticas del análisis bulk
            var bulkStats = new
            {
                TotalEvents = securityEvents.Length,
                HighRiskEvents = analysisResults.Count(r => r.RiskScore > 70),
                MediumRiskEvents = analysisResults.Count(r => r.RiskScore >= 40 && r.RiskScore <= 70),
                LowRiskEvents = analysisResults.Count(r => r.RiskScore < 40),
                AverageRiskScore = analysisResults.Average(r => r.RiskScore),
                MostCommonThreatCategory = analysisResults
                    .GroupBy(r => r.ThreatCategory)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key ?? "Unknown",
                ProcessingTimeMs = DateTime.UtcNow.Subtract(DateTime.UtcNow).TotalMilliseconds,
                Results = analysisResults
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            
            await response.WriteStringAsync(JsonSerializer.Serialize(bulkStats, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));

            _logger.LogInformation($"✅ Análisis bulk completado - {bulkStats.HighRiskEvents} eventos de alto riesgo detectados");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error en análisis bulk de amenazas");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error in bulk threat analysis: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("ThreatCorrelation")]
    public async Task<HttpResponseData> CorrelateThreat(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("🔗 Iniciando correlación de amenazas...");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var correlationRequest = JsonSerializer.Deserialize<ThreatCorrelationRequest>(requestBody);

            if (correlationRequest == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid correlation request");
                return badResponse;
            }

            // Realizar correlación de amenazas
            var correlatedIncidents = await _threatAnalysisService.CorrelateThreatsAsync(
                correlationRequest.SecurityEvent, 
                correlationRequest.CorrelationWindow);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            
            var result = new
            {
                PrimaryEventId = correlationRequest.SecurityEvent.Id,
                CorrelatedIncidents = correlatedIncidents,
                CorrelationCount = correlatedIncidents.Count,
                HighConfidenceCorrelations = correlatedIncidents.Count(c => c.CorrelationScore > 0.8),
                RecommendedCombinedResponse = correlatedIncidents.Any() 
                    ? await _threatAnalysisService.GenerateCombinedResponseAsync(correlatedIncidents)
                    : new List<ResponseAction>()
            };
            
            await response.WriteStringAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            
            _logger.LogInformation($"✅ Correlación completada - {correlatedIncidents.Count} incidentes correlacionados encontrados");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error en correlación de amenazas");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error correlating threats: {ex.Message}");
            return errorResponse;
        }
    }
}

public class ThreatCorrelationRequest
{
    public SecurityEvent SecurityEvent { get; set; } = new();
    public TimeSpan CorrelationWindow { get; set; } = TimeSpan.FromHours(1);
}

public class DDoSSeverityResult
{
    public string Severity { get; set; } = string.Empty;
    public List<string> RecommendedActions { get; set; } = new();
    public int EstimatedDurationMinutes { get; set; }
    public string BusinessImpact { get; set; } = string.Empty;
    public bool AutoMitigationEnabled { get; set; }
} 