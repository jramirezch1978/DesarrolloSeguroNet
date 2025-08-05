namespace SecureShop.Security;

public interface IEncryptionService
{
    Task<string> EncryptAsync(string plainText);
    Task<string> DecryptAsync(string encryptedText);
    Task<string> GetKeyVersionAsync();
}

public class KeyVaultEncryptionService : IEncryptionService
{
    private readonly ILogger<KeyVaultEncryptionService> _logger;
    private readonly string _encryptionKeyName = "SecureShop-Data-Encryption-Key";

    public KeyVaultEncryptionService(ILogger<KeyVaultEncryptionService> logger)
    {
        _logger = logger;
    }

    public async Task<string> EncryptAsync(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            // Simulación de encriptación para el laboratorio
            // En producción, usar Azure Key Vault
            var encrypted = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"ENC:{plainText}"));
            _logger.LogDebug("Texto encriptado exitosamente");
            return encrypted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encriptando datos");
            throw;
        }
    }

    public async Task<string> DecryptAsync(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return encryptedText;

        try
        {
            // Simulación de desencriptación para el laboratorio
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encryptedText));
            if (decoded.StartsWith("ENC:"))
            {
                return decoded.Substring(4);
            }
            return decoded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error desencriptando datos");
            return "[ENCRYPTED_DATA_ERROR]";
        }
    }

    public async Task<string> GetKeyVersionAsync()
    {
        return "v1.0.0";
    }
}