# 🔐 Laboratorio 38: Autenticación y Autorización Avanzada

**Duración:** 20 minutos  
**Objetivo:** Implementar claims transformation service y autorización basada en recursos

## 📋 Funcionalidades Implementadas

### 1. Claims Transformation Service
- Enriquecimiento automático de identidades Azure AD
- Agregación de roles específicos de la aplicación
- Claims de departamento y permisos granulares
- Información de tienda/ubicación

### 2. Authorization Handlers Personalizados
- Handler para verificación de propiedad de recursos
- Handler para permisos específicos
- Autorización basada en departamento

### 3. Políticas de Autorización Granulares
- Políticas básicas por rol
- Políticas basadas en permisos específicos
- Políticas combinadas (rol + departamento)
- Políticas con lógica compleja

## 🔧 Implementación

### Paso 1: Claims Transformation Service

El servicio `SecureClaimsTransformation` enriquece automáticamente las identidades:

```csharp
public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
{
    var identity = (ClaimsIdentity)principal.Identity!;
    var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var email = principal.FindFirst(ClaimTypes.Email)?.Value;

    try
    {
        // Agregar roles específicos de la aplicación
        var userRoles = await _userService.GetUserRolesAsync(userId);
        foreach (var role in userRoles)
        {
            if (!principal.IsInRole(role))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }

        // Agregar claims de departamento
        var department = await _userService.GetUserDepartmentAsync(userId);
        if (!string.IsNullOrEmpty(department))
        {
            identity.AddClaim(new Claim("department", department));
        }

        // Agregar permisos granulares
        var permissions = await _userService.GetUserPermissionsAsync(userId);
        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim("permission", permission));
        }
        
        return principal;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error enriqueciendo claims");
        return principal; // Seguro: devolver original en caso de error
    }
}
```

### Paso 2: User Service con Datos Simulados

```csharp
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
        Permissions = ["products.view", "products.create", "orders.view", 
                      "orders.manage", "reports.view"],
        StoreId = "store-001"
    },
    ["admin1@contoso.com"] = new UserProfile
    {
        Roles = ["Admin", "Manager", "Customer"],
        Department = "IT",
        Permissions = ["products.manage", "users.manage", "reports.manage", 
                      "system.admin"],
        StoreId = null
    }
};
```

### Paso 3: Authorization Requirements y Handlers

```csharp
// Requirement para verificar propiedad de recursos
public class OwnershipRequirement : IAuthorizationRequirement
{
    public OwnershipRequirement() { }
}

// Handler para propiedad de recursos
public class OwnershipAuthorizationHandler : AuthorizationHandler<OwnershipRequirement, IOwnedResource>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OwnershipRequirement requirement,
        IOwnedResource resource)
    {
        var currentUserId = context.User.FindFirst("oid")?.Value ?? 
                           context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Verificar si el usuario es propietario del recurso
        if (resource.OwnerId == currentUserId)
        {
            context.Succeed(requirement);
        }
        // Verificar si el usuario es administrador
        else if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
        }
        // Verificar si el usuario es manager del mismo departamento
        else if (context.User.IsInRole("Manager"))
        {
            var userDepartment = context.User.FindFirst("department")?.Value;
            if (!string.IsNullOrEmpty(userDepartment) && userDepartment == resource.Department)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}
```

### Paso 4: Configuración en Program.cs

```csharp
// Registrar servicios de autorización personalizados
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IClaimsTransformation, SecureClaimsTransformation>();
builder.Services.AddScoped<IAuthorizationHandler, OwnershipAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// Configurar políticas de autorización granulares
builder.Services.AddAuthorization(options =>
{
    // Políticas básicas por rol
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
        
    options.AddPolicy("ManagerOrAdmin", policy =>
        policy.RequireRole("Manager", "Admin"));

    // Políticas basadas en permisos específicos
    options.AddPolicy("CanManageProducts", policy =>
        policy.Requirements.Add(new PermissionRequirement("products.manage")));

    // Políticas combinadas (rol + departamento)
    options.AddPolicy("CanManageInventory", policy =>
        policy.RequireAssertion(context =>
            (context.User.IsInRole("Manager") || context.User.IsInRole("Admin")) &&
            (context.User.HasClaim("department", "Inventory") || context.User.IsInRole("Admin"))));

    // Políticas basadas en recursos
    options.AddPolicy("CanEditOwnProfile", policy =>
        policy.Requirements.Add(new OwnershipRequirement()));

    // Políticas con lógica compleja
    options.AddPolicy("CanApproveDiscounts", policy =>
        policy.RequireAssertion(context =>
        {
            // Solo managers y admins pueden aprobar descuentos
            if (!context.User.IsInRole("Manager") && !context.User.IsInRole("Admin"))
                return false;
                
            // Managers solo pueden aprobar en su tienda
            if (context.User.IsInRole("Manager") && !context.User.IsInRole("Admin"))
            {
                var userStore = context.User.FindFirst("store_id")?.Value;
                return !string.IsNullOrEmpty(userStore);
            }
            
            // Admins pueden aprobar en cualquier tienda
            return true;
        }));
});
```

