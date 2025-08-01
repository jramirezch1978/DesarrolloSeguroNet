# Laboratorio 1: Azure Security Center Avanzado

## 📋 Descripción
Configuración avanzada de Microsoft Defender for Cloud (anteriormente Azure Security Center) con planes específicos, políticas personalizadas y recursos de testing.

## 🎯 Objetivos
- Configurar Microsoft Defender for Cloud con planes específicos
- Crear políticas de seguridad personalizadas
- Implementar infraestructura de testing
- Configurar Log Analytics Workspace

## 📁 Contenido
- `Laboratorio1-SecurityCenter.md` - Guía completa del laboratorio
- Scripts de verificación y configuración

## ⏱️ Duración
**30 minutos**

## 🔧 Componentes Principales
- **Defender Plans:** Servers, App Services, Storage, SQL
- **Custom Policies:** HTTPS requirement, Storage encryption
- **Testing Resources:** VMs Windows/Linux, App Service, Storage Account
- **Log Analytics:** Workspace para monitoreo centralizado

## ✅ Resultado Esperado
- Security Center completamente configurado
- Políticas de seguridad implementadas
- Recursos de testing creados y monitoreados
- Secure Score visible y funcional

## ⚠️ Notas Importantes
- **Auto-provisioning deprecado:** Se usa Log Analytics Workspace manual
- **Variables corregidas:** `$ResourceGroupName` y `$Location` con mayúsculas
- **Costos:** Los planes de Defender tienen costos asociados

---
**Anterior:** [Laboratorio 0 - Configuración](../Laboratorio0-Configuracion/)  
**Siguiente:** [Laboratorio 2 - Vulnerability Assessment](../Laboratorio2-VulnerabilityAssessment/) 