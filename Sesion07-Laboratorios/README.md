# Laboratorios de Seguridad de Red - Sesión 7
## Diseño Seguro de Aplicaciones (.NET en Azure)

### Descripción
Este repositorio contiene los laboratorios prácticos de la Sesión 7 del curso "Diseño Seguro de Aplicaciones (.NET en Azure)", enfocados en Network Security Groups y Azure DDoS Protection.

### Estructura del Curso
- **Duración Total:** 90 minutos (4 laboratorios principales + 1 de configuración)
- **Modalidad:** Instructor-led con práctica individual
- **Herramientas:** Visual Studio Code + .NET 9 + C# + Azure Portal

### Laboratorios Incluidos

#### 🛠️ [Laboratorio 0: Verificación y Configuración del Entorno](./Laboratorio0-Setup/)
**Duración:** 15 minutos  
**Objetivo:** Preparar entorno completo para laboratorios de Network Security Groups y DDoS Protection

#### 🧪 [Laboratorio 1: Network Security Groups Avanzados](./Laboratorio1-NSG/)
**Duración:** 25 minutos  
**Objetivo:** Crear y configurar NSGs con reglas granulares para diferentes tipos de aplicaciones

#### 🧪 [Laboratorio 2: Azure DDoS Protection](./Laboratorio2-DDoS/)
**Duración:** 20 minutos  
**Objetivo:** Configurar Azure DDoS Protection Standard y entender las métricas de protección

#### 🧪 [Laboratorio 3: Testing y Simulación de Conectividad](./Laboratorio3-Testing/)
**Duración:** 20 minutos  
**Objetivo:** Usar Azure Network Watcher para testing de conectividad y análisis de tráfico

#### 🧪 [Laboratorio 4: Automatización y Alertas Avanzadas](./Laboratorio4-Automation/)
**Duración:** 25 minutos  
**Objetivo:** Implementar automatización de respuesta a incidentes usando Logic Apps y Azure Functions

### Objetivos de Aprendizaje

Al completar estos laboratorios, los estudiantes habrán logrado:

🔒 **Network Security Groups Avanzados:**
- ✅ Implementación de reglas granulares con prioridades optimizadas
- ✅ Uso de Service Tags para simplificación y automatización
- ✅ Application Security Groups para organización lógica
- ✅ Flow Logs habilitados para visibilidad completa

🛡️ **Azure DDoS Protection:**
- ✅ DDoS Protection Standard configurado y monitoreado
- ✅ Métricas y alertas de DDoS en tiempo real
- ✅ Dashboard de monitoreo para visibilidad ejecutiva
- ✅ Entendimiento de costos vs beneficios de protección

🔍 **Network Monitoring y Diagnostics:**
- ✅ Azure Network Watcher para troubleshooting
- ✅ IP Flow Verify para validación de reglas
- ✅ Traffic Analytics para insights avanzados
- ✅ Effective Security Rules analysis

🤖 **Automatización y Respuesta:**
- ✅ Logic Apps para orquestación de respuesta
- ✅ Azure Functions para análisis de patrones
- ✅ Integración con sistemas externos (ITSM)
- ✅ Alerting multi-canal y escalamiento

### Prerrequisitos
- Cuenta de Azure con permisos de Contributor
- Visual Studio Code instalado
- .NET 9 SDK instalado
- Azure CLI instalado
- Extensiones de VS Code para Azure y C#

### Estructura de Archivos
```
Sesion07-Laboratorios/
├── README.md
├── Laboratorio0-Setup/
│   ├── README.md
│   ├── setup-environment.ps1
│   └── verify-setup.ps1
├── Laboratorio1-NSG/
│   ├── README.md
│   ├── src/
│   │   ├── NSGManager.sln
│   │   └── NSGManager/
│   ├── scripts/
│   └── templates/
├── Laboratorio2-DDoS/
│   ├── README.md
│   ├── src/
│   ├── scripts/
│   └── monitoring/
├── Laboratorio3-Testing/
│   ├── README.md
│   ├── src/
│   │   ├── NetworkTester.sln
│   │   └── NetworkTester/
│   └── scripts/
├── Laboratorio4-Automation/
│   ├── README.md
│   ├── src/
│   │   ├── SecurityAutomation.sln
│   │   └── SecurityAutomation/
│   ├── functions/
│   └── logic-apps/
└── docs/
    ├── teoria-network-security-groups.md
    ├── teoria-ddos-protection.md
    └── referencias-adicionales.md
```

### Instrucciones de Uso
1. Comenzar por el Laboratorio 0 para configurar el entorno
2. Seguir los laboratorios en orden secuencial
3. Cada laboratorio tiene su propio README con instrucciones detalladas
4. Los scripts de PowerShell están incluidos para automatizar tareas repetitivas
5. El código C# está completamente funcional y listo para compilar

### Soporte y Recursos
- **Documentación oficial:** [Azure Network Security](https://docs.microsoft.com/azure/virtual-network/security-overview)
- **DDoS Protection:** [Azure DDoS Protection Documentation](https://docs.microsoft.com/azure/ddos-protection/)
- **Network Watcher:** [Azure Network Watcher Documentation](https://docs.microsoft.com/azure/network-watcher/)

### Troubleshooting Común
Consultar el archivo `docs/troubleshooting.md` para soluciones a problemas frecuentes.

### Limpieza de Recursos
⚠️ **Importante:** Después de completar los laboratorios, ejecutar los scripts de limpieza para evitar costos innecesarios, especialmente con DDoS Protection Standard.

---
**Autor:** Curso de Diseño Seguro de Aplicaciones (.NET en Azure)  
**Fecha:** Julio 2025  
**Versión:** 1.0 