### Paso 5: Controladores con Autorización

```csharp
public class HomeController : Controller
{
    [Authorize]
    public IActionResult Dashboard()
    {
        var model = new DashboardViewModel
        {
            UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "N/A",
            Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "N/A",
            Roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList(),
            Department = User.FindFirst("department")?.Value ?? "N/A",
            Permissions = User.Claims.Where(c => c.Type == "permission").Select(c => c.Value).ToList(),
            StoreId = User.FindFirst("store_id")?.Value ?? "N/A"
        };

        return View(model);
    }

    [Authorize(Policy = "AdminOnly")]
    public IActionResult AdminPanel()
    {
        return View();
    }

    [Authorize(Policy = "CanManageProducts")]
    public IActionResult ManageProducts()
    {
        return View();
    }

    [Authorize(Policy = "CanEditOwnProfile")]
    public async Task<IActionResult> EditProfile()
    {
        var currentUserId = User.FindFirst("oid")?.Value ?? 
                           User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        
        var profileResource = new UserProfileResource
        {
            OwnerId = currentUserId,
            Department = User.FindFirst("department")?.Value,
            Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "",
            DisplayName = User.Identity?.Name ?? ""
        };

        return View(profileResource);
    }
}
```

## 🚀 Testing y Validación

### Compilar y Verificar

```powershell
# Navegar al proyecto
cd Lab38-Autenticacion-Avanzada

# Compilar la solución
dotnet build

# Ejecutar la aplicación
cd src/SecureShop.Web
dotnet run --urls="https://localhost:7001"
```

### Probar Funcionalidades

1. **Dashboard de Usuario**: Navegar a `/Home/Dashboard` para ver claims enriquecidos
2. **Panel de Admin**: Acceder a `/Home/AdminPanel` (solo usuarios con rol Admin)
3. **Gestión de Productos**: Probar `/Home/ManageProducts` (requiere permiso `products.manage`)
4. **Editar Perfil**: Verificar `/Home/EditProfile` (autorización basada en recursos)

## 📊 Políticas de Autorización Implementadas

| Política | Requirement | Descripción |
|----------|-------------|-------------|
| **AdminOnly** | Rol Admin | Solo administradores |
| **ManagerOrAdmin** | Rol Manager o Admin | Gerentes y administradores |
| **CustomerAccess** | Rol Customer, Manager o Admin | Todos los usuarios autenticados |
| **CanManageProducts** | Permiso `products.manage` | Gestión de productos |
| **CanViewReports** | Permiso `reports.view` | Visualización de reportes |
| **CanManageUsers** | Permiso `users.manage` | Gestión de usuarios |
| **CanManageInventory** | Manager/Admin + Dept. Inventory | Gestión de inventario por departamento |
| **CanEditOwnProfile** | Propiedad del recurso | Edición de perfil propio |
| **CanApproveDiscounts** | Manager/Admin + Store ID | Aprobación de descuentos por tienda |

## 🔒 Características de Seguridad

### Claims Automáticos Agregados
- **Roles de aplicación**: Customer, Manager, Admin
- **Departamento**: Sales, IT, Inventory, etc.
- **Permisos**: products.view, orders.create, reports.manage, etc.
- **Store ID**: Identificador de tienda para managers

### Authorization Patterns
- **Role-based**: Autorización por roles tradicionales
- **Permission-based**: Permisos granulares específicos
- **Resource-based**: Verificación de propiedad de recursos
- **Hybrid**: Combinación de roles, departamentos y contexto

### Error Handling
- Logs detallados de decisiones de autorización
- Fallback seguro en caso de errores
- Información de debugging para desarrollo

## 📚 Recursos Adicionales

### Documentación Técnica
- [ASP.NET Core Authorization](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/)
- [Claims-based Authorization](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/claims)
- [Resource-based Authorization](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/resourcebased)

### Buenas Prácticas
- Usar authorization handlers para lógica compleja
- Implementar claims transformation para enriquecimiento automático
- Combinar múltiples métodos de autorización según necesidades
- Mantener logs detallados para auditoría y debugging

## ✅ Criterios de Completitud

- [ ] Claims transformation funcionando correctamente
- [ ] Usuarios obtienen roles y permisos automáticamente
- [ ] Políticas de autorización configuradas y funcionando
- [ ] Authorization handlers implementados
- [ ] Dashboard muestra claims enriquecidos
- [ ] Diferentes endpoints protegidos según políticas
- [ ] Autorización basada en recursos funcionando
- [ ] Aplicación compila sin errores

---

## 🎯 Próximo Laboratorio

**Lab 39: Implementación de Firma Digital** - Configurar certificados en Key Vault e implementar firma digital de transacciones.