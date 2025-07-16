# 🧪 LABORATORIO 2: DESARROLLO DE APLICACIÓN .NET 9 CON OAUTH 2.0

⏱️ **Duración**: 25 minutos  
🎯 **Objetivo**: Crear aplicación .NET 9 que implementa autenticación con Azure AD  
🔧 **Tecnologías**: .NET 9, ASP.NET Core MVC, Microsoft.Identity.Web, Azure AD  

## 📋 Prerrequisitos

- **Laboratorio 0** completado exitosamente
- **Laboratorio 1** completado exitosamente 
- Información de Azure AD configurada y guardada
- Visual Studio Code con extensiones instaladas

## 🚀 Estructura del Proyecto

```
DevSeguroWebApp/
├── Controllers/
│   ├── AccountController.cs      # Controlador de autenticación
│   └── HomeController.cs         # Controlador principal
├── Views/
│   ├── Account/
│   │   ├── Profile.cshtml         # Vista del perfil de usuario
│   │   └── SignedOut.cshtml       # Vista de sesión cerrada
│   ├── Home/
│   └── Shared/
│       └── _Layout.cshtml         # Layout principal
├── wwwroot/                       # Recursos estáticos
├── appsettings.json              # Configuración principal
├── appsettings.Development.json   # Configuración de desarrollo
├── Program.cs                    # Punto de entrada de la aplicación
└── DevSeguroWebApp.csproj        # Archivo de proyecto
```

## 🔧 Configuración Inicial

### Paso 1: Configurar appsettings.json

Actualizar `appsettings.json` con los valores de Azure AD obtenidos en el **Laboratorio 1**:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "[SU-TENANT-ID-AQUÍ]",
    "ClientId": "[SU-CLIENT-ID-AQUÍ]",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

### Paso 2: Configurar Client Secret

Para habilitar el flujo completo de autenticación, agregar el Client Secret a `appsettings.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "[SU-TENANT-ID-AQUÍ]",
    "ClientId": "[SU-CLIENT-ID-AQUÍ]",
    "ClientSecret": "[SU-CLIENT-SECRET-AQUÍ]",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

**⚠️ IMPORTANTE**: Nunca subir el Client Secret a control de versiones en producción.

## 🏃 Ejecutar la Aplicación

### Método 1: Desde Visual Studio Code

1. Abrir la carpeta del proyecto en VS Code
2. Abrir terminal integrada (`Ctrl + ``)
3. Ejecutar:
   ```bash
   dotnet build
   dotnet run
   ```

### Método 2: Desde PowerShell

```powershell
# Navegar al directorio del proyecto
cd LabSesion04/Laboratorio2-DesarrolloNET9/DevSeguroWebApp

# Compilar el proyecto
dotnet build

# Ejecutar la aplicación
dotnet run
```

### Verificar la Ejecución

- La aplicación debería ejecutarse en: `https://localhost:7001`
- Si aparece advertencia de certificado, aceptar (desarrollo local)
- Verificar que la página de inicio se carga correctamente

## 🔍 Funcionalidades Implementadas

### Autenticación
- **Inicio de sesión** con Azure AD
- **Cierre de sesión** seguro
- **Redirección** automática después del login

### Vistas
- **Página de inicio** con estado de autenticación
- **Perfil de usuario** con información básica
- **Confirmación de logout** con información de seguridad

### Controladores
- **AccountController**: Maneja autenticación y perfil
- **HomeController**: Página principal de la aplicación

## 🧪 Probar la Aplicación

### Flujo de Autenticación

1. **Acceder a la aplicación**: `https://localhost:7001`
2. **Iniciar sesión**: Click en botón de login
3. **Autenticarse**: Credenciales de Azure AD
4. **Verificar perfil**: Navegar a la página de perfil
5. **Cerrar sesión**: Logout seguro

### Verificaciones Esperadas

- ✅ Redirección correcta a Azure AD
- ✅ Retorno exitoso a la aplicación
- ✅ Información del usuario visible
- ✅ Claims básicos mostrados
- ✅ Logout funcionando correctamente

## 🚨 Troubleshooting

### Error: "No se puede cargar la página"
**Solución**: Verificar que la aplicación esté ejecutándose en el puerto 7001

### Error: "Redirect URI mismatch"
**Solución**: Verificar que las URIs en Azure AD coincidan con las configuradas

### Error: "Authentication failed"
**Solución**: Verificar TenantId y ClientId en appsettings.json

### Error: "Claims vacíos"
**Solución**: Verificar permisos en Azure AD (email, profile, openid)

## 📊 Componentes Técnicos

### Program.cs
- Configuración de Microsoft.Identity.Web
- Configuración de autenticación y autorización
- Pipeline de middleware en orden correcto

### AccountController
- Métodos de SignIn y SignOut
- Manejo de redirecciones
- Configuración de propiedades de autenticación

### Vistas
- Uso de `User.Identity` para verificar autenticación
- Acceso a claims del usuario autenticado
- Interfaz responsive con Bootstrap

## 🔄 Próximos Pasos

Una vez completado este laboratorio, continuar con:
- **Laboratorio 3**: Implementación de vistas y testing completo
- **Laboratorio 4**: Análisis avanzado de tokens JWT

## 📝 Notas Importantes

- La aplicación usa **Authorization Code Flow** con PKCE
- Los tokens se almacenan de forma segura en cookies
- El middleware de autenticación debe ir antes que autorización
- Las Razor Pages están habilitadas para Microsoft.Identity.Web

---

**Resultado esperado**: Aplicación .NET 9 funcional con autenticación Azure AD básica implementada y probada. 