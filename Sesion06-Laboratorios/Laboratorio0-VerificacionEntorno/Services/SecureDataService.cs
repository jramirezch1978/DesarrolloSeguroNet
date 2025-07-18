using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;

namespace DevSeguroWebApp.Services
{
    public class SecureDataService : ISecureDataService
    {
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly ILogger<SecureDataService> _logger;

        public SecureDataService(
            IDataProtectionProvider dataProtectionProvider,
            ILogger<SecureDataService> logger)
        {
            _dataProtectionProvider = dataProtectionProvider ?? throw new ArgumentNullException(nameof(dataProtectionProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string ProtectSensitiveData(object data, string purpose)
        {
            try
            {
                // Validaciones de entrada
                if (data == null)
                {
                    _logger.LogError("Data is null");
                    throw new ArgumentNullException(nameof(data), "Los datos a proteger no pueden ser nulos");
                }

                if (string.IsNullOrWhiteSpace(purpose))
                {
                    _logger.LogError("Purpose is null or empty");
                    throw new ArgumentException("El propósito de protección no puede estar vacío", nameof(purpose));
                }

                var protector = _dataProtectionProvider.CreateProtector(purpose);
                
                string jsonData;
                if (data is string stringData)
                {
                    jsonData = stringData;
                }
                else
                {
                    jsonData = JsonSerializer.Serialize(data);
                }

                if (string.IsNullOrEmpty(jsonData))
                {
                    _logger.LogError("Serialized data is null or empty");
                    throw new InvalidOperationException("Los datos serializados están vacíos");
                }

                var protectedData = protector.Protect(jsonData);
                
                if (string.IsNullOrEmpty(protectedData))
                {
                    _logger.LogError("Protected data is null or empty");
                    throw new InvalidOperationException("Error en la protección de datos: resultado vacío");
                }
                
                _logger.LogInformation("Data protected successfully with purpose: {Purpose}, original length: {OriginalLength}, protected length: {ProtectedLength}", 
                    purpose, jsonData.Length, protectedData.Length);
                
                return protectedData;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw operation exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error protecting data with purpose: {Purpose}", purpose);
                throw new InvalidOperationException($"Error inesperado al proteger datos: {ex.Message}", ex);
            }
        }

        public T UnprotectSensitiveData<T>(string protectedData, string purpose)
        {
            try
            {
                // Validaciones de entrada
                if (string.IsNullOrWhiteSpace(protectedData))
                {
                    _logger.LogError("Protected data is null or empty");
                    throw new ArgumentException("Los datos protegidos no pueden estar vacíos", nameof(protectedData));
                }

                if (string.IsNullOrWhiteSpace(purpose))
                {
                    _logger.LogError("Purpose is null or empty");
                    throw new ArgumentException("El propósito de protección no puede estar vacío", nameof(purpose));
                }

                var protector = _dataProtectionProvider.CreateProtector(purpose);
                var jsonData = protector.Unprotect(protectedData);
                
                if (string.IsNullOrEmpty(jsonData))
                {
                    _logger.LogError("Unprotected data is null or empty");
                    throw new InvalidOperationException("Error al desproteger datos: resultado vacío");
                }

                // Si T es string, devolver directamente
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)jsonData;
                }

                // Para otros tipos, deserializar
                var result = JsonSerializer.Deserialize<T>(jsonData);
                
                if (result == null)
                {
                    _logger.LogError("Deserialized result is null");
                    throw new InvalidOperationException("Error al deserializar los datos desprotegidos");
                }
                
                _logger.LogInformation("Data unprotected successfully with purpose: {Purpose}, data length: {DataLength}", 
                    purpose, jsonData.Length);
                
                return result;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as-is
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw operation exceptions as-is
            }
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                _logger.LogError(ex, "Cryptographic error unprotecting data with purpose: {Purpose}", purpose);
                throw new InvalidOperationException($"Error criptográfico: Los datos pueden estar corruptos o usar un propósito diferente. {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error with purpose: {Purpose}", purpose);
                throw new InvalidOperationException($"Error al deserializar datos JSON: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error unprotecting data with purpose: {Purpose}", purpose);
                throw new InvalidOperationException($"Error inesperado al desproteger datos: {ex.Message}", ex);
            }
        }

        // Métodos específicos para diferentes tipos de datos
        public string ProtectPersonalInfo(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                throw new ArgumentException("Los datos personales no pueden estar vacíos", nameof(data));
            }

            try
            {
                var protector = _dataProtectionProvider.CreateProtector("UserData.Personal.v1");
                return protector.Protect(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error protecting personal info");
                throw new InvalidOperationException($"Error al proteger información personal: {ex.Message}", ex);
            }
        }

        public string UnprotectPersonalInfo(string protectedData)
        {
            if (string.IsNullOrWhiteSpace(protectedData))
            {
                throw new ArgumentException("Los datos protegidos no pueden estar vacíos", nameof(protectedData));
            }

            try
            {
                var protector = _dataProtectionProvider.CreateProtector("UserData.Personal.v1");
                return protector.Unprotect(protectedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unprotecting personal info");
                throw new InvalidOperationException($"Error al desproteger información personal: {ex.Message}", ex);
            }
        }

        public string ProtectFinancialData(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                throw new ArgumentException("Los datos financieros no pueden estar vacíos", nameof(data));
            }

            try
            {
                var protector = _dataProtectionProvider.CreateProtector("UserData.Financial.v1");
                return protector.Protect(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error protecting financial data");
                throw new InvalidOperationException($"Error al proteger datos financieros: {ex.Message}", ex);
            }
        }

        public string UnprotectFinancialData(string protectedData)
        {
            if (string.IsNullOrWhiteSpace(protectedData))
            {
                throw new ArgumentException("Los datos protegidos no pueden estar vacíos", nameof(protectedData));
            }

            try
            {
                var protector = _dataProtectionProvider.CreateProtector("UserData.Financial.v1");
                return protector.Unprotect(protectedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unprotecting financial data");
                throw new InvalidOperationException($"Error al desproteger datos financieros: {ex.Message}", ex);
            }
        }
    }
} 