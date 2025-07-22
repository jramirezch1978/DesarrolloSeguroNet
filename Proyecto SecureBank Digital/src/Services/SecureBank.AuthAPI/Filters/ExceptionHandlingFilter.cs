using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SecureBank.AuthAPI.Services;
using System.Net;

namespace SecureBank.AuthAPI.Filters;

/// <summary>
/// Filtro global para manejo de excepciones
/// </summary>
public class ExceptionHandlingFilter : IExceptionFilter
{
    private readonly IAzureMonitorService _azureMonitor;
    private readonly ILogger<ExceptionHandlingFilter> _logger;

    public ExceptionHandlingFilter(IAzureMonitorService azureMonitor, ILogger<ExceptionHandlingFilter> logger)
    {
        _azureMonitor = azureMonitor;
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;
        var requestId = Guid.NewGuid().ToString();

        _logger.LogError(exception, "Excepción no manejada en {Action}. RequestId: {RequestId}", 
            context.ActionDescriptor.DisplayName, requestId);

        // Log de excepción en Azure Monitor
        _azureMonitor.TrackException(exception, new Dictionary<string, string>
        {
            ["RequestId"] = requestId,
            ["Controller"] = context.Controller.GetType().Name,
            ["Action"] = context.ActionDescriptor.DisplayName ?? "Unknown",
            ["IpAddress"] = GetClientIpAddress(context.HttpContext.Request),
            ["UserAgent"] = context.HttpContext.Request.Headers.UserAgent.ToString(),
            ["Timestamp"] = DateTime.UtcNow.ToString("O")
        });

        var response = new
        {
            Message = "Se ha producido un error interno del servidor",
            RequestId = requestId,
            Timestamp = DateTime.UtcNow
        };

        context.Result = new ObjectResult(response)
        {
            StatusCode = (int)HttpStatusCode.InternalServerError
        };

        context.ExceptionHandled = true;
    }

    private static string GetClientIpAddress(HttpRequest request)
    {
        return request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim() ??
               request.Headers["X-Real-IP"].FirstOrDefault() ??
               request.HttpContext.Connection.RemoteIpAddress?.ToString() ??
               "unknown";
    }
} 