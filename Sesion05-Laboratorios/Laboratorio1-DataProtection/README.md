# 🧪 Laboratorio 1: Implementación de Data Protection API Avanzada

## 📋 Información del Laboratorio

- **Duración**: 25 minutos
- **Objetivo**: Implementar Data Protection API con Azure Storage y Key Vault para protección enterprise-grade
- **Prerrequisitos**: Laboratorio 0 completado exitosamente

## 🎯 Objetivos Específicos

- Configurar Azure Storage Account para persistencia de claves
- Implementar Data Protection API con configuración enterprise
- Crear servicio de protección de datos con múltiples propósitos
- Configurar rotación automática de claves y logging

## 📦 Paso 1: Configurar Azure Storage Account para Data Protection (8 minutos)

### 🏗️ Crear Storage Account desde Azure Portal

1. **Navegar al Portal**:

   - Abrir Azure Portal: https://portal.azure.com
   - Buscar "Storage accounts" → Click "+ Create"
2. **Configuración Básica**:

   ```
   Resource group: rg-desarrollo-seguro-[SuNombre]
   Storage account name: stdevsgro[sunombre][numero]
   Region: East US
   Performance: Standard
   Redundancy: LRS (para laboratorio)
   ```
3. **Crear el Storage Account**:

   - Click "Review + create"
   - Click "Create"
   - Esperar a que se complete el deployment

### 🔑 Obtener Connection String

1. **Acceder a Access Keys**:
   - Ir a su Storage Account → Security + networking → Access keys
   - Click "Show keys"
   - Copiar "Connection string" de key1

### 📦 Crear Container para Keys

1. **Crear Container**:
   - Su Storage Account → Data storage → Containers
   - Click "+ Container"
   - Name: `dataprotection-keys`
   - Public access level: Private
   - Click "Create"

## ⚙️ Paso 2: Actualizar appsettings.json con Configuración Completa (5 minutos)

### 📝 Configuración de appsettings.json

Crear/actualizar el archivo `appsettings.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "[SU-TENANT-ID-AQUÍ]",
    "ClientId": "[SU-CLIENT-ID-AQUÍ]",
    "ClientSecret": "[SU-CLIENT-SECRET-AQUÍ]",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  },
  "DataProtection": {
    "ApplicationName": "DevSeguroApp-[SuNombre]",
    "StorageConnectionString": "[SU-STORAGE-CONNECTION-STRING]",
    "KeyLifetime": "90.00:00:00"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.AspNetCore.DataProtection": "Debug"
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://localhost:7001"
      }
    }
  }
}
```

### 📋 Variables a Reemplazar

| Variable                           | Descripción                          | Dónde Obtenerla                                            |
| ---------------------------------- | ------------------------------------- | ----------------------------------------------------------- |
| `[SU-TENANT-ID-AQUÍ]`           | ID del tenant Azure AD                | Azure Portal → Azure Active Directory → Properties        |
| `[SU-CLIENT-ID-AQUÍ]`           | ID de la aplicación registrada       | Azure Portal → App registrations                           |
| `[SU-CLIENT-SECRET-AQUÍ]`       | Secret de la aplicación              | Azure Portal → App registrations → Certificates & secrets |
| `[SU-STORAGE-CONNECTION-STRING]` | Connection string del Storage Account | Storage Account → Access keys                              |
| `[SuNombre]`                     | Su nombre para identificación única | -                                                           |

## 🔧 Paso 3: Configurar Data Protection en Program.cs (7 minutos)

### 📄 Actualizar Program.cs

Crear/actualizar el archivo `Program.cs`:

```csharp
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using Microsoft.AspNetCore.DataProtection;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Habilitar logging detallado en desarrollo
if (builder.Environment.IsDevelopment())
{
    IdentityModelEventSource.ShowPII = true;
}

// Configurar servicios básicos
builder.Services.AddControllersWithViews();

// Configurar Microsoft Identity Web
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// 🔐 CONFIGURACIÓN AVANZADA DE DATA PROTECTION
builder.Services.AddDataProtection(options =>
{
    // Nombre único de aplicación para aislamiento
    options.ApplicationDiscriminator = builder.Configuration["DataProtection:ApplicationName"];
})
.SetDefaultKeyLifetime(TimeSpan.Parse(builder.Configuration["DataProtection:KeyLifetime"]))
.PersistKeysToAzureBlobStorage(
    builder.Configuration["DataProtection:StorageConnectionString"],
    "dataprotection-keys",
    "keys.xml")
.SetApplicationName(builder.Configuration["DataProtection:ApplicationName"]);

// Configurar autorización
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddRazorPages();

var app = builder.Build();

// Configurar pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ORDEN CRÍTICO en .NET 9
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
```

