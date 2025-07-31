# 🔍 LABORATORIO 1: PENETRATION TESTING CON METODOLOGÍA OWASP
**Curso:** Diseño Seguro de Aplicaciones (.NET en Azure)  
**Sesión:** 9 - Pruebas de Penetración y Auditorías Avanzadas  
**Duración:** 25 minutos  
**Modalidad:** Práctica individual con aplicación vulnerable

## 🎯 OBJETIVO
Implementar metodología OWASP para penetration testing sistemático, ejecutando ataques controlados contra una aplicación web intencionalmente vulnerable para identificar y explotar vulnerabilidades de seguridad comunes.

## 📋 DESCRIPCIÓN
Este laboratorio guía a los estudiantes a través de un penetration test completo usando metodologías profesionales. Incluye la creación de una aplicación web vulnerable con fallas de seguridad intencionales que reflejan problemas del mundo real, seguido de la ejecución sistemática de pruebas de penetración para identificar, explotar y documentar vulnerabilidades.

## 🔑 CONCEPTOS CLAVE

### **Metodología OWASP Testing Guide**
- **Reconnaissance:** Recopilación de información sobre el objetivo
- **Scanning:** Identificación de servicios y vulnerabilidades potenciales
- **Exploitation:** Demostración práctica de explotación de vulnerabilidades
- **Post-exploitation:** Evaluación del alcance del compromiso
- **Reporting:** Documentación profesional de hallazgos

### **Vulnerabilidades del OWASP Top 10**
- **SQL Injection (A03:2021):** Inyección de código SQL malicioso
- **Cross-Site Scripting - XSS (A03:2021):** Ejecución de scripts en navegadores de víctimas
- **Broken Authentication (A07:2021):** Fallas en sistemas de autenticación
- **Security Misconfiguration (A05:2021):** Configuraciones inseguras de aplicaciones
- **Command Injection:** Ejecución de comandos de sistema operativo

### **Herramientas de Penetration Testing**
- **Nmap:** Escaneo de red y descubrimiento de servicios
- **cURL:** Cliente HTTP para testing manual de APIs
- **Burp Suite:** Proxy interceptor para análisis de aplicaciones web
- **OWASP ZAP:** Escáner de seguridad para aplicaciones web

## 🏗️ ARQUITECTURA DE LA APLICACIÓN VULNERABLE

```
VulnerableWebApp (.NET Core 9)
├── Controllers/
│   └── VulnerableController.cs    # Endpoints vulnerables
├── Models/
│   ├── User.cs                    # Modelo de usuario
│   ├── SecretData.cs             # Datos sensibles
│   └── AppDbContext.cs           # Contexto de base de datos
├── Program.cs                     # Configuración de aplicación
└── appsettings.json              # Configuración (con secretos expuestos)

Vulnerabilidades Implementadas:
├── SQL Injection en búsqueda de usuarios
├── XSS Reflejado en función de búsqueda
├── Authentication Bypass via tokens predictibles
├── Command Injection en función ping
├── Information Disclosure en endpoint debug
└── Broken Access Control en panel administrativo
```

## 📚 MARCO TEÓRICO

### **Fases del Penetration Testing**
1. **Pre-engagement:** Definición de alcance y reglas de engagement
2. **Information Gathering:** Recopilación pasiva y activa de información
3. **Threat Modeling:** Identificación de vectores de ataque potenciales
4. **Vulnerability Analysis:** Identificación y clasificación de vulnerabilidades
5. **Exploitation:** Demostración controlada de explotación
6. **Post-Exploitation:** Evaluación del impacto y movimiento lateral
7. **Reporting:** Documentación y recomendaciones de remediación

### **Categorías de Vulnerabilidades**
- **Injection Flaws:** SQL, NoSQL, OS, LDAP injection
- **Broken Authentication:** Session management, password policies
- **Sensitive Data Exposure:** Encryption, data protection
- **XML External Entities (XXE):** XML parsing vulnerabilities
- **Broken Access Control:** Authorization flaws
- **Security Misconfiguration:** Default configs, error handling
- **Cross-Site Scripting (XSS):** Stored, reflected, DOM-based
- **Insecure Deserialization:** Object injection attacks
- **Using Components with Known Vulnerabilities:** Dependency management
- **Insufficient Logging & Monitoring:** Detection and response

## 🎯 OBJETIVOS DE APRENDIZAJE
Al completar este laboratorio, los estudiantes podrán:

1. **Ejecutar reconnaissance** sistemático usando herramientas profesionales
2. **Identificar vulnerabilidades** mediante técnicas de scanning automatizado y manual
3. **Explotar vulnerabilidades** de forma controlada y ética
4. **Documentar hallazgos** siguiendo estándares profesionales
5. **Aplicar metodología OWASP** de manera estructurada y repetible
6. **Evaluar el impacto** de vulnerabilidades en contextos empresariales

## 🔧 HERRAMIENTAS UTILIZADAS

| Herramienta | Propósito | Tipo de Testing |
|-------------|-----------|-----------------|
| .NET Core 9 | Aplicación vulnerable | Target |
| Nmap | Network reconnaissance | Information Gathering |
| cURL | HTTP request testing | Manual Testing |
| Burp Suite | Web application proxy | Intercepting Proxy |
| SQLMap | SQL injection testing | Automated Exploitation |
| Browser DevTools | Client-side analysis | Manual Analysis |

