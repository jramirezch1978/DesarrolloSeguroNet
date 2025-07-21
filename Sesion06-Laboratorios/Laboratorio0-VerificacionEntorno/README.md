# 🛠️ LABORATORIO 0: VERIFICACIÓN Y CONFIGURACIÓN DEL ENTORNO

## 📋 Información General
- **⏱️ Duración:** 15 minutos
- **🎯 Objetivo:** Preparar entorno completo para laboratorios de infraestructura Azure
- **📚 Curso:** Diseño Seguro de Aplicaciones (.NET en Azure)
- **🔧 Tipo:** Configuración previa + Verificación (NO incluye proyecto web completo)

---

## ❗ **IMPORTANTE: Este laboratorio es SOLO configuración del entorno**

**Este laboratorio NO incluye el proyecto web completo** con Controllers, Views, Models, etc. Es únicamente para:
- ✅ Verificar que .NET 9 funciona
- ✅ Configurar herramientas básicas
- ✅ Validar acceso a Azure
- ✅ Confirmar que el entorno está listo

**El proyecto web completo empieza desde el Laboratorio 1 en adelante.**

---

## 📝 Paso 1: Instalación de Chocolatey (3 minutos)

### Para usuarios Windows 10/11:

1. **Abrir PowerShell como Administrador:**
   - Click derecho en el botón de Windows
   - Seleccionar "Windows PowerShell (Admin)" o "Terminal (Admin)"

2. **Verificar si Chocolatey está instalado:**
   ```powershell
   choco --version
   ```

3. **Si NO está instalado, ejecutar:**
   ```powershell
   # Cambiar política de ejecución temporalmente
   Set-ExecutionPolicy Bypass -Scope Process -Force
   
   # Instalar Chocolatey
   [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
   iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
   ```

4. **Verificar instalación:**
   ```powershell
   choco --version
   # Debe mostrar versión de Chocolatey
   ```

---

## 📝 Paso 2: Instalación de .NET 9 y Herramientas (5 minutos)

### Instalar .NET 9 SDK:
```powershell
# Instalar .NET 9 SDK (última versión)
choco install dotnet-9.0-sdk -y

# Instalar Azure CLI
choco install azure-cli -y

# Instalar Git (si no está instalado)
choco install git -y

# Refrescar variables de entorno
refreshenv
```

### Verificar instalaciones:
```powershell
# Verificar .NET 9
dotnet --version
# Debe mostrar: 9.0.x

# Verificar Azure CLI
az --version

# Verificar Git
git --version
```

---

## 📝 Paso 3: Configuración de Visual Studio Code (4 minutos)

### Instalar VS Code (si no está instalado):
```powershell
choco install vscode -y
```

### Extensiones requeridas para VS Code:

1. **Abrir VS Code y instalar extensiones:**
   - Presionar `Ctrl + Shift + X` para abrir extensiones
   - Buscar e instalar las siguientes extensiones:
     - **C# Dev Kit** (Microsoft) - ID: `ms-dotnettools.csdevkit`
     - **Azure Account** (Microsoft) - ID: `ms-vscode.azure-account`
     - **Azure Resources** (Microsoft) - ID: `ms-azuretools.vscode-azureresourcegroups`
     - **Azure CLI Tools** (Microsoft) - ID: `ms-vscode.azurecli`
     - **REST Client** (Huachao Mao) - ID: `humao.rest-client`

### Comando alternativo para instalar extensiones:
```powershell
# Desde línea de comandos
code --install-extension ms-dotnettools.csdevkit
code --install-extension ms-vscode.azure-account
code --install-extension ms-azuretools.vscode-azureresourcegroups
code --install-extension ms-vscode.azurecli
code --install-extension humao.rest-client
```

---

## 📝 Paso 4: Verificación de Acceso Azure (3 minutos)

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
3. Confirmar permisos para crear recursos de red

---

## 📝 Paso 5: Prueba del Proyecto de Verificación

### Ejecutar el proyecto simple de verificación:

1. **Abrir terminal en este directorio:**
   ```powershell
   cd Laboratorio0-VerificacionEntorno
   ```

2. **Restaurar dependencias:**
   ```powershell
   dotnet restore
   ```

3. **Ejecutar verificación:**
   ```powershell
   dotnet run
   ```

### **Salida esperada:**
```
🎯 LABORATORIO 0: VERIFICACIÓN DEL ENTORNO
==========================================

✅ .NET Version: 9.0.5
✅ .NET 9 está funcionando correctamente
✅ Logging configurado correctamente
✅ Azure Identity SDK cargado correctamente

🔍 Variables de entorno:
   Usuario: [su usuario]
   Máquina: [su máquina]
   OS: Microsoft Windows NT 10.0.x
   Directorio actual: [directorio actual]

🎉 VERIFICACIÓN DEL ENTORNO COMPLETADA
✅ El entorno está listo para los laboratorios de infraestructura Azure

📋 Próximo paso: Ejecutar Laboratorio 1 - Virtual Network
```

