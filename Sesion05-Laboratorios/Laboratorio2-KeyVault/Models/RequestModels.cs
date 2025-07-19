namespace DevSeguroWebApp.Models
{
    public class TestDataRequest
    {
        public string Data { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
    }

    public class StorageChangeRequest
    {
        public bool UseAzureStorage { get; set; }
    }

    public class CrossDecryptRequest
    {
        public string ProtectedData { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
    }

    public class SecretRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class SecretGetRequest
    {
        public string Name { get; set; } = string.Empty;
    }
} 