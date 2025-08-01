# 🛠️ LABORATORIO 0: VERIFICACIÓN Y CONFIGURACIÓN DEL ENTORNO

## 🎯 Objetivo
Preparar entorno completo para desarrollo de aplicación segura SecureShop aplicando principios de **Secure-by-Design** desde la configuración inicial.

## ⏱️ Duración: 15 minutos

## 📋 Conceptos Clave del Desarrollo Seguro

### 🔒 Secure-by-Design Mindset
> *"Es mucho más fácil construir sobre una base segura que retro-arreglar seguridad en una aplicación existente"*

El enfoque **Secure-by-Design** significa que cada decisión de configuración considera no solo funcionalidad inmediata sino también implicaciones de seguridad a largo plazo. No agregamos seguridad al final como una idea tardía, sino que cada decisión de diseño se evalúa a través del lente de seguridad.

### 🏗️ Fundamentos de Arquitectura Empresarial
Las herramientas que configuraremos representan lo mejor de la tecnología moderna, equilibrando capacidades avanzadas con estabilidad empresarial. Cada elección tecnológica ha sido tomada considerando:

- **Resistencia a ataques conocidos**
- **Capacidad de cumplir estándares regulatorios**
- **Facilidad de auditoría y monitoreo**
- **Escalabilidad empresarial**

## 📦 Herramientas Requeridas

### .NET Core 9 SDK
- **Propósito de Seguridad**: Incluye características de seguridad que no existían en versiones anteriores
- **Características**: Mejor gestión de secretos, protección mejorada contra ataques de temporización, validación automática más robusta
- **Instalación**:
```powershell
choco install dotnet-9.0-sdk -y
```

### Azure CLI
- **Propósito de Seguridad**: Gestión segura de recursos Azure con autenticación integrada
- **Características**: Comandos auditables, integración con Azure AD, gestión de identidades
- **Instalación**:
```powershell
choco install azure-cli -y
```

### Visual Studio Code + Extensiones
- **Propósito de Seguridad**: Desarrollo con análisis de código estático y herramientas de seguridad integradas
- **Extensiones Críticas**:
  - **C# Dev Kit**: Análisis de código con reglas de seguridad
  - **Azure Account**: Gestión segura de identidades Azure
  - **Azure Resources**: Monitoreo de recursos con perspectiva de seguridad
  - **REST Client**: Testing seguro de APIs

## 🛡️ Principios de Configuración Segura

### 1. Principio de Menor Privilegio
Cada herramienta y servicio tendrá exactamente los permisos mínimos necesarios para funcionar, nada más.

### 2. Defensa en Profundidad (Defense in Depth)
Múltiples capas de seguridad donde el compromiso de una capa no resulta en compromiso total del sistema:
- **Capa 1 - Network**: HTTPS + WAF
- **Capa 2 - Identity**: Azure AD + MFA  
- **Capa 3 - Application**: Input validation + CSRF protection
- **Capa 4 - Data**: Encryption at rest + in transit
- **Capa 5 - Monitoring**: Logging + alertas

### 3. Auditoría Total
Cada acción significativa será registrada para investigaciones forenses y cumplimiento regulatorio.

## 🚀 Pasos de Configuración

### Paso 1: Verificación de Chocolatey (3 minutos)

**Concepto**: Gestión de paquetes con verificación de integridad y fuentes confiables.

```powershell
# Verificar si Chocolatey está instalado
choco --version

# Si NO está instalado, ejecutar con política de seguridad temporal:
Set-ExecutionPolicy Bypass -Scope Process -Force
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
```

### Paso 2: Instalación de .NET Core 9 y Herramientas (5 minutos)

**Concepto**: Stack tecnológico con seguridad integrada desde el framework base.

```powershell
# Instalar .NET Core 9 SDK (última versión con características de seguridad)
choco install dotnet-9.0-sdk -y

# Instalar Azure CLI para gestión segura de recursos
choco install azure-cli -y

# Instalar Git con configuración segura
choco install git -y

# Refrescar variables de entorno
refreshenv

# Verificar instalaciones
dotnet --version    # Debe mostrar: 9.0.x
az --version
git --version
```

### Paso 3: Configuración de Visual Studio Code (4 minutos)

**Concepto**: IDE con herramientas de análisis de seguridad y desarrollo seguro integradas.

```powershell
# Instalar VS Code
choco install vscode -y

# Instalar extensiones críticas para desarrollo seguro
code --install-extension ms-dotnettools.csdevkit
code --install-extension ms-vscode.azure-account  
code --install-extension ms-azuretools.vscode-azureresourcegroups
code --install-extension ms-vscode.azurecli
code --install-extension humao.rest-client
```

### Paso 4: Verificación de Acceso Azure (3 minutos)

**Concepto**: Autenticación segura y verificación de permisos antes del desarrollo.

```powershell
# Login seguro a Azure con MFA
az login

# Verificar suscripciones disponibles
az account list --output table

# Verificar permisos en grupo de desarrollo seguro
az ad group member list --group "gu_desarrollo_seguro_aplicacion" --output table
```

## ✅ Verificación de Configuración Exitosa

Al completar este laboratorio, debe tener:

- [ ] .NET Core 9 instalado y funcionando
- [ ] Azure CLI configurado con autenticación MFA
- [ ] VS Code con extensiones de seguridad instaladas
- [ ] Acceso verificado a recursos Azure
- [ ] Comprensión de principios Secure-by-Design

## 🎯 Valor Profesional Generado

**Portfolio Evidence**: Configuración de entorno de desarrollo empresarial seguro  
**Skills Advancement**: Competencias en herramientas Azure y desarrollo seguro  
**Security Mindset**: Mentalidad de seguridad desde la configuración inicial  
**Tool Proficiency**: Dominio de herramientas estándar de la industria

## 🔗 Conexión con Laboratorios Siguientes

Esta configuración base será fundamental para:
- **Lab 34**: Diseño de arquitectura con herramientas configuradas
- **Lab 35**: Desarrollo .NET Core con SDK optimizado
- **Lab 36**: Integración Azure AD con CLI configurado
- **Lab 37**: Gestión Key Vault con accesos verificados

---

> **💡 Recuerda**: Cada decisión de configuración considera no solo funcionalidad inmediata sino también implicaciones de seguridad a largo plazo. Es la diferencia entre construir una casa y luego instalar cerraduras, versus diseñar una fortaleza desde los cimientos.