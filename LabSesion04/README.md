# 🚀 LabSesion04 - Diseño Seguro de Aplicaciones .NET en Azure

## 📋 Descripción General

Este proyecto contiene los **5 laboratorios prácticos** de la **Sesión 4** del curso "Diseño Seguro de Aplicaciones (.NET en Azure)". Cada laboratorio está diseñado para ser completado de forma independiente y progresiva, construyendo sobre los conocimientos adquiridos en los laboratorios anteriores.

**Duración Total**: 100 minutos  
**Modalidad**: Instructor-led con práctica individual  
**Tecnologías**: .NET 9, ASP.NET Core MVC, Azure AD, OAuth 2.0, OpenID Connect  

## 🗂️ Estructura del Proyecto

```
LabSesion04/
├── 📁 Laboratorio0-ConfiguracionEntorno/
│   ├── 📄 README.md                    # Guía de configuración del entorno
│   ├── 📄 setup-entorno.ps1           # Script automatizado de configuración
│   └── 📄 troubleshooting.md          # Solución de problemas comunes
│
├── 📁 Laboratorio1-AzureADRegistration/
│   ├── 📄 README.md                    # Guía de registro en Azure AD
│   ├── 📄 azure-ad-config.template.txt # Plantilla de configuración
│   └── 📄 verificacion-azure.md       # Lista de verificación
│
├── 📁 Laboratorio2-DesarrolloNET9/
│   ├── 📄 README.md                    # Guía de desarrollo básico
│   └── 📁 DevSeguroWebApp/            # Proyecto .NET 9 básico
│       ├── 📄 appsettings.json
│       ├── 📄 Program.cs
│       ├── 📁 Controllers/
│       │   └── 📄 AccountController.cs
│       └── 📁 Views/
│           └── 📁 Account/
│               ├── 📄 Profile.cshtml
│               └── 📄 SignedOut.cshtml
│
├── 📁 Laboratorio3-VistasYTesting/
│   ├── 📄 README.md                    # Guía de vistas y testing
│   └── 📁 DevSeguroWebApp/            # Proyecto .NET 9 extendido
│       ├── 📄 appsettings.json
│       ├── 📄 Program.cs
│       ├── 📁 Controllers/
│       │   └── 📄 AccountController.cs  # Con métodos extendidos
│       └── 📁 Views/
│           ├── 📁 Account/
│           │   ├── 📄 Profile.cshtml    # Vista completa
│           │   └── 📄 SignedOut.cshtml
│           └── 📁 Shared/
│               └── 📄 _Layout.cshtml    # Layout con navegación
│
├── 📁 Laboratorio4-AnalisisTokensJWT/
│   ├── 📄 README.md                    # Guía de análisis de tokens
│   └── 📁 DevSeguroWebApp/            # Proyecto .NET 9 con análisis JWT
│       ├── 📄 appsettings.json
│       ├── 📄 Program.cs
│       ├── 📁 Controllers/
│       │   └── 📄 AccountController.cs  # Con análisis de tokens
│       └── 📁 Views/
│           └── 📁 Account/
│               ├── 📄 Profile.cshtml
│               ├── 📄 SignedOut.cshtml
│               └── 📄 TokenInfo.cshtml  # Vista de análisis JWT
│
└── 📄 README.md                        # Este archivo
```

## 🧪 Laboratorios Incluidos

### 🛠️ Laboratorio 0: Configuración del Entorno de Desarrollo
**⏱️ Duración**: 15 minutos  
**🎯 Objetivo**: Instalar y configurar todas las herramientas necesarias  

**Componentes**:
- ✅ Instalación de Chocolatey
- ✅ Instalación de .NET 9 SDK
- ✅ Instalación de Visual Studio Code
- ✅ Configuración de extensiones
- ✅ Script de automatización PowerShell

**Herramientas Configuradas**:
- Chocolatey (gestor de paquetes)
- .NET 9 SDK
- Visual Studio Code + extensiones
- C# Dev Kit + Microsoft.Identity.Web

---