### 🔍 Puntos Clave de la Configuración

1. **ApplicationDiscriminator**: Aísla las claves por aplicación
2. **SetDefaultKeyLifetime**: Claves rotan cada 90 días
3. **PersistKeysToAzureBlobStorage**: Almacena claves en Azure Storage
4. **SetApplicationName**: Identifica únicamente la aplicación

## 🛠️ Paso 4: Crear Servicio de Protección de Datos (5 minutos)

### 📁 Crear Directorio Services

```bash
mkdir Services
```

### 📄 Crear ISecureDataService Interface

Crear archivo `Services/ISecureDataService.cs`:

```csharp
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
```

### 📄 Crear SecureDataService Implementation

Crear archivo `Services/SecureDataService.cs`:

```csharp
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
```

### 🔗 Registrar el Servicio

Añadir en `Program.cs` antes de `var app = builder.Build();`:

```csharp
// Registrar servicio de protección de datos
builder.Services.AddScoped<ISecureDataService, SecureDataService>();
```

## 🧪 Paso 5: Testing Básico de Data Protection

### 📄 Crear Controller de Testing

Crear archivo `Controllers/DataProtectionTestController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using DevSeguroWebApp.Services;

namespace DevSeguroWebApp.Controllers
{
    public class DataProtectionTestController : Controller
    {
        private readonly ISecureDataService _secureDataService;
        private readonly ILogger<DataProtectionTestController> _logger;

        public DataProtectionTestController(
            ISecureDataService secureDataService,
            ILogger<DataProtectionTestController> logger)
        {
            _secureDataService = secureDataService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult TestProtection([FromBody] TestDataRequest request)
        {
            try
            {
                // Proteger datos
                var protectedData = _secureDataService.ProtectSensitiveData(request.Data, request.Purpose);
              
                // Desproteger datos para verificar
                var unprotectedData = _secureDataService.UnprotectSensitiveData<string>(protectedData, request.Purpose);
              
                return Json(new
                {
                    Success = true,
                    OriginalData = request.Data,
                    ProtectedData = protectedData,
                    UnprotectedData = unprotectedData,
                    ProtectedLength = protectedData.Length,
                    OriginalLength = request.Data.Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in data protection test");
                return Json(new { Success = false, Error = ex.Message });
            }
        }
    }

    public class TestDataRequest
    {
        public string Data { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
    }
}
```

### 📄 Crear Vista de Testing

Crear directorio y archivo `Views/DataProtectionTest/Index.cshtml`:

```html
@{
    ViewData["Title"] = "Data Protection Test";
}

<div class="container">
    <h2>🔐 Data Protection API Test</h2>
  
    <div class="row">
        <div class="col-md-6">
            <div class="card">
                <div class="card-header">
                    <h5>Test Data Protection</h5>
                </div>
                <div class="card-body">
                    <div class="mb-3">
                        <label for="testData" class="form-label">Datos a Proteger:</label>
                        <textarea class="form-control" id="testData" rows="3" 
                            placeholder="Ingrese datos para proteger..."></textarea>
                    </div>
                    <div class="mb-3">
                        <label for="purpose" class="form-label">Propósito:</label>
                        <select class="form-select" id="purpose">
                            <option value="UserData.Personal.v1">Información Personal</option>
                            <option value="UserData.Financial.v1">Datos Financieros</option>
                            <option value="UserData.Medical.v1">Información Médica</option>
                            <option value="UserData.Custom.v1">Propósito Personalizado</option>
                        </select>
                    </div>
                    <button type="button" class="btn btn-primary" onclick="testProtection()">
                        🔒 Probar Protección
                    </button>
                </div>
            </div>
        </div>
      
        <div class="col-md-6">
            <div class="card">
                <div class="card-header">
                    <h5>Resultados</h5>
                </div>
                <div class="card-body">
                    <div id="results">
                        <p class="text-muted">Los resultados aparecerán aquí...</p>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

<script>
async function testProtection() {
    const data = document.getElementById('testData').value;
    const purpose = document.getElementById('purpose').value;
  
    if (!data.trim()) {
        alert('Por favor ingrese datos para proteger');
        return;
    }
  
    try {
        const response = await fetch('@Url.Action("TestProtection", "DataProtectionTest")', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                data: data,
                purpose: purpose
            })
        });
      
        const result = await response.json();
      
        if (result.success) {
            document.getElementById('results').innerHTML = `
                <div class="alert alert-success">
                    <h6>✅ Protección Exitosa</h6>
                    <p><strong>Datos Originales:</strong> ${result.originalData}</p>
                    <p><strong>Datos Protegidos:</strong></p>
                    <code style="word-break: break-all;">${result.protectedData}</code>
                    <p class="mt-2"><strong>Verificación:</strong> ${result.unprotectedData}</p>
                    <p><strong>Tamaño Original:</strong> ${result.originalLength} bytes</p>
                    <p><strong>Tamaño Protegido:</strong> ${result.protectedLength} bytes</p>
                </div>
            `;
        } else {
            document.getElementById('results').innerHTML = `
                <div class="alert alert-danger">
                    <h6>❌ Error en Protección</h6>
                    <p>${result.error}</p>
                </div>
            `;
        }
    } catch (error) {
        document.getElementById('results').innerHTML = `
            <div class="alert alert-danger">
                <h6>❌ Error de Conexión</h6>
                <p>${error.message}</p>
            </div>
        `;
    }
}
</script>
```

