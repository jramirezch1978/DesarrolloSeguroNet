# 🔐 LABORATORIO 36: INTEGRACIÓN CON AZURE AD Y CONFIGURACIÓN DE ROLES

## 🎯 Objetivo
Implementar autenticación OAuth 2.0/OpenID Connect y autorización basada en claims, transformando nuestra aplicación de un sistema aislado a un participante sofisticado en un ecosistema de identidad empresarial.

## ⏱️ Duración: 20 minutos

## 🎭 Conceptos Fundamentales de Autenticación Empresarial

### 🏛️ De Sistema Aislado a Ecosistema de Identidad
> *"La integración de Azure AD en .NET transforma nuestra aplicación de un sistema aislado que maneja su propia autenticación, a un participante sofisticado en un ecosistema de identidad empresarial. Es como la diferencia entre tener guardias propios versus integrarse con un sistema de seguridad coordinado a nivel ciudad."*

### 🔐 OAuth 2.0 + OpenID Connect en Acción
**OAuth 2.0 + OpenID Connect** no es solo una configuración técnica - implementamos los mismos protocolos que usa:
- **Google** cuando haces "Iniciar sesión con Google" 
- **Microsoft** para Office 365
- **Apple** para "Sign in with Apple"

Es un estándar probado que maneja **miles de millones de autenticaciones diariamente**.

### 🎯 Arquitectura de Claims-Based Authorization

#### **Separación de Responsabilidades**
- **Azure AD**: "¿Quién eres?" - Maneja identidad y autenticación
- **Nuestro Sistema**: "¿Qué puedes hacer aquí?" - Maneja autorización específica de aplicación

Esta separación permite que Azure AD proporcione identidad, pero no conoce los roles específicos de nuestra aplicación. Un usuario podría ser "Gerente de Inventario" en nuestro sistema, pero Azure AD solo sabe que son empleados de la empresa.

#### **Transformación de Claims**
```csharp
// Azure AD proporciona: nombre, email, identificador único
var userObjectId = principal.GetObjectId();
var userEmail = principal.GetDisplayName();

// Nuestro sistema agrega: roles específicos de aplicación
var roles = await _userRoleService.GetUserRolesAsync(objectId);
foreach (var role in roles)
{
    identity.AddClaim(new Claim(ClaimTypes.Role, role));
}
```

## 🛡️ Principios de Seguridad Implementados

### **Tokens JWT con Expiración Corta**
- **Expiración**: 1 hora máximo
- **Ventaja de Seguridad**: Incluso si alguien roba un token, tiene ventana limitada de uso
- **Caso Real**: En 2020, atacantes robaron credenciales de Twitter. Con nuestro sistema de tokens con expiración corta, el impacto habría sido limitado porque los tokens se habrían vuelto inútiles rápidamente.

### **Autenticación Multifactor (MFA) Obligatoria**
- **Principio**: Requiere tanto contraseña como acceso físico al teléfono
- **Protección**: Resiste ataques de credential stuffing y phishing
- **Implementación**: Azure AD maneja automáticamente la aplicación de MFA

### **Auditoría Completa de Autenticación**
```csharp
OnTokenValidated = async context =>
{
    var userObjectId = context.Principal?.GetObjectId();
    var userEmail = context.Principal?.GetDisplayName();
    
    _logger.LogInformation("Usuario autenticado: {Email} (ID: {ObjectId})", 
        userEmail, userObjectId);
    
    // Sincronización automática con base de datos local
    await _userService.ProcessUserLogin(context.Principal);
}
```

## 🎯 Pasos de Implementación

### Paso 1: Registrar Aplicación en Azure AD (8 minutos)

**Concepto**: Establecer una identidad de aplicación que será auditada, monitoreada, y gestionada a lo largo del ciclo de vida completo.

