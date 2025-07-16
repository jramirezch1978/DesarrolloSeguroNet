# 🧪 LABORATORIO 4: ANÁLISIS AVANZADO DE TOKENS JWT Y DEBUGGING

⏱️ **Duración**: 15 minutos  
🎯 **Objetivo**: Analizar tokens JWT en profundidad y implementar debugging avanzado para .NET 9  
🔧 **Tecnologías**: .NET 9, ASP.NET Core MVC, Microsoft.Identity.Web, Azure AD, JWT.io  

## 📋 Prerrequisitos

- **Laboratorio 0** completado exitosamente
- **Laboratorio 1** completado exitosamente 
- **Laboratorio 2** completado exitosamente
- **Laboratorio 3** completado exitosamente
- Información de Azure AD configurada y guardada
- Usuario autenticado con tokens JWT válidos

## 📝 Objetivos de Aprendizaje

- ✅ Analizar tokens JWT en profundidad (Access, ID, Refresh)
- ✅ Implementar debugging avanzado de autenticación
- ✅ Integrar con JWT.io para análisis externo
- ✅ Crear herramientas de copia y análisis de claims
- ✅ Validar estructura y contenido de tokens JWT
- ✅ Comprender el ciclo de vida de tokens OAuth 2.0

## ⚠️ CONFIGURACIÓN CRÍTICA DE AZURE AD PORTAL

### 🚨 **IMPORTANTE: Para que aparezcan los tokens JWT reales**

**ANTES de comenzar el laboratorio**, debes habilitar Access Tokens en Azure AD Portal:

#### **1. 🌐 Navegar a Azure Portal:**
```
https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Authentication/appId/684b5144-95ee-4ff7-a725-f80f7ad715c7
```

#### **2. 🔧 En Authentication → Advanced Settings, cambiar:**

**❌ CONFIGURACIÓN ACTUAL (Laboratorio 1-3):**
```
✅ ID tokens (used for implicit and hybrid flows) - HABILITADO
❌ Access tokens (used for implicit flows) - DESHABILITADO
```

**✅ CONFIGURACIÓN REQUERIDA (Laboratorio 4):**
```
✅ ID tokens (used for implicit and hybrid flows) - MANTENER HABILITADO
✅ Access tokens (used for implicit flows) - HABILITAR ESTE
```

#### **3. 💾 Guardar cambios en Azure Portal**

#### **4. 🔄 CRÍTICO: Cerrar sesión y autenticarse de nuevo**

**Sin este cambio, los tokens aparecerán como "Ausente" en el análisis.**

### **🤔 ¿Por qué este cambio?**

#### **🔐 Principio de Seguridad por Defecto:**
Azure AD implementa **"Secure by Default"**, lo que significa:
- **Por defecto**: Solo emite **ID Tokens** (para identificación)
- **Access Tokens**: Requieren habilitación explícita (para APIs)
- **Refresh Tokens**: Requieren configuración adicional (para renovación)

#### **📚 Diferencia entre Laboratorios:**
- **Laboratorios 1-3**: Configuración de seguridad básica (solo ID tokens)
- **Laboratorio 4**: Análisis académico completo (requiere Access + ID tokens)
- **En producción real**: Mantener Access tokens deshabilitados por seguridad

#### **🚨 ¿Por qué aparecen como "Ausente"?**
1. **Azure AD no los emite** si no están habilitados en Portal
2. **SaveTokens = true** solo guarda tokens que realmente recibe
3. **Sin configuración correcta** = No hay tokens que guardar

### **📊 Resultado Esperado tras la configuración:**
- ✅ **Access Token**: Presente (después de habilitar en Portal)
- ✅ **ID Token**: Presente (ya estaba habilitado)
- ❓ **Refresh Token**: Puede seguir ausente (ver configuración adicional abajo)

### **🔄 CONFIGURACIÓN ADICIONAL PARA REFRESH TOKEN**

#### **¿Por qué el Refresh Token sigue ausente?**

**El Refresh Token requiere configuración adicional en Azure AD:**

#### **1. 🌐 En Azure Portal → Tu App Registration → API Permissions:**

