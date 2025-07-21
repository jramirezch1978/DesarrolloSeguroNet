# 🛠️ Laboratorio 0: Verificación y Configuración del Entorno

## Información General
- **Duración:** 15 minutos
- **Objetivo:** Preparar entorno completo para laboratorios de Network Security Groups y DDoS Protection
- **Modalidad:** Práctica individual guiada

## Fundamentos Teóricos

### Importancia de la Configuración del Entorno
La preparación adecuada del entorno de desarrollo es fundamental para el éxito de cualquier proyecto de infraestructura en la nube. En Azure, esto es especialmente crítico porque trabajamos con recursos que tienen implicaciones de costo y seguridad reales.

### Herramientas Requeridas

#### 1. Chocolatey Package Manager
Chocolatey es el administrador de paquetes más popular para Windows, que nos permite instalar y gestionar software de manera automatizada y reproducible.

**¿Por qué Chocolatey?**
- Instalación automatizada de múltiples herramientas
- Versionado consistente
- Fácil mantenimiento y actualización
- Scripts reproducibles para equipos

#### 2. .NET 9 SDK
La última versión de .NET proporciona:
- Mejor performance y menor consumo de memoria
- Nuevas características de seguridad
- Compatibilidad mejorada con Azure
- Soporte nativo para contenedores

#### 3. Azure CLI
La herramienta de línea de comandos oficial de Microsoft para Azure:
- Gestión completa de recursos Azure
- Automatización mediante scripts
- Integración con CI/CD pipelines
- Autenticación unificada

#### 4. Visual Studio Code con Extensiones
VS Code es el editor preferido para desarrollo en Azure debido a:
- Integración nativa con Azure
- IntelliSense para Azure Resource Manager
- Debugging integrado para Azure Functions
- Git y DevOps workflows

### Arquitectura de Seguridad que Construiremos

En estos laboratorios implementaremos una arquitectura de seguridad multicapa:

```
Internet → Azure Load Balancer → NSG (Subnet) → NSG (NIC) → Virtual Machines
                                         ↓
                                   DDoS Protection
                                         ↓
                                 Flow Logs & Analytics
                                         ↓
                                 Automated Response
```

**Capas de Seguridad:**
1. **Azure DDoS Protection**: Protección contra ataques volumétricos
2. **Network Security Groups**: Firewall distribuido con reglas granulares  
3. **Application Security Groups**: Organización lógica por función
4. **Network Monitoring**: Visibilidad completa del tráfico
5. **Automated Response**: Respuesta inteligente a incidentes

## Laboratorio Práctico

### Paso 1: Verificación de Chocolatey (3 minutos)

#### Verificar si está instalado
```powershell
# Abrir PowerShell como Administrador
choco --version
```

#### Si no está instalado, ejecutar:
```powershell
# Cambiar política de ejecución temporalmente
Set-ExecutionPolicy Bypass -Scope Process -Force

# Instalar Chocolatey
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))

# Verificar instalación
choco --version
```

### Paso 2: Instalación de .NET 9 y Herramientas (5 minutos)

```powershell
# Instalar .NET 9 SDK (última versión)
choco install dotnet-9.0-sdk -y

# Instalar Azure CLI
choco install azure-cli -y

# Instalar Git (si no está instalado)
choco install git -y

# Instalar Visual Studio Code (si no está instalado)
choco install vscode -y

# Refrescar variables de entorno
refreshenv
```

#### Verificar instalaciones:
```powershell
# Verificar .NET 9
dotnet --version
# Debe mostrar: 9.0.x

# Verificar Azure CLI
az --version

# Verificar Git
git --version

# Verificar VS Code
code --version
```

### Paso 3: Configuración de Visual Studio Code (4 minutos)

#### Instalar extensiones esenciales:
```powershell
# Extensiones para Azure y .NET
code --install-extension ms-dotnettools.csdevkit
code --install-extension ms-vscode.azure-account
code --install-extension ms-azuretools.vscode-azureresourcegroups
code --install-extension ms-vscode.azurecli
code --install-extension humao.rest-client
code --install-extension ms-azure-devops.azure-pipelines
```

#### Verificar extensiones instaladas:
```powershell
code --list-extensions
```

### Paso 4: Autenticación con Azure (3 minutos)

#### Login a Azure:
```powershell
# Autenticarse con Azure
az login

# Verificar suscripciones disponibles
az account list --output table

# Establecer suscripción por defecto (si tienen múltiples)
# az account set --subscription "SUBSCRIPTION_ID"
```

#### Verificar permisos:
```powershell
# Verificar acceso al resource group del curso
az group list --output table

# Verificar roles asignados
az role assignment list --assignee [su-email] --output table
```

#### Configurar ubicación por defecto:
```powershell
# Establecer region por defecto
az configure --defaults location=eastus
```

## Scripts de Automatización

### Script Principal de Configuración
El archivo `setup-environment.ps1` automatiza todo el proceso:

```powershell
# Ver contenido del script
Get-Content .\setup-environment.ps1

# Ejecutar script de configuración
.\setup-environment.ps1
```

### Script de Verificación
El archivo `verify-setup.ps1` valida que todo esté correctamente configurado:

```powershell
# Ejecutar verificación completa
.\verify-setup.ps1
```

## Verificación Final

### Checklist de Configuración Exitosa
- [ ] Chocolatey instalado y funcionando
- [ ] .NET 9 SDK instalado (versión 9.0.x)
- [ ] Azure CLI instalado y autenticado
- [ ] Visual Studio Code con extensiones Azure
- [ ] Git configurado
- [ ] Acceso verificado a suscripción Azure
- [ ] Permisos apropiados para crear recursos

### Próximos Pasos
Una vez completada la configuración, estarán listos para:
1. **Laboratorio 1**: Implementar Network Security Groups avanzados
2. **Laboratorio 2**: Configurar Azure DDoS Protection Standard
3. **Laboratorio 3**: Realizar testing de conectividad con Network Watcher
4. **Laboratorio 4**: Implementar automatización de respuesta a incidentes

## Troubleshooting Común

### Error: "Execution Policy Restricted"
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Error: "choco command not found"
```powershell
# Cerrar y reabrir PowerShell como Administrador
refreshenv
```

### Error: "Azure CLI login failed"
```powershell
# Limpiar cache de autenticación
az account clear
az login --use-device-code
```

### Error: "VS Code extensions not installing"
```powershell
# Verificar que VS Code esté en PATH
code --version
# Si falla, reiniciar terminal después de instalar VS Code
```

## Conceptos Clave Aprendidos

### Principios de DevOps para Seguridad
- **Infraestructura como Código**: Scripts automatizados y reproducibles
- **Configuración Declarativa**: Especificar el estado deseado
- **Versionado de Herramientas**: Consistencia entre ambientes
- **Automatización**: Reducir errores humanos

### Mejores Prácticas de Configuración
- **Principio de Menor Privilegio**: Solo los permisos necesarios
- **Documentación**: Scripts auto-documentados
- **Verificación**: Validar cada paso de configuración
- **Reversibilidad**: Poder deshacer cambios si es necesario

---

**¡Configuración completa!** Ahora tienen un entorno robusto y profesional para implementar soluciones de seguridad empresarial en Azure.

**Siguiente:** [Laboratorio 1 - Network Security Groups Avanzados](../Laboratorio1-NSG/README.md) 