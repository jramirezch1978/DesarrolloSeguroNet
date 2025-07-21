# 🧪 Laboratorio 1: Network Security Groups Avanzados

## Información General
- **Duración:** 25 minutos
- **Objetivo:** Crear y configurar NSGs con reglas granulares para diferentes tipos de aplicaciones
- **Modalidad:** Práctica individual con código .NET

## Fundamentos Teóricos

### ¿Qué son los Network Security Groups?

Los Network Security Groups (NSGs) son el componente fundamental de seguridad de red en Azure. Funcionan como un firewall distribuido y stateful que opera en las capas 3 y 4 del modelo OSI, proporcionando control granular sobre el tráfico de red hacia y desde recursos de Azure.

### Arquitectura de NSGs

Los NSGs implementan una arquitectura de **"deny by default"** con las siguientes características:

```
┌─────────────────────────────────────────────────────────┐
│                    INTERNET                              │
└─────────────────────┬───────────────────────────────────┘
                      │
                ┌─────▼─────┐
                │ Azure LB  │
                └─────┬─────┘
                      │
        ┌─────────────▼─────────────┐
        │     NSG (Subnet Level)    │ ◄── Evaluación 1
        │   Priority 100-4096       │
        └─────────────┬─────────────┘
                      │
            ┌─────────▼─────────┐
            │     Subnet        │
            │   (10.0.1.0/24)   │
            └─────────┬─────────┘
                      │
        ┌─────────────▼─────────────┐
        │      NSG (NIC Level)      │ ◄── Evaluación 2  
        │   Priority 100-4096       │
        └─────────────┬─────────────┘
                      │
                ┌─────▼─────┐
                │    VM     │
                └───────────┘
```

### Service Tags: La Revolución en Simplicidad

Los Service Tags son etiquetas dinámicas que representan grupos de prefijos de direcciones IP de servicios específicos de Azure:

#### Service Tags Fundamentales
- **Internet**: Todo el tráfico público de Internet
- **VirtualNetwork**: Toda la red virtual, incluyendo peerings y VPN
- **AzureLoadBalancer**: IPs de health probes de Azure Load Balancer
- **Storage**: Todos los endpoints de Azure Storage
- **Sql**: Todos los endpoints de Azure SQL Database
- **AzureActiveDirectory**: Endpoints de Azure AD

#### Service Tags Regionales
```csharp
// Ejemplos de Service Tags regionales
"Storage.EastUS"           // Solo Storage en East US
"Sql.WestEurope"          // Solo SQL en West Europe
"AzureMonitor.CentralUS"  // Solo Azure Monitor en Central US
```

### Application Security Groups (ASGs)

Los ASGs permiten agrupar máquinas virtuales lógicamente por función, independientemente de su dirección IP:

```csharp
// Agrupación tradicional por IP
"10.0.1.4", "10.0.1.5", "10.0.1.6" // Web Servers

// Agrupación moderna por función
"ASG-WebServers" // Contiene cualquier VM asignada, sin importar IP
```

### Procesamiento de Reglas NSG

#### Algoritmo de Evaluación
1. **Orden de prioridad**: 100 (más alta) → 4096 (más baja)
2. **Primera coincidencia gana**: Se detiene en la primera regla que coincida
3. **Deny implícito**: Si ninguna regla permite, se bloquea por defecto

#### Flujo de Evaluación para Tráfico Entrante
```
Internet → Subnet NSG → NIC NSG → Virtual Machine
```

#### Flujo de Evaluación para Tráfico Saliente  
```
Virtual Machine → NIC NSG → Subnet NSG → Internet
```

## Laboratorio Práctico

### Paso 1: Preparación del Entorno (5 minutos)

#### Crear Resource Group Base
```bash
# Establecer variables
$resourceGroup = "rg-nsg-lab-$env:USERNAME"
$location = "eastus"
$vnetName = "vnet-nsg-lab"

# Crear resource group
az group create --name $resourceGroup --location $location
```

