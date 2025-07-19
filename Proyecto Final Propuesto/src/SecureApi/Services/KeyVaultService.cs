using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System.Collections.Concurrent;

namespace SecureApi.Services;

public class KeyVaultService : IKeyVaultService
{
    private readonly SecretClient _secretClient;
    private readonly ILogger<KeyVaultService> _logger;
    private readonly ConcurrentDictionary<string, (string Value, DateTime Expiry)> _cache;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

    public KeyVaultService(IConfiguration configuration, ILogger<KeyVaultService> logger)
    {
        _logger = logger;
        _cache = new ConcurrentDictionary<string, (string, DateTime)>();

        var keyVaultUrl = configuration["KeyVault:VaultUrl"];
        if (string.IsNullOrEmpty(keyVaultUrl))
        {
            throw new InvalidOperationException("KeyVault:VaultUrl no está configurado");
        }

        var credential = new DefaultAzureCredential();
        _secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        
        _logger.LogInformation("KeyVaultService inicializado con URL: {KeyVaultUrl}", keyVaultUrl);
    }

    public async Task<string?> GetSecretAsync(string secretName)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(secretName);

            // Verificar caché primero
            if (_cache.TryGetValue(secretName, out var cachedValue) && cachedValue.Expiry > DateTime.UtcNow)
            {
                _logger.LogDebug("Secreto {SecretName} obtenido desde caché", secretName);
                return cachedValue.Value;
            }

            // Obtener desde Key Vault
            var response = await _secretClient.GetSecretAsync(secretName);
            var secretValue = response.Value.Value;

            // Guardar en caché
            _cache.TryAdd(secretName, (secretValue, DateTime.UtcNow.Add(_cacheExpiry)));

            _logger.LogInformation("Secreto {SecretName} obtenido exitosamente de Key Vault", secretName);
            return secretValue;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Secreto {SecretName} no encontrado en Key Vault", secretName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el secreto {SecretName} de Key Vault", secretName);
            throw;
        }
    }

    public async Task<bool> SetSecretAsync(string secretName, string secretValue)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(secretName);
            ArgumentException.ThrowIfNullOrEmpty(secretValue);

            await _secretClient.SetSecretAsync(secretName, secretValue);
            
            // Actualizar caché
            _cache.AddOrUpdate(secretName, 
                (secretValue, DateTime.UtcNow.Add(_cacheExpiry)),
                (key, oldValue) => (secretValue, DateTime.UtcNow.Add(_cacheExpiry)));

            _logger.LogInformation("Secreto {SecretName} establecido exitosamente en Key Vault", secretName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al establecer el secreto {SecretName} en Key Vault", secretName);
            return false;
        }
    }

    public async Task<bool> DeleteSecretAsync(string secretName)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(secretName);

            var operation = await _secretClient.StartDeleteSecretAsync(secretName);
            await operation.WaitForCompletionAsync();

            // Remover del caché
            _cache.TryRemove(secretName, out _);

            _logger.LogInformation("Secreto {SecretName} eliminado exitosamente de Key Vault", secretName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar el secreto {SecretName} de Key Vault", secretName);
            return false;
        }
    }

    public async Task<Dictionary<string, string>> GetAllSecretsAsync()
    {
        try
        {
            var secrets = new Dictionary<string, string>();
            
            await foreach (var secretProperties in _secretClient.GetPropertiesOfSecretsAsync())
            {
                if (secretProperties.Enabled == true)
                {
                    var secret = await GetSecretAsync(secretProperties.Name);
                    if (secret != null)
                    {
                        secrets[secretProperties.Name] = secret;
                    }
                }
            }

            _logger.LogInformation("Obtenidos {Count} secretos de Key Vault", secrets.Count);
            return secrets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los secretos de Key Vault");
            throw;
        }
    }
} 