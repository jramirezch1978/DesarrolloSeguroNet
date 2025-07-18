using Azure.Security.KeyVault.Secrets;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Identity;
using System.Text;

namespace DevSeguroWebApp.Services
{
    public class KeyVaultService : IKeyVaultService
    {
        private readonly SecretClient _secretClient;
        private readonly KeyClient _keyClient;
        private readonly ILogger<KeyVaultService> _logger;

        public KeyVaultService(
            SecretClient secretClient,
            KeyClient keyClient,
            ILogger<KeyVaultService> logger)
        {
            _secretClient = secretClient;
            _keyClient = keyClient;
            _logger = logger;
        }

        public async Task<string> GetSecretAsync(string secretName)
        {
            try
            {
                var secret = await _secretClient.GetSecretAsync(secretName);
                _logger.LogInformation("Successfully retrieved secret: {SecretName}", secretName);
                return secret.Value.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secret: {SecretName}", secretName);
                throw;
            }
        }

        public async Task SetSecretAsync(string secretName, string secretValue)
        {
            try
            {
                await _secretClient.SetSecretAsync(secretName, secretValue);
                _logger.LogInformation("Successfully set secret: {SecretName}", secretName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting secret: {SecretName}", secretName);
                throw;
            }
        }

        public async Task<string> EncryptDataAsync(string keyName, string plaintext)
        {
            try
            {
                var key = await _keyClient.GetKeyAsync(keyName);
                var cryptoClient = new CryptographyClient(key.Value.Id, new DefaultAzureCredential());
                
                var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
                var encryptResult = await cryptoClient.EncryptAsync(EncryptionAlgorithm.RsaOaep, plaintextBytes);
                
                var encryptedBase64 = Convert.ToBase64String(encryptResult.Ciphertext);
                _logger.LogInformation("Successfully encrypted data using key: {KeyName}", keyName);
                
                return encryptedBase64;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting data with key: {KeyName}", keyName);
                throw;
            }
        }

        public async Task<string> DecryptDataAsync(string keyName, string ciphertext)
        {
            try
            {
                var key = await _keyClient.GetKeyAsync(keyName);
                var cryptoClient = new CryptographyClient(key.Value.Id, new DefaultAzureCredential());
                
                var ciphertextBytes = Convert.FromBase64String(ciphertext);
                var decryptResult = await cryptoClient.DecryptAsync(EncryptionAlgorithm.RsaOaep, ciphertextBytes);
                
                var plaintext = Encoding.UTF8.GetString(decryptResult.Plaintext);
                _logger.LogInformation("Successfully decrypted data using key: {KeyName}", keyName);
                
                return plaintext;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting data with key: {KeyName}", keyName);
                throw;
            }
        }

        public async Task<Dictionary<string, string>> GetAllSecretsAsync()
        {
            try
            {
                var secrets = new Dictionary<string, string>();
                
                await foreach (var secretProperty in _secretClient.GetPropertiesOfSecretsAsync())
                {
                    if (secretProperty.Enabled == true)
                    {
                        try
                        {
                            var secret = await _secretClient.GetSecretAsync(secretProperty.Name);
                            // Mask sensitive values for display
                            var maskedValue = MaskSensitiveValue(secret.Value.Value);
                            secrets[secretProperty.Name] = maskedValue;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not retrieve secret: {SecretName}", secretProperty.Name);
                            secrets[secretProperty.Name] = "[Error retrieving secret]";
                        }
                    }
                }
                
                _logger.LogInformation("Retrieved {Count} secrets from Key Vault", secrets.Count);
                return secrets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all secrets from Key Vault");
                throw;
            }
        }

        public async Task<bool> SecretExistsAsync(string secretName)
        {
            try
            {
                await _secretClient.GetSecretAsync(secretName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsConnectedAsync()
        {
            try
            {
                // Intentar listar secretos como test de conectividad
                await foreach (var secret in _secretClient.GetPropertiesOfSecretsAsync())
                {
                    // Solo necesitamos verificar que podemos conectar
                    break;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Key Vault connectivity test failed");
                return false;
            }
        }

        public async Task<string> GetKeyVaultInfoAsync()
        {
            try
            {
                var secretCount = 0;
                await foreach (var secret in _secretClient.GetPropertiesOfSecretsAsync())
                {
                    secretCount++;
                }

                var vaultUri = _secretClient.VaultUri.ToString();
                return $"Key Vault: {vaultUri}, Secrets: {secretCount}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Key Vault info");
                return "Error getting Key Vault info";
            }
        }

        private static string MaskSensitiveValue(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= 8)
                return "***";
            
            return value.Substring(0, 4) + new string('*', Math.Min(value.Length - 8, 20)) + value.Substring(value.Length - 4);
        }
    }
} 