**Agregar permisos offline:**
```
Microsoft Graph → Delegated permissions:
✅ offline_access (para Refresh Tokens)
```

#### **2. 🔧 En Authentication → Advanced Settings:**

**Habilitar también:**
```
✅ ID tokens (used for implicit and hybrid flows) - YA HABILITADO
✅ Access tokens (used for implicit flows) - HABILITAR
✅ Allow public client flows - HABILITAR (para refresh tokens)
```

#### **3. 💾 Grant Admin Consent:**
- Ve a **API Permissions**
- Click en **"Grant admin consent for [Tu Tenant]"**
- Confirmar los nuevos permisos

#### **4. 🔄 Cerrar sesión y autenticarse nuevamente**

### **📊 Resultado Final Esperado:**
- ✅ **Access Token**: Presente
- ✅ **ID Token**: Presente  
- ✅ **Refresh Token**: Presente (después de configuración completa)

---

## 🚀 Pasos del Laboratorio

### Paso 1: Crear Vista de Análisis de Tokens (10 minutos)

La vista principal `TokenInfo.cshtml` incluye:

#### **Funcionalidades Principales:**
- **Carga Dinámica**: Información de tokens cargada via JavaScript
- **Análisis Completo**: Access Token, ID Token, Refresh Token
- **Claims Detallados**: Tabla interactiva con descripciones
- **Modal de Token**: Visualización completa del JWT
- **Integración JWT.io**: Análisis externo de tokens

#### **Características de la Interfaz:**
- **Spinner de Carga**: Mientras obtiene información de tokens
- **Cards Responsivos**: Información del usuario y estado de tokens
- **Tabla Interactiva**: Claims con funcionalidad de copia
- **Modal Completo**: Token JWT completo con advertencias de seguridad

### Paso 2: Testing de Análisis de Tokens (5 minutos)

#### **Secuencia de Testing:**

1. **🔑 Autenticarse en la aplicación** si no está autenticado
2. **🔍 Navegar a "Análisis de Tokens"** desde el menú
3. **📊 Verificar que carga correctamente:**
   - Información del usuario presente
   - Estado de tokens (Access, ID, Refresh)
   - Tabla de claims poblada con descripciones
4. **🔗 Probar funcionalidad de copia:**
   - Click en cualquier valor de claim
   - Verificar que se copia al portapapeles
   - Verificar feedback visual (cambio de color)
5. **🎯 Probar vista de token completo:**
   - Click en "Ver ID Token Completo"
   - Verificar que abre modal
   - Verificar que muestra token completo
6. **📋 Probar copia de token completo:**
   - Click en "Copiar Token"
   - Verificar que se copia al portapapeles
7. **🌐 Probar análisis en JWT.io:**
   - Click en "Analizar en JWT.io"
   - Verificar que abre jwt.io en nueva pestaña
   - Pegar token y analizar estructura

## 🔍 Análisis en JWT.io

### **Elementos a verificar en jwt.io:**

#### **✅ Header:**
- `alg: RS256` (algoritmo de firma)
- `typ: JWT` (tipo de token)
- `kid: key ID` para verificación

#### **✅ Payload (Claims importantes):**
- `iss`: https://login.microsoftonline.com/[tenant]/v2.0
- `aud`: [su-client-id]
- `sub`: [unique-user-identifier]
- `name`: [nombre-del-usuario]
- `preferred_username`: [email-del-usuario]
- `exp`: [timestamp-expiracion]
- `iat`: [timestamp-emision]

#### **✅ Signature:**
- Aparecerá como "Invalid" (normal, no tenemos la clave pública)
- Verificar que la estructura es correcta

## 🎨 Funcionalidades de la Interfaz

### **Vista Principal de Análisis**
- **Información del Usuario**: Nombre, autenticación, tipo de auth, total de claims
- **Estado de Tokens**: Presencia de Access, ID y Refresh tokens
- **Claims Detallados**: Tabla con numeración, tipo, valor y descripción
- **Descripciones de Claims**: Explicación de cada claim estándar

