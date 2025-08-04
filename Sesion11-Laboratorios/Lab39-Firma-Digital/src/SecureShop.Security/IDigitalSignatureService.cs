namespace SecureShop.Security;

public interface IDigitalSignatureService
{
    Task<SignedDocument> SignDocumentAsync<T>(T document, string documentType) where T : class;
    Task<bool> VerifySignatureAsync(SignedDocument signedDocument);
    Task<DigitalSignatureInfo> GetSignatureInfoAsync(SignedDocument signedDocument);
}

public class SignedDocument
{
    public string DocumentId { get; set; } = Guid.NewGuid().ToString();
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentContent { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string SigningCertificateThumbprint { get; set; } = string.Empty;
    public DateTime SignedAt { get; set; }
    public string SignedBy { get; set; } = string.Empty;
    public string HashAlgorithm { get; set; } = "SHA256";
    public string SignatureAlgorithm { get; set; } = "RSA-PKCS1";
}

public class DigitalSignatureInfo
{
    public bool IsValid { get; set; }
    public string SignerName { get; set; } = string.Empty;
    public DateTime SignedAt { get; set; }
    public string CertificateThumbprint { get; set; } = string.Empty;
    public DateTime CertificateExpiry { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
}