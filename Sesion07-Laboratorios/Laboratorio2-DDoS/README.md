# 🧪 Laboratorio 2: Azure DDoS Protection

## Información General
- **Duración:** 20 minutos
- **Objetivo:** Configurar Azure DDoS Protection Standard y entender las métricas de protección
- **Modalidad:** Práctica individual con monitoreo en tiempo real

## Fundamentos Teóricos

### ¿Qué es Azure DDoS Protection?

Azure DDoS Protection es un servicio de mitigación de ataques distribuidos de denegación de servicio que protege las aplicaciones de Azure contra algunos de los ataques más comunes y devastadores en Internet.

### Tipos de Ataques DDoS

Los ataques DDoS se clasifican en tres categorías principales:

#### 1. Ataques Volumétricos (Layer 3/4)
```
🌊 UDP Floods
├── Objetivo: Saturar ancho de banda
├── Método: Paquetes UDP masivos a puertos aleatorios  
├── Volumen: Hasta 1+ Tbps
└── Detección: Análisis de volumen y patrones

🌊 ICMP Floods  
├── Objetivo: Agotar recursos de red
├── Método: Ping masivo coordinado
├── Volumen: Millones de paquetes/segundo
└── Detección: Anomalías en tráfico ICMP

🌊 Amplification Attacks
├── Objetivo: Magnificar el ataque usando servidores legítimos
├── Método: DNS, NTP, SSDP reflection
├── Amplificación: 50x - 4000x del tráfico original
└── Detección: Patrones de tráfico asimétrico
```

#### 2. Ataques de Protocolo (Layer 3/4)
```
⚡ SYN Floods
├── Objetivo: Agotar tabla de conexiones TCP
├── Método: SYN packets sin completar handshake
├── Impacto: Conexiones legítimas rechazadas
└── Detección: Ratio SYN/ACK anómalo

⚡ Ping of Death
├── Objetivo: Causar crashes del sistema
├── Método: Paquetes malformados fragmentados
├── Impacto: Inestabilidad del sistema operativo
└── Detección: Análisis de fragmentación
```

#### 3. Ataques de Aplicación (Layer 7)
```
🎯 HTTP Floods
├── Objetivo: Agotar recursos del servidor web
├── Método: Peticiones HTTP legítimas en volumen
├── Dificultad: Difícil de distinguir de tráfico real
└── Detección: Análisis de comportamiento y patrones

🎯 Slowloris
├── Objetivo: Agotar conexiones HTTP disponibles
├── Método: Conexiones parciales de larga duración
├── Eficiencia: Requiere pocos recursos del atacante
└── Detección: Análisis de tiempo de conexión
```

### Azure DDoS Protection: Básico vs Standard

| Característica | DDoS Basic | DDoS Standard |
|----------------|------------|---------------|
| **Costo** | Gratuito | ~$2,944/mes |
| **Cobertura** | L3/L4 básica | L3/L4/L7 avanzada |
| **Capacidad** | Limitada | 2+ Tbps |
| **ML Adaptativo** | ❌ | ✅ |
| **Métricas detalladas** | ❌ | ✅ |
| **DRR Team** | ❌ | ✅ |
| **SLA** | Ninguno | 99.99% |
| **Protección de costos** | ❌ | ✅ |

### Machine Learning Adaptativo

Azure DDoS Protection Standard utiliza algoritmos de ML que aprenden los patrones normales de tráfico:

```csharp
// Conceptual ML Detection Logic
public class DDoSDetectionEngine
{
    public async Task<AttackAssessment> AnalyzeTraffic(TrafficPattern current)
    {
        var baseline = await GetHistoricalBaseline();
        var anomalyScore = CalculateAnomalyScore(current, baseline);
        
        if (anomalyScore > CRITICAL_THRESHOLD)
        {
            return new AttackAssessment
            {
                IsAttack = true,
                Confidence = anomalyScore,
                RecommendedAction = MitigationAction.ImmediateBlock,
                EstimatedAttackVector = ClassifyAttackType(current)
            };
        }
        
        return AttackAssessment.Normal;
    }
    
    private double CalculateAnomalyScore(TrafficPattern current, Baseline baseline)
    {
        // Análisis multidimensional:
        var volumeAnomaly = AnalyzeVolumeDeviation(current.Volume, baseline.TypicalVolume);
        var patternAnomaly = AnalyzePatternDeviation(current.Patterns, baseline.TypicalPatterns);
        var geoAnomaly = AnalyzeGeographicDistribution(current.SourceIPs, baseline.TypicalGeo);
        var temporalAnomaly = AnalyzeTemporalPatterns(current.Timing, baseline.TypicalTiming);
        
        return WeightedAverage(volumeAnomaly, patternAnomaly, geoAnomaly, temporalAnomaly);
    }
}
```

