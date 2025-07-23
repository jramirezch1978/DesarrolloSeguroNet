namespace SecureBank.AuthAPI.Middleware;

/// <summary>
/// Middleware para agregar headers de seguridad
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Agregar headers de seguridad estándar
        if (!context.Response.Headers.ContainsKey("X-Content-Type-Options"))
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

        if (!context.Response.Headers.ContainsKey("X-Frame-Options"))
            context.Response.Headers.Add("X-Frame-Options", "DENY");

        if (!context.Response.Headers.ContainsKey("X-XSS-Protection"))
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

        if (!context.Response.Headers.ContainsKey("Referrer-Policy"))
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

        if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
            context.Response.Headers.Add("Content-Security-Policy", 
                "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:;");

        if (!context.Response.Headers.ContainsKey("Strict-Transport-Security") && context.Request.IsHttps)
            context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");

        if (!context.Response.Headers.ContainsKey("Permissions-Policy"))
            context.Response.Headers.Add("Permissions-Policy", 
                "geolocation=(), microphone=(), camera=(), payment=(), usb=()");

        await _next(context);
    }
} 