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
            _dataProtectionProvider = dataProtectionProvider;
            _logger = logger;
        }

        public string ProtectSensitiveData(object data, string purpose)
        {
            try
            {
                var protector = _dataProtectionProvider.CreateProtector(purpose);
                var jsonData = JsonSerializer.Serialize(data);
                var protectedData = protector.Protect(jsonData);
                
                _logger.LogInformation("Data protected with purpose: {Purpose}", purpose);
                return protectedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error protecting data with purpose: {Purpose}", purpose);
                throw;
            }
        }

        public T UnprotectSensitiveData<T>(string protectedData, string purpose)
        {
            try
            {
                var protector = _dataProtectionProvider.CreateProtector(purpose);
                var jsonData = protector.Unprotect(protectedData);
                var result = JsonSerializer.Deserialize<T>(jsonData);
                
                _logger.LogInformation("Data unprotected with purpose: {Purpose}", purpose);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unprotecting data with purpose: {Purpose}", purpose);
                throw;
            }
        }

        // Métodos específicos para diferentes tipos de datos
        public string ProtectPersonalInfo(string data)
        {
            var protector = _dataProtectionProvider.CreateProtector("UserData.Personal.v1");
            return protector.Protect(data);
        }

        public string UnprotectPersonalInfo(string protectedData)
        {
            var protector = _dataProtectionProvider.CreateProtector("UserData.Personal.v1");
            return protector.Unprotect(protectedData);
        }

        public string ProtectFinancialData(string data)
        {
            var protector = _dataProtectionProvider.CreateProtector("UserData.Financial.v1");
            return protector.Protect(data);
        }

        public string UnprotectFinancialData(string protectedData)
        {
            var protector = _dataProtectionProvider.CreateProtector("UserData.Financial.v1");
            return protector.Unprotect(protectedData);
        }
    }
} 