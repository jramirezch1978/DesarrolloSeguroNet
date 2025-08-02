# Código Fuente - Laboratorios de Seguridad

## 📋 Descripción
Código fuente y ejemplos para los laboratorios de la Sesión 8: "Pruebas y Auditorías de Seguridad en Azure".

## 📁 Contenido

### 🔧 Aplicaciones .NET Core
- **Secure Score Analyzer** - Aplicación para análisis de Secure Score
- **Vulnerability Scanner** - Herramientas de escaneo de vulnerabilidades
- **Security Reporter** - Generación de reportes de seguridad

### 🐳 Contenedores Docker
- **Vulnerable Test Images** - Imágenes con vulnerabilidades conocidas
- **Security Tools** - Herramientas de seguridad en contenedores

### 📊 Scripts y Utilidades
- **PowerShell Scripts** - Automatización de tareas
- **Azure CLI Scripts** - Configuración de recursos
- **JSON Templates** - Plantillas para políticas y configuraciones

## 🚀 Estructura de Proyectos

```
code/
├── SecureScoreAnalyzer/          # Aplicación .NET Core
│   ├── Program.cs
│   ├── SecureScoreService.cs
│   └── appsettings.json
├── VulnerabilityScanner/         # Herramientas de escaneo
│   ├── OpenVAS/
│   └── Qualys/
├── SecurityReporter/             # Generador de reportes
│   ├── ReportGenerator.cs
│   └── Templates/
└── Docker/                       # Imágenes Docker
    ├── vulnerable-app/
    └── security-tools/
```

## 💻 Tecnologías Utilizadas
- **.NET Core 9** - Aplicaciones principales
- **Azure Resource Manager** - APIs de Azure
- **Docker** - Contenedores y orquestación
- **PowerShell** - Automatización
- **Azure CLI** - Gestión de recursos

## 🔍 Características
- **Mock Data** - Datos simulados para demostración
- **Error Handling** - Manejo robusto de errores
- **Logging** - Registro detallado de operaciones
- **Configuration** - Configuración flexible
- **Testing** - Casos de prueba incluidos

## ⚠️ Notas de Seguridad
- **Solo para Testing:** El código está diseñado para laboratorios
- **No usar en Producción:** Contiene configuraciones de demostración
- **Vulnerable Images:** Solo para propósitos educativos
- **Credenciales:** Nunca incluir credenciales reales

---
**Volver:** [README Principal](../README.md) 