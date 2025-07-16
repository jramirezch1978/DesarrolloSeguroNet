# 🧪 LABORATORIO 3: IMPLEMENTACIÓN DE VISTAS Y TESTING

⏱️ **Duración**: 25 minutos  
🎯 **Objetivo**: Crear vistas para autenticación y probar el flujo completo con .NET 9  
🔧 **Tecnologías**: .NET 9, ASP.NET Core MVC, Microsoft.Identity.Web, Azure AD, Bootstrap 5  

## 📋 Prerrequisitos

- **Laboratorio 0** completado exitosamente
- **Laboratorio 1** completado exitosamente 
- **Laboratorio 2** completado exitosamente
- Información de Azure AD configurada y guardada
- Visual Studio Code con extensiones instaladas

## 📝 Objetivos de Aprendizaje

- ✅ Implementar vistas avanzadas para autenticación
- ✅ Probar el flujo completo de OAuth 2.0/OpenID Connect
- ✅ Mostrar información detallada de tokens JWT
- ✅ Crear interfaz de usuario responsive con Bootstrap 5
- ✅ Implementar navegación condicional basada en autenticación
- ✅ Validar estados de sesión y claims de usuario

## 🚀 Pasos del Laboratorio

### Paso 1: Modificar Layout Principal para .NET 9 (8 minutos)

El layout actualizado incluye:
- Navbar con diseño moderno usando Bootstrap 5
- Navegación condicional basada en estado de autenticación
- Información del usuario en dropdown
- Footer con información del sistema

**Características del Layout:**
- Tema oscuro (`bg-dark`) para el navbar
- Iconos emoji para mejor UX
- Dropdown con opciones de perfil y logout
- Información dinámica del estado de autenticación

### Paso 2: Crear Vista de Perfil Avanzada (8 minutos)

La vista de perfil incluye:
- **Información del Usuario**: Nombre, email, IDs únicos
- **Estado de Autenticación**: Método, framework, tiempo de auth
- **Claims Completos**: Tabla interactiva con todos los claims
- **Información de Seguridad**: Estadísticas y acciones disponibles
- **Información Técnica**: Detalles del servidor y sistema

**Funcionalidades Especiales:**
- Click en claims para copiar al portapapeles
- Tabla responsive con ordenamiento
- Badges y colores para diferenciación

### Paso 3: Actualizar Vista Home para mostrar estado de autenticación (4 minutos)

La vista Home incluye:
- **Estado Autenticado**: Resumen de sesión con acciones disponibles
- **Estado No Autenticado**: Información de seguridad y proceso de auth
- **Información del Sistema**: Detalles técnicos del servidor
- **Características de Seguridad**: Explicación de OAuth 2.0 y tecnologías

### Paso 4: Vista de Sesión Cerrada (SignedOut)

Vista que confirma el cierre de sesión seguro:
- Confirmación visual con iconos
- Información de seguridad del logout
- Acciones para regresar o autenticarse nuevamente

## 🎨 Características de la Interfaz

### Navegación Principal
- **Navbar responsive** con Bootstrap 5
- **Navegación condicional** basada en autenticación
- **Dropdown de usuario** con opciones contextuales
- **Botón de login** prominente para usuarios no autenticados

### Vistas Implementadas
- **Home/Index.cshtml**: Página principal con estado dinámico
- **Account/Profile.cshtml**: Perfil completo con claims e información
- **Account/SignedOut.cshtml**: Confirmación de logout
- **Account/TokenInfo.cshtml**: Análisis de tokens JWT (adicional)
- **Account/TestAuthentication.cshtml**: Herramientas de testing (adicional)
- **Account/ClaimsAnalysis.cshtml**: Análisis avanzado de claims (adicional)

### Funcionalidades Extendidas
- **Análisis de Tokens**: Vista detallada de información JWT
- **Testing de Autenticación**: Herramientas para debugging
- **Análisis de Claims**: Categorización y análisis de claims
- **Exportación de Datos**: Descarga de tokens e información

## 📊 Testing Completo del Flujo de Autenticación

### Secuencia de Testing:

1. **🏠 Página de Inicio:**
   - ✅ Verificar estado "No Autenticado"
   - ✅ Verificar información del sistema (.NET 9)
   - ✅ Click en "Iniciar Sesión con Azure AD"

2. **🔄 Redirección a Azure AD:**
   - ✅ Verificar URL contiene login.microsoftonline.com
   - ✅ Ingresar credenciales de usuario invitado
   - ✅ Aceptar permisos si se solicita

