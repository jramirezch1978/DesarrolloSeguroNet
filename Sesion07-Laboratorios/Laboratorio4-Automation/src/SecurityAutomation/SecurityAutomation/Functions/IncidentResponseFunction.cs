using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SecurityAutomation.Models;
using SecurityAutomation.Services;
using System.Net;
using System.Text.Json;

namespace SecurityAutomation.Functions;

public class IncidentResponseFunction
{
    private readonly ILogger<IncidentResponseFunction> _logger;
    private readonly IIncidentResponseService _incidentResponseService;
    private readonly IWorkflowOrchestrationService _workflowService;
    private readonly INotificationService _notificationService;

    public IncidentResponseFunction(
        ILogger<IncidentResponseFunction> logger,
        IIncidentResponseService incidentResponseService,
        IWorkflowOrchestrationService workflowService,
        INotificationService notificationService)
    {
        _logger = logger;
        _incidentResponseService = incidentResponseService;
        _workflowService = workflowService;
        _notificationService = notificationService;
    }

    [Function("HandleSecurityIncident")]
    public async Task<HttpResponseData> HandleIncident(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("🚨 Iniciando respuesta automática a incidente...");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var securityEvent = JsonSerializer.Deserialize<SecurityEvent>(requestBody);

            if (securityEvent == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid security event format");
                return badResponse;
            }

            _logger.LogInformation($"📋 Procesando incidente: {securityEvent.EventType} - Severidad: {securityEvent.Severity}");

            // Generar respuesta basada en el tipo y severidad del evento
            var incidentResponse = await _incidentResponseService.GenerateResponseAsync(securityEvent);

            // Ejecutar workflow de respuesta
            var workflowResult = await _workflowService.ExecuteResponseWorkflowAsync(incidentResponse);

            // Notificar según la severidad
            if (securityEvent.RequiresImmedateAction)
            {
                await _notificationService.SendCriticalAlertAsync(securityEvent, incidentResponse);
            }
            else
            {
                await _notificationService.SendStandardAlertAsync(securityEvent, incidentResponse);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            
            var result = new
            {
                IncidentId = incidentResponse.Id,
                EventId = securityEvent.Id,
                ResponseType = incidentResponse.ResponseType,
                ActionsExecuted = incidentResponse.Actions.Count,
                SuccessfulActions = incidentResponse.Actions.Count(a => a.Success),
                WorkflowStatus = workflowResult.Status,
                ExecutionTime = workflowResult.ExecutionTime,
                NextSteps = workflowResult.NextSteps
            };
            
            await response.WriteStringAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            
            _logger.LogInformation($"✅ Respuesta a incidente completada - {incidentResponse.Actions.Count(a => a.Success)}/{incidentResponse.Actions.Count} acciones exitosas");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error en respuesta a incidente");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error handling incident: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("ExecuteResponseAction")]
    public async Task<HttpResponseData> ExecuteAction(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("⚡ Ejecutando acción de respuesta...");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var responseAction = JsonSerializer.Deserialize<ResponseAction>(requestBody);

            if (responseAction == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid response action format");
                return badResponse;
            }

            _logger.LogInformation($"🎯 Ejecutando acción: {responseAction.ActionType} en {responseAction.TargetResource}");

            // Ejecutar la acción específica
            var executionResult = await _incidentResponseService.ExecuteActionAsync(responseAction);

            // Log resultado
            if (executionResult.Success)
            {
                _logger.LogInformation($"✅ Acción {responseAction.ActionType} ejecutada exitosamente");
            }
            else
            {
                _logger.LogError($"❌ Acción {responseAction.ActionType} falló: {executionResult.ErrorMessage}");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            
            await response.WriteStringAsync(JsonSerializer.Serialize(executionResult, new JsonSerializerOptions { WriteIndented = true }));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error ejecutando acción de respuesta");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error executing response action: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("CreateIncidentTicket")]
    public async Task<HttpResponseData> CreateTicket(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("🎫 Creando ticket de incidente...");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var ticketRequest = JsonSerializer.Deserialize<IncidentTicketRequest>(requestBody);

            if (ticketRequest == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid ticket request format");
                return badResponse;
            }

            // Crear ticket en sistema externo (ServiceNow, Jira, etc.)
            var ticketResult = await _incidentResponseService.CreateIncidentTicketAsync(ticketRequest);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            
            await response.WriteStringAsync(JsonSerializer.Serialize(ticketResult, new JsonSerializerOptions { WriteIndented = true }));
            
            _logger.LogInformation($"✅ Ticket creado exitosamente: {ticketResult.TicketId}");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creando ticket de incidente");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error creating incident ticket: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("EscalateIncident")]
    public async Task<HttpResponseData> EscalateIncident(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("📈 Escalando incidente...");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var escalationRequest = JsonSerializer.Deserialize<IncidentEscalationRequest>(requestBody);

            if (escalationRequest == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid escalation request format");
                return badResponse;
            }

            // Ejecutar escalamiento según nivel
            var escalationResult = await _incidentResponseService.EscalateIncidentAsync(escalationRequest);

            // Notificar a las partes apropiadas
            await _notificationService.SendEscalationNotificationAsync(escalationRequest, escalationResult);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            
            await response.WriteStringAsync(JsonSerializer.Serialize(escalationResult, new JsonSerializerOptions { WriteIndented = true }));
            
            _logger.LogInformation($"✅ Incidente escalado a nivel: {escalationResult.EscalationLevel}");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error escalando incidente");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error escalating incident: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("GetIncidentStatus")]
    public async Task<HttpResponseData> GetIncidentStatus(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("📊 Consultando estado de incidente...");

        try
        {
            string incidentId = string.Empty;

            // Obtener ID del incidente (de query parameter o body)
            if (req.Query.AllKeys.Contains("incidentId"))
            {
                incidentId = req.Query["incidentId"];
            }
            else if (req.Method == "POST")
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var statusRequest = JsonSerializer.Deserialize<Dictionary<string, string>>(requestBody);
                incidentId = statusRequest?.GetValueOrDefault("incidentId", string.Empty) ?? string.Empty;
            }

            if (string.IsNullOrEmpty(incidentId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Incident ID is required");
                return badResponse;
            }

            // Obtener estado del incidente
            var incidentStatus = await _incidentResponseService.GetIncidentStatusAsync(incidentId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            
            await response.WriteStringAsync(JsonSerializer.Serialize(incidentStatus, new JsonSerializerOptions { WriteIndented = true }));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error consultando estado de incidente");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error getting incident status: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("AutoRemediation")]
    public async Task<HttpResponseData> AutoRemediation(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("🔧 Iniciando remediación automática...");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var remediationRequest = JsonSerializer.Deserialize<AutoRemediationRequest>(requestBody);

            if (remediationRequest == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid remediation request format");
                return badResponse;
            }

            // Ejecutar remediación automática
            var remediationResult = await _incidentResponseService.ExecuteAutoRemediationAsync(remediationRequest);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            
            await response.WriteStringAsync(JsonSerializer.Serialize(remediationResult, new JsonSerializerOptions { WriteIndented = true }));
            
            _logger.LogInformation($"✅ Remediación automática completada - Estado: {remediationResult.Status}");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error en remediación automática");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error in auto-remediation: {ex.Message}");
            return errorResponse;
        }
    }
}

// Clases auxiliares para requests
public class IncidentTicketRequest
{
    public SecurityEvent SecurityEvent { get; set; } = new();
    public string Priority { get; set; } = string.Empty;
    public string AssignmentGroup { get; set; } = string.Empty;
    public string Category { get; set; } = "Security Incident";
    public Dictionary<string, object> CustomFields { get; set; } = new();
}

public class IncidentEscalationRequest
{
    public string IncidentId { get; set; } = string.Empty;
    public string CurrentLevel { get; set; } = string.Empty;
    public string TargetLevel { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public List<string> NotifyUsers { get; set; } = new();
}

public class AutoRemediationRequest
{
    public string IncidentId { get; set; } = string.Empty;
    public string RemediationType { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool RequireApproval { get; set; }
    public List<string> ApprovalUsers { get; set; } = new();
}

// Clases de resultado
public class WorkflowExecutionResult
{
    public string Status { get; set; } = string.Empty;
    public TimeSpan ExecutionTime { get; set; }
    public List<string> NextSteps { get; set; } = new();
    public Dictionary<string, object> Results { get; set; } = new();
}

public class IncidentTicketResult
{
    public string TicketId { get; set; } = string.Empty;
    public string TicketUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class EscalationResult
{
    public string IncidentId { get; set; } = string.Empty;
    public string EscalationLevel { get; set; } = string.Empty;
    public List<string> NotifiedUsers { get; set; } = new();
    public DateTime EscalatedAt { get; set; } = DateTime.UtcNow;
}

public class IncidentStatusResult
{
    public string IncidentId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public List<ResponseAction> Actions { get; set; } = new();
    public string AssignedTo { get; set; } = string.Empty;
}

public class AutoRemediationResult
{
    public string RemediationId { get; set; } = Guid.NewGuid().ToString();
    public string Status { get; set; } = string.Empty;
    public List<ResponseAction> ExecutedActions { get; set; } = new();
    public string Result { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
} 