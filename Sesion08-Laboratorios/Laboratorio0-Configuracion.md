# 🛠️ Laboratorio 0: Verificación y Configuración del Entorno

## ⏱️ Duración: 15 minutos
## 🎯 Objetivo: Preparar entorno completo para laboratorios de Security Assessment y Vulnerability Scanning

---

## 📋 Prerrequisitos
- Windows 10/11 con PowerShell como Administrador
- Acceso a Internet para descargas
- Cuenta de Azure con permisos de Security Center
- Mínimo 4GB RAM disponible

---

## 🚀 Paso 1: Instalación de Chocolatey (3 minutos)

### Verificar si Chocolatey está instalado:
```powershell
# Abrir PowerShell como Administrador
choco --version
```

### Si NO está instalado, ejecutar:
```powershell
# Cambiar política de ejecución temporalmente
Set-ExecutionPolicy Bypass -Scope Process -Force

# Instalar Chocolatey
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))

# Verificar instalación
choco --version
```

**✅ Verificación:** Debe mostrar la versión de Chocolatey instalada.

---

## 🔧 Paso 2: Instalación de .NET Core 9 y Herramientas (5 minutos)

### Instalar herramientas principales:
```powershell
# Instalar .NET Core 9 SDK
choco install dotnet-9.0-sdk -y

# Instalar Azure CLI
choco install azure-cli -y

# Instalar Git (si no está instalado)
choco install git -y

# Instalar herramientas adicionales de seguridad
choco install nmap -y
choco install postman -y

# Refrescar variables de entorno
refreshenv
```

### Verificar instalaciones:
```powershell
# Verificar .NET Core 9
dotnet --version
# Debe mostrar: 9.0.x

# Verificar Azure CLI
az --version

# Verificar Git
git --version

# Verificar Nmap
nmap --version
```

**✅ Verificación:** Todas las herramientas deben mostrar sus versiones correctamente.

---

## 💻 Paso 3: Configuración de Visual Studio Code (4 minutos)

### Instalar VS Code (si no está instalado):
```powershell
choco install vscode -y
```

### Extensiones requeridas para VS Code:
Abrir VS Code y instalar las siguientes extensiones:

1. **C# Dev Kit** (Microsoft) - ID: `ms-dotnettools.csdevkit`
2. **Azure Account** (Microsoft) - ID: `ms-vscode.azure-account`
3. **Azure Resources** (Microsoft) - ID: `ms-azuretools.vscode-azureresourcegroups`
4. **Azure CLI Tools** (Microsoft) - ID: `ms-vscode.azurecli`
5. **REST Client** (Huachao Mao) - ID: `humao.rest-client`
6. **Azure Security Center** (Microsoft) - ID: `ms-azuretools.vscode-azuresecuritycenter`

### Comando alternativo para instalar extensiones:
```powershell
# Desde línea de comandos
code --install-extension ms-dotnettools.csdevkit
code --install-extension ms-vscode.azure-account
code --install-extension ms-azuretools.vscode-azureresourcegroups
code --install-extension ms-vscode.azurecli
code --install-extension humao.rest-client
code --install-extension ms-azuretools.vscode-azuresecuritycenter
```

**✅ Verificación:** Todas las extensiones deben aparecer como instaladas en VS Code.

---

## 🔐 Paso 4: Verificación de Acceso Azure (3 minutos)

### Autenticación con Azure:
```powershell
# Login a Azure
az login

# Verificar suscripciones disponibles
az account list --output table

# Verificar grupo de usuarios (si aplica)
az ad group member list --group "gu_desarrollo_seguro_aplicacion" --output table
```

### Verificar permisos en Azure Portal:
1. Navegar a: https://portal.azure.com
2. Verificar acceso como usuario invitado
3. Confirmar permisos para Security Center

**✅ Verificación:** Debe poder acceder al portal y ver sus suscripciones.

---

## 🧪 Paso 5: Verificación Final del Entorno

