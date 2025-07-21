namespace SecureApi.Services;

public interface IKeyVaultService
{
    Task<string?> GetSecretAsync(string secretName);
    Task<bool> SetSecretAsync(string secretName, string secretValue);
    Task<bool> DeleteSecretAsync(string secretName);
    Task<Dictionary<string, string>> GetAllSecretsAsync();
} 