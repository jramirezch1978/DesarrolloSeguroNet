namespace DevSeguroWebApp.Services
{
    public interface ISecureDataService
    {
        string ProtectSensitiveData(object data, string purpose);
        T UnprotectSensitiveData<T>(string protectedData, string purpose);
        string ProtectPersonalInfo(string data);
        string UnprotectPersonalInfo(string protectedData);
        string ProtectFinancialData(string data);
        string UnprotectFinancialData(string protectedData);
    }
} 