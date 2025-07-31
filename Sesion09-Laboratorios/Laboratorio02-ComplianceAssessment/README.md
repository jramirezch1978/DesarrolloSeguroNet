# Laboratorio 2: Compliance Framework Assessment

## 📋 Descripción General

Este laboratorio implementa un sistema comprensivo de evaluación de cumplimiento basado en marcos internacionales ISO 27001, SOC 2 Type II y NIST Cybersecurity Framework. Los participantes desarrollarán herramientas automatizadas para evaluar postura de seguridad Azure y generar reportes de compliance de nivel profesional.

## 🎯 Objetivos del Laboratorio

### Objetivos Principales
- **Implementar Assessment Automatizado**: Desarrollar herramientas que evalúen compliance contra múltiples frameworks
- **Mapear Controles de Seguridad**: Vincular configuraciones Azure con requerimientos específicos de compliance
- **Generar Reportes Ejecutivos**: Crear documentación que traduzca hallazgos técnicos a riesgo empresarial
- **Establecer Métricas de Compliance**: Implementar scoring cuantitativo para diferentes frameworks

### Competencias Desarrolladas
1. **Framework Assessment**: ISO 27001, SOC 2, NIST CSF
2. **Azure Security Evaluation**: Análisis comprensivo de configuraciones
3. **Gap Analysis**: Identificación sistemática de brechas de compliance
4. **Risk Quantification**: Traducción de gaps técnicos a impacto empresarial
5. **Automated Reporting**: Generación de documentación professional

## 🛠️ Tecnologías y Herramientas

### Stack Tecnológico
- **.NET Core 9.0**: Platform de desarrollo principal
- **Azure SDK for .NET**: Integración con servicios Azure
- **Azure Resource Manager**: Gestión y evaluación de recursos
- **PowerShell Core**: Automatización y scripting avanzado

### Herramientas de Assessment
- **Azure CLI**: Interface de línea de comandos para Azure
- **Azure Policy**: Evaluación de configuraciones y compliance
- **Azure Security Center**: Análisis de postura de seguridad
- **ScoutSuite**: Herramienta de auditoría multi-cloud

## 📁 Estructura del Proyecto

```
Laboratorio02-ComplianceAssessment/
├── README.md                           # Esta documentación
├── ComplianceAssessmentTool/            # Herramienta principal de assessment
│   ├── Program.cs                       # Motor de evaluación principal
│   ├── Models/                          # Modelos de datos de compliance
│   │   ├── ComplianceFramework.cs       # Definiciones de frameworks
│   │   ├── ComplianceFinding.cs         # Estructura de hallazgos
│   │   └── AssessmentReport.cs          # Modelos de reportes
│   ├── Assessors/                       # Evaluadores específicos por dominio
│   │   ├── IdentityAccessAssessor.cs    # Evaluación IAM
│   │   ├── NetworkSecurityAssessor.cs   # Evaluación de red
│   │   ├── DataProtectionAssessor.cs    # Evaluación de protección de datos
│   │   └── MonitoringAssessor.cs        # Evaluación de monitoreo
│   ├── Frameworks/                      # Implementaciones específicas de frameworks
│   │   ├── ISO27001Assessor.cs          # Assessment ISO 27001
│   │   ├── SOC2Assessor.cs              # Assessment SOC 2 Type II
│   │   └── NISTCSFAssessor.cs           # Assessment NIST CSF
│   └── ComplianceAssessmentTool.csproj  # Configuración del proyecto
├── PolicyTemplates/                     # Plantillas de Azure Policy
│   ├── ISO27001-Policies.json           # Políticas para ISO 27001
│   ├── SOC2-Policies.json               # Políticas para SOC 2
│   └── NIST-Policies.json               # Políticas para NIST CSF
├── Reports/                             # Reportes generados
│   ├── compliance-assessment-report.json # Reporte técnico detallado
│   ├── executive-summary.md             # Resumen ejecutivo
│   ├── gap-analysis-matrix.csv          # Matriz de análisis de brechas
│   └── remediation-roadmap.md           # Plan de remediación
├── Scripts/                             # Scripts de automatización
│   ├── deploy-policies.ps1              # Despliegue de políticas de compliance
│   ├── compliance-scan.ps1              # Escaneo automatizado de compliance
│   └── generate-reports.ps1             # Generación de reportes
└── Documentation/                       # Documentación adicional
    ├── frameworks-mapping.md            # Mapeo entre frameworks
    ├── azure-controls-matrix.md         # Matriz de controles Azure
    └── scoring-methodology.md           # Metodología de scoring
```

