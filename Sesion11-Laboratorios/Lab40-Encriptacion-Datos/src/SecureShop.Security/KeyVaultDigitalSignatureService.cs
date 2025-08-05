using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace SecureShop.Security;

public class KeyVaultDigitalSignatureService : IDigitalSignatureService
{
    private readonly CertificateClient _certificateClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeyVaultDigitalSignatureService> _logger;
    private readonly string _certificateName = "SecureShop-Signing-Certificate";

    public KeyVaultDigitalSignatureService(
        IConfiguration configuration,
        ILogger<KeyVaultDigitalSignatureService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var keyVaultUri = _configuration["KeyVault:VaultUri"];
        if (string.IsNullOrEmpty(keyVaultUri))
        {
            throw new ArgumentException("KeyVault:VaultUri no configurado");
        }

        _certificateClient = new CertificateClient(new Uri(keyVaultUri), new DefaultAzureCredential());
    }

    public async Task<SignedDocument> SignDocumentAsync<T>(T document, string documentType) where T : class
    {
        try
        {
            // 1. Serializar el documento
            var documentJson = JsonSerializer.Serialize(document, new JsonSerializerOptions 
            { 
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // 2. Calcular hash del documento
            var documentBytes = Encoding.UTF8.GetBytes(documentJson);
            var documentHash = SHA256.HashData(documentBytes);

            // 3. Obtener certificado de Key Vault
            var certificateResponse = await _certificateClient.GetCertificateAsync(_certificateName);
            var certificate = certificateResponse.Value;

            // 4. Obtener la clave de firma
            var keyClient = new CryptographyClient(certificate.KeyId, new DefaultAzureCredential());

            // 5. Firmar el hash
            var signResult = await keyClient.SignAsync(SignatureAlgorithm.RS256, documentHash);

            // 6. Crear documento firmado
            var signedDocument = new SignedDocument
            {
                DocumentType = documentType,
                DocumentContent = documentJson,
                Signature = Convert.ToBase64String(signResult.Signature),
                SigningCertificateThumbprint = Convert.ToHexString(certificate.Properties.X509Thumbprint),
                SignedAt = DateTime.UtcNow,
                SignedBy = "System", // En implementación real, obtener del contexto de usuario
                HashAlgorithm = "SHA256",
                SignatureAlgorithm = "RS256"
            };

            _logger.LogInformation("Documento {DocumentType} firmado exitosamente. ID: {DocumentId}", 
                documentType, signedDocument.DocumentId);

            return signedDocument;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error firmando documento de tipo {DocumentType}", documentType);
            throw new InvalidOperationException($"Error en proceso de firma: {ex.Message}", ex);
        }
    }

    public async Task<bool> VerifySignatureAsync(SignedDocument signedDocument)
    {
        try
        {
            // 1. Recalcular hash del documento original
            var documentBytes = Encoding.UTF8.GetBytes(signedDocument.DocumentContent);
            var documentHash = SHA256.HashData(documentBytes);

            // 2. Obtener certificado usando thumbprint
            var certificate = await GetCertificateByThumbprintAsync(signedDocument.SigningCertificateThumbprint);
            if (certificate == null)
            {
                _logger.LogWarning("Certificado no encontrado para thumbprint: {Thumbprint}", 
                    signedDocument.SigningCertificateThumbprint);
                return false;
            }

            // 3. Obtener cliente de criptografía
            var keyClient = new CryptographyClient(certificate.KeyId, new DefaultAzureCredential());

            // 4. Verificar la firma
            var signatureBytes = Convert.FromBase64String(signedDocument.Signature);
            var verifyResult = await keyClient.VerifyAsync(SignatureAlgorithm.RS256, documentHash, signatureBytes);

            _logger.LogInformation("Verificación de firma para documento {DocumentId}: {IsValid}", 
                signedDocument.DocumentId, verifyResult.IsValid);

            return verifyResult.IsValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verificando firma del documento {DocumentId}", signedDocument.DocumentId);
            return false;
        }
    }

    public async Task<DigitalSignatureInfo> GetSignatureInfoAsync(SignedDocument signedDocument)
    {
        var info = new DigitalSignatureInfo
        {
            SignedAt = signedDocument.SignedAt,
            CertificateThumbprint = signedDocument.SigningCertificateThumbprint,
            SignerName = signedDocument.SignedBy
        };

        try
        {
            // Verificar la firma
            info.IsValid = await VerifySignatureAsync(signedDocument);

            // Obtener información del certificado
            var certificate = await GetCertificateByThumbprintAsync(signedDocument.SigningCertificateThumbprint);
            if (certificate != null)
            {
                info.CertificateExpiry = certificate.Properties.ExpiresOn?.DateTime ?? DateTime.MinValue;
                
                // Validaciones adicionales
                if (certificate.Properties.ExpiresOn < DateTimeOffset.UtcNow)
                {
                    info.ValidationErrors.Add("Certificado expirado");
                    info.IsValid = false;
                }

                if (!certificate.Properties.Enabled.GetValueOrDefault(false))
                {
                    info.ValidationErrors.Add("Certificado deshabilitado");
                    info.IsValid = false;
                }
            }
            else
            {
                info.ValidationErrors.Add("Certificado no encontrado");
                info.IsValid = false;
            }
        }
        catch (Exception ex)
        {
            info.ValidationErrors.Add($"Error en validación: {ex.Message}");
            info.IsValid = false;
            _logger.LogError(ex, "Error obteniendo información de firma para documento {DocumentId}", 
                signedDocument.DocumentId);
        }

        return info;
    }

    private async Task<KeyVaultCertificateWithPolicy?> GetCertificateByThumbprintAsync(string thumbprint)
    {
        try
        {
            // En implementación simplificada, usamos el certificado principal
            // En producción, mantendríamos un mapeo de thumbprints a nombres de certificados
            var certificateResponse = await _certificateClient.GetCertificateAsync(_certificateName);
            var certificate = certificateResponse.Value;

            var certThumbprint = Convert.ToHexString(certificate.Properties.X509Thumbprint);
            if (string.Equals(certThumbprint, thumbprint, StringComparison.OrdinalIgnoreCase))
            {
                return certificate;
            }

            _logger.LogWarning("Thumbprint solicitado {RequestedThumbprint} no coincide con certificado actual {CurrentThumbprint}",
                thumbprint, certThumbprint);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo certificado por thumbprint {Thumbprint}", thumbprint);
            return null;
        }
    }
}