#### Crear Virtual Network con Subredes
```bash
# Crear VNET principal
az network vnet create `
  --resource-group $resourceGroup `
  --name $vnetName `
  --address-prefix 10.2.0.0/16 `
  --subnet-name snet-web `
  --subnet-prefix 10.2.1.0/24

# Crear subred para aplicaciones
az network vnet subnet create `
  --resource-group $resourceGroup `
  --vnet-name $vnetName `
  --name snet-app `
  --address-prefix 10.2.2.0/24

# Crear subred para bases de datos
az network vnet subnet create `
  --resource-group $resourceGroup `
  --vnet-name $vnetName `
  --name snet-data `
  --address-prefix 10.2.3.0/24
```

### Paso 2: Implementación de NSGs con .NET (10 minutos)

El proyecto `NSGManager` incluye una aplicación de consola completa para gestionar NSGs programáticamente.

#### Características del NSGManager
- **Creación automatizada** de NSGs con mejores prácticas
- **Gestión de reglas** granulares por prioridad
- **Soporte completo** para Application Security Groups
- **Validación** de configuraciones antes de aplicar
- **Logging detallado** de todas las operaciones

#### Ejecutar la Aplicación
```bash
# Navegar al directorio del proyecto
cd src/NSGManager

# Restaurar paquetes
dotnet restore

# Compilar aplicación
dotnet build

# Ejecutar con parámetros
dotnet run -- --resource-group $resourceGroup --location $location
```

#### Opciones de Comando
```bash
# Crear NSGs básicos
dotnet run -- create-basic

# Crear NSGs con ASGs
dotnet run -- create-advanced

# Validar configuración existente
dotnet run -- validate

# Generar reporte de seguridad
dotnet run -- security-report

# Limpiar recursos
dotnet run -- cleanup
```

### Paso 3: Configuration de NSGs Avanzados (10 minutos)

#### NSG para Web Tier - Características Avanzadas
```csharp
// Reglas implementadas automáticamente por NSGManager
var webNsgRules = new[]
{
    // Priority 100: HTTPS desde Internet
    new SecurityRule
    {
        Priority = 100,
        Name = "Allow-HTTPS-Inbound",
        Access = "Allow",
        Direction = "Inbound",
        Protocol = "TCP",
        SourcePortRange = "*",
        DestinationPortRange = "443",
        SourceAddressPrefix = "Internet",
        DestinationAddressPrefix = "VirtualNetwork"
    },
    
    // Priority 110: HTTP desde Internet (para redirecciones)
    new SecurityRule
    {
        Priority = 110,
        Name = "Allow-HTTP-Inbound", 
        Access = "Allow",
        Direction = "Inbound",
        Protocol = "TCP",
        SourcePortRange = "*",
        DestinationPortRange = "80",
        SourceAddressPrefix = "Internet",
        DestinationAddressPrefix = "VirtualNetwork"
    },
    
    // Priority 120: Health Probes
    new SecurityRule
    {
        Priority = 120,
        Name = "Allow-AzureLB-Inbound",
        Access = "Allow", 
        Direction = "Inbound",
        Protocol = "*",
        SourcePortRange = "*",
        DestinationPortRange = "*",
        SourceAddressPrefix = "AzureLoadBalancer",
        DestinationAddressPrefix = "*"
    },
    
    // Priority 4000: Bloqueo explícito de protocolos inseguros
    new SecurityRule
    {
        Priority = 4000,
        Name = "Deny-Insecure-Protocols",
        Access = "Deny",
        Direction = "Inbound", 
        Protocol = "TCP",
        SourcePortRange = "*",
        DestinationPortRanges = new[] { "23", "21", "25", "110", "143" },
        SourceAddressPrefix = "*",
        DestinationAddressPrefix = "*"
    }
};
```

#### Application Security Groups - Implementación
```csharp
// ASGs creados automáticamente
var asgs = new[]
{
    "asg-webservers",    // Servidores web (IIS, Apache, Nginx)
    "asg-appservers",    // Servidores de aplicación (.NET, Java, Node.js)  
    "asg-dbservers",     // Servidores de base de datos (SQL Server, MySQL)
    "asg-mgmtservers"    // Servidores de gestión (Bastion, Jump boxes)
};
```

#### Reglas Basadas en ASGs
```csharp
// Reglas de micro-segmentación
var asgRules = new[]
{
    // Web servers pueden comunicarse con app servers
    new SecurityRule
    {
        Priority = 100,
        Name = "Allow-Web-to-App",
        Access = "Allow",
        Direction = "Outbound",
        Protocol = "TCP", 
        SourceApplicationSecurityGroups = new[] { "asg-webservers" },
        DestinationApplicationSecurityGroups = new[] { "asg-appservers" },
        DestinationPortRanges = new[] { "80", "443", "8080", "8443" }
    },
    
    // App servers pueden comunicarse con DB servers
    new SecurityRule
    {
        Priority = 110,
        Name = "Allow-App-to-DB",
        Access = "Allow",
        Direction = "Outbound", 
        Protocol = "TCP",
        SourceApplicationSecurityGroups = new[] { "asg-appservers" },
        DestinationApplicationSecurityGroups = new[] { "asg-dbservers" },
        DestinationPortRanges = new[] { "1433", "3306", "5432", "1521" }
    },
    
    // Bloqueo explícito: Web servers NO pueden acceder a DB servers
    new SecurityRule
    {
        Priority = 200,
        Name = "Deny-Web-to-DB",
        Access = "Deny",
        Direction = "Outbound",
        Protocol = "*", 
        SourceApplicationSecurityGroups = new[] { "asg-webservers" },
        DestinationApplicationSecurityGroups = new[] { "asg-dbservers" },
        DestinationPortRange = "*"
    }
};
```

## Conceptos Avanzados Implementados

### 1. Zero Trust Network Architecture
```csharp
// Principios implementados en el código
public class ZeroTrustPrinciples
{
    // Nunca confíes, siempre verifica
    public bool NeverTrustAlwaysVerify => true;
    