## 📝 VULNERABILIDADES IMPLEMENTADAS

### **1. SQL Injection (Crítica)**
- **Ubicación:** `/api/Vulnerable/user/{id}`
- **Tipo:** Union-based SQL injection
- **Impacto:** Extracción completa de base de datos
- **CVSS:** 9.8 (Crítica)

### **2. Cross-Site Scripting (Alta)**
- **Ubicación:** `/api/Vulnerable/search?query=`
- **Tipo:** Reflected XSS
- **Impacto:** Robo de sesiones, defacing
- **CVSS:** 6.1 (Media)

### **3. Authentication Bypass (Crítica)**
- **Ubicación:** `/api/Vulnerable/admin/secrets`
- **Tipo:** Predictable token generation
- **Impacto:** Acceso no autorizado a funciones administrativas
- **CVSS:** 8.1 (Alta)

### **4. Command Injection (Alta)**
- **Ubicación:** `/api/Vulnerable/ping`
- **Tipo:** OS command injection
- **Impacto:** Ejecución remota de comandos
- **CVSS:** 8.8 (Alta)

### **5. Information Disclosure (Media)**
- **Ubicación:** `/api/Vulnerable/debug/config`
- **Tipo:** Sensitive information exposure
- **Impacto:** Exposición de credenciales y configuraciones
- **CVSS:** 5.3 (Media)

## 🚀 INSTRUCCIONES DE EJECUCIÓN

### Fase 1: Despliegue de Aplicación Vulnerable
```bash
cd Lab1-PenetrationTesting/VulnerableWebApp
dotnet restore
dotnet build
dotnet run
```

### Fase 2: Reconnaissance
```bash
# Escaneo de puertos
nmap -sV localhost

# Descubrimiento de endpoints
curl -X GET http://localhost:5000/swagger/index.html
```

### Fase 3: Vulnerability Assessment
```bash
# Testing SQL Injection
curl -X GET "http://localhost:5000/api/Vulnerable/user/1' OR '1'='1"

# Testing XSS
curl -X GET "http://localhost:5000/api/Vulnerable/search?query=<script>alert('XSS')</script>"

# Testing Authentication Bypass
curl -X GET "http://localhost:5000/api/Vulnerable/admin/secrets?token=admin_1_1640995200"
```

### Fase 4: Exploitation
```bash
# SQL Injection para extracción de datos
curl -X GET "http://localhost:5000/api/Vulnerable/user/999 UNION SELECT Id,Title,Content,1 FROM SecretData--"

# Command Injection
curl -X POST http://localhost:5000/api/Vulnerable/ping \
  -H "Content-Type: application/json" \
  -d '{"host":"127.0.0.1 && dir"}'
```

## ⚠️ CONSIDERACIONES DE SEGURIDAD

### **Uso Ético y Legal**
- Esta aplicación es INTENCIONALMENTE vulnerable
- SOLO usar en entornos controlados y autorizados
- NO desplegar en redes de producción
- NO usar técnicas aprendidas contra sistemas sin autorización explícita

### **Aislamiento de Red**
- Ejecutar en máquinas virtuales aisladas
- Usar redes privadas para testing
- Desconectar de internet si es necesario

### **Documentación de Actividades**
- Registrar todas las pruebas realizadas
- Documentar hallazgos y evidencias
- Mantener logs para auditoría

## 📖 CASOS DE USO EMPRESARIALES

### **Evaluación de Seguridad Pre-Producción**
- Identificar vulnerabilidades antes del despliegue
- Validar efectividad de controles de seguridad
- Cumplir requerimientos de compliance

### **Training de Equipos de Desarrollo**
- Educar sobre vulnerabilidades comunes
- Demostrar impacto de fallas de seguridad
- Mejorar prácticas de desarrollo seguro

### **Penetration Testing Profesional**
- Metodología estructurada para evaluaciones
- Documentación estándar para reportes
- Técnicas reconocidas por la industria

## 📊 MÉTRICAS DE ÉXITO

### **Técnicas**
- [ ] Identificación de todas las vulnerabilidades implementadas
- [ ] Explotación exitosa de al menos 3 vulnerabilidades críticas
- [ ] Documentación completa de hallazgos
- [ ] Aplicación correcta de metodología OWASP

### **Comprensión**
- [ ] Explicación del impacto empresarial de cada vulnerabilidad
- [ ] Identificación de técnicas de remediación
- [ ] Comprensión de marcos de evaluación de riesgo
- [ ] Capacidad de comunicar hallazgos técnicos

## 📖 REFERENCIAS ADICIONALES
- [OWASP Web Security Testing Guide](https://owasp.org/www-project-web-security-testing-guide/)
- [OWASP Top 10 2021](https://owasp.org/Top10/)
- [PTES Technical Guidelines](http://www.pentest-standard.org/index.php/PTES_Technical_Guidelines)
- [NIST SP 800-115 - Technical Guide to Information Security Testing](https://csrc.nist.gov/publications/detail/sp/800-115/final)

## 🏆 ENTREGABLES
1. **Aplicación Vulnerable Funcional:** Aplicación desplegada y accesible
2. **Reporte de Reconnaissance:** Información recopilada sobre el objetivo
3. **Vulnerability Assessment:** Lista de vulnerabilidades identificadas
4. **Proof of Concepts:** Demostraciones de explotación exitosa
5. **Documentación Técnica:** Hallazgos documentados siguiendo estándares profesionales