### 🧪 Laboratorio 1: Registro y Configuración de Aplicación en Azure AD
**⏱️ Duración**: 20 minutos  
**🎯 Objetivo**: Crear y configurar una aplicación en Azure AD para OAuth 2.0/OpenID Connect  

**Componentes**:
- ✅ Creación de App Registration
- ✅ Configuración de Redirect URIs
- ✅ Configuración de API Permissions
- ✅ Creación de Client Secret
- ✅ Plantilla de configuración

**Configuración Resultante**:
- App Registration completa
- Client ID y Tenant ID
- Redirect URIs configurados
- Permisos: `User.Read`, `email`, `profile`, `openid`

---

### 🧪 Laboratorio 2: Desarrollo de Aplicación .NET 9 con OAuth 2.0
**⏱️ Duración**: 25 minutos  
**🎯 Objetivo**: Crear aplicación .NET 9 con autenticación Azure AD básica  

**Componentes**:
- ✅ Proyecto ASP.NET Core MVC (.NET 9)
- ✅ Integración con Microsoft.Identity.Web
- ✅ Configuración de autenticación
- ✅ Controlador de cuenta básico
- ✅ Vistas básicas de perfil

**Funcionalidades**:
- Inicio de sesión con Azure AD
- Cierre de sesión seguro
- Vista de perfil básica
- Manejo de claims de usuario

---

### 🧪 Laboratorio 3: Implementación de Vistas y Testing Completo
**⏱️ Duración**: 25 minutos  
**🎯 Objetivo**: Crear vistas completas y probar el flujo de autenticación  

**Componentes**:
- ✅ Layout principal con navegación
- ✅ Vista de perfil extendida
- ✅ Vista de confirmación de logout
- ✅ Vista de inicio actualizada
- ✅ Testing completo del flujo

**Funcionalidades Extendidas**:
- Navegación con estado de autenticación
- Información detallada del usuario
- Tabla completa de claims
- Información técnica del servidor
- Interfaz responsive con Bootstrap 5

---

### 🧪 Laboratorio 4: Análisis Avanzado de Tokens JWT
**⏱️ Duración**: 15 minutos  
**🎯 Objetivo**: Analizar tokens JWT en profundidad e implementar debugging avanzado  

**Componentes**:
- ✅ Vista de análisis de tokens
- ✅ Endpoint para obtener información de tokens
- ✅ Endpoint para token completo
- ✅ Integración con jwt.io
- ✅ Funcionalidades de debugging

**Funcionalidades Avanzadas**:
- Análisis completo de tokens JWT
- Visualización de estructura de tokens
- Integración con jwt.io para análisis
- Funcionalidades de copia al portapapeles
- Debugging avanzado (solo en desarrollo)

## 🚀 Guía de Inicio Rápido

### 1. Configuración del Entorno
```powershell
# Navegar al Laboratorio 0
cd LabSesion04/Laboratorio0-ConfiguracionEntorno

# Ejecutar script de configuración como administrador
.\setup-entorno.ps1
```

### 2. Configuración de Azure AD
```powershell
# Seguir la guía del Laboratorio 1
cd ../Laboratorio1-AzureADRegistration

# Leer README.md para pasos detallados
# Completar azure-ad-config.template.txt con sus datos
```

### 3. Desarrollo de la Aplicación
```powershell
# Navegar al laboratorio deseado (2, 3, o 4)
cd ../Laboratorio2-DesarrolloNET9/DevSeguroWebApp

# Configurar appsettings.json con sus datos de Azure AD
# Ejecutar la aplicación
dotnet run
```

### 4. Acceso a la Aplicación
- **URL**: `https://localhost:7001`
- **Credenciales**: Su cuenta de Azure AD configurada
- **Certificado**: Aceptar advertencia de desarrollo

## 📊 Progreso de Laboratorios

| Laboratorio | Estado | Componentes | Funcionalidades |
|-------------|--------|-------------|-----------------|
| **Lab 0** | ✅ Completo | Configuración del entorno | Chocolatey, .NET 9, VS Code |
| **Lab 1** | ✅ Completo | Azure AD Registration | App Registration, Permisos, Secrets |
| **Lab 2** | ✅ Completo | Desarrollo .NET 9 básico | Autenticación, Perfil básico |
| **Lab 3** | ✅ Completo | Vistas y Testing completo | UI completa, Testing end-to-end |
| **Lab 4** | ✅ Completo | Análisis JWT avanzado | Análisis tokens, Debugging |