## 🏛️ Frameworks de Compliance Implementados

### ISO 27001:2022 - Information Security Management
**Dominios Evaluados:**
- **A.5** - Políticas de Seguridad de la Información
- **A.6** - Organización de la Seguridad de la Información
- **A.8** - Gestión de Activos
- **A.9** - Control de Acceso
- **A.12** - Seguridad de las Operaciones
- **A.13** - Seguridad de las Comunicaciones
- **A.14** - Adquisición, Desarrollo y Mantenimiento de Sistemas

**Controles Clave Evaluados:**
```
A.9.1.2 - Access to networks and network services
A.9.2.3 - Management of privileged access rights  
A.9.4.2 - Secure log-on procedures
A.12.6.1 - Management of technical vulnerabilities
A.13.1.1 - Network controls
A.14.2.1 - Secure development policy
```

### SOC 2 Type II - Trust Services Criteria
**Criterios Evaluados:**
- **Security (Obligatorio)** - Protección contra acceso no autorizado
- **Availability** - Disponibilidad del sistema según acuerdos
- **Processing Integrity** - Procesamiento completo y preciso
- **Confidentiality** - Protección de información confidencial
- **Privacy** - Manejo de información personal

**Controles SOC 2 Mapeados:**
```
CC6.1 - Logical access controls
CC6.2 - Prior to issuing system credentials
CC6.3 - User access modifications
CC7.1 - Data transmission and disposal
CC8.1 - Change management procedures
```

### NIST Cybersecurity Framework 2.0
**Funciones Principales:**
- **IDENTIFY** - Entendimiento del contexto empresarial
- **PROTECT** - Desarrollo e implementación de salvaguardas
- **DETECT** - Identificación oportuna de eventos de ciberseguridad
- **RESPOND** - Desarrollo e implementación de actividades de respuesta
- **RECOVER** - Desarrollo e implementación de planes de resiliencia

**Categorías Específicas Evaluadas:**
```
ID.AM - Asset Management
PR.AC - Identity Management and Access Control
PR.DS - Data Security
DE.CM - Security Continuous Monitoring
RS.RP - Response Planning
RC.RP - Recovery Planning
```

## 🔍 Metodología de Assessment

### Fase 1: Discovery y Inventario (20 minutos)
**Objetivo**: Identificar y catalogar todos los recursos Azure en scope

```powershell
# Ejecutar discovery automatizado
cd Laboratorio02-ComplianceAssessment/Scripts
.\compliance-scan.ps1 -SubscriptionId "your-subscription-id" -Phase Discovery

# El script ejecutará:
# - Inventario de recursos Azure
# - Identificación de configuraciones de seguridad  
# - Catalogación de políticas activas
# - Mapeo de controles de acceso
```

**Entregables:**
- Inventario completo de recursos Azure
- Mapa de configuraciones de seguridad actuales
- Baseline de políticas y controles existentes

### Fase 2: Control Assessment (25 minutos)
**Objetivo**: Evaluar controles específicos contra requerimientos de frameworks

```powershell
# Ejecutar assessment por framework
.\compliance-scan.ps1 -Framework ISO27001 -DetailedAssessment

# Evaluará automáticamente:
# - Configuraciones IAM contra controles A.9
# - Seguridad de red contra controles A.13
# - Gestión de vulnerabilidades contra controles A.12
# - Protección de datos contra controles A.8
```

