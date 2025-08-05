using Microsoft.Extensions.Logging;

namespace SecureShop.Security;

public interface IUserService
{
    Task<List<string>> GetUserRolesAsync(string userId);
    Task<string?> GetUserDepartmentAsync(string userId);
    Task<List<string>> GetUserPermissionsAsync(string userId);
    Task<string?> GetUserStoreIdAsync(string userId);
    Task SyncUserFromAzureAdAsync(string objectId, string email, string? displayName = null);
}

public class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;
    
    // Simulación de base de datos de usuarios para el lab
    private static readonly Dictionary<string, UserProfile> _users = new()
    {
        ["user1@contoso.com"] = new UserProfile
        {
            Roles = ["Customer"],
            Department = "Sales",
            Permissions = ["products.view", "orders.create"],
            StoreId = "store-001"
        },
        ["manager1@contoso.com"] = new UserProfile
        {
            Roles = ["Manager", "Customer"],
            Department = "Sales",
            Permissions = ["products.view", "products.create", "orders.view", "orders.manage", "reports.view"],
            StoreId = "store-001"
        },
        ["admin1@contoso.com"] = new UserProfile
        {
            Roles = ["Admin", "Manager", "Customer"],
            Department = "IT",
            Permissions = ["products.manage", "users.manage", "reports.manage", "system.admin"],
            StoreId = null
        }
    };

    public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
    }

    public async Task<List<string>> GetUserRolesAsync(string userId)
    {
        await Task.Delay(1); // Simular operación async
        var email = ExtractEmailFromUserId(userId);
        
        if (_users.TryGetValue(email, out var user))
        {
            return user.Roles;
        }

        // Usuario por defecto como Customer
        return ["Customer"];
    }

    public async Task<string?> GetUserDepartmentAsync(string userId)
    {
        await Task.Delay(1);
        var email = ExtractEmailFromUserId(userId);
        
        if (_users.TryGetValue(email, out var user))
        {
            return user.Department;
        }

        return "General";
    }

    public async Task<List<string>> GetUserPermissionsAsync(string userId)
    {
        await Task.Delay(1);
        var email = ExtractEmailFromUserId(userId);
        
        if (_users.TryGetValue(email, out var user))
        {
            return user.Permissions;
        }

        // Permisos mínimos por defecto
        return ["products.view"];
    }

    public async Task<string?> GetUserStoreIdAsync(string userId)
    {
        await Task.Delay(1);
        var email = ExtractEmailFromUserId(userId);
        
        if (_users.TryGetValue(email, out var user))
        {
            return user.StoreId;
        }

        return null;
    }

    public async Task SyncUserFromAzureAdAsync(string objectId, string email, string? displayName = null)
    {
        // En implementación real, sincronizaríamos con base de datos
        _logger.LogInformation("Sincronizando usuario {Email} (ObjectId: {ObjectId})", email, objectId);
        await Task.CompletedTask;
    }

    private string ExtractEmailFromUserId(string userId)
    {
        // En Azure AD, el userId puede ser el ObjectId o el UPN
        // Para el lab, asumimos que podemos mapear a emails conocidos
        return userId.Contains("@") ? userId : "user1@contoso.com";
    }
}

public class UserProfile
{
    public List<string> Roles { get; set; } = new();
    public string Department { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
    public string? StoreId { get; set; }
}