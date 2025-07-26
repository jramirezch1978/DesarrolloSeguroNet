# 🎯 Guía Completa - Sesión 8: Pruebas y Auditorías de Seguridad en Azure

## 📋 Descripción General

Esta sesión se enfoca en implementar capacidades avanzadas de evaluación de seguridad usando Azure Security Center, herramientas de vulnerability assessment, y metodologías profesionales de auditoría. Los estudiantes aprenderán a configurar y utilizar herramientas de nivel empresarial para evaluar continuamente la postura de seguridad de sus infraestructuras Azure.

---

## 🏗️ Estructura de Laboratorios

### 🛠️ Laboratorio 0: Verificación y Configuración del Entorno
**Duración:** 15 minutos  
**Objetivo:** Preparar entorno completo para laboratorios de Security Assessment y Vulnerability Scanning

**Componentes principales:**
- Instalación de Chocolatey y herramientas de desarrollo
- Configuración de .NET Core 9 SDK y Azure CLI
- Instalación de extensiones de Visual Studio Code
- Verificación de acceso a Azure Portal y Security Center

**Resultados esperados:**
- ✅ Entorno de desarrollo completamente configurado
- ✅ Todas las herramientas necesarias instaladas y funcionando
- ✅ Acceso a Azure confirmado con permisos adecuados
- ✅ Extensiones de VS Code instaladas para desarrollo de seguridad

---

### 🧪 Laboratorio 1: Implementación de Azure Security Center Avanzado
**Duración:** 25 minutos  
**Objetivo:** Configurar y utilizar Azure Security Center para evaluación continua de seguridad

**Componentes principales:**
- Configuración de Microsoft Defender for Cloud con planes específicos
- Creación de infraestructura de testing (VMs, App Services, Storage)
- Implementación de políticas personalizadas de seguridad
- Configuración de auto-provisioning y Log Analytics workspace

**Resultados esperados:**
- ✅ Microsoft Defender for Cloud configurado con planes específicos
- ✅ Secure Score monitoring y análisis de tendencias
- ✅ Custom security policies implementadas y asignadas
- ✅ Regulatory compliance dashboard configurado

---

### 🧪 Laboratorio 2: Vulnerability Assessment y Scanning Automatizado
**Duración:** 20 minutos  
**Objetivo:** Implementar vulnerability scanning usando Azure Security Center y herramientas externas

**Componentes principales:**
- Configuración de Qualys integration para VMs
- Implementación de container security assessment
- Despliegue de OpenVAS como scanner complementario
- Configuración de vulnerability assessment automatizado

**Resultados esperados:**
- ✅ Qualys agents instalados y reportando en VMs
- ✅ Container registry scanning habilitado y funcional
- ✅ OpenVAS scanner operacional y accesible
- ✅ Vulnerability reports generándose automáticamente

---

### 🧪 Laboratorio 3: Análisis de Secure Score y Automatización
**Duración:** 15 minutos  
**Objetivo:** Crear automatización para monitoreo continuo y respuesta a alertas de seguridad

**Componentes principales:**
- Desarrollo de aplicación .NET Core para análisis de Secure Score
- Configuración de Logic App para respuesta automática a alertas
- Implementación de reporting automatizado y métricas de seguridad
- Integración con sistemas externos (email, tickets)

**Resultados esperados:**
- ✅ Aplicación .NET Core para análisis de Secure Score
- ✅ Logic App para respuesta automática a alertas
- ✅ Reporting automatizado y métricas de seguridad
- ✅ Integration con sistemas externos (email, tickets)

---

## 🎯 Resultados de Aprendizaje Alcanzados

Al completar todos los laboratorios, los estudiantes habrán logrado:

### 🔒 Azure Security Center Avanzado:
- ✅ Microsoft Defender for Cloud configurado con planes específicos
- ✅ Secure Score monitoring y análisis de tendencias
- ✅ Custom security policies implementadas y asignadas
- ✅ Regulatory compliance dashboard configurado

### 🛡️ Vulnerability Assessment:
- ✅ Qualys integration habilitada para VMs
- ✅ Container registry scanning configurado
- ✅ OpenVAS implementado como scanner complementario
- ✅ Automated vulnerability detection funcionando

