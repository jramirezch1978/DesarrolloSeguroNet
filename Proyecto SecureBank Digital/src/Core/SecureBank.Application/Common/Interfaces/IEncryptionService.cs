namespace SecureBank.Application.Common.Interfaces;

/// <summary>
/// Servicio de encriptación para SecureBank Digital
/// Implementa encriptación AES-256 para datos sensibles según el prompt inicial
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encripta texto plano usando AES-256
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Desencripta texto encriptado
    /// </summary>
    string Decrypt(string encryptedText);

    /// <summary>
    /// Encripta un arreglo de bytes
    /// </summary>
    byte[] Encrypt(byte[] plainData);

    /// <summary>
    /// Desencripta un arreglo de bytes
    /// </summary>
    byte[] Decrypt(byte[] encryptedData);

    /// <summary>
    /// Genera un hash irreversible usando BCrypt (para PINs y respuestas de seguridad)
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifica un password contra su hash
    /// </summary>
    bool VerifyPassword(string password, string hash);

    /// <summary>
    /// Genera un hash SHA-256 para integridad de datos
    /// </summary>
    string GenerateHash(string input);

    /// <summary>
    /// Verifica la integridad de datos usando SHA-256
    /// </summary>
    bool VerifyHash(string input, string hash);

    /// <summary>
    /// Genera un token aleatorio criptográficamente seguro
    /// </summary>
    string GenerateSecureToken(int length = 32);

    /// <summary>
    /// Genera un código numérico aleatorio (para verificación SMS)
    /// </summary>
    string GenerateNumericCode(int length = 6);

    /// <summary>
    /// Encripta datos para almacenamiento en base de datos (PII protection)
    /// </summary>
    string EncryptPersonalData(string data);

    /// <summary>
    /// Desencripta datos personales desde base de datos
    /// </summary>
    string DecryptPersonalData(string encryptedData);
}

/// <summary>
/// Resultado de operación de encriptación
/// </summary>
public class EncryptionResult
{
    public string EncryptedData { get; init; } = string.Empty;
    public string InitializationVector { get; init; } = string.Empty;
    public DateTime EncryptedAt { get; init; }
    public string KeyVersion { get; init; } = string.Empty;
}

/// <summary>
/// Configuración de encriptación
/// </summary>
public class EncryptionOptions
{
    public string MasterKey { get; set; } = string.Empty;
    public int KeyRotationDays { get; set; } = 90;
    public bool EnableKeyRotation { get; set; } = true;
    public string Algorithm { get; set; } = "AES-256-GCM";
    public int KeySize { get; set; } = 256;
    public int BlockSize { get; set; } = 128;
} 