**Entregables:**
- Matrix de compliance por framework
- Identificación de gaps específicos
- Scoring cuantitativo por dominio

### Fase 3: Gap Analysis (20 minutos)
**Objetivo**: Identificar brechas específicas y priorizar remediación

```csharp
// El assessment tool ejecutará análisis como:
var gapAnalysis = await assessmentEngine.AnalyzeComplianceGaps(
    targetFrameworks: new[] { "ISO27001", "SOC2", "NIST" },
    currentConfiguration: azureEnvironment,
    businessContext: organizationProfile
);

// Generará priorización basada en:
// - Severidad del riesgo
// - Costo de remediación  
// - Impacto empresarial
// - Dependencias técnicas
```

**Entregables:**
- Análisis detallado de brechas por framework
- Priorización de remediación basada en riesgo
- Estimaciones de esfuerzo y costo

### Fase 4: Reporting (10 minutos)
**Objetivo**: Generar documentación ejecutiva y técnica

```powershell
# Generar reportes comprehensivos
.\generate-reports.ps1 -AssessmentResults "latest-assessment.json"

# Producirá:
# - Executive summary con KPIs de compliance
# - Technical findings con detalles de implementación
# - Remediation roadmap con cronogramas
# - Cost-benefit analysis para inversiones de seguridad
```

**Entregables:**
- Reporte ejecutivo con métricas de compliance
- Documentación técnica detallada
- Plan de remediación priorizado
- Business case para inversiones de seguridad

## 📊 Metodología de Scoring

### Sistema de Puntuación Cuantitativa

#### Scoring por Framework (0-100%)
```csharp
public class ComplianceScore
{
    public double ImplementedControls { get; set; }    // Controles implementados
    public double PartialControls { get; set; }       // Controles parcialmente implementados  
    public double MissingControls { get; set; }       // Controles faltantes
    public double OverallScore { get; set; }          // Score general ponderado
    public RiskLevel RiskRating { get; set; }         // Calificación de riesgo
}

// Cálculo del score:
// OverallScore = (ImplementedControls * 1.0) + (PartialControls * 0.5) + (MissingControls * 0.0)
// RiskRating basado en thresholds: >90% = Low, 70-90% = Medium, <70% = High
```

#### Ponderación por Criticidad de Control
| Tipo de Control | Peso | Justificación |
|-----------------|------|---------------|
| Acceso y Autenticación | 25% | Base fundamental de seguridad |
| Protección de Datos | 20% | Impacto directo en compliance |
| Monitoreo y Logging | 15% | Detección y respuesta |
| Gestión de Vulnerabilidades | 15% | Prevención proactiva |
| Seguridad de Red | 15% | Perímetro de defensa |
| Gestión de Configuración | 10% | Operaciones y mantenimiento |

#### Matriz de Riesgo Empresarial
```
Compliance Score vs Business Impact:

90-100%  │ ✅ COMPLIANT     │ Low Risk      │ Maintenance Mode
80-89%   │ ⚠️ MOSTLY COMPLIANT │ Low-Med Risk  │ Minor Gaps
70-79%   │ ⚠️ PARTIALLY COMPLIANT │ Medium Risk   │ Significant Gaps  
60-69%   │ ❌ NON-COMPLIANT │ High Risk     │ Major Remediation
<60%     │ ❌ CRITICAL GAPS │ Critical Risk │ Emergency Action
```

## 🔧 Configuración e Instalación

### Prerrequisitos
```powershell
# Verificar .NET Core 9
dotnet --version  # Debe ser 9.0.x

# Verificar Azure CLI
az --version

# Verificar permisos Azure
az account show
```

### Configuración de Permisos Azure
```powershell
# Roles mínimos requeridos para assessment:
# - Reader: Para leer configuraciones de recursos
# - Security Reader: Para acceder a configuraciones de seguridad
# - Policy Reader: Para evaluar políticas asignadas

# Verificar roles asignados
az role assignment list --assignee $(az account show --query user.name -o tsv) --output table
```