### **Modal de Token Completo**
- **Advertencia de Seguridad**: Información sobre tokens en desarrollo
- **Instrucciones**: Pasos para usar JWT.io
- **Textarea**: Token completo en formato monospace
- **Botones de Acción**: Copiar token y abrir JWT.io

### **Funcionalidad de Copia**
- **Copia Individual**: Click en cualquier claim para copiar
- **Feedback Visual**: Cambio de color al copiar
- **Copia de Token**: Botón para copiar token completo

## 📊 Controlador Extendido

### **Métodos Implementados:**
- `TokenInfo()`: Vista principal de análisis
- `GetTokenInfo()`: API para obtener información de tokens
- `GetFullIdToken()`: API para obtener token completo
- `TestAuthentication()`: Herramientas de testing
- `ClaimsAnalysis()`: Análisis avanzado de claims

### **Manejo de Errores:**
- Try-catch para obtención de tokens
- Mensajes de error descriptivos
- Fallback para tokens no disponibles

## 🔧 Configuración y Ejecución

### **Compilar y Ejecutar:**
```bash
# Navegar al directorio del proyecto
cd LabSesion04/Laboratorio4-AnalisisTokensJWT/DevSeguroWebApp

# Compilar proyecto
dotnet build

# Ejecutar aplicación
dotnet run
```

### **Verificar la Aplicación:**
- ✅ Aplicación ejecutándose en: `https://localhost:7001`
- ✅ Aceptar advertencia de certificado (desarrollo local)
- ✅ Verificar página de inicio del laboratorio 4

## 📁 Estructura del Proyecto

```
DevSeguroWebApp/
├── Controllers/
│   └── AccountController.cs       # Controlador con análisis de tokens
├── Views/
│   ├── Account/
│   │   ├── TokenInfo.cshtml        # Vista principal de análisis
│   │   ├── Profile.cshtml          # Vista de perfil
│   │   ├── TestAuthentication.cshtml # Testing
│   │   └── ClaimsAnalysis.cshtml   # Análisis de claims
│   ├── Home/
│   │   └── Index.cshtml            # Página principal Lab 4
│   └── Shared/
│       └── _Layout.cshtml          # Layout con navegación
├── wwwroot/                        # Recursos estáticos
├── appsettings.json               # Configuración Azure AD
├── Program.cs                     # Configuración de la aplicación
└── DevSeguroWebApp.csproj         # Archivo de proyecto
```

## 🔒 Consideraciones de Seguridad

### **Advertencias Importantes:**
- **Tokens en Desarrollo**: Solo mostrar tokens completos en desarrollo
- **No Exposición en Producción**: Nunca exponer tokens en producción
- **Clave Pública**: Signature aparecerá como "Invalid" sin la clave pública
- **Manejo Seguro**: Tokens se obtienen del contexto de autenticación

### **Mejores Prácticas:**
- Validación de tokens automática
- Manejo de errores robusto
- Feedback visual para acciones del usuario
- Integración segura con herramientas externas

## 🛠️ Herramientas de Debugging

### **Funcionalidades Implementadas:**
- **Análisis de Tokens**: Información detallada de JWT
- **Testing de Autenticación**: Herramientas de debugging
- **Análisis de Claims**: Categorización de claims
- **Copia de Claims**: Funcionalidad de portapapeles
- **Integración JWT.io**: Análisis externo

### **📋 Explicación Completa de Claims JWT**

#### **🔑 Claims de Identificación del Usuario:**
- **`sub`** (Subject): Identificador único inmutable del usuario en Azure AD
- **`name`**: Nombre completo del usuario tal como aparece en Azure AD
- **`preferred_username`**: Email del usuario (nombre preferido para login)
- **`given_name`**: Nombre de pila del usuario
- **`family_name`**: Apellido del usuario
- **`oid`** (Object ID): ID único del objeto de usuario en Azure AD (diferente de `sub`)

