using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureBank.Application.Common.Interfaces;

namespace SecureBank.Security.Services;

/// <summary>
/// Servicio de encriptación para SecureBank Digital
/// Implementa AES-256 para datos sensibles y BCrypt para passwords según el prompt inicial
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly EncryptionOptions _options;
    private readonly ILogger<EncryptionService> _logger;
    private readonly byte[] _masterKey;

    public EncryptionService(IOptions<EncryptionOptions> options, ILogger<EncryptionService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _masterKey = Convert.FromBase64String(_options.MasterKey);
        
        ValidateConfiguration();
    }

    /// <summary>
    /// Encripta texto plano usando AES-256-GCM
    /// </summary>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = Encrypt(plainTextBytes);
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encriptando texto plano");
            throw new SecurityException("Error durante la encriptación", ex);
        }
    }

    /// <summary>
    /// Desencripta texto encriptado
    /// </summary>
    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return string.Empty;

        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var decryptedBytes = Decrypt(encryptedBytes);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error desencriptando texto");
            throw new SecurityException("Error durante la desencriptación", ex);
        }
    }

    /// <summary>
    /// Encripta un arreglo de bytes usando AES-256-GCM
    /// </summary>
    public byte[] Encrypt(byte[] plainData)
    {
        if (plainData == null || plainData.Length == 0)
            return Array.Empty<byte>();

        try
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.GCM;
            aes.Key = _masterKey;

            // Generar nonce aleatorio de 12 bytes (recomendado para GCM)
            var nonce = new byte[12];
            RandomNumberGenerator.Fill(nonce);

            // Preparar buffer para resultado: nonce (12) + tag (16) + encrypted data
            var result = new byte[12 + 16 + plainData.Length];
            
            // Copiar nonce al inicio
            Array.Copy(nonce, 0, result, 0, 12);

            // Configurar transformador
            using var encryptor = aes.CreateEncryptor();
            
            // Para GCM, necesitamos usar una implementación específica
            var encrypted = EncryptGcm(plainData, _masterKey, nonce);
            Array.Copy(encrypted, 0, result, 12, encrypted.Length);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encriptando datos binarios");
            throw new SecurityException("Error durante la encriptación de datos", ex);
        }
    }

    /// <summary>
    /// Desencripta un arreglo de bytes
    /// </summary>
    public byte[] Decrypt(byte[] encryptedData)
    {
        if (encryptedData == null || encryptedData.Length < 28) // 12 (nonce) + 16 (tag) mínimo
            return Array.Empty<byte>();

        try
        {
            // Extraer nonce (primeros 12 bytes)
            var nonce = new byte[12];
            Array.Copy(encryptedData, 0, nonce, 0, 12);

            // Extraer datos encriptados (resto)
            var encrypted = new byte[encryptedData.Length - 12];
            Array.Copy(encryptedData, 12, encrypted, 0, encrypted.Length);

            return DecryptGcm(encrypted, _masterKey, nonce);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error desencriptando datos binarios");
            throw new SecurityException("Error durante la desencriptación de datos", ex);
        }
    }

    /// <summary>
    /// Genera un hash irreversible usando BCrypt
    /// </summary>
    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("La contraseña no puede estar vacía", nameof(password));

        try
        {
            // Usar BCrypt con factor de trabajo 12 (muy seguro)
            return BCrypt.Net.BCrypt.HashPassword(password, 12);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando hash de contraseña");
            throw new SecurityException("Error durante el hash de contraseña", ex);
        }
    }

    /// <summary>
    /// Verifica un password contra su hash
    /// </summary>
    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error verificando contraseña");
            return false;
        }
    }

    /// <summary>
    /// Genera un hash SHA-256 para integridad de datos
    /// </summary>
    public string GenerateHash(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        try
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando hash SHA-256");
            throw new SecurityException("Error durante la generación de hash", ex);
        }
    }

    /// <summary>
    /// Verifica la integridad de datos usando SHA-256
    /// </summary>
    public bool VerifyHash(string input, string hash)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(hash))
            return false;

        try
        {
            var computedHash = GenerateHash(input);
            return computedHash == hash;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error verificando hash");
            return false;
        }
    }

    /// <summary>
    /// Genera un token aleatorio criptográficamente seguro
    /// </summary>
    public string GenerateSecureToken(int length = 32)
    {
        if (length <= 0)
            throw new ArgumentException("La longitud debe ser mayor a cero", nameof(length));

        try
        {
            var bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando token seguro");
            throw new SecurityException("Error durante la generación de token", ex);
        }
    }

    /// <summary>
    /// Genera un código numérico aleatorio
    /// </summary>
    public string GenerateNumericCode(int length = 6)
    {
        if (length <= 0 || length > 10)
            throw new ArgumentException("La longitud debe estar entre 1 y 10", nameof(length));

        try
        {
            var random = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                random.Append(RandomNumberGenerator.GetInt32(0, 10));
            }
            return random.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando código numérico");
            throw new SecurityException("Error durante la generación de código", ex);
        }
    }

    /// <summary>
    /// Encripta datos personales para almacenamiento (PII protection)
    /// </summary>
    public string EncryptPersonalData(string data)
    {
        if (string.IsNullOrEmpty(data))
            return string.Empty;

        // Agregar timestamp y version para rotación de claves
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var versionedData = $"{_options.KeyVersion}:{timestamp}:{data}";
        
        return Encrypt(versionedData);
    }

    /// <summary>
    /// Desencripta datos personales desde base de datos
    /// </summary>
    public string DecryptPersonalData(string encryptedData)
    {
        if (string.IsNullOrEmpty(encryptedData))
            return string.Empty;

        try
        {
            var decryptedData = Decrypt(encryptedData);
            
            // Extraer datos originales (formato: version:timestamp:data)
            var parts = decryptedData.Split(':', 3);
            if (parts.Length != 3)
            {
                _logger.LogWarning("Formato de datos personales encriptados inválido");
                return string.Empty;
            }

            var version = parts[0];
            var timestamp = long.Parse(parts[1]);
            var originalData = parts[2];

            // Verificar si la clave ha expirado
            var encryptionDate = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            var daysSinceEncryption = (DateTimeOffset.UtcNow - encryptionDate).TotalDays;

            if (_options.EnableKeyRotation && daysSinceEncryption > _options.KeyRotationDays)
            {
                _logger.LogInformation("Datos encriptados con clave antigua (versión: {Version}, días: {Days})", 
                    version, daysSinceEncryption);
                
                // En producción, aquí se re-encriptaría con la nueva clave
                // Por ahora solo loggeamos la advertencia
            }

            return originalData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error desencriptando datos personales");
            throw new SecurityException("Error durante la desencriptación de datos personales", ex);
        }
    }

    // Métodos privados para AES-GCM
    private byte[] EncryptGcm(byte[] plaintext, byte[] key, byte[] nonce)
    {
        using var aes = new AesGcm(key);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16]; // GCM tag size
        
        aes.Encrypt(nonce, plaintext, ciphertext, tag);
        
        // Combinar ciphertext y tag
        var result = new byte[ciphertext.Length + tag.Length];
        Array.Copy(ciphertext, 0, result, 0, ciphertext.Length);
        Array.Copy(tag, 0, result, ciphertext.Length, tag.Length);
        
        return result;
    }

    private byte[] DecryptGcm(byte[] encryptedData, byte[] key, byte[] nonce)
    {
        if (encryptedData.Length < 16)
            throw new ArgumentException("Datos encriptados inválidos");

        using var aes = new AesGcm(key);
        
        // Separar ciphertext y tag
        var ciphertext = new byte[encryptedData.Length - 16];
        var tag = new byte[16];
        
        Array.Copy(encryptedData, 0, ciphertext, 0, ciphertext.Length);
        Array.Copy(encryptedData, ciphertext.Length, tag, 0, 16);
        
        var plaintext = new byte[ciphertext.Length];
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        
        return plaintext;
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(_options.MasterKey))
            throw new InvalidOperationException("Master key no configurada");

        if (_masterKey.Length != 32) // 256 bits
            throw new InvalidOperationException("Master key debe ser de 256 bits (32 bytes)");

        if (_options.KeyRotationDays <= 0)
            throw new InvalidOperationException("Días de rotación de clave inválidos");

        _logger.LogInformation("Servicio de encriptación inicializado correctamente");
    }
}

/// <summary>
/// Excepción personalizada para errores de seguridad
/// </summary>
public class SecurityException : Exception
{
    public SecurityException(string message) : base(message) { }
    public SecurityException(string message, Exception innerException) : base(message, innerException) { }
} 