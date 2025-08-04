# 🔧 Laboratorio 0: Configuración y Verificación de Prerrequisitos

**Duración:** 5 minutos  
**Objetivo:** Confirmar que se completaron los laboratorios previos y verificar el entorno

## 📋 Verificación de Prerrequisitos

### Paso 1: Verificar Laboratorios Previos Completados

Verificar que tienes la aplicación SecureShop funcionando:

```powershell
# Navegar al directorio del proyecto
cd src/SecureShop.Web

# Verificar que la aplicación compila
dotnet build

# Verificar que las dependencias están instaladas
dotnet list package
```

### Paso 2: Crear Estructura Básica (SOLO si no tienes el proyecto)

Si NO tienes el proyecto base completado, ejecutar setup rápido:

```powershell
# Crear estructura básica
mkdir SecureShop
cd SecureShop
mkdir src, tests, docs

cd src
dotnet new sln -n SecureShop
dotnet new web -n SecureShop.Web
dotnet new classlib -n SecureShop.Security
dotnet new classlib -n SecureShop.Data
dotnet sln add **/*.csproj
```

### Paso 3: Agregar Paquetes Esenciales

```powershell
# En SecureShop.Web
cd SecureShop.Web
dotnet add package Microsoft.Identity.Web --version 3.2.0
dotnet add package Azure.Security.KeyVault.Secrets --version 4.7.0
dotnet add package Azure.Identity --version 1.12.1
dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 9.0.0

# En SecureShop.Security
cd ../SecureShop.Security
dotnet add package Azure.Security.KeyVault.Certificates --version 4.7.0
dotnet add package Azure.Security.KeyVault.Keys --version 4.7.0
dotnet add package Microsoft.Extensions.Logging.Abstractions --version 9.0.0
dotnet add package Microsoft.AspNetCore.Authorization --version 9.0.0

# En SecureShop.Data
cd ../SecureShop.Data
dotnet add package Microsoft.EntityFrameworkCore --version 9.0.0
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.0
```

## 🔐 Estructura del Proyecto

```
SecureShop/
├── src/
│   ├── SecureShop.Web/          # Aplicación web principal
│   ├── SecureShop.Security/     # Servicios de seguridad
│   └── SecureShop.Data/         # Modelos y contexto de datos
├── tests/                       # Proyectos de pruebas
├── docs/                       # Documentación
└── SecureShop.sln              # Archivo de solución
```

## 🎯 Estado Esperado

Antes de continuar con los siguientes laboratorios, asegúrate de tener:

- ✅ Proyecto SecureShop creado y compilando
- ✅ Dependencias de Azure instaladas
- ✅ Estructura de proyectos configurada
- ✅ Todos los paquetes NuGet restaurados

## 📦 Paquetes Principales Instalados

### SecureShop.Web
- **Microsoft.Identity.Web** (3.2.0) - Autenticación Azure AD
- **Azure.Security.KeyVault.Secrets** (4.7.0) - Gestión de secretos
- **Azure.Identity** (1.12.1) - Identidad de Azure
- **Microsoft.EntityFrameworkCore.InMemory** (9.0.0) - Base de datos en memoria

### SecureShop.Security
- **Azure.Security.KeyVault.Certificates** (4.7.0) - Certificados digitales
- **Azure.Security.KeyVault.Keys** (4.7.0) - Claves criptográficas
- **Microsoft.AspNetCore.Authorization** (9.0.0) - Autorización avanzada
- **Microsoft.Extensions.Logging.Abstractions** (9.0.0) - Logging

### SecureShop.Data
- **Microsoft.EntityFrameworkCore** (9.0.0) - ORM
- **Microsoft.EntityFrameworkCore.SqlServer** (9.0.0) - Proveedor SQL Server

## 🔍 Verificación Final

```powershell
# Compilar toda la solución
dotnet build

# Verificar estructura de archivos
dir
```

## 🚀 Próximos Pasos

Una vez completada la configuración, podrás proceder con:

1. **Lab 38** - Autenticación y Autorización Avanzada
2. **Lab 39** - Implementación de Firma Digital
3. **Lab 40** - Encriptación de Datos de Aplicación
4. **Lab 41** - Pruebas Integrales de Seguridad

## 📝 Configuración de Azure (Verificación)

```powershell
# Verificar login a Azure
az login

# Verificar membresía del grupo (si aplica)
az ad group member list --group "gu_desarrollo_seguro_aplicacion" --output table

# Verificar acceso a suscripción
az account show --output table
```

## ⚠️ Nota Importante

Si encuentras errores de vulnerabilidades en `Microsoft.Identity.Web` v3.2.0, esto es normal para el laboratorio. En producción, siempre usa las versiones más recientes y sin vulnerabilidades conocidas.

## 📚 Recursos Adicionales

- [Microsoft Identity Web Documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web)
- [Azure Key Vault Developer Guide](https://docs.microsoft.com/en-us/azure/key-vault/general/)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)