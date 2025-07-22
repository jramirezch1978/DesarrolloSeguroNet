using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SecureBank.AuthAPI.Services;

namespace SecureBank.AuthAPI.Filters;

/// <summary>
/// Filtro de acción para auditoría de seguridad
/// </summary>
public class SecurityAuditActionFilter : IAsyncActionFilter
{
    private readonly IAzureMonitorService _azureMonitor;
    private readonly ILogger<SecurityAuditActionFilter> _logger;

    public SecurityAuditActionFilter(IAzureMonitorService azureMonitor, ILogger<SecurityAuditActionFilter> logger)
    {
        _azureMonitor = azureMonitor;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var startTime = DateTime.UtcNow;
        var actionName = context.ActionDescriptor.DisplayName;
        var controllerName = context.Controller.GetType().Name;
        var ipAddress = GetClientIpAddress(context.HttpContext.Request);
        var userAgent = context.HttpContext.Request.Headers.UserAgent.ToString();

        try
        {
            // Log de inicio de acción
            await _azureMonitor.LogUserBehaviorAsync("ActionStarted", new
            {
                Controller = controllerName,
                Action = actionName,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Timestamp = startTime
            });

            var executedContext = await next();

            var duration = DateTime.UtcNow - startTime;

            // Log de finalización de acción
            await _azureMonitor.LogUserBehaviorAsync("ActionCompleted", new
            {
                Controller = controllerName,
                Action = actionName,
                Duration = duration.TotalMilliseconds,
                StatusCode = context.HttpContext.Response.StatusCode,
                Success = executedContext.Exception == null,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Timestamp = DateTime.UtcNow
            });

            if (executedContext.Exception != null)
            {
                _azureMonitor.TrackException(executedContext.Exception, new Dictionary<string, string>
                {
                    ["Controller"] = controllerName,
                    ["Action"] = actionName,
                    ["IpAddress"] = ipAddress
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en SecurityAuditActionFilter para {Action}", actionName);
        }
    }

    private static string GetClientIpAddress(HttpRequest request)
    {
        return request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim() ??
               request.Headers["X-Real-IP"].FirstOrDefault() ??
               request.HttpContext.Connection.RemoteIpAddress?.ToString() ??
               "unknown";
    }
} 