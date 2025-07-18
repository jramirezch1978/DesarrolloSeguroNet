using Azure.Security.KeyVault.Secrets;
using Azure.Security.KeyVault.Keys;

namespace DevSeguroWebApp.Services
{
    public interface IKeyVaultService
    {
        Task<string> GetSecretAsync(string secretName);
        Task SetSecretAsync(string secretName, string secretValue);
        Task<string> EncryptDataAsync(string keyName, string plaintext);
        Task<string> DecryptDataAsync(string keyName, string ciphertext);
        Task<Dictionary<string, string>> GetAllSecretsAsync();
        Task<bool> SecretExistsAsync(string secretName);
        Task<bool> IsConnectedAsync();
        Task<string> GetKeyVaultInfoAsync();
    }
} 