---

## ✅ Checklist de Verificación

Marque cada elemento una vez completado:

### **🔧 Herramientas Básicas:**
- [ ] Chocolatey instalado y funcionando
- [ ] .NET 9 SDK instalado (versión 9.0.x)
- [ ] Azure CLI instalado y funcionando
- [ ] Git instalado y configurado

### **💻 Visual Studio Code:**
- [ ] VS Code instalado
- [ ] C# Dev Kit extension instalada
- [ ] Azure Account extension instalada
- [ ] Azure Resources extension instalada
- [ ] Azure CLI Tools extension instalada
- [ ] REST Client extension instalada

### **☁️ Azure Access:**
- [ ] Azure CLI login exitoso
- [ ] Acceso al Azure Portal verificado
- [ ] Suscripciones listadas correctamente
- [ ] Permisos para crear recursos confirmados

### **✅ Verificación Final:**
- [ ] Proyecto de verificación ejecuta sin errores
- [ ] .NET 9 funcionando correctamente
- [ ] Azure SDK cargando correctamente
- [ ] Variables de entorno mostrándose

---

## 🚨 Troubleshooting Común

### Error: "Cannot execute PowerShell scripts"
- **Solución:** Ejecutar `Set-ExecutionPolicy Bypass -Scope Process -Force`

### Error: "Chocolatey command not found"
- **Solución:** Reiniciar PowerShell o ejecutar `refreshenv`

### Error: "dotnet command not found"
- **Solución:** 
  - Verificar instalación: `choco list dotnet-9.0-sdk`
  - Reinstalar si es necesario: `choco install dotnet-9.0-sdk -y --force`
  - Ejecutar `refreshenv`

### Error: "Azure CLI login failed"
- **Solución:** 
  - Verificar conexión a Internet
  - Intentar `az login --use-device-code`
  - Contactar al instructor para verificar permisos

### Error: "Cannot access Azure Portal"
- **Solución:**
  - Verificar que está usando la cuenta correcta del curso
  - Limpiar cache del navegador
  - Intentar navegación privada/incógnito

### Warning: "Azure.Identity vulnerabilities"
- **Nota:** Son warnings conocidos, no afectan la funcionalidad del laboratorio
- **Acción:** Continuar con el laboratorio normalmente

---

## 🎯 Resultado Esperado

Al completar este laboratorio, debe tener:

1. **✅ Entorno de desarrollo completo** con todas las herramientas necesarias
2. **✅ Acceso funcional a Azure** con permisos apropiados
3. **✅ Visual Studio Code configurado** con todas las extensiones requeridas
4. **✅ Verificación exitosa** del proyecto simple de .NET 9
5. **✅ Base sólida** para los siguientes laboratorios de infraestructura

---

## 📋 Diferencia con otros Laboratorios

### **🛠️ Laboratorio 0 (Este):**
- **Tipo:** Console Application simple
- **Propósito:** Solo verificación del entorno
- **Contenido:** Verificación básica de .NET 9 + Azure SDK
- **Sin:** Controllers, Views, Models, Azure AD, etc.

### **🌐 Laboratorios 1-4:**
- **Tipo:** Proyecto web completo con Azure AD
- **Propósito:** Implementación práctica de infraestructura
- **Contenido:** Controllers, Views, Models, Azure AD, Key Vault, etc.
- **Con:** Toda la funcionalidad web y seguridad

---

## 📋 Próximos Pasos

Una vez completado este laboratorio, estará listo para:

- **✅ Laboratorio 1:** Creación de Virtual Network Segura (CON proyecto web completo)
- **✅ Laboratorio 2:** Implementación de Network Security Groups
- **✅ Laboratorio 3:** Azure Bastion y Jump Box
- **✅ Laboratorio 4:** Testing y Arquitectura Hub-and-Spoke

---

## 📚 Recursos Adicionales

- [Documentación de Chocolatey](https://chocolatey.org/docs)
- [.NET 9 Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [Azure CLI Reference](https://docs.microsoft.com/en-us/cli/azure/)
- [VS Code Azure Extensions](https://code.visualstudio.com/docs/azure/extensions)

---

**¡Excelente trabajo!** Su entorno está ahora preparado para implementar infraestructuras de red seguras en Azure. En el próximo laboratorio comenzará con el proyecto web completo y la implementación de redes virtuales. 🚀