### Instalación del Assessment Tool
```powershell
# Navegar al directorio del laboratorio
cd Laboratorio02-ComplianceAssessment/ComplianceAssessmentTool

# Restaurar dependencias
dotnet restore

# Compilar el proyecto
dotnet build

# Configurar connection a Azure
az login
az account set --subscription "your-subscription-id"

# Ejecutar assessment inicial
dotnet run -- --subscription "your-subscription-id" --frameworks "ISO27001,SOC2,NIST"
```

## 📋 Casos de Uso Específicos

### CU-001: Assessment ISO 27001 Completo
```powershell
# Ejecutar assessment completo ISO 27001
dotnet run -- --framework ISO27001 --detailed --export-evidence

# El tool evaluará:
# - A.9 (Access Control): 47 controles específicos
# - A.12 (Operations Security): 31 controles
# - A.13 (Communications Security): 23 controles
# - A.14 (System Development): 19 controles

# Generará:
# - Detailed findings report
# - Evidence package para auditores
# - Gap analysis con remediation steps
```

### CU-002: SOC 2 Type II Readiness Assessment
```csharp
// El assessment evaluará efectividad operacional de controles:
var soc2Assessment = new SOC2TypeIIAssessor();
var findings = await soc2Assessment.EvaluateControlEffectiveness(
    evaluationPeriod: TimeSpan.FromMonths(6),  // Período de efectividad requerido
    evidenceRequirements: SOC2EvidenceTypes.All,
    auditTrailRequired: true
);

// Verificará evidencia para:
// - Logical access control reviews (monthly)
// - Vulnerability management procedures (continuous)  
// - Incident response procedures (as-needed)
// - Change management documentation (per-change)
```

### CU-003: NIST CSF Maturity Assessment
```powershell
# Assessment de madurez por función NIST
.\Scripts\nist-maturity-assessment.ps1

# Evaluará nivel de madurez (1-4) para:
# - IDENTIFY: Asset management, governance, risk assessment
# - PROTECT: Access control, data security, protective processes
# - DETECT: Anomalies detection, continuous monitoring  
# - RESPOND: Response planning, communications, analysis
# - RECOVER: Recovery planning, improvements, communications

# Generará roadmap de madurez con timelines específicos
```

## 📈 Métricas y KPIs de Compliance

### KPIs Operacionales
- **Coverage Rate**: % de recursos Azure evaluados automáticamente
- **Detection Rate**: % de configuraciones incorrectas identificadas
- **False Positive Rate**: % de findings que no representan riesgo real
- **Remediation Rate**: % de findings resueltos dentro de SLA

### KPIs Empresariales  
- **Compliance Score**: Puntuación agregada de compliance (0-100%)
- **Risk Reduction**: Reducción cuantificada de exposición de riesgo
- **Audit Readiness**: % de evidencia requerida disponible automáticamente
- **Cost Avoidance**: Valor de multas/incidentes evitados por compliance

### Dashboard de Métricas
```csharp
public class ComplianceDashboard
{
    public ComplianceScorecard Overall { get; set; }
    public Dictionary<string, FrameworkScore> ByFramework { get; set; }
    public TrendAnalysis HistoricalTrends { get; set; }
    public List<CriticalFinding> RequiresAttention { get; set; }
    public RemediationProgress ProgressToDate { get; set; }
}

// El dashboard se actualiza automáticamente y proporciona:
// - Real-time compliance posture
// - Trend analysis over time  
// - Predictive analytics para audit readiness
// - ROI analysis de inversiones en compliance
```

## 🔧 Troubleshooting

### Problemas Comunes

#### Error: "Insufficient permissions for subscription analysis"
```powershell
# Verificar roles necesarios
az role assignment list --assignee $(az account show --query user.name -o tsv) `
  --query "[?roleDefinitionName=='Reader' || roleDefinitionName=='Security Reader']"