#### **Crear Registro con Azure CLI**
```powershell
# Variables de configuración empresarial
$appName = "SecureShop-WebApp-$(Get-Date -Format 'yyyyMMdd-HHmm')"
$redirectUri = "https://localhost:7000/signin-oidc"
$logoutUri = "https://localhost:7000/signout-callback-oidc"

# Crear registro de aplicación
$appRegistration = az ad app create `
    --display-name $appName `
    --sign-in-audience "AzureADMyOrg" `
    --web-redirect-uris $redirectUri `
    --web-logout-url $logoutUri `
    --query "{appId:appId,objectId:id}" `
    --output json | ConvertFrom-Json

Write-Host "✅ Aplicación registrada:" -ForegroundColor Green
Write-Host "App ID: $($appRegistration.appId)" -ForegroundColor Yellow
Write-Host "Object ID: $($appRegistration.objectId)" -ForegroundColor Yellow
```

#### **¿Por qué estos parámetros específicos?**

**--sign-in-audience "AzureADMyOrg"**: Restringe acceso solo a usuarios de nuestra organización Azure AD. Otras opciones ampliarían significativamente la superficie de ataque.

**--web-redirect-uris específicas**: Azure AD solo redirigirá a URLs pre-registradas. Esto previene ataques de open redirect donde atacantes podrían engañar a usuarios para autenticarse y ser redirigidos a sitios maliciosos.

#### **Configurar Permisos API**
```powershell
# Configurar permisos mínimos necesarios
az ad app permission add `
    --id $appRegistration.appId `
    --api 00000003-0000-0000-c000-000000000000 `
    --api-permissions e1fe6dd8-ba31-4d61-89e7-88639da4683d=Scope

# Otorgar consentimiento administrativo
az ad app permission grant --id $appRegistration.appId --api 00000003-0000-0000-c000-000000000000
```

**User.Read**: Permiso mínimo necesario siguiendo principio de menor privilegio.

### Paso 2: Configurar Autenticación en .NET (7 minutos)

**Concepto**: Implementar un pipeline de autenticación que resiste ataques conocidos y proporciona auditoría completa.

#### **Configuración de appsettings.json Segura**
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "yourdomain.onmicrosoft.com",
    "TenantId": "[Key Vault: AzureAd--TenantId]",
    "ClientId": "[Key Vault: AzureAd--ClientId]",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

**¿Por qué TenantId y ClientId en Key Vault?** Aunque no son secretos tradicionales, representan información de identificación específica del ambiente. En desarrollo usamos un tenant diferente que en producción.

#### **Program.cs con Autenticación Avanzada**
```csharp
// Configurar Microsoft Identity Web (Azure AD)
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        
        // Configurar eventos de autenticación para auditoría
        options.Events = new OpenIdConnectEvents
        {
            OnTokenValidated = async context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                
                var userObjectId = context.Principal?.GetObjectId();
                var userEmail = context.Principal?.GetDisplayName();
                
                logger.LogInformation("Usuario autenticado: {Email} (ID: {ObjectId})", 
                    userEmail, userObjectId);
                
                // Sincronización con base de datos local
                var userService = context.HttpContext.RequestServices
                    .GetRequiredService<IUserService>();
                await userService.ProcessUserLogin(context.Principal);
            },
            
            OnAuthenticationFailed = async context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                
                logger.LogError("Fallo de autenticación: {Error}", 
                    context.Exception?.Message);
            }
        };
    });
```

### Paso 3: Crear Controladores con Autorización (5 minutos)

**Concepto**: Implementar autorización granular que considera tanto el rol del usuario como la relación con recursos específicos.

#### **HomeController con Políticas de Autorización**
```csharp
[Authorize]
public class HomeController : Controller
{
    [AllowAnonymous]
    public IActionResult Index() => View();