### 🔍 Security Assessment Automation:
- ✅ Aplicación .NET Core para análisis de Secure Score
- ✅ Logic App para respuesta automática a alertas
- ✅ Reporting automatizado y métricas de seguridad
- ✅ Integration con sistemas externos (email, tickets)

### 🤖 Continuous Security Monitoring:
- ✅ Log Analytics workspace configurado
- ✅ Security policies enforcement habilitado
- ✅ Automated compliance checking funcionando
- ✅ Real-time threat detection activo

---

## 🛠️ Herramientas y Tecnologías Utilizadas

### Herramientas de Desarrollo:
- **Visual Studio Code** con extensiones de Azure
- **.NET Core 9 SDK** para desarrollo de aplicaciones
- **Azure CLI** para automatización de infraestructura
- **PowerShell** para scripting y automatización

### Servicios de Azure:
- **Microsoft Defender for Cloud** (anteriormente Security Center)
- **Azure Policy** para governance de seguridad
- **Log Analytics** para monitoreo centralizado
- **Logic Apps** para automatización de workflows
- **Azure Container Registry** para container security
- **Azure VMs** para vulnerability assessment

### Herramientas de Seguridad:
- **Qualys** para vulnerability scanning de VMs
- **OpenVAS** como scanner complementario
- **Nmap** para network discovery
- **Postman** para testing de APIs

---

## 📊 Métricas de Éxito

### Indicadores de Implementación Exitosa:

**Security Center Configuration:**
- ✅ Microsoft Defender plans habilitados para todos los servicios
- ✅ Secure Score visible y reportando métricas
- ✅ Custom policies creadas y asignadas correctamente
- ✅ Compliance dashboard mostrando estado actual

**Vulnerability Assessment:**
- ✅ Qualys agents instalados y reportando en VMs
- ✅ Container registry scanning habilitado y funcional
- ✅ OpenVAS scanner operacional y accesible
- ✅ Vulnerability reports generándose automáticamente

**Automation & Monitoring:**
- ✅ Aplicación .NET compilando y ejecutando sin errores
- ✅ Logic App respondiendo a triggers HTTP
- ✅ Security reports generándose en formato JSON
- ✅ Email notifications funcionando correctamente

**Integration & Analytics:**
- ✅ Log Analytics workspace recibiendo datos
- ✅ Security policies enforcement activo
- ✅ Trend analysis disponible para Secure Score
- ✅ Automated response workflows funcionando

---

## 🚨 Troubleshooting Común

### Error: "Insufficient permissions for Security Center"
**Solución:**
```powershell
# Verificar roles asignados
az role assignment list --assignee $env:USERNAME --output table

# El usuario necesita rol "Security Reader" o "Security Admin"
# Contactar al administrador de la suscripción si es necesario
```

### Error: "Qualys agent installation failed"
**Solución:**
```powershell
# Verificar que la VM está ejecutándose
az vm get-instance-view --name vm-windows-test --resource-group $resourceGroupName --query instanceView.statuses

# Reintentar instalación manual
az vm extension set --publisher "Qualys" --name "QualysAgent" --vm-name "vm-windows-test" --resource-group $resourceGroupName --force-update
```

### Error: ".NET application compilation failed"
**Solución:**
```powershell
# Verificar versión de .NET
dotnet --version

# Limpiar y restaurar paquetes
dotnet clean
dotnet restore
dotnet build --verbose

# Verificar que todas las dependencias están instaladas
dotnet list package
```

### Error: "Logic App trigger URL not working"
**Solución:**
1. Azure Portal → Logic Apps → logic-security-response-[username]
2. Logic app designer → When an HTTP request is received
3. Copy HTTP POST URL (se genera después de guardar)
4. Verificar que el JSON schema está correctamente configurado

---

## 📝 Notas Importantes

### Costos y Consideraciones:
- **Costos:** Los planes de Defender Standard generan costos adicionales (~$15-30/mes por recurso)
- **Tiempo de Propagación:** Los cambios pueden tardar hasta 24 horas en reflejarse completamente
- **Permisos:** Asegúrese de tener permisos adecuados para Security Center
- **Limpieza:** Recuerde ejecutar los scripts de limpieza al final de todos los laboratorios

