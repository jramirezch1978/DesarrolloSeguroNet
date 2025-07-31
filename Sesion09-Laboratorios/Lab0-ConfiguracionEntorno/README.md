# 🛠️ LABORATORIO 0: CONFIGURACIÓN DEL ENTORNO
**Curso:** Diseño Seguro de Aplicaciones (.NET en Azure)  
**Sesión:** 9 - Pruebas de Penetración y Auditorías Avanzadas  
**Duración:** 15 minutos  
**Modalidad:** Práctica individual con verificación automatizada

## 🎯 OBJETIVO
Preparar un entorno completo y profesional para pruebas de penetración y evaluaciones de compliance, instalando y configurando todas las herramientas necesarias para los laboratorios de seguridad avanzada.

## 📋 DESCRIPCIÓN
Este laboratorio establece la base tecnológica para ejecutar pruebas de penetración éticas y evaluaciones de compliance usando metodologías profesionales. Configura herramientas especializadas de ciberseguridad, verifica acceso a Azure, y prepara el ambiente de desarrollo para evaluaciones de seguridad de nivel empresarial.

## 🔑 CONCEPTOS CLAVE

### **Chocolatey Package Manager**
- **Definición:** Gestor de paquetes para Windows que automatiza instalación de software
- **Importancia:** Facilita instalación consistente y actualización de herramientas de seguridad
- **Uso Empresarial:** Estándar para gestión de herramientas en equipos de ciberseguridad

### **Herramientas de Penetration Testing**
- **Nmap:** Escáner de red para descubrimiento de servicios y vulnerabilidades
- **Burp Suite:** Proxy para análisis de seguridad de aplicaciones web
- **Wireshark:** Analizador de protocolos de red para interceptación de tráfico
- **Postman:** Cliente HTTP para testing de APIs y endpoints

### **Azure CLI y PowerShell**
- **Azure CLI:** Interfaz de línea de comandos para gestión de recursos Azure
- **PowerShell:** Shell y lenguaje de scripting para automatización de tareas
- **Integración:** Permite auditorías automatizadas de configuración cloud

## 🏗️ ARQUITECTURA DEL ENTORNO

```
Entorno de Penetration Testing
├── Sistema Operativo: Windows 10/11
├── Gestor de Paquetes: Chocolatey
├── Framework de Desarrollo: .NET Core 9
├── Herramientas de Red: Nmap, Wireshark
├── Herramientas Web: Burp Suite, OWASP ZAP
├── Herramientas Cloud: Azure CLI, PowerShell
├── Editor: Visual Studio Code + Extensiones
└── Cliente HTTP: Postman
```

## 📚 MARCO TEÓRICO

### **Metodología de Preparación de Entorno**
1. **Instalación Base:** Sistema operativo y dependencias fundamentales
2. **Herramientas Especializadas:** Software específico para ciberseguridad
3. **Configuración de Acceso:** Credenciales y permisos para sistemas objetivo
4. **Verificación Funcional:** Pruebas de conectividad y funcionalidad

### **Consideraciones de Seguridad**
- **Aislamiento:** Usar máquinas virtuales para testing destructivo
- **Permisos:** Configurar acceso mínimo necesario para evaluaciones
- **Documentación:** Registrar todas las herramientas y configuraciones
- **Legalidad:** Verificar autorizaciones antes de usar herramientas de penetración

## 🎯 OBJETIVOS DE APRENDIZAJE
Al completar este laboratorio, los estudiantes podrán:

1. **Instalar y configurar** un entorno completo de penetration testing
2. **Verificar funcionalidad** de herramientas especializadas de ciberseguridad  
3. **Establecer conectividad** con servicios Azure para auditorías cloud
4. **Documentar configuraciones** para reproducibilidad y auditoría
5. **Aplicar mejores prácticas** de seguridad en configuración de herramientas

## 🔧 HERRAMIENTAS UTILIZADAS

| Herramienta | Versión | Propósito | Categoría |
|-------------|---------|-----------|-----------|
| Chocolatey | Latest | Gestión de paquetes | Infraestructura |
| .NET Core | 9.0+ | Framework de desarrollo | Desarrollo |
| Azure CLI | Latest | Gestión de recursos cloud | Cloud |
| Nmap | 7.95+ | Escaneo de red | Reconocimiento |
| Burp Suite | Community | Proxy de seguridad web | Web Testing |
| Wireshark | Latest | Análisis de protocolos | Network Analysis |
| Postman | Latest | Cliente HTTP/API | API Testing |
| Git | Latest | Control de versiones | Desarrollo |

## 📝 ENTREGABLES
1. **Entorno Configurado:** Todas las herramientas instaladas y funcionales
2. **Verificación Azure:** Conectividad confirmada con servicios cloud
3. **Documentación:** Log de instalación y configuraciones aplicadas
4. **Pruebas Funcionales:** Verificación de cada herramienta instalada

## 🚀 INSTRUCCIONES DE EJECUCIÓN

### Paso 1: Ejecutar Verificación de Entorno
```powershell
cd Lab0-ConfiguracionEntorno
.\scripts\VerificarEntorno.ps1
```

### Paso 2: Instalar Herramientas (si necesario)
```powershell
.\scripts\InstalarHerramientas.ps1
```

### Paso 3: Configurar Azure Access
```powershell
.\scripts\ConfigurarAzure.ps1
```

### Paso 4: Verificar Instalación Completa
```powershell
.\scripts\VerificarInstalacion.ps1
```

## ⚠️ CONSIDERACIONES IMPORTANTES

### **Seguridad**
- Ejecutar solo en entornos autorizados
- No usar herramientas contra sistemas sin permiso explícito
- Mantener herramientas actualizadas para evitar vulnerabilidades

### **Legalidad**
- Todas las pruebas deben realizarse en infraestructura propia o autorizada
- Documentar permisos y autorizaciones antes de usar herramientas
- Seguir políticas organizacionales de ciberseguridad

### **Mejores Prácticas**
- Usar máquinas virtuales para aislamiento
- Mantener documentación detallada de configuraciones
- Realizar respaldos antes de modificaciones significativas

## 📖 REFERENCIAS ADICIONALES
- [OWASP Testing Guide](https://owasp.org/www-project-web-security-testing-guide/)
- [NIST Cybersecurity Framework](https://www.nist.gov/cyberframework)
- [Azure Security Documentation](https://docs.microsoft.com/en-us/azure/security/)
- [Penetration Testing Execution Standard](http://www.pentest-standard.org/)

## 🏆 CRITERIOS DE ÉXITO
- [ ] Chocolatey instalado y funcional
- [ ] .NET Core 9 SDK instalado y verificado
- [ ] Azure CLI autenticado y operacional
- [ ] Todas las herramientas de penetration testing instaladas
- [ ] Visual Studio Code configurado con extensiones
- [ ] Conectividad Azure verificada
- [ ] Documentación de configuración completada