    // Menor privilegio por defecto
    public string DefaultAccess => "Deny";
    
    // Verificación explícita de cada conexión
    public bool ExplicitVerification => true;
    
    // Segmentación de red granular
    public bool MicroSegmentation => true;
}
```

### 2. Compliance y Auditoría
```csharp
// Configuraciones que cumplen estándares
public class ComplianceSettings
{
    // PCI DSS: Segmentación de red
    public bool PCIDSSCompliant => HasDatabaseSegmentation();
    
    // HIPAA: Cifrado en tránsito obligatorio
    public bool HIPAACompliant => RequiresHTTPS();
    
    // SOX: Auditoría completa de accesos
    public bool SOXCompliant => EnablesFullAuditing();
    
    // GDPR: Control de ubicación geográfica
    public bool GDPRCompliant => RestrictsGeographicAccess();
}
```

### 3. Automatización DevSecOps
```csharp
// Integración con pipelines CI/CD
public class DevSecOpsIntegration
{
    // Validación automática en deployment
    public async Task<bool> ValidateSecurityPosture()
    {
        var violations = await SecurityScanner.ScanNSGRules();
        return violations.Count == 0;
    }
    
    // Rollback automático si se detectan vulnerabilidades
    public async Task AutoRollbackIfInsecure()
    {
        if (!await ValidateSecurityPosture())
        {
            await RollbackToLastKnownGoodState();
        }
    }
}
```

## Scripts de PowerShell Incluidos

### script-create-infrastructure.ps1
- Crea toda la infraestructura base
- Configura VNETs con subredes optimizadas
- Establece convenciones de naming

### script-deploy-nsgs.ps1  
- Despliega NSGs con mejores prácticas
- Configura reglas según el patrón de arquitectura
- Valida configuraciones post-deployment

### script-test-connectivity.ps1
- Prueba conectividad entre todos los tiers
- Valida que las reglas NSG funcionan correctamente
- Genera reportes de conectividad

### script-security-audit.ps1
- Audita configuraciones de seguridad
- Identifica reglas demasiado permisivas
- Sugiere mejoras basadas en mejores prácticas

## Templates ARM/Bicep

### nsg-web-tier.bicep
```bicep
// Template optimizado para web tier
resource webNSG 'Microsoft.Network/networkSecurityGroups@2023-09-01' = {
  name: 'nsg-web-${environmentName}'
  location: location
  properties: {
    securityRules: [
      {
        name: 'Allow-HTTPS-Inbound'
        properties: {
          priority: 100
          access: 'Allow'
          direction: 'Inbound'
          protocol: 'TCP'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: 'Internet'
          destinationAddressPrefix: 'VirtualNetwork'
        }
      }
      // ... más reglas optimizadas
    ]
  }
}
```

## Monitoreo y Observabilidad

### Flow Logs Habilitados
El laboratorio configura automáticamente:
- **Flow Logs v2** para captura detallada
- **Storage Account** optimizado para logs
- **Log Analytics Workspace** para análisis
- **Alertas proactivas** basadas en patrones

### Métricas Clave Monitoreadas
- **Packets Allowed/Denied**: Volumen de tráfico por decisión
- **Flows Created**: Nuevas conexiones por minuto
- **Rule Hit Count**: Qué reglas se activan más frecuentemente
- **Latency Impact**: Impacto en performance de las reglas NSG

## Resultados de Aprendizaje

Al completar este laboratorio, habrán dominado:

### Conocimientos Técnicos
- ✅ **Arquitectura NSG avanzada** con múltiples capas
- ✅ **Service Tags** para simplificación y mantenimiento  
- ✅ **Application Security Groups** para escalabilidad
- ✅ **Programación con Azure SDK** para automatización
- ✅ **Templates Infrastructure as Code** con Bicep

### Habilidades Prácticas
- ✅ **Diseño de reglas** granulares y seguras
- ✅ **Troubleshooting** de conectividad de red
- ✅ **Automatización** de deployment y gestión
- ✅ **Compliance** con frameworks de seguridad
- ✅ **Monitoreo proactivo** y alerting

### Competencias Empresariales
- ✅ **Zero Trust Architecture** implementación práctica
- ✅ **DevSecOps Integration** en pipelines CI/CD
- ✅ **Cost Optimization** de recursos de red
- ✅ **Risk Assessment** y mitigación
- ✅ **Disaster Recovery** planning para redes

## Arquitectura Final Implementada

```
                    ┌─────────────────┐
                    │    INTERNET     │
                    └─────────┬───────┘
                              │
                    ┌─────────▼───────┐
                    │  Azure LB/AppGW │
                    └─────────┬───────┘
                              │
         ┌────────────────────▼────────────────────┐
         │           WEB TIER (snet-web)           │
         │  NSG: Allow 80,443 from Internet        │
         │  ASG: asg-webservers                   │
         └────────────────────┬────────────────────┘
                              │ Allow 8080,8443
         ┌────────────────────▼────────────────────┐
         │          APP TIER (snet-app)            │
         │  NSG: Allow from Web Tier only          │
         │  ASG: asg-appservers                   │
         └────────────────────┬────────────────────┘
                              │ Allow 1433,3306,5432
         ┌────────────────────▼────────────────────┐
         │         DATA TIER (snet-data)           │
         │  NSG: Allow from App Tier only          │
         │  ASG: asg-dbservers                    │
         └─────────────────────────────────────────┘
```

## Troubleshooting Común

### Error: "NSG rule conflicts"
```powershell
# Verificar prioridades y conflictos
.\scripts\script-validate-rules.ps1 -ResourceGroup $resourceGroup
```

### Error: "Cannot reach application"
```powershell
# Test de conectividad end-to-end
.\scripts\script-test-connectivity.ps1 -Verbose
```

### Error: "Performance issues"
```powershell
# Análisis de impacto de NSG en latencia
.\scripts\script-performance-analysis.ps1
```

---

**¡Excelente trabajo!** Han implementado una arquitectura de seguridad de red enterprise-grade que puede proteger aplicaciones críticas de negocio.

**Siguiente:** [Laboratorio 2 - Azure DDoS Protection](../Laboratorio2-DDoS/README.md) 