### Métricas Clave de DDoS Protection

#### Métricas de Detección
- **Under DDoS Attack** (Boolean): Indica si está bajo ataque activo
- **DDoS mitigation flow rate** (Flows/sec): Tasa de mitigación
- **Inbound packets dropped** (Packets/sec): Paquetes bloqueados por segundo
- **Inbound bytes dropped** (Bytes/sec): Bytes bloqueados por segundo

#### Métricas de Análisis
- **Max attack vector count**: Número máximo de vectores de ataque simultáneos
- **Attack duration**: Duración del ataque en segundos
- **Attack magnitude**: Volumen máximo del ataque (Gbps/Mpps)

## Laboratorio Práctico

### Paso 1: Preparación de Recursos de Testing (5 minutos)

#### Crear Public IP para Testing
```bash
# Variables del laboratorio
$resourceGroup = "rg-nsg-lab-$env:USERNAME"
$location = "eastus"
$publicIpName = "pip-ddos-test"

# Crear Public IP estática
az network public-ip create \
  --resource-group $resourceGroup \
  --name $publicIpName \
  --allocation-method Static \
  --sku Standard \
  --location $location \
  --tags Purpose=DDoSTest Environment=Lab
```

#### Crear Application Gateway como Target
```bash
# Crear Application Gateway para testing
az network application-gateway create \
  --name appgw-ddos-test \
  --location $location \
  --resource-group $resourceGroup \
  --vnet-name vnet-nsg-lab \
  --subnet snet-appgw \
  --capacity 2 \
  --sku Standard_v2 \
  --http-settings-cookie-based-affinity Disabled \
  --frontend-port 80 \
  --http-settings-port 80 \
  --http-settings-protocol Http \
  --public-ip-address $publicIpName \
  --tags Purpose=DDoSTest Environment=Lab
```

### Paso 2: Configuración de DDoS Protection Standard (8 minutos)

⚠️ **Nota de Costos:** DDoS Protection Standard cuesta ~$2,944/mes. Para el laboratorio, lo configuraremos y deshabilitaremos al final.

#### Crear DDoS Protection Plan
```bash
# Crear DDoS Protection Plan
az network ddos-protection create \
  --resource-group $resourceGroup \
  --name ddos-plan-lab \
  --location $location \
  --tags Environment=Lab Purpose=Testing
```

#### Habilitar DDoS Protection en VNET
```bash
# Obtener el ID del DDoS Protection Plan
$ddosPlanId = az network ddos-protection show \
  --resource-group $resourceGroup \
  --name ddos-plan-lab \
  --query id --output tsv

# Habilitar DDoS Protection en la VNET
az network vnet update \
  --resource-group $resourceGroup \
  --name vnet-nsg-lab \
  --ddos-protection true \
  --ddos-protection-plan $ddosPlanId
```

#### Verificar Configuración
```bash
# Verificar que DDoS Protection está habilitado
az network vnet show \
  --resource-group $resourceGroup \
  --name vnet-nsg-lab \
  --query "{DDoSProtectionEnabled:enableDdosProtection, DDoSPlan:ddosProtectionPlan.id}"
```

### Paso 3: Configuración de Monitoreo Avanzado (7 minutos)

#### Crear Action Group para Alertas
```bash
# Crear Action Group para notificaciones
az monitor action-group create \
  --resource-group $resourceGroup \
  --name ag-ddos-alerts \
  --short-name ddosalert
# Nota: Agregar email/SMS/webhook según necesidades
```

