using System.Diagnostics;
using System.Text;
using SecureBank.AuthAPI.Services;

namespace SecureBank.AuthAPI.Middleware;

/// <summary>
/// Middleware para logging detallado de requests en Azure Monitor
/// Captura información para análisis de Machine Learning y detección de fraude
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Ignorar health checks y swagger
        if (ShouldSkipLogging(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var requestId = Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;

        // Capturar información del request
        var requestInfo = await CaptureRequestInfo(context, requestId, startTime);

        // Log del request entrante
        _logger.LogInformation("Request iniciado: {RequestId} {Method} {Path}", 
            requestId, context.Request.Method, context.Request.Path);

        Exception? exception = null;
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            
            // Capturar información del response
            var responseInfo = CaptureResponseInfo(context, stopwatch.ElapsedMilliseconds, exception);
            
            // Combinar información para Azure Monitor
            var logData = CombineRequestResponseInfo(requestInfo, responseInfo);
            
            // Enviar a Azure Monitor (si el servicio está disponible)
            var azureMonitor = context.RequestServices.GetService<IAzureMonitorService>();
            if (azureMonitor != null)
            {
                try
                {
                    await azureMonitor.LogUserBehaviorAsync("HttpRequest", logData, 
                        requestInfo.UserId ?? "anonymous");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando log a Azure Monitor");
                }
            }

            // Log local
            var level = DetermineLogLevel(context.Response.StatusCode, exception);
            _logger.Log(level, 
                "Request completado: {RequestId} {Method} {Path} {StatusCode} en {Duration}ms",
                requestId, context.Request.Method, context.Request.Path, 
                context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<RequestInfo> CaptureRequestInfo(HttpContext context, string requestId, DateTime startTime)
    {
        var request = context.Request;
        
        return new RequestInfo
        {
            RequestId = requestId,
            Timestamp = startTime,
            Method = request.Method,
            Path = request.Path.Value ?? "",
            QueryString = request.QueryString.Value ?? "",
            UserAgent = request.Headers.UserAgent.ToString(),
            IpAddress = GetClientIpAddress(request),
            DeviceFingerprint = request.Headers["X-Device-Fingerprint"].ToString(),
            SessionId = request.Headers["X-Session-Id"].ToString(),
            Referer = request.Headers.Referer.ToString(),
            AcceptLanguage = request.Headers.AcceptLanguage.ToString(),
            ContentType = request.ContentType ?? "",
            ContentLength = request.ContentLength ?? 0,
            IsHttps = request.IsHttps,
            Host = request.Host.Value ?? "",
            Scheme = request.Scheme,
            Protocol = request.Protocol,
            Headers = ExtractRelevantHeaders(request.Headers),
            UserId = ExtractUserId(context),
            TimeOfDay = startTime.Hour,
            DayOfWeek = startTime.DayOfWeek.ToString(),
            IsWeekend = startTime.DayOfWeek == DayOfWeek.Saturday || startTime.DayOfWeek == DayOfWeek.Sunday,
            RequestBody = await CaptureRequestBody(context.Request)
        };
    }

    private ResponseInfo CaptureResponseInfo(HttpContext context, long durationMs, Exception? exception)
    {
        var response = context.Response;
        
        return new ResponseInfo
        {
            StatusCode = response.StatusCode,
            Duration = durationMs,
            ContentType = response.ContentType ?? "",
            ContentLength = response.ContentLength ?? 0,
            Headers = ExtractRelevantResponseHeaders(response.Headers),
            HasException = exception != null,
            ExceptionType = exception?.GetType().Name,
            ExceptionMessage = exception?.Message,
            IsSuccess = response.StatusCode >= 200 && response.StatusCode < 300,
            IsClientError = response.StatusCode >= 400 && response.StatusCode < 500,
            IsServerError = response.StatusCode >= 500,
            ResponseCategory = CategorizeResponse(response.StatusCode)
        };
    }

    private object CombineRequestResponseInfo(RequestInfo request, ResponseInfo response)
    {
        return new
        {
            // Identificadores
            RequestId = request.RequestId,
            Timestamp = request.Timestamp,
            
            // Request info
            Method = request.Method,
            Path = request.Path,
            QueryString = request.QueryString,
            Host = request.Host,
            IsHttps = request.IsHttps,
            
            // User info para ML
            UserId = request.UserId,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            DeviceFingerprint = request.DeviceFingerprint,
            SessionId = request.SessionId,
            
            // Timing para ML de patrones
            TimeOfDay = request.TimeOfDay,
            DayOfWeek = request.DayOfWeek,
            IsWeekend = request.IsWeekend,
            
            // Headers relevantes para detección de anomalías
            AcceptLanguage = request.AcceptLanguage,
            Referer = request.Referer,
            
            // Request size para análisis de comportamiento
            ContentLength = request.ContentLength,
            HasRequestBody = request.ContentLength > 0,
            
            // Response info
            StatusCode = response.StatusCode,
            Duration = response.Duration,
            ResponseContentLength = response.ContentLength,
            
            // Categorización para ML
            IsSuccess = response.IsSuccess,
            IsClientError = response.IsClientError,
            IsServerError = response.IsServerError,
            ResponseCategory = response.ResponseCategory,
            
            // Error info para análisis de patrones
            HasException = response.HasException,
            ExceptionType = response.ExceptionType,
            
            // Métricas calculadas para ML
            RequestsPerSecond = CalculateRequestRate(request.UserId, request.IpAddress),
            SuspiciousActivity = DetectSuspiciousActivity(request, response),
            RiskScore = CalculateRiskScore(request, response),
            
            // Geolocalización simulada (en producción sería real)
            Country = DetermineCountryFromIp(request.IpAddress),
            City = DetermineCityFromIp(request.IpAddress),
            
            // Request body insights (sin datos sensibles)
            RequestBodySize = request.RequestBody?.Length ?? 0,
            HasSensitiveFields = DetectSensitiveFields(request.RequestBody)
        };
    }

    private async Task<string?> CaptureRequestBody(HttpRequest request)
    {
        // Solo capturar para endpoints de análisis y sin datos sensibles
        if (request.ContentLength > 10000 || 
            !request.ContentType?.Contains("application/json") == true ||
            request.Path.StartsWithSegments("/api/auth"))
        {
            return null;
        }

        try
        {
            request.EnableBuffering();
            var buffer = new byte[Convert.ToInt32(request.ContentLength ?? 0)];
            await request.Body.ReadAsync(buffer);
            request.Body.Position = 0; // Reset para el siguiente middleware
            
            var body = Encoding.UTF8.GetString(buffer);
            
            // Censurar datos sensibles antes del logging
            return CensorSensitiveData(body);
        }
        catch
        {
            return null;
        }
    }

    private Dictionary<string, string> ExtractRelevantHeaders(IHeaderDictionary headers)
    {
        var relevantHeaders = new Dictionary<string, string>();
        
        var headersToCapture = new[]
        {
            "Accept", "Accept-Encoding", "Accept-Language", "Cache-Control",
            "Connection", "DNT", "Sec-Fetch-Dest", "Sec-Fetch-Mode", "Sec-Fetch-Site",
            "X-Requested-With", "X-Forwarded-For", "X-Real-IP"
        };

        foreach (var header in headersToCapture)
        {
            if (headers.TryGetValue(header, out var value))
            {
                relevantHeaders[header] = value.ToString();
            }
        }

        return relevantHeaders;
    }

    private Dictionary<string, string> ExtractRelevantResponseHeaders(IHeaderDictionary headers)
    {
        var relevantHeaders = new Dictionary<string, string>();
        
        var headersToCapture = new[]
        {
            "Cache-Control", "Content-Encoding", "Set-Cookie", "X-Response-Time"
        };

        foreach (var header in headersToCapture)
        {
            if (headers.TryGetValue(header, out var value))
            {
                relevantHeaders[header] = value.ToString();
            }
        }

        return relevantHeaders;
    }

    // Métodos auxiliares para ML y análisis
    private string GetClientIpAddress(HttpRequest request)
    {
        return request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim() ??
               request.Headers["X-Real-IP"].FirstOrDefault() ??
               request.HttpContext.Connection.RemoteIpAddress?.ToString() ??
               "unknown";
    }

    private string? ExtractUserId(HttpContext context)
    {
        return context.User?.FindFirst("sub")?.Value ??
               context.User?.FindFirst("user_id")?.Value;
    }

    private string CensorSensitiveData(string body)
    {
        if (string.IsNullOrEmpty(body)) return body;
        
        // Censurar campos sensibles comunes
        var sensitiveFields = new[] { "pin", "password", "secret", "token", "key", "ssn", "document" };
        
        var result = body;
        foreach (var field in sensitiveFields)
        {
            // Patrón simple para JSON
            var pattern = $"\"{field}\"\\s*:\\s*\"[^\"]*\"";
            result = System.Text.RegularExpressions.Regex.Replace(result, pattern, 
                $"\"{field}\": \"***\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        
        return result;
    }

    private double CalculateRequestRate(string? userId, string ipAddress)
    {
        // Implementación simplificada - en producción usaríamos Redis o cache distribuido
        return Random.Shared.NextDouble() * 10; // Simulado
    }

    private bool DetectSuspiciousActivity(RequestInfo request, ResponseInfo response)
    {
        // Lógica de detección de actividad sospechosa
        return request.UserAgent.Contains("bot", StringComparison.OrdinalIgnoreCase) ||
               response.StatusCode == 401 ||
               request.Method == "POST" && response.Duration > 5000;
    }

    private int CalculateRiskScore(RequestInfo request, ResponseInfo response)
    {
        var score = 0;
        
        if (response.IsClientError) score += 2;
        if (response.IsServerError) score += 3;
        if (request.UserAgent.Contains("bot", StringComparison.OrdinalIgnoreCase)) score += 5;
        if (response.Duration > 10000) score += 1;
        if (!request.IsHttps) score += 2;
        
        return Math.Min(score, 10); // Cap at 10
    }

    private string DetermineCountryFromIp(string ipAddress)
    {
        // Simulado - en producción usaríamos un servicio de geolocalización
        return ipAddress.StartsWith("192.168") ? "Peru" : "Unknown";
    }

    private string DetermineCityFromIp(string ipAddress)
    {
        // Simulado - en producción usaríamos un servicio de geolocalización
        return ipAddress.StartsWith("192.168") ? "Lima" : "Unknown";
    }

    private bool DetectSensitiveFields(string? requestBody)
    {
        if (string.IsNullOrEmpty(requestBody)) return false;
        
        var sensitiveKeywords = new[] { "pin", "password", "secret", "token", "ssn", "document" };
        return sensitiveKeywords.Any(keyword => 
            requestBody.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private string CategorizeResponse(int statusCode)
    {
        return statusCode switch
        {
            >= 200 and < 300 => "Success",
            >= 300 and < 400 => "Redirect",
            >= 400 and < 500 => "ClientError",
            >= 500 => "ServerError",
            _ => "Unknown"
        };
    }

    private LogLevel DetermineLogLevel(int statusCode, Exception? exception)
    {
        if (exception != null || statusCode >= 500) return LogLevel.Error;
        if (statusCode >= 400) return LogLevel.Warning;
        return LogLevel.Information;
    }

    private bool ShouldSkipLogging(PathString path)
    {
        var pathsToSkip = new[] { "/health", "/swagger", "/favicon.ico" };
        return pathsToSkip.Any(p => path.StartsWithSegments(p));
    }
}

// Clases auxiliares para estructurar información
public class RequestInfo
{
    public string RequestId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string QueryString { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string DeviceFingerprint { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string Referer { get; set; } = string.Empty;
    public string AcceptLanguage { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long ContentLength { get; set; }
    public bool IsHttps { get; set; }
    public string Host { get; set; } = string.Empty;
    public string Scheme { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? UserId { get; set; }
    public int TimeOfDay { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public bool IsWeekend { get; set; }
    public string? RequestBody { get; set; }
}

public class ResponseInfo
{
    public int StatusCode { get; set; }
    public long Duration { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long ContentLength { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public bool HasException { get; set; }
    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }
    public bool IsSuccess { get; set; }
    public bool IsClientError { get; set; }
    public bool IsServerError { get; set; }
    public string ResponseCategory { get; set; } = string.Empty;
} 