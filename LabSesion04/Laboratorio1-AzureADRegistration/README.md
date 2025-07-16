# 🧪 LABORATORIO 1: REGISTRO Y CONFIGURACIÓN DE APLICACIÓN EN AZURE AD

⏱️ **Duración**: 20 minutos  
🎯 **Objetivo**: Crear y configurar una aplicación .NET en Azure AD para autenticación OAuth 2.0/OpenID Connect  
🔧 **Tecnologías**: Azure Active Directory, OAuth 2.0, OpenID Connect  

## 📋 Prerrequisitos

- **Laboratorio 0** completado exitosamente
- Acceso a Azure Portal con permisos de administrador
- Cuenta de Azure AD configurada (de sesiones anteriores)
- Grupo "gu_desarrollo_seguro_aplicacion" creado

## 🔍 Verificación de Configuraciones Previas (5 minutos)

### ✅ De Sesión 1:
- [ ] Azure AD Tenant configurado con Security Defaults habilitados
- [ ] Usuario administrativo creado y funcional
- [ ] Grupo "gu_desarrollo_seguro_aplicacion" con todos los participantes

### ✅ De Sesión 2:
- [ ] Multi-Factor Authentication (MFA) configurado
- [ ] Conditional Access básico implementado

### ✅ De Sesión 3:
- [ ] Network Security Groups (NSGs) configurados
- [ ] Virtual Network (VNet) básica creada

### 🔧 Verificación Rápida (5 minutos)

1. **Acceso a Azure Portal**:
   - Navegar a: https://portal.azure.com
   - Iniciar sesión con credenciales de usuario invitado
   - Verificar que aparece en el tenant correcto

2. **Verificar Grupo de Desarrollo**:
   - Azure Active Directory → Groups
   - Buscar: "gu_desarrollo_seguro_aplicacion"
   - Verificar que está listado como miembro

3. **Configuración Básica Azure AD** (Si es necesario):
   - Azure Active Directory → Properties
   - Verificar que Security Defaults están habilitados
   - Si no están habilitados, activarlos ahora

## 🏗️ Paso 1: Crear App Registration (8 minutos)

### 1.1 Navegar a App Registrations

1. Ir a **Azure Portal** → **Azure Active Directory**
2. En el menú lateral, seleccionar **App registrations**
3. Hacer clic en **New registration**

### 1.2 Configurar Application Registration

**Configuración básica**:
- **Name**: `DevSeguroApp-[SuNombre]`
  - Ejemplo: `DevSeguroApp-Juan`
- **Supported account types**: 
  - ✅ **Accounts in this organizational directory only (Single tenant)**
- **Redirect URI**: 
  - **Platform**: Web
  - **URI**: `https://localhost:7001/signin-oidc`

### 1.3 Completar Registration

1. Hacer clic en **Register**
2. **⚠️ IMPORTANTE**: Anotar los siguientes valores:
   - **Application (client) ID**: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`
   - **Directory (tenant) ID**: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`

> 💡 **Consejo**: Crear un archivo de texto para guardar estos valores, los necesitará en laboratorios posteriores.

## 🔧 Paso 2: Configurar Authentication Settings (7 minutos)

### 2.1 Navegar a Authentication

1. En su **App Registration** → **Authentication**
2. Verificar que aparece la URI que configuró anteriormente

### 2.2 Configurar Redirect URIs

**Web Redirect URIs**:
- ✅ `https://localhost:7001/signin-oidc`
- ✅ `https://localhost:7001/signout-callback-oidc`

**Para agregar la segunda URI**:
1. Hacer clic en **Add URI**
2. Ingresar: `https://localhost:7001/signout-callback-oidc`

### 2.3 Configurar Logout URL

**Front-channel logout URL**:
- Ingresar: `https://localhost:7001/signout-oidc`

### 2.4 Configurar Token Settings

**Advanced Settings**:
- ✅ **ID tokens (used for implicit and hybrid flows)** - Habilitar
- ❌ **Access tokens (used for implicit flows)** - Mantener deshabilitado

**Allow public client flows**: **No**

### 2.5 Guardar Configuración

Hacer clic en **Save** para aplicar los cambios.

## 🔑 Paso 3: Configurar API Permissions (5 minutos)

