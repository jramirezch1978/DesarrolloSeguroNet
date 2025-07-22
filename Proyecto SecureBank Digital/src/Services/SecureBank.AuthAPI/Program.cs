using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using FluentValidation;
using MediatR;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SecureBank.Infrastructure.Data;
using SecureBank.Application.Common.Interfaces;
using SecureBank.Infrastructure.Security.Services;
using SecureBank.Infrastructure.Services;
using SecureBank.AuthAPI.Services;
using SecureBank.AuthAPI.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// CONFIGURACIÓN DE AZURE KEY VAULT
// =============================================================================
if (!builder.Environment.IsDevelopment())
{
    var keyVaultEndpoint = builder.Configuration["AzureKeyVault:VaultUri"];
    if (!string.IsNullOrEmpty(keyVaultEndpoint))
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultEndpoint),
            new DefaultAzureCredential());
    }
}

// =============================================================================
// CONFIGURACIÓN DE AZURE MONITOR Y APPLICATION INSIGHTS
// =============================================================================
var connectionString = builder.Configuration.GetConnectionString("ApplicationInsights");
if (!string.IsNullOrEmpty(connectionString))
{
    // Azure Monitor con OpenTelemetry
    builder.Services.AddOpenTelemetry()
        .UseAzureMonitor(options =>
        {
            options.ConnectionString = connectionString;
        })
        .WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.Filter = httpContext =>
                {
                    // Filtrar endpoints de health check
                    return !httpContext.Request.Path.StartsWithSegments("/health");
                };
                options.EnrichWithHttpRequest = (activity, httpRequest) =>
                {
                    activity.SetTag("securebank.user_agent", httpRequest.Headers.UserAgent.ToString());
                    activity.SetTag("securebank.ip_address", GetClientIpAddress(httpRequest));
                };
                options.EnrichWithHttpResponse = (activity, httpResponse) =>
                {
                    activity.SetTag("securebank.response_size", httpResponse.ContentLength);
                };
            });
            tracing.AddHttpClientInstrumentation();
            tracing.AddEntityFrameworkCoreInstrumentation();
        })
        .WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation();
            metrics.AddHttpClientInstrumentation();
            metrics.AddRuntimeInstrumentation();
        })
        .WithLogging(logging =>
        {
            logging.AddApplicationInsightsConsole();
        });

    // Application Insights
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = connectionString;
        options.EnableAdaptiveSampling = true;
        options.EnableQuickPulseMetricStream = true;
        options.EnableAuthenticationTrackingJavaScript = true;
        options.EnableDependencyTrackingTelemetryModule = true;
        options.EnablePerformanceCounterCollectionModule = true;
    });
}

// =============================================================================
// CONFIGURACIÓN DE SERVICIOS PRINCIPALES
// =============================================================================

// Entity Framework con PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(30);
    });
    
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging(false); // Seguridad: nunca en producción
        options.EnableDetailedErrors(true);
    }
});

builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());

// MediatR para CQRS
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(SecureBank.Application.AssemblyReference).Assembly);
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(SecureBank.Application.AssemblyReference).Assembly);

// AutoMapper
builder.Services.AddAutoMapper(typeof(SecureBank.Application.AssemblyReference).Assembly);

// =============================================================================
// CONFIGURACIÓN DE SERVICIOS DE SEGURIDAD
// =============================================================================

// Servicios de encriptación y JWT
builder.Services.Configure<EncryptionOptions>(
    builder.Configuration.GetSection("Encryption"));
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt"));

builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAzureMonitorService, AzureMonitorService>();

// Autenticación JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT Secret key not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
        RequireExpirationTime = true,
        RequireSignedTokens = true
    };
    
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userService = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
            var azureMonitor = context.HttpContext.RequestServices.GetRequiredService<IAzureMonitorService>();
            
            // Log de validación de token exitosa
            await azureMonitor.LogSecurityEventAsync("TokenValidated", new
            {
                UserId = userService.UserId,
                IpAddress = userService.IpAddress,
                UserAgent = userService.UserAgent,
                Timestamp = DateTime.UtcNow
            });
        },
        OnAuthenticationFailed = async context =>
        {
            var azureMonitor = context.HttpContext.RequestServices.GetRequiredService<IAzureMonitorService>();
            
            // Log de falla de autenticación
            await azureMonitor.LogSecurityEventAsync("AuthenticationFailed", new
            {
                Exception = context.Exception.Message,
                IpAddress = GetClientIpAddress(context.Request),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                Timestamp = DateTime.UtcNow
            });
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireCustomerRole", policy =>
        policy.RequireRole("Customer", "CustomerPremium", "CustomerBusiness"));
    
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole("Administrator", "Manager"));
    
    options.AddPolicy("RequireVerifiedUser", policy =>
        policy.RequireClaim("is_verified", "True"));
});

