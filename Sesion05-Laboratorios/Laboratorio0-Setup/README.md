# 🛠️ Laboratorio 0: Verificación y Configuración del Entorno

## 📋 Información del Laboratorio
- **Duración**: 10 minutos
- **Objetivo**: Verificar configuraciones previas y preparar entorno para laboratorios avanzados
- **Prerrequisitos**: .NET 9, Visual Studio Code, Azure CLI

## 🎯 Objetivos Específicos
- Verificar que tienen el proyecto base de la Sesión 4
- Instalar paquetes Azure adicionales necesarios
- Verificar acceso y permisos en Azure Portal
- Preparar entorno para laboratorios de Data Protection y Key Vault

## 📝 Paso 1: Verificación de Configuraciones Previas (3 minutos)

### ✅ Verificar .NET 9
```bash
# Verificar versión de .NET
dotnet --version
# Debe mostrar: 9.0.x
```

### ✅ Verificar Proyecto Anterior
```bash
# Navegar al proyecto (si existe de Sesión 4)
cd DevSeguroApp/DevSeguroWebApp
dir
# Debe mostrar estructura del proyecto anterior
```

### 🔧 Setup Rápido (si NO tienen proyecto de Sesión 4)
```bash
# Crear estructura base
mkdir DevSeguroApp
cd DevSeguroApp
dotnet new mvc -n DevSeguroWebApp --framework net9.0
cd DevSeguroWebApp

# Instalar paquetes básicos
dotnet add package Microsoft.Identity.Web --version 3.2.0
```

## 📦 Paso 2: Instalar Paquetes Adicionales para Sesión 5 (4 minutos)

### Azure Key Vault Packages
```bash
# Azure Key Vault y Data Protection packages
dotnet add package Azure.Security.KeyVault.Keys --version 4.6.0
dotnet add package Azure.Security.KeyVault.Secrets --version 4.6.0
dotnet add package Azure.Security.KeyVault.Certificates --version 4.6.0
dotnet add package Azure.Identity --version 1.12.0
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets --version 1.3.2

# Data Protection con Azure Storage
dotnet add package Microsoft.AspNetCore.DataProtection.AzureStorage --version 9.0.0
dotnet add package Microsoft.AspNetCore.DataProtection.AzureKeyVault --version 9.0.0
```

### Verificar Packages Instalados
```bash
# Verificar que todos los paquetes se instalaron correctamente
dotnet list package
```

### 📋 Lista de Packages Esperados
Al ejecutar `dotnet list package`, debe aparecer:

| Package | Version | Descripción |
|---------|---------|-------------|
| Azure.Security.KeyVault.Keys | 4.6.0 | Gestión de claves de Key Vault |
| Azure.Security.KeyVault.Secrets | 4.6.0 | Gestión de secretos de Key Vault |
| Azure.Security.KeyVault.Certificates | 4.6.0 | Gestión de certificados de Key Vault |
| Azure.Identity | 1.12.0 | Autenticación con Azure |
| Azure.Extensions.AspNetCore.Configuration.Secrets | 1.3.2 | Configuration Provider para Key Vault |
| Microsoft.AspNetCore.DataProtection.AzureStorage | 9.0.0 | Data Protection con Azure Storage |
| Microsoft.AspNetCore.DataProtection.AzureKeyVault | 9.0.0 | Data Protection con Key Vault |
| Microsoft.Identity.Web | 3.2.0 | Integración con Azure AD |

## 🌐 Paso 3: Verificar Acceso a Azure Portal (3 minutos)

### 1. Acceso al Portal
1. **Navegar a Azure Portal**:
   - URL: https://portal.azure.com
   - Iniciar sesión con usuario invitado

### 2. Verificar Permisos de Grupos
2. **Verificar Membresía de Grupo**:
   - Azure Active Directory → Groups
   - Buscar: "gu_desarrollo_seguro_aplicacion"
   - Verificar que aparecen como miembro

### 3. Verificar Permisos de Key Vault
3. **Verificar Capacidad de Crear Recursos**:
   - Buscar "Key vaults" en portal
   - Verificar que pueden crear recursos (botón "+ Create" disponible)

### 🚨 Importante
**Si no tienen acceso**: Notificar al instructor inmediatamente.

## 🧪 Verificación de Setup Completo

### Script de Verificación Automatizada
Crear archivo `verify-setup.ps1`:

```powershell
# Verificación de Setup - Laboratorio Sesión 5
Write-Host "=== Verificación de Setup - Sesión 5 ===" -ForegroundColor Green

# Verificar .NET 9
Write-Host "1. Verificando .NET 9..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version
if ($dotnetVersion -like "9.*") {
    Write-Host "✅ .NET $dotnetVersion detectado" -ForegroundColor Green
} else {
    Write-Host "❌ .NET 9 no encontrado. Versión actual: $dotnetVersion" -ForegroundColor Red
}

# Verificar Azure CLI
Write-Host "2. Verificando Azure CLI..." -ForegroundColor Yellow
try {
    $azVersion = az --version | Select-String "azure-cli" | Select-Object -First 1
    Write-Host "✅ Azure CLI detectado" -ForegroundColor Green
} catch {
    Write-Host "❌ Azure CLI no encontrado" -ForegroundColor Red
}

# Verificar proyecto existe
Write-Host "3. Verificando estructura de proyecto..." -ForegroundColor Yellow
if (Test-Path "DevSeguroWebApp.csproj") {
    Write-Host "✅ Proyecto DevSeguroWebApp encontrado" -ForegroundColor Green
    
    # Verificar packages críticos
    Write-Host "4. Verificando packages Azure..." -ForegroundColor Yellow
    $packages = dotnet list package
    
    $requiredPackages = @(
        "Azure.Security.KeyVault.Keys",
        "Azure.Security.KeyVault.Secrets",
        "Azure.Identity",
        "Microsoft.AspNetCore.DataProtection.AzureStorage"
    )
    
    foreach ($package in $requiredPackages) {
        if ($packages -like "*$package*") {
            Write-Host "✅ $package instalado" -ForegroundColor Green
        } else {
            Write-Host "❌ $package NO instalado" -ForegroundColor Red
        }
    }
} else {
    Write-Host "❌ Proyecto no encontrado en directorio actual" -ForegroundColor Red
}

# Verificar conectividad a Azure
Write-Host "5. Verificando conectividad Azure..." -ForegroundColor Yellow
try {
    $azAccount = az account show --query "name" -o tsv 2>$null
    if ($azAccount) {
        Write-Host "✅ Conectado a Azure: $azAccount" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Azure CLI no autenticado. Ejecutar: az login" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️  Verificar conexión a Azure" -ForegroundColor Yellow
}

Write-Host "=== Verificación Completada ===" -ForegroundColor Green
```

### Ejecutar Verificación
```bash
# En PowerShell
powershell -ExecutionPolicy Bypass -File verify-setup.ps1

# En Bash/Terminal
# Convertir a script bash si es necesario
```

## ✅ Checklist de Completación

Marcar cuando esté completado:

- [ ] ✅ .NET 9 verificado y funcionando
- [ ] ✅ Proyecto base creado o existente
- [ ] ✅ Packages Azure Key Vault instalados
- [ ] ✅ Packages Data Protection instalados  
- [ ] ✅ Azure CLI instalado y funcionando
- [ ] ✅ Acceso a Azure Portal verificado
- [ ] ✅ Permisos de grupo confirmados
- [ ] ✅ Capacidad de crear Key Vault verificada
- [ ] ✅ Script de verificación ejecutado exitosamente

## 🚨 Troubleshooting Común

### Error: "dotnet command not found"
**Solución**: 
```bash
# Instalar .NET 9 SDK
# Windows: Descargar desde https://dotnet.microsoft.com/download
# Linux: sudo apt-get install dotnet-sdk-9.0
# macOS: brew install dotnet
```

### Error: "az command not found"
**Solución**:
```bash
# Windows con Chocolatey
choco install azure-cli -y

# Linux
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# macOS
brew install azure-cli
```

### Error: "Access denied" en Azure Portal
**Solución**:
1. Verificar que el instructor los haya añadido al grupo
2. Cerrar sesión y volver a iniciar sesión
3. Usar modo incógnito/privado en el navegador

### Error: "Package not found" al instalar
**Solución**:
```bash
# Limpiar caché y restaurar
dotnet clean
dotnet restore
dotnet add package [nombre-package] --version [version]
```

## 🎯 Resultado Esperado

Al completar este laboratorio, debe tener:

1. **Entorno .NET 9 Configurado**:
   - SDK instalado y funcionando
   - Proyecto ASP.NET Core creado

2. **Packages Azure Instalados**:
   - Todos los packages de Key Vault y Data Protection
   - Dependencies resueltas correctamente

3. **Acceso Azure Verificado**:
   - Azure CLI autenticado
   - Permisos de portal confirmados
   - Capacidad de crear recursos

4. **Setup Automatizado**:
   - Script de verificación funcionando
   - Checklist completado

## ➡️ Próximo Paso

Una vez completado exitosamente este laboratorio, proceder con:
**[Laboratorio 1: Implementación de Data Protection API Avanzada](../Laboratorio1-DataProtection/)**

---
⚡ **Nota Importante**: No proceder al siguiente laboratorio hasta completar todos los items del checklist. 