## 🏃‍♂️ Ejecutar y Probar

### ▶️ Ejecutar la Aplicación

```bash
# Navegar al proyecto
cd DevSeguroApp/DevSeguroWebApp

# Compilar y ejecutar
dotnet build
dotnet run
```

### 🧪 Realizar Pruebas

1. **Navegar a la aplicación**:

   - URL: https://localhost:7001/DataProtectionTest
2. **Probar diferentes tipos de datos**:

   - **Personal**: "Juan Pérez, CC: 1234567890"
   - **Financiero**: "Tarjeta: 4111-1111-1111-1111, CVV: 123"
   - **Médico**: "Paciente: María López, Alergias: Penicilina"
3. **Verificar diferentes propósitos**:

   - Cada propósito debe generar diferentes encriptaciones
   - Los datos protegidos con un propósito no deben desencriptarse con otro

## ✅ Checklist de Completación

Marcar cuando esté completado:

- [ ] ✅ Azure Storage Account creado y configurado
- [ ] ✅ Container "dataprotection-keys" creado
- [ ] ✅ Connection string obtenido y configurado
- [ ] ✅ appsettings.json actualizado correctamente
- [ ] ✅ Program.cs configurado con Data Protection
- [ ] ✅ ISecureDataService interface creada
- [ ] ✅ SecureDataService implementación creada
- [ ] ✅ Servicio registrado en DI container
- [ ] ✅ Controller de testing creado
- [ ] ✅ Vista de testing implementada
- [ ] ✅ Aplicación ejecuta sin errores
- [ ] ✅ Testing básico funcionando
- [ ] ✅ Múltiples propósitos funcionando correctamente

## 🚨 Troubleshooting Común

### Error: "Could not connect to storage account"

**Solución**:

1. Verificar que el connection string esté correcto
2. Confirmar que el container existe
3. Verificar permisos de acceso al storage account

### Error: "DataProtection key not found"

**Solución**:

```bash
# Limpiar y reconstruir
dotnet clean
dotnet build
```

### Error: "ApplicationDiscriminator cannot be empty"

**Solución**:

- Verificar que el valor en appsettings.json esté configurado
- Confirmar que la configuración se está leyendo correctamente

### Error: "Invalid connection string format"

**Solución**:

- Verificar que el connection string esté entre comillas
- Confirmar que no hay caracteres especiales sin escapar

## 🎯 Resultado Esperado

Al completar este laboratorio, debe tener:

1. **Data Protection API Funcionando**:

   - Claves persistiendo en Azure Storage
   - Múltiples protectores configurados
   - Rotación automática de claves
2. **Servicio de Protección**:

   - Interface bien definida
   - Implementación robusta con logging
   - Métodos específicos por tipo de datos
3. **Testing Implementado**:

   - Controller de testing funcional
   - Vista interactiva para pruebas
   - Verificación end-to-end
4. **Configuración Enterprise**:

   - Logging detallado configurado
   - Manejo de errores apropiado
   - Configuración centralizada

## ➡️ Próximo Paso

Una vez completado exitosamente este laboratorio, proceder con:
**[Laboratorio 2: Integración Completa con Azure Key Vault](../Laboratorio2-KeyVault/)**

---

⚡ **Nota Importante**: Las claves se crearán automáticamente en Azure Storage al primera ejecución de la aplicación.