3. **↩️ Redirección de Vuelta:**
   - ✅ Verificar regreso a https://localhost:7001
   - ✅ Verificar que aparece "Hola, [nombre]" en navbar
   - ✅ Verificar estado "Autenticado" en página inicio

4. **👤 Página de Perfil:**
   - ✅ Click en "Ver Mi Perfil Completo"
   - ✅ Verificar información de usuario completa
   - ✅ Verificar tabla de claims poblada
   - ✅ Verificar información técnica (.NET 9)

5. **🔍 Información de Tokens:**
   - ✅ Click en "Analizar Tokens JWT"
   - ✅ Verificar información detallada de tokens
   - ✅ Verificar claims específicos mostrados

6. **🚪 Logout:**
   - ✅ Click en "Cerrar Sesión"
   - ✅ Verificar redirección a Azure AD logout
   - ✅ Verificar página "Sesión Cerrada Exitosamente"
   - ✅ Verificar que estado vuelve a "No Autenticado"

## 📁 Estructura del Proyecto

```
DevSeguroWebApp/
├── Controllers/
│   ├── AccountController.cs      # Controlador de autenticación extendido
│   └── HomeController.cs         # Controlador principal
├── Views/
│   ├── Account/
│   │   ├── Profile.cshtml         # Vista de perfil completa
│   │   ├── SignedOut.cshtml       # Vista de cierre de sesión
│   │   ├── TokenInfo.cshtml       # Análisis de tokens (adicional)
│   │   ├── TestAuthentication.cshtml # Testing (adicional)
│   │   └── ClaimsAnalysis.cshtml  # Análisis de claims (adicional)
│   ├── Home/
│   │   └── Index.cshtml           # Página principal actualizada
│   └── Shared/
│       └── _Layout.cshtml         # Layout principal con navegación
├── wwwroot/                       # Recursos estáticos
├── appsettings.json              # Configuración Azure AD
├── Program.cs                    # Configuración de la aplicación
└── DevSeguroWebApp.csproj        # Archivo de proyecto
```

## 🔧 Configuración y Ejecución

### Compilar y Ejecutar:
```bash
# En la terminal integrada de VS Code
dotnet build
dotnet run
```

### Verificar la Aplicación:
- ✅ Aplicación ejecutándose en: `https://localhost:7001`
- ✅ Aceptar advertencia de certificado (desarrollo local)
- ✅ Verificar página de inicio actualizada

## 🛡️ Características de Seguridad

### OAuth 2.0 / OpenID Connect:
- **Authorization Code Flow** con PKCE
- **Tokens JWT firmados** por Azure AD
- **Refresh tokens seguros**
- **Validación automática de tokens**

### Tecnologías Utilizadas:
- **.NET 9** (última versión)
- **Microsoft.Identity.Web 3.3.0**
- **Azure Active Directory**
- **Bootstrap 5** para UI
- **Font Awesome** para iconos

## ✅ Resultado Esperado

Al completar este laboratorio, tendrás:

- **Interfaz de usuario moderna** y responsive con Bootstrap 5
- **Flujo de autenticación funcionando** perfectamente end-to-end
- **Información detallada del usuario** y claims visible
- **Estados de autenticación** claramente diferenciados
- **Información técnica de .NET 9** visible en todas las vistas
- **Logout seguro** funcionando correctamente
- **Herramientas de testing** y análisis de tokens
- **Navegación condicional** basada en estado de autenticación

## 🐛 Troubleshooting

### Error: "No se puede cargar la página"
**Solución**: Verificar que la aplicación esté ejecutándose en el puerto correcto

### Error: "Redirect URI mismatch"
**Solución**: Verificar que las URIs en Azure AD coincidan con las configuradas

### Error: "Authentication failed"
**Solución**: Verificar TenantId y ClientId en appsettings.json

### Error: "Claims vacíos"
**Solución**: Verificar permisos en Azure AD (email, profile, openid)

## 🔄 Próximos Pasos

Una vez completado este laboratorio, continuar con:
- **Laboratorio 4**: Análisis avanzado de tokens JWT y debugging
- **Implementación de autorización** basada en roles
- **Personalización de claims** y scopes
- **Implementación de refresh tokens**

---

**Resultado esperado**: Aplicación .NET 9 con interfaz completa, flujo de autenticación funcional y herramientas de testing implementadas. 