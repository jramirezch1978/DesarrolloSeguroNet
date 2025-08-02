// ==========================================
// LABORATORIO 37: AZURE KEY VAULT INTEGRATION
// ==========================================
// Este archivo demuestra la integración completa con Azure Key Vault
// Manejo seguro de secretos sin credenciales hardcodeadas

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace SecureShop.Security;

/// <summary>
/// Configuración central de Key Vault para SecureShop
/// Implementa patrones de seguridad empresarial para gestión de secretos
/// </summary>
public static class KeyVaultConfiguration
{
    /// <summary>
    /// Configura Key Vault en el pipeline de configuración
    /// Soporta múltiples ambientes con fallback seguro
    /// </summary>
    public static IConfigurationBuilder AddSecureShopKeyVault(
        this IConfigurationBuilder builder, 
        IWebHostEnvironment environment)
    {
        var config = builder.Build();
        var keyVaultUri = config["KeyVault:VaultUri"];
        
        if (!environment.IsDevelopment() && !string.IsNullOrEmpty(keyVaultUri))
        {
            try
            {
                // ===== MANAGED IDENTITY CONFIGURATION =====
                var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    // Intentar métodos en orden de preferencia
                    ExcludeInteractiveBrowserCredential = true,
                    ExcludeSharedTokenCacheCredential = true,
                    ExcludeVisualStudioCredential = environment.IsProduction(),
                    ExcludeVisualStudioCodeCredential = environment.IsProduction(),
                    ExcludeAzureCliCredential = environment.IsProduction(),
                    ExcludeEnvironmentCredential = false,
                    ExcludeManagedIdentityCredential = false
                });

                // ===== KEY VAULT INTEGRATION =====
                builder.AddAzureKeyVault(new Uri(keyVaultUri), credential, options =>
                {
                    // Configurar mapeo de nombres de secretos
                    options.Manager = new SecureShopKeyVaultSecretManager();
                    
                    // Configurar reintentos para resiliencia
                    options.ReloadInterval = TimeSpan.FromMinutes(30);
                });

                Console.WriteLine($"✅ Key Vault configurado: {keyVaultUri}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error configurando Key Vault: {ex.Message}");
                Console.WriteLine("🔧 Continuando con configuración local...");
            }
        }
        else
        {
            Console.WriteLine("🔧 Usando configuración local (desarrollo)");
        }

        return builder;
    }
}

/// <summary>
/// Gestor personalizado de secretos de Key Vault
/// Implementa convenciones de nombres y filtros de seguridad
/// </summary>
public class SecureShopKeyVaultSecretManager : KeyVaultSecretManager
{
    private static readonly Dictionary<string, string> SecretMappings = new()
    {
        // Mapeo de nombres de Key Vault a configuración .NET
        ["ConnectionStrings--DefaultConnection"] = "ConnectionStrings:DefaultConnection",
        ["AzureAd--TenantId"] = "AzureAd:TenantId",
        ["AzureAd--ClientId"] = "AzureAd:ClientId",
        ["Encryption--DataProtectionKey"] = "Encryption:DataProtectionKey",
        ["ExternalServices--PaymentGatewayKey"] = "ExternalServices:PaymentGatewayKey",
        ["ExternalServices--EmailServiceKey"] = "ExternalServices:EmailServiceKey",
        ["Authentication--JwtSigningKey"] = "Authentication:JwtSigningKey"
    };

    /// <summary>
    /// Convierte nombres de Key Vault a claves de configuración .NET
    /// </summary>
    public override string GetKey(KeyVaultSecret secret)
    {
        // Buscar mapeo específico
        if (SecretMappings.TryGetValue(secret.Name, out var mappedKey))
        {
            return mappedKey;
        }

        // Convertir notación de Key Vault (--) a .NET (:)
        return secret.Name.Replace("--", ":");
    }

