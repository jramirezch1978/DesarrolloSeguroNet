# Scripts de Automatización

## 📋 Descripción
Scripts de PowerShell para automatizar la configuración del entorno y limpieza de recursos de los laboratorios de la Sesión 8.

## 📁 Contenido

### 🔧 Scripts de Configuración
- **`setup-environment.ps1`** - Configuración automática del entorno
  - Instala Chocolatey y herramientas necesarias
  - Configura Azure CLI y autenticación
  - Crea resource group base
  - Instala extensiones de VS Code

### 🧹 Scripts de Limpieza
- **`cleanup-resources.ps1`** - Limpieza completa de recursos
  - Deshabilita planes de Defender
  - Elimina políticas personalizadas
  - Borra resource group principal
  - Limpia archivos locales

## 🚀 Uso

### Configuración Inicial
```powershell
# Ejecutar como administrador
.\scripts\setup-environment.ps1
```

### Limpieza Final
```powershell
# Ejecutar al finalizar todos los laboratorios
.\scripts\cleanup-resources.ps1
```

## ⚠️ Notas Importantes
- **Ejecutar como Administrador:** Los scripts requieren permisos elevados
- **Confirmación:** El script de limpieza pide confirmación antes de eliminar recursos
- **Costos:** La limpieza evita costos innecesarios en Azure
- **Backup:** Los scripts no hacen backup, asegúrate de guardar trabajo importante

## 🔍 Verificación
Después de ejecutar `setup-environment.ps1`, se genera automáticamente:
- `verify-environment.ps1` - Script de verificación del entorno

---
**Volver:** [README Principal](../README.md) 