    [Authorize]
    public IActionResult Dashboard()
    {
        var userObjectId = User.GetObjectId();
        var userName = User.GetDisplayName();
        var userRoles = User.Claims
            .Where(c => c.Type == "roles" || c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        ViewBag.UserObjectId = userObjectId;
        ViewBag.UserName = userName;
        ViewBag.UserRoles = userRoles;

        _logger.LogInformation("Usuario {UserName} accedió al dashboard con roles: {Roles}", 
            userName, string.Join(", ", userRoles));

        return View();
    }

    [Authorize(Policy = "AdminOnly")]
    public IActionResult AdminPanel() => View();

    [Authorize(Policy = "ManagerOrAdmin")]
    public IActionResult Reports() => View();
}
```

#### **ProductController con Autorización Contextual**
```csharp
[Authorize]
public class ProductController : Controller
{
    // Todos pueden ver productos
    [Authorize(Policy = "CustomerAccess")]
    public async Task<IActionResult> Index()
    {
        var products = await _context.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return View(products);
    }

    // Solo managers y admins pueden crear productos
    [Authorize(Policy = "ManagerOrAdmin")]
    public IActionResult Create() => View();

    [HttpPost]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductCreateModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var product = new Product
        {
            Name = model.Name,
            Description = model.Description,
            Price = model.Price,
            CreatedBy = Guid.Parse(User.GetObjectId()!),
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Producto {ProductName} creado por usuario {UserId}", 
            product.Name, User.GetObjectId());

        TempData["SuccessMessage"] = "Producto creado exitosamente";
        return RedirectToAction(nameof(Index));
    }

    // Solo admins pueden eliminar productos
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        // Soft delete para preservar auditoría
        product.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogWarning("Producto {ProductId} eliminado por usuario {UserId}", 
            id, User.GetObjectId());

        TempData["SuccessMessage"] = "Producto eliminado exitosamente";
        return RedirectToAction(nameof(Index));
    }
}
```

## 📋 Entregables del Laboratorio

Al completar este laboratorio tendrás:

- [ ] Aplicación registrada en Azure AD con configuración segura
- [ ] Autenticación OAuth 2.0/OpenID Connect implementada
- [ ] Autorización basada en roles configurada
- [ ] Controladores con políticas de seguridad granulares
- [ ] Vistas con elementos de seguridad integrados
- [ ] Auditoría completa de eventos de autenticación
- [ ] Sincronización automática entre Azure AD y base de datos local

## 🔍 Casos de Estudio Preventivos

### **Slack (2019)**
- **Implementación Similar**: Usa OAuth 2.0 con Azure AD para empresas que usan Office 365
- **Patrón Idéntico**: Mantiene registro local de usuarios vinculado a identidad externa
- **Escala**: Protege comunicaciones de 12+ millones de usuarios diarios activos

### **British Airways (2019)**
- **Problema**: Sistema comprometido por validación solo del lado cliente
- **Nuestra Protección**: Validación en múltiples capas habría bloqueado el ataque
- **Resultado**: Nuestro sistema implementa autorización del lado servidor con auditoría

## 💡 Valor Profesional Generado

**Portfolio Evidence**: Sistema de autenticación empresarial completo con Azure AD  
**Skills Advancement**: Competencias en OAuth 2.0, OpenID Connect, y claims-based authorization  
**Security Mindset**: Comprensión profunda de ecosistemas de identidad empresarial  
**Enterprise Integration**: Patrones usados por organizaciones Fortune 500

## 🔗 Preparación para Laboratorio 37

La autenticación implementada será fundamental para:
- **Key Vault**: Managed Identity basada en usuarios autenticados
- **Cifrado**: Servicios que requieren contexto de usuario para operaciones seguras
- **Auditoría**: Trazabilidad completa desde autenticación hasta operaciones de datos

---

> **💡 Mindset Clave**: Nunca confiamos en tokens sin validación, nunca almacenamos credenciales localmente, y siempre mantenemos auditoría completa de actividad de autenticación. Es una arquitectura diseñada para resistir tanto ataques externos como amenazas internas.