#### Configurar Alertas de DDoS
```bash
# Obtener el ID del Public IP
$pipId = az network public-ip show \
  --resource-group $resourceGroup \
  --name $publicIpName \
  --query id --output tsv

# Alerta para ataques DDoS
az monitor metrics alert create \
  --name alert-ddos-attack \
  --resource-group $resourceGroup \
  --scopes $pipId \
  --condition "max 'UnderDDoSAttack' > 0" \
  --description "Alerta cuando está bajo ataque DDoS" \
  --evaluation-frequency PT1M \
  --window-size PT5M \
  --severity 0 \
  --action ag-ddos-alerts

# Alerta para paquetes bloqueados
az monitor metrics alert create \
  --name alert-ddos-packets-dropped \
  --resource-group $resourceGroup \
  --scopes $pipId \
  --condition "max 'PacketsDroppedDDoS' > 1000" \
  --description "Alerta cuando se bloquean >1000 paquetes/min" \
  --evaluation-frequency PT1M \
  --window-size PT5M \
  --severity 1 \
  --action ag-ddos-alerts
```

#### Crear Dashboard de Monitoreo
```json
{
  "dashboardName": "DDoS Protection Monitoring",
  "widgets": [
    {
      "type": "metrics",
      "title": "Under DDoS Attack",
      "resource": "$pipId",
      "metric": "UnderDDoSAttack",
      "aggregation": "Maximum"
    },
    {
      "type": "metrics", 
      "title": "Packets Dropped DDoS",
      "resource": "$pipId",
      "metric": "PacketsDroppedDDoS",
      "aggregation": "Total"
    },
    {
      "type": "metrics",
      "title": "Bytes Dropped DDoS", 
      "resource": "$pipId",
      "metric": "BytesDroppedDDoS",
      "aggregation": "Total"
    },
    {
      "type": "metrics",
      "title": "Max Attack Vector Count",
      "resource": "$pipId", 
      "metric": "MaxAttackVectorCount",
      "aggregation": "Maximum"
    }
  ]
}
```

### Paso 4: Implementación de Aplicación de Monitoreo (.NET) (Opcional)

La aplicación `DDoSMonitor` proporciona monitoreo en tiempo real y alertas personalizadas.

#### Ejecutar Aplicación de Monitoreo
```bash
# Navegar al directorio del proyecto
cd src/DDoSMonitor

# Configurar variables de entorno
$env:AZURE_SUBSCRIPTION_ID = "your-subscription-id"
$env:AZURE_RESOURCE_GROUP = $resourceGroup  
$env:AZURE_PUBLIC_IP_NAME = $publicIpName

# Ejecutar monitoreo
dotnet run -- monitor --interval 30 --alert-threshold 1000
```

#### Funcionalidades de la Aplicación
- **Monitoreo en tiempo real** de métricas DDoS
- **Alertas personalizables** basadas en umbrales
- **Logging detallado** de eventos de seguridad
- **Dashboard en consola** con métricas visuales
- **Exportación de reportes** en múltiples formatos

## Arquitectura de Protección Implementada

```
                    ┌─────────────────┐
                    │    ATACANTES    │
                    │   (Botnets)     │
                    └─────────┬───────┘
                              │ Ataque DDoS
                              │ (1+ Tbps)
                    ┌─────────▼───────┐
                    │  AZURE EDGE     │
                    │   DDoS Basic    │ ◄── Primera línea de defensa
                    └─────────┬───────┘
                              │ Tráfico filtrado
                    ┌─────────▼───────┐
                    │ DDoS STANDARD   │
                    │ ML Detection    │ ◄── Análisis inteligente
                    └─────────┬───────┘
                              │ Tráfico limpio
         ┌────────────────────▼────────────────────┐
         │           PUBLIC IP + NSG               │
         │     (Application Gateway)               │
         └────────────────────┬────────────────────┘
                              │ Tráfico legítimo
         ┌────────────────────▼────────────────────┐
         │          APLICACIÓN PROTEGIDA           │
         │         (Web App / VM Scale Set)        │
         └─────────────────────────────────────────┘
```

### Flujo de Protección

1. **Edge Filtering**: Azure Global Network filtra ataques obvios
2. **ML Analysis**: Algoritmos analizan patrones en tiempo real  
3. **Adaptive Mitigation**: Respuesta automática a amenazas detectadas
4. **Clean Traffic**: Solo tráfico legítimo llega a la aplicación
5. **Continuous Learning**: Sistema aprende de cada ataque

