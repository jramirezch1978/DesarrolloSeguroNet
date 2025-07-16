# 🛠️ LABORATORIO 0: CONFIGURACIÓN DEL ENTORNO DE DESARROLLO

⏱️ **Duración**: 15 minutos
🎯 **Objetivo**: Instalar y configurar todas las herramientas necesarias para el desarrollo
🔧 **Tecnologías**: Chocolatey, .NET 9, Visual Studio Code

## 📋 Prerrequisitos

- Sistema operativo Windows 10/11
- Permisos de administrador
- Conexión a internet estable

## 🚀 Paso 1: Instalación de Chocolatey (5 minutos)

Chocolatey es un gestor de paquetes para Windows que facilita la instalación de herramientas de desarrollo.

### 1.1 Abrir PowerShell como Administrador

- **Método 1**: Click derecho en el botón de Windows → Windows PowerShell (Admin)
- **Método 2**: Presionar `Win + X` → Windows PowerShell (Admin)

### 1.2 Verificar Política de Ejecución

```powershell
Get-ExecutionPolicy
```

Si aparece "Restricted", ejecutar:

```powershell
Set-ExecutionPolicy AllSigned
```

### 1.3 Instalar Chocolatey

```powershell
Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
```

### 1.4 Verificar Instalación

```powershell
choco --version
```

**Resultado esperado**: `2.2.2` (o versión más reciente)

## 🔧 Paso 2: Instalación de .NET 9 Core (3 minutos)

### 2.1 Instalar .NET 9 SDK

```powershell
choco install dotnet-9.0-sdk -y
```

### 2.2 Verificar Instalación

```powershell
# Cerrar y reabrir PowerShell (como usuario normal)
dotnet --version
```

**Resultado esperado**: `9.0.x` (donde x es el número de build)

### 2.3 Verificar SDKs Instalados

```powershell
dotnet --list-sdks
```

**Resultado esperado**: .NET 9.0.x listado

## 📝 Paso 3: Instalación de Visual Studio Code (2 minutos)

### 3.1 Instalar VS Code

```powershell
choco install vscode -y
```

### 3.2 Verificar Instalación

```powershell
code --version
```

## 🧩 Paso 4: Instalación de Extensiones Esenciales para VS Code (5 minutos)

### 4.1 Abrir Visual Studio Code

```powershell
code
```

### 4.2 Instalar Extensiones desde la Terminal

Presionar `Ctrl + Shift + P` y escribir "Terminal: Create New Terminal"

```bash
# Extensión principal para C# y .NET
code --install-extension ms-dotnettools.csharp

# IntelliSense mejorado para C#
code --install-extension ms-dotnettools.csdevkit

# Soporte para Azure
code --install-extension ms-vscode.vscode-node-azure-pack

# Herramientas para desarrollo web
code --install-extension ms-vscode.vscode-json

# Git integration mejorada
code --install-extension eamodio.gitlens

# Resaltado de sintaxis mejorado
code --install-extension ms-vscode.PowerShell

# Debugger para .NET
code --install-extension ms-dotnettools.vscode-dotnet-runtime
```

### 4.3 Verificar Extensiones Instaladas

- Presionar `Ctrl + Shift + X` (abrir panel de extensiones)
- Verificar que aparezcan instaladas:
  - ✅ C# Dev Kit
  - ✅ C#
  - ✅ Azure Account
  - ✅ Azure Resources
  - ✅ GitLens
  - ✅ PowerShell

### 4.4 Configurar VS Code para .NET 9

- Presionar `Ctrl + Shift + P`
- Escribir: ".NET: New Project"
- Si aparece la opción, significa que .NET está correctamente integrado

## ✅ Verificación Final del Entorno

Ejecutar estos comandos para verificar que todo está funcionando:

```powershell
# Verificar Chocolatey
choco --version

# Verificar .NET 9
dotnet --version
dotnet --info

# Verificar VS Code
code --version

# Crear proyecto de prueba
mkdir test-dotnet9
cd test-dotnet9
dotnet new console
dotnet run
```

**Resultado esperado del proyecto de prueba**:

```
Hello, World!
```

## 🚨 Troubleshooting Común

### Error: "choco no se reconoce como comando"

**Solución**:

1. Cerrar y reabrir PowerShell como administrador
2. Reiniciar Windows si es necesario

### Error: ".NET 9 no se encuentra"

**Solución**:

1. Reiniciar VS Code
2. Verificar variables de entorno PATH
3. Reinstalar: `choco uninstall dotnet-9.0-sdk && choco install dotnet-9.0-sdk -y`

### Error: "Extensiones de C# no funcionan"

**Solución**:

1. Reiniciar VS Code
2. `Ctrl + Shift + P` → "Developer: Reload Window"
3. Verificar que .NET 9 SDK está instalado: `dotnet --version`

## 📋 Checklist de Completación

- [ ] Chocolatey instalado y funcionando
- [ ] .NET 9 SDK instalado y verificado
- [ ] Visual Studio Code instalado
- [ ] Extensiones de C# y Azure instaladas
- [ ] Proyecto de prueba "Hello, World!" funcionando
- [ ] Entorno listo para desarrollo seguro

## 🔄 Próximos Pasos

Una vez completado este laboratorio, continuar con:

- **Laboratorio 1**: Registro y configuración de aplicación en Azure AD
- **Laboratorio 2**: Desarrollo de aplicación .NET 9 con OAuth 2.0

---

**Nota**: Este laboratorio debe completarse antes de proceder con los laboratorios posteriores, ya que establece el entorno de desarrollo necesario para todo el curso.
