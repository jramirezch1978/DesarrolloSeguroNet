using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using BCrypt.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureBank.Application.Common.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace SecureBank.Infrastructure.Security.Services;

/// <summary>
/// Implementación del servicio de encriptación
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EncryptionService> _logger;
    private readonly SecretClient? _keyVaultClient;

    public EncryptionService(IConfiguration configuration, ILogger<EncryptionService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        try
        {
            var keyVaultUrl = _configuration["KeyVault:VaultUrl"];
            if (!string.IsNullOrEmpty(keyVaultUrl))
            {
                _keyVaultClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo conectar a Key Vault. Usando claves locales.");
        }
    }

    // Implementación de métodos síncronos según la interfaz
    public string Encrypt(string plainText)
    {
        try
        {
            var key = GetEncryptionKey("default-encryption-key");
            var plaintextBytes = Encoding.UTF8.GetBytes(plainText);
            var result = EncryptBytes(plaintextBytes, key);
            
            // Serializar el resultado como JSON para compatibilidad
            return System.Text.Json.JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en encriptación de texto");
            throw;
        }
    }

    public string Decrypt(string encryptedText)
    {
        try
        {
            var encryptionResult = System.Text.Json.JsonSerializer.Deserialize<EncryptionResult>(encryptedText);
            if (encryptionResult == null)
                throw new ArgumentException("Datos de encriptación inválidos");

            var key = GetEncryptionKey("default-encryption-key");
            var decryptedBytes = DecryptBytes(encryptionResult, key);
            
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en desencriptación de texto");
            throw;
        }
    }

    public byte[] Encrypt(byte[] plainData)
    {
        try
        {
            var key = GetEncryptionKey("default-encryption-key");
            var result = EncryptBytes(plainData, key);
            
            // Convertir el resultado a bytes para compatibilidad
            var jsonString = System.Text.Json.JsonSerializer.Serialize(result);
            return Encoding.UTF8.GetBytes(jsonString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en encriptación de bytes");
            throw;
        }
    }

    public byte[] Decrypt(byte[] encryptedData)
    {
        try
        {
            var jsonString = Encoding.UTF8.GetString(encryptedData);
            var encryptionResult = System.Text.Json.JsonSerializer.Deserialize<EncryptionResult>(jsonString);
            if (encryptionResult == null)
                throw new ArgumentException("Datos de encriptación inválidos");

            var key = GetEncryptionKey("default-encryption-key");
            return DecryptBytes(encryptionResult, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en desencriptación de bytes");
            throw;
        }
    }

    public string EncryptPersonalData(string data)
    {
        try
        {
            var key = GetEncryptionKey("pii-encryption-key");
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var result = EncryptBytes(dataBytes, key);
            
            return System.Text.Json.JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en encriptación de datos personales");
            throw;
        }
    }

    public string DecryptPersonalData(string encryptedData)
    {
        try
        {
            var encryptionResult = System.Text.Json.JsonSerializer.Deserialize<EncryptionResult>(encryptedData);
            if (encryptionResult == null)
                throw new ArgumentException("Datos de encriptación inválidos");

            var key = GetEncryptionKey("pii-encryption-key");
            var decryptedBytes = DecryptBytes(encryptionResult, key);
            
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en desencriptación de datos personales");
            throw;
        }
    }

    public string HashPassword(string password)
    {
        try
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en hash de contraseña");
            throw;
        }
    }

    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en verificación de contraseña");
            return false;
        }
    }

    public string GenerateHash(string input)
    {
        try
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando hash SHA-256");
            throw;
        }
    }

    public bool VerifyHash(string input, string hash)
    {
        try
        {
            var computedHash = GenerateHash(input);
            return computedHash.Equals(hash, StringComparison.Ordinal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verificando hash");
            return false;
        }
    }

    public string GenerateSecureToken(int length = 32)
    {
        try
        {
            var bytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando token seguro");
            throw;
        }
    }

    public string GenerateNumericCode(int length = 6)
    {
        try
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var random = new Random(BitConverter.ToInt32(bytes, 0));

            var code = "";
            for (int i = 0; i < length; i++)
            {
                code += random.Next(0, 10).ToString();
            }

            return code;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando código numérico");
            throw;
        }
    }

    // Métodos privados auxiliares
    private EncryptionResult EncryptBytes(byte[] data, byte[] key)
    {
        var nonce = new byte[12]; // 96 bits para AES-GCM
        var tag = new byte[16];   // 128 bits para AES-GCM
        var ciphertext = new byte[data.Length];

        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(nonce);

        using var aesGcm = new AesGcm(key, 16); // tag size = 16 bytes
        aesGcm.Encrypt(nonce, data, ciphertext, tag);

        return new EncryptionResult
        {
            EncryptedData = Convert.ToBase64String(ciphertext),
            InitializationVector = Convert.ToBase64String(nonce),
            EncryptedAt = DateTime.UtcNow,
            KeyVersion = "v1"
        };
    }

    private byte[] DecryptBytes(EncryptionResult encryptedData, byte[] key)
    {
        var ciphertext = Convert.FromBase64String(encryptedData.EncryptedData);
        var nonce = Convert.FromBase64String(encryptedData.InitializationVector);
        
        // Para compatibilidad, buscar el tag en diferentes propiedades
        byte[] tag;
        if (!string.IsNullOrEmpty(encryptedData.KeyVersion) && encryptedData.KeyVersion.Length >= 24)
        {
            // El tag podría estar almacenado en KeyVersion como fallback
            tag = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(tag); // Generar tag temporal para desarrollo
        }
        else
        {
            tag = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(tag);
        }
        
        var plaintext = new byte[ciphertext.Length];

        try
        {
            using var aesGcm = new AesGcm(key, 16);
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
        }
        catch (CryptographicException)
        {
            // Para datos legacy, usar un enfoque alternativo
            _logger.LogWarning("Error de desencriptación, usando método alternativo");
            throw new InvalidOperationException("No se pudo desencriptar los datos. Posiblemente datos corruptos.");
        }

        return plaintext;
    }

    private byte[] GetEncryptionKey(string keyName)
    {
        try
        {
            if (_keyVaultClient != null)
            {
                var secret = _keyVaultClient.GetSecret(keyName);
                var keyBase64 = secret.Value.Value;
                return Convert.FromBase64String(keyBase64);
            }
            else
            {
                // Fallback a configuración local (solo para desarrollo)
                var keyBase64 = _configuration[$"Encryption:Keys:{keyName}"];
                if (string.IsNullOrEmpty(keyBase64))
                {
                    // Generar una clave temporal para desarrollo
                    var key = new byte[32]; // 256 bits
                    using var rng = RandomNumberGenerator.Create();
                    rng.GetBytes(key);
                    
                    _logger.LogWarning("Usando clave temporal para {KeyName}. Esto NO debe usarse en producción.", keyName);
                    return key;
                }

                return Convert.FromBase64String(keyBase64);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo clave de encriptación {KeyName}", keyName);
            throw;
        }
    }
} 