#### **🏢 Claims del Tenant y Aplicación:**
- **`iss`** (Issuer): URL de quien emitió el token (https://login.microsoftonline.com/[tenant]/v2.0)
- **`aud`** (Audience): Para quién está destinado el token (tu Client ID)
- **`tid`** (Tenant ID): Identificador del tenant de Azure AD
- **`appid`**: ID de la aplicación que solicitó el token
- **`ver`**: Versión del token (1.0 o 2.0)

#### **⏰ Claims de Tiempo y Sesión:**
- **`exp`** (Expiration): Timestamp Unix cuando expira el token
- **`iat`** (Issued At): Timestamp Unix cuando fue emitido el token
- **`nbf`** (Not Before): Timestamp Unix antes del cual el token no es válido
- **`auth_time`**: Timestamp Unix cuando el usuario se autenticó por última vez
- **`sid`** (Session ID): Identificador de la sesión de autenticación

#### **🔐 Claims de Autenticación y Seguridad:**
- **`amr`** (Authentication Method Reference): Cómo se autenticó (password, mfa, etc.)
- **`acr`** (Authentication Context Class Reference): Nivel de confianza de autenticación
- **`aio`**: Azure Internal Object - Uso interno de Azure (información de sesión)
- **`rh`** (Refresh Hash): Hash relacionado con refresh tokens
- **`uti`** (Unique Token Identifier): Identificador único de este token específico

#### **📧 Claims de Contacto y Perfil:**
- **`email`**: Dirección de correo electrónico del usuario
- **`email_verified`**: Si el email ha sido verificado (true/false)
- **`roles`**: Roles asignados al usuario en la aplicación
- **`groups`**: Grupos de Azure AD a los que pertenece el usuario

#### **🌐 Claims Personalizados de Azure AD:**
- Claims que empiezan con **`http://schemas.microsoft.com/`**: Claims estándar de Microsoft
- Claims que empiezan con **`http://schemas.xmlsoap.org/`**: Claims estándar SOAP/XML
- Claims sin namespace: Claims simples de OpenID Connect

### **🎯 ¿Por qué son importantes estos Claims?**

#### **🔍 Para Identificación:**
- **`sub`** + **`oid`**: Identificación única del usuario
- **`name`** + **`preferred_username`**: Información para mostrar en UI

#### **🔒 Para Seguridad:**
- **`exp`** + **`iat`**: Validar que el token no ha expirado
- **`aud`** + **`iss`**: Verificar que el token es para tu aplicación

#### **📊 Para Autorización:**
- **`roles`** + **`groups`**: Determinar permisos del usuario
- **`amr`**: Verificar método de autenticación utilizado

#### **💡 Para Debugging:**
- **`sid`** + **`uti`**: Rastrear sesiones específicas
- **`aio`** + **`rh`**: Información interna de Azure para troubleshooting

## ✅ Resultado Esperado

Al completar este laboratorio, tendrás:

- **Vista de análisis de tokens** funcionando completamente
- **Información detallada** de usuario y tokens visible
- **Funcionalidad de copia** al portapapeles operativa
- **Modal de token completo** mostrando JWT completo
- **Integración con JWT.io** funcionando
- **Claims con descripciones** explicativas
- **Comprensión completa** de la estructura JWT
- **Herramientas de debugging** funcionales

## 🐛 Troubleshooting Completo

### **🚨 PROBLEMA: Todos los tokens aparecen como "Ausente"**

#### **Diagnóstico:**
```json
{
  "accessToken": null,
  "idToken": null,
  "refreshToken": null,
  "hasAccessToken": false,
  "hasIdToken": false,
  "hasRefreshToken": false
}
```

#### **Soluciones en orden de prioridad:**

**1. ✅ Verificar configuración Azure AD Portal:**
- Ir a **Authentication → Advanced Settings**
- Habilitar **Access tokens** y **ID tokens**
- **Guardar cambios**

**2. 🔄 Cerrar sesión completamente:**
- Hacer logout de la aplicación
- Cerrar navegador (limpiar cache de tokens)
- Volver a autenticarse

**3. 🔧 Verificar Program.cs:**
- Confirmar `SaveTokens = true`
- Verificar que eventos `OnTokenResponseReceived` están configurados

**4. 🌐 Verificar URL correcta:**
- Aplicación ejecutándose en `https://localhost:7001`
- Redirect URIs en Azure AD coinciden exactamente

### **❓ PROBLEMA: Solo ID Token aparece, Access Token ausente**

#### **Causa:** 
Azure AD no está configurado para emitir Access Tokens

#### **Solución:**
```
Azure Portal → Tu App → Authentication → Advanced Settings:
✅ ID tokens: YA HABILITADO  
✅ Access tokens: HABILITAR ESTE
```

### **❓ PROBLEMA: Refresh Token siempre ausente**

#### **Causa:** 
Faltan permisos `offline_access` y configuración adicional

#### **Solución completa:**

**1. API Permissions:**
```
Microsoft Graph → Delegated permissions:
✅ User.Read (ya existe)
✅ email (ya existe)  
✅ profile (ya existe)
✅ openid (ya existe)
✅ offline_access (AGREGAR ESTE)
```

**2. Advanced Settings:**
```
✅ ID tokens: HABILITADO
✅ Access tokens: HABILITADO  
✅ Allow public client flows: HABILITAR
```

**3. Grant Admin Consent:**
- **API Permissions** → **Grant admin consent**
- Confirmar permisos

### **🔧 PROBLEMA: Error de configuración de puerto**

#### **Síntomas:**
- Redirección fallida después de login
- Error "redirect_uri_mismatch"

#### **Solución:**
Verificar consistencia:
- **appsettings.json**: `"Url": "https://localhost:7001"`
- **launchSettings.json**: `"applicationUrl": "https://localhost:7001"`  
- **Azure AD Portal**: `https://localhost:7001/signin-oidc`

### **💻 PROBLEMA: Error al copiar al portapapeles**
**Solución**: Verificar que el navegador soporte la API de clipboard y que sea HTTPS

### **🖼️ PROBLEMA: Modal no se abre**
**Solución**: Verificar que Bootstrap esté cargado correctamente

### **🌐 PROBLEMA: JWT.io no funciona**
**Solución**: Verificar que se permite abrir nuevas pestañas en el navegador

### **📊 PROBLEMA: Claims con información truncada**

#### **Causa:**
Algunos claims de Azure AD son muy largos (como `aio`)

#### **Comportamiento normal:**
- Claims largos se muestran completos en la tabla
- Son válidos aunque parezcan "raros"
- Contienen información interna de Azure AD

### **🔄 PROBLEMA: "SaveTokens no funciona"**

#### **Verificación paso a paso:**

**1. Confirmar configuración Program.cs:**
```csharp
options.SaveTokens = true;
options.Events = new OpenIdConnectEvents { ... };
```

**2. Verificar orden en pipeline:**
```csharp
app.UseAuthentication();  // ANTES de
app.UseAuthorization();   // ESTO
```

**3. Confirmar que Azure AD envía tokens:**
- Revisar Network tab en DevTools
- Buscar respuesta de token endpoint
- Verificar que contiene access_token e id_token

### **⚡ CONFIGURACIÓN COMPLETA PARA REFRESH TOKEN:**

**Si el Refresh Token sigue ausente después de agregar `offline_access`, seguir esta secuencia EXACTA:**

#### **1. 🔧 Configuración en Program.cs (CRÍTICA):**
```csharp
// Configurar Microsoft Identity Web con tokens guardados
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        options.SaveTokens = true;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
        
        // CRÍTICO PARA REFRESH TOKEN: Agregar scope offline_access explícitamente
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("offline_access"); // ESTE ES CRÍTICO
        
        // Forzar consent para obtener refresh token
        options.Prompt = "consent";
        
        // Configuración adicional para refresh tokens
        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = false;
    });
```

#### **2. 🌐 Verificar Azure Portal - API Permissions:**
```
Azure Portal → Tu App Registration → API permissions

DEBE CONTENER:
✅ User.Read (Microsoft Graph - Delegated)
✅ email (Microsoft Graph - Delegated)  
✅ profile (Microsoft Graph - Delegated)
✅ openid (Microsoft Graph - Delegated)
✅ offline_access (Microsoft Graph - Delegated) ← CRÍTICO
```

#### **3. 💾 Grant Admin Consent (MUY IMPORTANTE):**
```
API permissions → "Grant admin consent for [Tu Tenant]"
→ Confirmar → Verificar que TODOS muestren "Status: Granted" (verde)
```

#### **4. 🔄 SECUENCIA CRÍTICA DE AUTENTICACIÓN:**

**Paso 1: Limpiar cache completamente:**
```
1. Cerrar TODOS los navegadores
2. Abrir navegador nuevo (modo incógnito preferible)
3. Ir a https://localhost:7001
```

**Paso 2: Nueva autenticación COMPLETA:**
```
1. Click "Iniciar Sesión"
2. DEBE aparecer pantalla de "Grant Permissions" solicitando:
   - Read user profile
   - Maintain access to data you have given it access to
   - Sign you in and read your profile
   - Access data offline
3. Click "Accept" / "Aceptar" TODOS los permisos
```

**Paso 3: Verificar inmediatamente:**
```
1. Ir a "Análisis de Tokens" 
2. Refresh Token DEBE aparecer como "Presente"
```

#### **5. 🚨 Si NO aparece la pantalla de consent:**

**Opción A: Forzar revocación de permisos:**
```
Azure Portal → Azure Active Directory → Users 
→ [Tu usuario] → Applications 
→ Buscar tu app → Remove/Revoke access
→ Volver a autenticarte desde cero
```

**Opción B: URL directa con consent forzado:**
```
https://login.microsoftonline.com/[TU-TENANT-ID]/oauth2/v2.0/authorize?client_id=[TU-CLIENT-ID]&response_type=code&scope=openid%20profile%20email%20offline_access&redirect_uri=https://localhost:7001/signin-oidc&prompt=consent
```

#### **6. 🏢 Verificar políticas de Tenant (si sigue sin funcionar):**

**Posibles restricciones empresariales:**
- **Tenant Policy**: Algunos tenants empresariales bloquean refresh tokens por política de seguridad
- **Assignment required**: Ve a Enterprise applications → Tu App → Properties
- **Admin consent required**: Verificar que no requiera consent especial de administrador

#### **7. ⚙️ Configuraciones alternativas avanzadas:**

**Si usas appsettings.json:**
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "[TU-TENANT-ID]",
    "ClientId": "[TU-CLIENT-ID]",
    "ClientSecret": "[TU-CLIENT-SECRET]",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc",
    "Scope": "openid profile email offline_access"
  }
}
```

### **⚡ SOLUCIÓN RÁPIDA COMPLETA:**

**Si nada funciona, seguir esta secuencia exacta:**

1. **Program.cs** → Agregar configuración de scopes y prompt consent
2. **Azure Portal** → Habilitar Access tokens
3. **Azure Portal** → Agregar `offline_access` permission  
4. **Azure Portal** → Grant admin consent (CRÍTICO)
5. **Navegador** → Cerrar todos, abrir incógnito
6. **Aplicación** → Login desde cero
7. **Consent** → Aceptar TODOS los permisos nuevos
8. **Verificar** → Ir a "Análisis de Tokens"

### **📊 Resultado Final Esperado:**
- ✅ **Access Token**: Presente
- ✅ **ID Token**: Presente  
- ✅ **Refresh Token**: Presente (después de configuración completa y consent)

### **🔍 Diagnosis Final:**
**Si después de TODOS estos pasos el Refresh Token sigue ausente:**
- **Probable causa**: Política de tenant empresarial que restringe refresh tokens
- **Solución**: Contactar administrador de Azure AD o usar tenant personal para testing académico

## 📈 Funcionalidades Adicionales

### **Más Allá del Documento:**
- **Análisis de Claims**: Categorización avanzada
- **Testing de Autenticación**: Herramientas de debugging
- **Interfaz Moderna**: Bootstrap 5 y Font Awesome
- **Navegación Mejorada**: Menús contextuales
- **Feedback Visual**: Indicadores de estado

## 🔄 Próximos Pasos

- **Implementar autorización** basada en roles
- **Análisis de Refresh Tokens** más detallado
- **Validación de firma** con clave pública
- **Monitoreo de expiración** de tokens
- **Logging de eventos** de autenticación

---

**Resultado esperado**: Herramientas completas de análisis de tokens JWT funcionando perfectamente con interfaz moderna y debugging avanzado. 