## Simulación de Ataques (Solo para Testing Ético)

⚠️ **ADVERTENCIA**: Solo realizar en recursos propios y con fines educativos.

### Simulación Básica de Carga
```bash
# Usar herramientas como Apache Bench para testing básico
# SOLO en sus propios recursos
ab -n 10000 -c 100 http://your-app-gateway-ip/

# O usar PowerShell para simulación simple
1..1000 | ForEach-Object -Parallel {
    try {
        Invoke-WebRequest -Uri "http://your-app-gateway-ip/" -TimeoutSec 1
    } catch {}
} -ThrottleLimit 50
```

### Monitorear Respuesta de DDoS Protection
```bash
# Observar métricas durante la simulación
az monitor metrics list \
  --resource $pipId \
  --metric "PacketsInDDoS,PacketsDroppedDDoS,BytesInDDoS,BytesDroppedDDoS" \
  --start-time "2025-07-21T19:00:00Z" \
  --end-time "2025-07-21T20:00:00Z" \
  --interval PT1M
```

## Resultados de Aprendizaje

Al completar este laboratorio, habrán dominado:

### Conocimientos Técnicos
- ✅ **Diferencias DDoS Basic vs Standard** y casos de uso
- ✅ **Configuración completa** de DDoS Protection Standard
- ✅ **Interpretación de métricas** y análisis de ataques
- ✅ **Alerting avanzado** con Action Groups y Logic Apps
- ✅ **Cost management** y optimización de protección

### Habilidades Prácticas
- ✅ **Deployment automatizado** de protección DDoS
- ✅ **Monitoreo en tiempo real** de amenazas
- ✅ **Incident response** durante ataques
- ✅ **Forensic analysis** post-ataque
- ✅ **Capacity planning** para protección

### Competencias Empresariales
- ✅ **Risk assessment** de amenazas DDoS
- ✅ **Business continuity** planning
- ✅ **Compliance** con SLAs de disponibilidad
- ✅ **Cost-benefit analysis** de protecciones
- ✅ **Stakeholder communication** durante incidentes

## Métricas de Éxito

### Indicadores de Protección Efectiva
- **Detección automática** de ataques en <60 segundos
- **Mitigación efectiva** con <1% de falsos positivos
- **Availability SLA** mantenido durante ataques
- **Clean traffic** preservado sin impacto de latencia

### KPIs de Monitoreo
- **MTTR** (Mean Time To Response): <2 minutos
- **MTTD** (Mean Time To Detection): <1 minuto  
- **Attack Success Rate**: <0.1%
- **Business Impact**: $0 en downtime costs

## Troubleshooting Común

### Error: "DDoS Protection Plan creation failed"
```bash
# Verificar límites de suscripción
az account list-locations --output table
az provider show --namespace Microsoft.Network --query registrationState
```

### Error: "Metrics not appearing"
```bash
# Verificar configuración de monitoreo
az monitor diagnostic-settings list --resource $pipId
# Puede tomar hasta 15 minutos para aparecer métricas iniciales
```

### Error: "Alerts not triggering"
```bash
# Verificar Action Group
az monitor action-group show --resource-group $resourceGroup --name ag-ddos-alerts
# Verificar configuración de umbrales en alertas
```

## Limpieza de Recursos

⚠️ **Importante**: Deshabilitar DDoS Protection Standard para evitar costos continuos.

```bash
# Deshabilitar DDoS Protection en VNET
az network vnet update \
  --resource-group $resourceGroup \
  --name vnet-nsg-lab \
  --ddos-protection false

# Eliminar DDoS Protection Plan
az network ddos-protection delete \
  --resource-group $resourceGroup \
  --name ddos-plan-lab

# Eliminar Application Gateway y Public IP si no se necesitan
az network application-gateway delete \
  --resource-group $resourceGroup \
  --name appgw-ddos-test

az network public-ip delete \
  --resource-group $resourceGroup \
  --name $publicIpName
```

---

**¡Excelente trabajo!** Han implementado una protección DDoS enterprise-grade que puede defenderse contra ataques de múltiples Terabits por segundo.

**Siguiente:** [Laboratorio 3 - Testing y Simulación de Conectividad](../Laboratorio3-Testing/README.md) 