## 🔧 Requisitos Técnicos

### Sistema Operativo
- Windows 10/11 (recomendado)
- macOS 10.15+ (compatible)
- Linux Ubuntu 20.04+ (compatible)

### Herramientas Requeridas
- **Chocolatey** (Windows package manager)
- **.NET 9 SDK** (versión más reciente)
- **Visual Studio Code** con extensiones
- **Azure AD Tenant** con permisos de administrador

### Extensiones de VS Code
- C# Dev Kit (`ms-dotnettools.csdevkit`)
- C# (`ms-dotnettools.csharp`)
- Azure Account (`ms-vscode.azure-account`)
- Azure Resources (`ms-vscode.azureresourcegroups`)
- GitLens (`eamodio.gitlens`)
- PowerShell (`ms-vscode.powershell`)

## 🔐 Configuración de Seguridad

### Azure AD
- **Tenant Type**: Single tenant
- **Account Types**: Accounts in this organizational directory only
- **Redirect URIs**: 
  - `https://localhost:7001/signin-oidc`
  - `https://localhost:7001/signout-callback-oidc`
- **Logout URL**: `https://localhost:7001/signout-oidc`

### API Permissions
- **Microsoft Graph (Delegated)**:
  - `User.Read` - Leer perfil del usuario
  - `email` - Acceso al email
  - `profile` - Acceso al perfil
  - `openid` - Autenticación OpenID

### Flujo de Autenticación
- **OAuth 2.0**: Authorization Code Flow
- **PKCE**: Proof Key for Code Exchange (habilitado)
- **Token Types**: ID Token, Access Token, Refresh Token
- **Token Storage**: Cookies seguras (HttpOnly, Secure)

## 📚 Recursos Adicionales

### Documentación
- [Microsoft Identity Web Documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web)
- [Azure AD App Registration Guide](https://docs.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app)
- [.NET 9 Documentation](https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9)

### Herramientas de Análisis
- [jwt.io](https://jwt.io/) - JWT Token Analyzer
- [Azure AD Token Analyzer](https://jwt.ms/) - Microsoft JWT Analyzer
- [Postman](https://www.postman.com/) - API Testing

### Troubleshooting
- **Certificados SSL**: Ejecutar `dotnet dev-certs https --trust`
- **Puertos**: Verificar que el puerto 7001 esté disponible
- **Permisos**: Verificar permisos de administrador en Azure AD
- **Tokens**: Verificar fecha de expiración y configuración

## 🎯 Objetivos de Aprendizaje

Al completar todos los laboratorios, los estudiantes habrán:

1. **Configurado** un entorno de desarrollo .NET 9 completo
2. **Registrado** y configurado una aplicación en Azure AD
3. **Desarrollado** una aplicación web segura con autenticación OAuth 2.0
4. **Implementado** vistas completas y probado el flujo de autenticación
5. **Analizado** tokens JWT en profundidad y implementado debugging avanzado

## 🏆 Competencias Desarrolladas

- **Desarrollo Seguro**: Implementación de autenticación robusta
- **Azure AD**: Configuración y administración de identidades
- **OAuth 2.0/OpenID Connect**: Comprensión profunda de protocolos
- **JWT**: Análisis y validación de tokens
- **.NET 9**: Desarrollo moderno con ASP.NET Core
- **Testing**: Verificación completa de funcionalidades de seguridad

## 📧 Soporte

Para soporte técnico o preguntas sobre los laboratorios:
- Consultar la documentación incluida en cada laboratorio
- Revisar las secciones de troubleshooting
- Consultar los recursos adicionales proporcionados

---

**Desarrollado para**: Curso de Diseño Seguro de Aplicaciones (.NET en Azure) - Sesión 4  
**Versión**: 1.0  
**Última actualización**: Julio 2025  
**Framework**: .NET 9  
**Plataforma**: Azure Active Directory 