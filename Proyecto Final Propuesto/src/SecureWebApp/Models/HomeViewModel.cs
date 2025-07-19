namespace SecureWebApp.Models;

public class HomeViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public bool IsAuthenticated { get; set; }
    public List<ClaimInfo> Claims { get; set; } = new();
}

public class ClaimInfo
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class ApiTestViewModel
{
    public List<ApiTestResult> Results { get; set; } = new();
    public bool HasAccessToken { get; set; }
    public string ApiBaseUrl { get; set; } = string.Empty;
}

public class ApiTestResult
{
    public string Endpoint { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public bool Success { get; set; }
    public string Response { get; set; } = string.Empty;
    public bool RequiresAuth { get; set; }
} 