### Seguridad:
- **Configuraciones de Testing:** Las configuraciones de seguridad son para propósitos educativos
- **Credenciales:** Las credenciales utilizadas son para demostración
- **Ambiente:** Se recomienda usar una suscripción de desarrollo/pruebas

### Automatización:
- **Scripts:** Todos los scripts están incluidos para facilitar la implementación
- **Verificación:** Scripts de verificación incluidos para validar cada paso
- **Limpieza:** Scripts de limpieza para evitar costos innecesarios

---

## 🔗 Recursos Adicionales

### Documentación Oficial:
- [Azure Security Center Documentation](https://docs.microsoft.com/azure/security-center/)
- [Microsoft Defender for Cloud](https://docs.microsoft.com/azure/defender-for-cloud/)
- [Azure Policy Documentation](https://docs.microsoft.com/azure/governance/policy/)
- [Security Center Pricing](https://azure.microsoft.com/pricing/details/security-center/)

### Herramientas de Seguridad:
- [Azure Security Center Vulnerability Assessment](https://docs.microsoft.com/azure/security-center/deploy-vulnerability-assessment-vm)
- [Container Security in Azure](https://docs.microsoft.com/azure/defender-for-cloud/defender-for-containers-introduction)
- [OpenVAS Documentation](https://www.openvas.org/)
- [Qualys Integration Guide](https://docs.microsoft.com/azure/security-center/deploy-vulnerability-assessment-vm#deploy-the-qualys-built-in-vulnerability-scanner)

### Automatización:
- [Azure Logic Apps Documentation](https://docs.microsoft.com/azure/logic-apps/)
- [Azure Resource Manager SDK](https://docs.microsoft.com/dotnet/api/overview/azure/resource-manager)
- [Security Center REST API](https://docs.microsoft.com/rest/api/securitycenter/)
- [Azure Monitor Logs](https://docs.microsoft.com/azure/azure-monitor/logs/)

---

## 🎯 Próximos Pasos

Una vez completada la Sesión 8, los estudiantes estarán preparados para:

### Sesión 9: Pruebas y Auditorías Parte 2
- **Penetration Testing** con herramientas como OWASP ZAP y Burp Suite
- **Attack Simulation** usando Metasploit y otras herramientas
- **Advanced Security Testing** metodologías
- **Compliance Frameworks** avanzados

### Preparación Recomendada:
1. **Explorar OWASP ZAP** - descargar y familiarizarse con la interfaz
2. **Revisar Burp Suite** - entender pruebas basadas en proxy
3. **Familiarizarse con Metasploit** - conceptos básicos de penetration testing
4. **Instalar herramientas adicionales** - Wireshark, Fiddler, etc.

---

## 🎉 ¡FELICITACIONES!

Al completar exitosamente la Sesión 8, los estudiantes han logrado:

### 🏆 Capacidades Adquiridas:
- **Enterprise-grade Security Assessment** usando herramientas de nivel profesional
- **Automated Vulnerability Management** con integración completa
- **Continuous Security Monitoring** con alertas en tiempo real
- **Compliance Automation** con frameworks regulatorios
- **Security Analytics** con reporting ejecutivo

### 💼 Aplicabilidad Profesional:
- **Directamente aplicable** en organizaciones de cualquier tamaño
- **Compatible con estándares** de la industria (OWASP, NIST, ISO)
- **Escalable** para infraestructuras complejas
- **Integrable** con SIEM y SOAR platforms existentes
- **ROI medible** a través de métricas de seguridad cuantificadas

### 🚀 Valor Empresarial:
La solución implementada puede:
- **Detectar vulnerabilidades** en tiempo real
- **Automatizar respuesta** a incidentes de seguridad
- **Mantener compliance** continuo con regulaciones
- **Proporcionar visibility** ejecutiva del riesgo
- **Integrar** con herramientas de security operations existentes

---

**¡Nos vemos en la Sesión 9 para continuar con Penetration Testing y Attack Simulation! 🚀**

---

*Documento generado para el curso "Diseño Seguro de Aplicaciones (.NET en Azure)" - Sesión 8*
*Fecha: Julio 2025 | Versión: 1.0* 