# Si no tiene permisos suficientes:
az role assignment create --assignee $(az account show --query user.name -o tsv) `
  --role "Security Reader" --scope "/subscriptions/$(az account show --query id -o tsv)"
```

#### Error: "Azure Resource Manager rate limiting"
```csharp
// El assessment tool incluye rate limiting automático:
public class AzureResourceAnalyzer
{
    private readonly RateLimitingService _rateLimiter;
    
    public async Task<AssessmentResults> AnalyzeResources()
    {
        await _rateLimiter.WaitForSlot(); // Respeta límites de Azure ARM
        // ... análisis de recursos
    }
}
```

#### Error: "Policy evaluation timeout"
```powershell
# Para suscripciones grandes, usar assessment incremental:
.\compliance-scan.ps1 -Mode Incremental -ResourceGroups "rg1,rg2,rg3"

# O usar assessment paralelo:
.\compliance-scan.ps1 -ParallelResourceGroups 5 -MaxConcurrency 10
```

## 🎓 Recursos de Aprendizaje

### Documentación de Frameworks
- **ISO 27001:2022**: [ISO Official Documentation](https://www.iso.org/standard/27001)
- **SOC 2 Type II**: [AICPA Trust Services Criteria](https://www.aicpa.org/content/dam/aicpa/interestareas/frc/assuranceadvisoryservices/downloadabledocuments/trust-services-criteria.pdf)
- **NIST CSF 2.0**: [NIST Cybersecurity Framework](https://www.nist.gov/cyberframework)

### Azure Security Resources
- **Azure Security Benchmark**: [Microsoft Documentation](https://docs.microsoft.com/en-us/security/benchmark/azure/)
- **Azure Policy Samples**: [GitHub Repository](https://github.com/Azure/azure-policy)
- **Azure Security Center**: [Security Recommendations](https://docs.microsoft.com/en-us/azure/security-center/)

### Compliance Tools
- **ScoutSuite**: [Multi-cloud Security Auditing](https://github.com/nccgroup/ScoutSuite)
- **Prowler**: [AWS/Azure Security Assessment](https://github.com/prowler-cloud/prowler)
- **Azure Compliance Manager**: [Microsoft 365 Compliance](https://compliance.microsoft.com/)

## 🏆 Certificación de Competencias

Al completar este laboratorio exitosamente, el participante habrá demostrado:

### Competencias de Compliance
- ✅ **Multi-Framework Assessment**: Evaluación contra ISO 27001, SOC 2, NIST CSF
- ✅ **Gap Analysis**: Identificación sistemática de brechas de compliance  
- ✅ **Risk Quantification**: Traducción de gaps a impacto empresarial
- ✅ **Evidence Collection**: Recopilación automatizada de evidencia de auditoría

### Competencias Técnicas Azure
- ✅ **Azure Resource Analysis**: Evaluación comprehensiva de configuraciones
- ✅ **Policy Implementation**: Desarrollo y despliegue de políticas de compliance
- ✅ **Automated Assessment**: Herramientas de evaluación continua
- ✅ **Security Monitoring**: Implementación de monitoreo de compliance

### Competencias Profesionales
- ✅ **Executive Reporting**: Comunicación de riesgo a nivel C-suite
- ✅ **Audit Preparation**: Preparación para auditorías de terceros
- ✅ **Compliance Strategy**: Desarrollo de estrategias de compliance organizacional
- ✅ **Business Case Development**: ROI analysis para inversiones en compliance

---

## 📞 Soporte y Contacto

**Instructor**: Jhonny Ramirez Chiroque  
**Curso**: Diseño Seguro de Aplicaciones (.NET en Azure)  
**Sesión**: 09 - Pruebas de Penetración y Auditorías Avanzadas  
**Fecha**: 25 de Julio de 2025

Para soporte técnico o consultas sobre assessment de compliance, contactar durante las horas de sesión programadas.