// =============================================================================
// CONFIGURACIÓN DE RATE LIMITING
// =============================================================================
builder.Services.AddRateLimiter(options =>
{
    // Rate limiting para login attempts
    options.AddFixedWindowLimiter("LoginAttempts", config =>
    {
        config.PermitLimit = 5;
        config.Window = TimeSpan.FromMinutes(15);
        config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        config.QueueLimit = 0; // No queue, reject immediately
    });
    
    // Rate limiting para registro de usuarios
    options.AddFixedWindowLimiter("Registration", config =>
    {
        config.PermitLimit = 3;
        config.Window = TimeSpan.FromHours(1);
        config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        config.QueueLimit = 0;
    });
    
    // Rate limiting general por IP
    options.AddFixedWindowLimiter("GeneralApi", config =>
    {
        config.PermitLimit = 100;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        config.QueueLimit = 10;
    });
    
    options.OnRejected = async (context, token) =>
    {
        var azureMonitor = context.HttpContext.RequestServices.GetRequiredService<IAzureMonitorService>();
        
        await azureMonitor.LogSecurityEventAsync("RateLimitExceeded", new
        {
            IpAddress = GetClientIpAddress(context.HttpContext.Request),
            Endpoint = context.HttpContext.Request.Path,
            UserAgent = context.HttpContext.Request.Headers.UserAgent.ToString(),
            Timestamp = DateTime.UtcNow
        });
        
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.");
    };
});

// =============================================================================
// CONFIGURACIÓN DE CONTROLLERS Y API
// =============================================================================
builder.Services.AddControllers(options =>
{
    options.Filters.Add<SecurityAuditActionFilter>();
    options.Filters.Add<ExceptionHandlingFilter>();
})
.AddNewtonsoftJson(options =>
{
    options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
    options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
});

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = Microsoft.AspNetCore.Mvc.ApiVersionReader.Combine(
        new Microsoft.AspNetCore.Mvc.QueryStringApiVersionReader("version"),
        new Microsoft.AspNetCore.Mvc.HeaderApiVersionReader("X-Version"));
    options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SecureBank Digital - Authentication API",
        Version = "v1",
        Description = "API de autenticación segura para SecureBank Digital",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "SecureBank Digital",
            Email = "dev@securebankdigital.pe"
        }
    });
    
    // Configuración de seguridad JWT en Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// =============================================================================
// CONFIGURACIÓN DE HEALTH CHECKS
// =============================================================================
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database")
    .AddCheck("azure_keyvault", () =>
    {
        try
        {
            var keyVaultUri = builder.Configuration["AzureKeyVault:VaultUri"];
            return !string.IsNullOrEmpty(keyVaultUri) 
                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Key Vault configured")
                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded("Key Vault not configured");
        }
        catch
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Key Vault error");
        }
    });

// =============================================================================
// CONFIGURACIÓN DE CORS
// =============================================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("SecureBankPolicy", policy =>
    {
        policy
            .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "https://localhost:7001" })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });
});

// =============================================================================
// BUILD Y CONFIGURACIÓN DE MIDDLEWARE
// =============================================================================
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SecureBank Auth API v1");
        options.RoutePrefix = "swagger";
        options.DisplayRequestDuration();
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
    
    await next();
});

app.UseHttpsRedirection();
app.UseCors("SecureBankPolicy");
app.UseRateLimiter();

// Middleware personalizado
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Health checks
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                duration = x.Value.Duration.TotalMilliseconds
            })
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

app.MapControllers();

// Inicialización de base de datos en desarrollo
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        await context.Database.EnsureCreatedAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error inicializando base de datos");
    }
}

app.Run();

// Helper method
static string GetClientIpAddress(HttpRequest request)
{
    return request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim() ??
           request.Headers["X-Real-IP"].FirstOrDefault() ??
           request.HttpContext.Connection.RemoteIpAddress?.ToString() ??
           "unknown";
} 