    /// <summary>
    /// Filtrar secretos que deben cargarse
    /// Implementa whitelist approach para seguridad
    /// </summary>
    public override bool Load(KeyVaultSecret secret)
    {
        // Lista blanca de prefijos permitidos
        var allowedPrefixes = new[]
        {
            "ConnectionStrings",
            "AzureAd",
            "Encryption",
            "ExternalServices",
            "Authentication",
            "Logging",
            "Features"
        };

        return allowedPrefixes.Any(prefix => 
            secret.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Servicio empresarial para gestión de secretos
/// Implementa cache, reintentos y auditoría completa
/// </summary>
public interface ISecureSecretService
{
    Task<string?> GetSecretAsync(string secretName);
    Task<T?> GetSecretAsync<T>(string secretName, Func<string, T> converter);
    Task<Dictionary<string, string>> GetSecretsAsync(string[] secretNames);
    Task SetSecretAsync(string secretName, string secretValue);
    Task<bool> SecretExistsAsync(string secretName);
    Task RotateSecretAsync(string secretName, Func<Task<string>> secretGenerator);
}

/// <summary>
/// Implementación de producción con Azure Key Vault
/// Optimizada para performance y resiliencia
/// </summary>
public class KeyVaultSecretService : ISecureSecretService
{
    private readonly SecretClient _secretClient;
    private readonly ILogger<KeyVaultSecretService> _logger;
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores;

    // Configuración de cache y reintentos
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly int MaxRetries = 3;

    public KeyVaultSecretService(
        SecretClient secretClient,
        ILogger<KeyVaultSecretService> logger,
        IMemoryCache cache)
    {
        _secretClient = secretClient;
        _logger = logger;
        _cache = cache;
        _semaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
    }

    /// <summary>
    /// Obtiene secreto con cache inteligente y reintentos
    /// </summary>
    public async Task<string?> GetSecretAsync(string secretName)
    {
        var cacheKey = $"secret:{secretName}";
        
        // ===== VERIFICAR CACHE PRIMERO =====
        if (_cache.TryGetValue(cacheKey, out string? cachedValue))
        {
            _logger.LogDebug("📋 Secreto obtenido de cache: {SecretName}", secretName);
            return cachedValue;
        }

        // ===== OBTENER SEMÁFORO PARA EVITAR MÚLTIPLES REQUESTS =====
        var semaphore = _semaphores.GetOrAdd(secretName, _ => new SemaphoreSlim(1, 1));
        
        await semaphore.WaitAsync();
        try
        {
            // Verificar cache nuevamente (podría haber sido llenado por otro thread)
            if (_cache.TryGetValue(cacheKey, out cachedValue))
            {
                return cachedValue;
            }

            // ===== OBTENER DE KEY VAULT CON REINTENTOS =====
            string? secretValue = null;
            var lastException = default(Exception);

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    var response = await _secretClient.GetSecretAsync(secretName);
                    secretValue = response.Value.Value;
                    
                    _logger.LogInformation("🔐 Secreto obtenido de Key Vault: {SecretName} (intento {Attempt})", 
                        secretName, attempt);
                    break;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning("⚠️ Error obteniendo secreto {SecretName} (intento {Attempt}): {Error}",
                        secretName, attempt, ex.Message);

                    if (attempt < MaxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
                        await Task.Delay(delay);
                    }
                }
            }

            if (secretValue == null)
            {
                _logger.LogError("❌ Falló obtención de secreto {SecretName} después de {MaxRetries} intentos: {Error}",
                    secretName, MaxRetries, lastException?.Message);
                return null;
            }

            // ===== CACHEAR CON EXPIRACIÓN SLIDING =====
            var cacheOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = CacheDuration,
                AbsoluteExpirationRelativeToNow = CacheExpiration,
                Priority = CacheItemPriority.Normal
            };

            _cache.Set(cacheKey, secretValue, cacheOptions);
            
            return secretValue;
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Obtiene secreto con conversión de tipo
    /// </summary>
    public async Task<T?> GetSecretAsync<T>(string secretName, Func<string, T> converter)
    {
        try
        {
            var secretValue = await GetSecretAsync(secretName);
            if (secretValue == null) return default(T);

            return converter(secretValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error convirtiendo secreto {SecretName} a tipo {Type}: {Error}",
                secretName, typeof(T).Name, ex.Message);
            return default(T);
        }
    }

    /// <summary>
    /// Obtiene múltiples secretos en paralelo
    /// </summary>
    public async Task<Dictionary<string, string>> GetSecretsAsync(string[] secretNames)
    {
        var results = new ConcurrentDictionary<string, string>();
        
        var tasks = secretNames.Select(async secretName =>
        {
            var value = await GetSecretAsync(secretName);
            if (value != null)
            {
                results.TryAdd(secretName, value);
            }
        });

        await Task.WhenAll(tasks);
        
        _logger.LogInformation("📦 Obtenidos {Count} de {Total} secretos solicitados",
            results.Count, secretNames.Length);
            
        return results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Establece o actualiza un secreto
    /// </summary>
    public async Task SetSecretAsync(string secretName, string secretValue)
    {
        try
        {
            var response = await _secretClient.SetSecretAsync(secretName, secretValue);
            
            // Invalidar cache
            var cacheKey = $"secret:{secretName}";
            _cache.Remove(cacheKey);
            
            _logger.LogInformation("✅ Secreto actualizado en Key Vault: {SecretName} (Version: {Version})",
                secretName, response.Value.Properties.Version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error actualizando secreto {SecretName}: {Error}",
                secretName, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Verifica si un secreto existe
    /// </summary>
    public async Task<bool> SecretExistsAsync(string secretName)
    {
        try
        {
            await _secretClient.GetSecretAsync(secretName);
            return true;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error verificando existencia de secreto {SecretName}: {Error}",
                secretName, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Rota un secreto usando generador personalizado
    /// Implementa rotación segura con rollback
    /// </summary>
    public async Task RotateSecretAsync(string secretName, Func<Task<string>> secretGenerator)
    {
        var backupName = $"{secretName}-backup-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        
        try
        {
            // ===== PASO 1: CREAR BACKUP DEL SECRETO ACTUAL =====
            var currentSecret = await GetSecretAsync(secretName);
            if (currentSecret != null)
            {
                await _secretClient.SetSecretAsync(backupName, currentSecret);
                _logger.LogInformation("💾 Backup creado para secreto {SecretName}: {BackupName}",
                    secretName, backupName);
            }

            // ===== PASO 2: GENERAR NUEVO SECRETO =====
            var newSecretValue = await secretGenerator();
            
            // ===== PASO 3: ACTUALIZAR SECRETO PRINCIPAL =====
            await SetSecretAsync(secretName, newSecretValue);
            
            _logger.LogInformation("🔄 Secreto rotado exitosamente: {SecretName}", secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error rotando secreto {SecretName}: {Error}",
                secretName, ex.Message);
            
            // ===== ROLLBACK: RESTAURAR DESDE BACKUP =====
            try
            {
                var backupSecret = await _secretClient.GetSecretAsync(backupName);
                await SetSecretAsync(secretName, backupSecret.Value.Value);
                
                _logger.LogWarning("↩️ Rollback exitoso para secreto {SecretName}", secretName);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogCritical(rollbackEx, "🚨 CRÍTICO: Falló rollback para secreto {SecretName}",
                    secretName);
            }
            
            throw;
        }
    }
}

/// <summary>
/// Implementación para desarrollo local
/// Usa appsettings.json con logging de seguridad
/// </summary>
public class LocalSecretService : ISecureSecretService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LocalSecretService> _logger;

    public LocalSecretService(IConfiguration configuration, ILogger<LocalSecretService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<string?> GetSecretAsync(string secretName)
    {
        var key = secretName.Replace("--", ":");
        var value = _configuration[key];
        
        if (value != null)
        {
            _logger.LogDebug("🔧 Secreto obtenido de configuración local: {SecretName}", secretName);
        }
        else
        {
            _logger.LogWarning("⚠️ Secreto no encontrado en configuración local: {SecretName}", secretName);
        }
        
        return Task.FromResult(value);
    }

    public async Task<T?> GetSecretAsync<T>(string secretName, Func<string, T> converter)
    {
        var value = await GetSecretAsync(secretName);
        return value != null ? converter(value) : default(T);
    }

    public async Task<Dictionary<string, string>> GetSecretsAsync(string[] secretNames)
    {
        var results = new Dictionary<string, string>();
        
        foreach (var secretName in secretNames)
        {
            var value = await GetSecretAsync(secretName);
            if (value != null)
            {
                results[secretName] = value;
            }
        }
        
        return results;
    }

    public Task SetSecretAsync(string secretName, string secretValue)
    {
        _logger.LogWarning("⚠️ SetSecret no implementado en servicio local: {SecretName}", secretName);
        return Task.CompletedTask;
    }

    public Task<bool> SecretExistsAsync(string secretName)
    {
        var value = _configuration[secretName.Replace("--", ":")];
        return Task.FromResult(!string.IsNullOrEmpty(value));
    }

    public Task RotateSecretAsync(string secretName, Func<Task<string>> secretGenerator)
    {
        _logger.LogWarning("⚠️ RotateSecret no implementado en servicio local: {SecretName}", secretName);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Extensiones para registro de servicios de Key Vault
/// </summary>
public static class SecretServiceExtensions
{
    /// <summary>
    /// Registra servicios de gestión de secretos
    /// </summary>
    public static IServiceCollection AddSecureShopSecrets(
        this IServiceCollection services, 
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var keyVaultUri = configuration["KeyVault:VaultUri"];
        
        if (!environment.IsDevelopment() && !string.IsNullOrEmpty(keyVaultUri))
        {
            // ===== CONFIGURACIÓN DE PRODUCCIÓN CON KEY VAULT =====
            services.AddSingleton<SecretClient>(provider =>
            {
                var credential = new DefaultAzureCredential();
                return new SecretClient(new Uri(keyVaultUri), credential);
            });
            
            services.AddScoped<ISecureSecretService, KeyVaultSecretService>();
            
            services.AddMemoryCache(); // Para cache de secretos
        }
        else
        {
            // ===== CONFIGURACIÓN DE DESARROLLO LOCAL =====
            services.AddScoped<ISecureSecretService, LocalSecretService>();
        }
        
        return services;
    }
}

/// <summary>
/// Helper para configuración de connection strings desde Key Vault
/// </summary>
public static class SecureConnectionStringHelper
{
    /// <summary>
    /// Obtiene connection string de forma segura con fallback
    /// </summary>
    public static async Task<string> GetConnectionStringAsync(
        ISecureSecretService secretService,
        IConfiguration fallbackConfiguration,
        string connectionName = "DefaultConnection")
    {
        // Intentar obtener de Key Vault primero
        var keyVaultKey = $"ConnectionStrings--{connectionName}";
        var connectionString = await secretService.GetSecretAsync(keyVaultKey);
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            return connectionString;
        }
        
        // Fallback a configuración local
        var fallbackKey = $"ConnectionStrings:{connectionName}";
        connectionString = fallbackConfiguration[fallbackKey];
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{connectionName}' not found in Key Vault or local configuration");
        }
        
        return connectionString;
    }
}