### 3.1 Navegar a API Permissions

1. En su **App Registration** → **API permissions**
2. Verificar que aparece el permiso por defecto

### 3.2 Verificar Permissions por Defecto

**Microsoft Graph**:
- ✅ **User.Read (Delegated)** - Ya debe estar presente

### 3.3 Agregar Additional Permissions

1. Hacer clic en **Add a permission**
2. Seleccionar **Microsoft Graph**
3. Seleccionar **Delegated permissions**
4. Buscar y agregar los siguientes permisos:
   - ✅ **email**
   - ✅ **profile**
   - ✅ **openid** (normalmente ya está incluido)

### 3.4 Configurar Admin Consent (Opcional)

Si tiene permisos de administrador:
1. Hacer clic en **Grant admin consent for [TenantName]**
2. Confirmar la acción

## 📝 Paso 4: Crear Client Secret (Preparación para Laboratorio 2)

### 4.1 Navegar a Certificates & secrets

1. En su **App Registration** → **Certificates & secrets**
2. Seleccionar la pestaña **Client secrets**

### 4.2 Crear New Client Secret

1. Hacer clic en **New client secret**
2. **Configuración**:
   - **Description**: `DevSeguroApp-Secret`
   - **Expires**: **6 months**
3. Hacer clic en **Add**

### 4.3 Guardar Secret Value

**⚠️ CRÍTICO**: 
- Copiar inmediatamente el **VALUE** del secret
- Guardarlo en un lugar seguro
- **No podrá verlo nuevamente después de salir de esta página**

## 📋 Información para Laboratorios Posteriores

Crear un archivo `azure-ad-config.txt` con la siguiente información:

```
=== CONFIGURACIÓN AZURE AD - LABORATORIO 1 ===
Application (client) ID: [SU-CLIENT-ID-AQUÍ]
Directory (tenant) ID: [SU-TENANT-ID-AQUÍ]
Client Secret: [SU-CLIENT-SECRET-AQUÍ]

Redirect URIs:
- https://localhost:7001/signin-oidc
- https://localhost:7001/signout-callback-oidc

Logout URL:
- https://localhost:7001/signout-oidc

API Permissions:
- User.Read (Delegated)
- email (Delegated)
- profile (Delegated)
- openid (Delegated)
```

## ✅ Verificación Final

### Checklist de Completación:

- [ ] Aplicación registrada en Azure AD
- [ ] Client ID y Tenant ID documentados
- [ ] Redirect URIs configurados correctamente:
  - [ ] `https://localhost:7001/signin-oidc`
  - [ ] `https://localhost:7001/signout-callback-oidc`
- [ ] Front-channel logout URL configurado
- [ ] ID tokens habilitados
- [ ] API Permissions configurados:
  - [ ] User.Read
  - [ ] email
  - [ ] profile
  - [ ] openid
- [ ] Client Secret generado y guardado
- [ ] Archivo de configuración creado

### Verificación en Azure Portal:

1. **Overview**: Verificar que se muestra la información básica de la aplicación
2. **Authentication**: Verificar que aparecen las 2 redirect URIs
3. **API permissions**: Verificar que aparecen los 4 permisos
4. **Certificates & secrets**: Verificar que aparece el secret creado

## 🚨 Troubleshooting Común

### Error: "No tengo permisos para crear App Registration"
**Solución**: 
- Verificar que tiene permisos de administrador en Azure AD
- Contactar al administrador del tenant para obtener permisos

### Error: "Redirect URI no válida"
**Solución**:
- Verificar que la URI está exactamente como: `https://localhost:7001/signin-oidc`
- Verificar que el puerto es 7001, no 7000

### Error: "No puedo ver el Client Secret"
**Solución**:
- El secret solo se muestra una vez al crearlo
- Si no lo guardó, debe crear un nuevo secret

## 🔄 Próximos Pasos

Una vez completado este laboratorio, continuar con:
- **Laboratorio 2**: Desarrollo de aplicación .NET 9 con OAuth 2.0
- **Laboratorio 3**: Implementación de vistas y testing
- **Laboratorio 4**: Análisis avanzado de tokens JWT

---

**Importante**: Guarde toda la información de configuración en un lugar seguro. La necesitará en los laboratorios posteriores para configurar la aplicación .NET. 