### Script de verificación completa:
```powershell
# Crear script de verificación
$verificationScript = @"
Write-Host "=== VERIFICACIÓN DEL ENTORNO ===" -ForegroundColor Green

# Verificar .NET
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET Core: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ .NET Core no encontrado" -ForegroundColor Red
}

# Verificar Azure CLI
try {
    $azVersion = az version --output json | ConvertFrom-Json
    Write-Host "✅ Azure CLI: $($azVersion.'azure-cli')" -ForegroundColor Green
} catch {
    Write-Host "❌ Azure CLI no encontrado" -ForegroundColor Red
}

# Verificar Git
try {
    $gitVersion = git --version
    Write-Host "✅ Git: $gitVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Git no encontrado" -ForegroundColor Red
}

# Verificar Nmap
try {
    $nmapVersion = nmap --version | Select-Object -First 1
    Write-Host "✅ Nmap: $nmapVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Nmap no encontrado" -ForegroundColor Red
}

# Verificar VS Code
try {
    $vscodeVersion = code --version | Select-Object -First 1
    Write-Host "✅ VS Code: $vscodeVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ VS Code no encontrado" -ForegroundColor Red
}

# Verificar conexión Azure
try {
    $account = az account show --output json | ConvertFrom-Json
    Write-Host "✅ Azure: Conectado como $($account.user.name)" -ForegroundColor Green
    Write-Host "   Suscripción: $($account.name)" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Azure: No conectado" -ForegroundColor Red
}

Write-Host "`n=== VERIFICACIÓN COMPLETADA ===" -ForegroundColor Green
"@

# Guardar y ejecutar script
$verificationScript | Out-File -FilePath "verify-environment.ps1" -Encoding UTF8
.\verify-environment.ps1
```

**✅ Verificación Final:** Todos los componentes deben mostrar ✅ verde.

---

## 🚨 Troubleshooting Común

### Error: "Chocolatey no se reconoce como comando"
**Solución:**
```powershell
# Cerrar y abrir PowerShell como Administrador
# O ejecutar:
refreshenv
```

### Error: "Política de ejecución restringida"
**Solución:**
```powershell
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Error: "Azure CLI no encontrado"
**Solución:**
```powershell
# Reinstalar Azure CLI
choco uninstall azure-cli -y
choco install azure-cli -y
refreshenv
```

### Error: "No se pueden instalar extensiones de VS Code"
**Solución:**
1. Abrir VS Code manualmente
2. Ir a Extensiones (Ctrl+Shift+X)
3. Buscar e instalar cada extensión individualmente

---

## 📊 Checklist de Verificación

- [ ] Chocolatey instalado y funcionando
- [ ] .NET Core 9 SDK instalado
- [ ] Azure CLI instalado y autenticado
- [ ] Git instalado
- [ ] Nmap instalado
- [ ] VS Code instalado con extensiones
- [ ] Acceso a Azure Portal confirmado
- [ ] Permisos de Security Center verificados

---

## 🎯 Próximos Pasos

Una vez completada la verificación del entorno, estará listo para:

1. **Laboratorio 1:** Implementación de Azure Security Center Avanzado
2. **Laboratorio 2:** Vulnerability Assessment y Scanning Automatizado
3. **Laboratorio 3:** Análisis de Secure Score y Automatización

---

## 📝 Notas Importantes

- **Conexión a Internet:** Asegúrese de tener una conexión estable durante la instalación
- **Permisos de Administrador:** Algunas herramientas requieren permisos elevados
- **Espacio en Disco:** Reserve al menos 2GB para todas las herramientas
- **Tiempo de Instalación:** El proceso completo puede tomar 10-15 minutos

---

## 🔗 Recursos Adicionales

- [Documentación oficial de Chocolatey](https://chocolatey.org/docs)
- [Guía de instalación de .NET Core](https://docs.microsoft.com/dotnet/core/install/)
- [Documentación de Azure CLI](https://docs.microsoft.com/cli/azure/)
- [Extensiones de VS Code para Azure](https://marketplace.visualstudio.com/search?term=azure&target=VSCode)

---

**✅ ¡Entorno configurado exitosamente! Está listo para comenzar con los laboratorios de seguridad.** 