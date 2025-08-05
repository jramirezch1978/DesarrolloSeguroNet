using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace SecureShop.Security;

// Requirement para verificar propiedad de recursos
public class OwnershipRequirement : IAuthorizationRequirement
{
    public OwnershipRequirement() { }
}

// Requirement para verificar permisos específicos
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

// Requirement para verificar acceso por departamento
public class DepartmentRequirement : IAuthorizationRequirement
{
    public string Department { get; }
    
    public DepartmentRequirement(string department)
    {
        Department = department;
    }
}

// Authorization Handler para propiedad de recursos
public class OwnershipAuthorizationHandler : AuthorizationHandler<OwnershipRequirement, IOwnedResource>
{
    private readonly ILogger<OwnershipAuthorizationHandler> _logger;

    public OwnershipAuthorizationHandler(ILogger<OwnershipAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OwnershipRequirement requirement,
        IOwnedResource resource)
    {
        var currentUserId = context.User.FindFirst("oid")?.Value ?? 
                           context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(currentUserId))
        {
            _logger.LogWarning("Usuario sin ID válido intentando acceder a recurso");
            return Task.CompletedTask;
        }

        // Verificar si el usuario es propietario del recurso
        if (resource.OwnerId == currentUserId)
        {
            context.Succeed(requirement);
            _logger.LogInformation("Acceso autorizado por propiedad para usuario {UserId}", currentUserId);
        }
        // Verificar si el usuario es administrador
        else if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            _logger.LogInformation("Acceso autorizado por rol Admin para usuario {UserId}", currentUserId);
        }
        // Verificar si el usuario es manager del mismo departamento
        else if (context.User.IsInRole("Manager"))
        {
            var userDepartment = context.User.FindFirst("department")?.Value;
            if (!string.IsNullOrEmpty(userDepartment) && userDepartment == resource.Department)
            {
                context.Succeed(requirement);
                _logger.LogInformation("Acceso autorizado por departamento {Department} para manager {UserId}", 
                    userDepartment, currentUserId);
            }
        }

        return Task.CompletedTask;
    }
}

// Authorization Handler para permisos específicos
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var hasPermission = context.User.HasClaim("permission", requirement.Permission);
        
        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

// Interfaz para recursos que tienen propietario
public interface IOwnedResource
{
    string OwnerId { get; }
    string? Department { get; }
}

// Ejemplo de recurso que implementa IOwnedResource
public class UserProfileResource : IOwnedResource
{
    public string OwnerId { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}