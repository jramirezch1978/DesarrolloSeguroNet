# 🎓 SESIÓN 06: SEGURIDAD DE INFRAESTRUCTURA AZURE

## 📋 Información General del Curso
- **📚 Curso:** Diseño Seguro de Aplicaciones (.NET en Azure)
- **🗓️ Sesión:** 06 - Finalización Módulo 2 + Inicio Módulo 3
- **⏱️ Duración Total:** 90 minutos (4 laboratorios principales + setup)
- **🎯 Modalidad:** Instructor-led con práctica individual
- **🔧 Herramientas:** Visual Studio Code + .NET 9 + C# + Azure Portal

---

## 🌟 Objetivos de la Sesión

Al completar esta sesión, los estudiantes podrán:

1. **🏗️ Diseñar arquitecturas de red seguras** con segmentación apropiada
2. **🛡️ Implementar Network Security Groups** con reglas granulares
3. **🦘 Configurar acceso administrativo seguro** usando Azure Bastion y Jump Boxes
4. **🎯 Aplicar principios de Defense in Depth** en infraestructura de Azure
5. **📊 Validar y documentar** arquitecturas de red empresariales

---

## 📚 CONCEPTOS TEÓRICOS FUNDAMENTALES

Antes de implementar los laboratorios prácticos, es crucial comprender los fundamentos teóricos que sustentan la seguridad de infraestructura en Azure. Estos conceptos forman la base sobre la cual construiremos arquitecturas robustas y escalables.

### 🔐 **1. Segmentación de Recursos en Azure**

La segmentación es uno de los conceptos más poderosos en seguridad de nube. **¿Por qué segmentar?** Imaginen que tienen un centro comercial. No pondrían la joyería al lado del patio de comidas sin ningún tipo de separación, ¿verdad? Cada tipo de negocio tiene diferentes niveles de seguridad, diferentes tipos de clientes, y diferentes riesgos.

#### **Beneficios de la Segmentación:**
- **🔒 Aislamiento de cargas críticas:** Si su sistema de facturación tiene un problema, no queremos que afecte al sistema de recursos humanos
- **⚡ Minimizar el "blast radius":** Cuando algo sale mal, queremos contener el impacto como compartimentos estancos en un barco
- **📋 Compliance por diseño:** GDPR exige que los datos personales estén separados de otros tipos de información
- **🚀 Optimización de performance:** Evitamos que aplicaciones hambrientas de recursos afecten a las críticas

#### **Niveles de Segmentación:**
```
🏢 Suscripción Level
    ├── 🎯 Grupo de Recursos Level  
    │   ├── 🌐 Virtual Network Level
    │   │   ├── 📦 Subnet Level
    │   │   └── 🛡️ NSG Level
    │   └── 🔐 Application Level
    └── 🔑 Identity Level
```

### 🛡️ **2. Defense in Depth Strategy**

Defense in Depth es como diseñar un castillo medieval en el siglo XXI. No se confían en una sola muralla, sino en múltiples capas de protección que trabajan en conjunto.

#### **Capas de Protección en Azure:**

```
🌐 INTERNET
    ▼
┌─────────────────────────────────────────┐
│ 🛡️ Edge Protection (CDN + DDoS)        │
│   • Azure CDN con filtrado             │
│   • DDoS Protection Standard           │
└─────────────────────────────────────────┘
    ▼
┌─────────────────────────────────────────┐
│ 🔥 Perimeter Security (WAF + Firewall) │
│   • Web Application Firewall           │
│   • Azure Firewall con threat intel    │
└─────────────────────────────────────────┘
    ▼
┌─────────────────────────────────────────┐
│ ⚖️ Load Balancing & Distribution        │
│   • Application Gateway                │
│   • Load Balancer con health probes    │
└─────────────────────────────────────────┘
    ▼
┌─────────────────────────────────────────┐
│ 🌐 Network Segmentation (VNETs + NSGs) │
│   • Virtual Networks aisladas          │
│   • Network Security Groups            │
└─────────────────────────────────────────┘
    ▼
┌─────────────────────────────────────────┐
│ 💻 Host Security (VMs + Containers)    │
│   • OS hardening y patching            │
│   • Antimalware y monitoring           │
└─────────────────────────────────────────┘
    ▼
┌─────────────────────────────────────────┐
│ 🔐 Application Security (Code + Auth)  │
│   • Azure AD + Conditional Access      │
│   • Data encryption en tránsito/reposo │
└─────────────────────────────────────────┘
```

### 🌐 **3. Azure Virtual Networks - Fundamentos**

Las Virtual Networks (VNETs) son la fundación de toda arquitectura segura en Azure. Piensen en ellas como el terreno donde van a construir su ciudad digital.

#### **Componentes Clave:**

##### **📍 Address Space Planning**
```
RFC 1918 Private Ranges:
├── 10.0.0.0/8     (16,777,216 IPs) - Enterprise Large
├── 172.16.0.0/12  (1,048,576 IPs)  - Enterprise Medium  
└── 192.168.0.0/16 (65,536 IPs)     - Small Networks
```

**Ejemplo de Planificación Empresarial:**
```
Hub VNET (10.1.0.0/16) - 65,534 IPs
├── DMZ Subnet (10.1.1.0/24) - 254 IPs
├── Private Subnet (10.1.2.0/24) - 254 IPs
├── Data Subnet (10.1.3.0/24) - 254 IPs
├── Management Subnet (10.1.10.0/24) - 254 IPs
└── Bastion Subnet (10.1.100.0/26) - 64 IPs

Spoke VNETs:
├── Production (10.2.0.0/16)
├── Development (10.3.0.0/16)
└── Testing (10.4.0.0/16)
```

##### **🔄 Routing Inteligente**
Azure maneja mucho del routing automáticamente, pero podemos personalizar rutas para casos específicos. Es como tener semáforos y señalizaciones inteligentes que dirigen el tráfico eficientemente.

##### **🔍 DNS Resolution**
Azure proporciona DNS automático, pero para casos empresariales a menudo queremos control granular con Azure Private DNS Zones.

### 🔗 **4. Modelos de Conectividad VNET**

#### **🌟 Hub-and-Spoke (Recomendado para Empresas)**
```
                🌐 INTERNET
                      │
                      ▼
        ┌─────────────────────────────────┐
        │         HUB VNET                │
        │    🛡️ Servicios Compartidos    │
        │    • Azure Firewall             │
        │    • VPN Gateway                │
        │    • DNS Servers                │
        │    • Monitoring Central         │
        └─────────────────────────────────┘
                      │
        ┌─────────────┼─────────────┐
        │             │             │
        ▼             ▼             ▼
┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│ PROD SPOKE  │ │ DEV SPOKE   │ │ TEST SPOKE  │
│ 🏭 Workload │ │ 🔧 Workload │ │ 🧪 Workload │
└─────────────┘ └─────────────┘ └─────────────┘
```

**Ventajas del Hub-and-Spoke:**
- ✅ Centralización de políticas de seguridad
- ✅ Control granular del tráfico
- ✅ Escalabilidad (agregar un nuevo spoke es simple)
- ✅ Optimización de costos (servicios compartidos)

#### **🕸️ Mesh (Solo para casos específicos)**
Conecta todas las VNETs entre sí. Es eficiente para tráfico pero se vuelve complejo rápidamente:
- 4 VNETs = 6 conexiones
- 10 VNETs = 45 conexiones
- **No escala bien para empresas**

### 🔌 **5. Private Endpoints vs Service Endpoints**

Una de las decisiones más importantes en seguridad de Azure es cómo conectar con servicios PaaS de manera segura.

#### **🛣️ Service Endpoints**
```
VNET ──────► [Service Endpoint] ──────► Azure Storage
     (Ruta Optimizada)                  (Endpoint Público)
```
- **Propósito:** Ruta express desde VNET hasta servicios Azure
- **Ventaja:** Tráfico por backbone de Azure, no por Internet
- **Limitación:** El servicio mantiene endpoint público
- **Costo:** Incluido, sin costo adicional

#### **🔒 Private Endpoints (Recomendado para Datos Críticos)**
```
VNET ──────► [Private Endpoint: 10.0.3.4] ──────► Azure Storage
     (Completamente Privado)                      (Sin Endpoint Público)
```
- **Propósito:** Endpoint completamente privado dentro de la VNET
- **Ventaja:** Elimina completamente la exposición pública
- **Caso de uso:** Datos críticos, compliance estricto
- **Costo:** ~$8-10 USD/mes por endpoint

### 📐 **6. Subnet Design Patterns**

Los patrones de diseño de subredes son como los planos arquitectónicos de un edificio: determinan cómo todo se conecta y se protege.

#### **🛡️ Enfoque "Security-First"**
Diseñamos pensando primero en seguridad, luego optimizamos para performance y costo.

```
🏗️ Subnet Architecture Pattern:

DMZ Subnet (10.1.1.0/24) - 🌐 Internet-Facing
├── Load Balancers (distribución de tráfico)
├── WAF + Application Gateway (inspección)
├── Reverse Proxies (enmascaramiento)
└── Public-facing services

Private Subnet (10.1.2.0/24) - 🏢 Internal Business Logic
├── Application Servers (.NET Core apps)
├── Microservices (APIs internas)
├── Business Logic Processing
└── Internal Web Services

Data Subnet (10.1.3.0/24) - 🗄️ Data Storage & Processing
├── SQL Servers y Databases
├── Redis Cache Layers
├── Storage Accounts
└── Backup Systems

Management Subnet (10.1.10.0/24) - 🔧 Administrative Access
├── Jump Boxes para administración
├── Monitoring Tools
├── Administrative Services
└── Backup/Recovery Tools
```

#### **📊 Principio de Flujo de Tráfico**
```
Internet ──► DMZ ──► Private ──► Data
   ▲                              │
   └─── Management ◄──────────────┘
        (Control y Monitoring)
```

**Regla Fundamental:** El tráfico fluye desde menos confiable a más confiable, **nunca al revés**.

### 🏰 **7. DMZ Implementation**

La DMZ (Zona Desmilitarizada) es como el área de recepción de una embajada: controlada, monitoreada, pero accesible.

#### **🎯 Propósito y Componentes:**
```
🌐 INTERNET
    │
    ▼
┌─────────────────────────────────────────┐
│            DMZ SUBNET                   │
│ ┌─────────────────────────────────────┐ │
│ │ 🛡️ Web Application Firewall        │ │
│ │ • Inspección HTTP/HTTPS             │ │
│ │ • Detección de inyecciones SQL      │ │
│ │ • Protección XSS                    │ │
│ └─────────────────────────────────────┘ │
│ ┌─────────────────────────────────────┐ │
│ │ ⚖️ Application Gateway              │ │
│ │ • Load balancing inteligente        │ │
│ │ • Health probes                     │ │
│ │ • SSL termination                   │ │
│ └─────────────────────────────────────┘ │
│ ┌─────────────────────────────────────┐ │
│ │ 🦘 Bastion Hosts                   │ │
│ │ • Acceso administrativo seguro      │ │
│ │ • No exposición directa SSH/RDP     │ │
│ │ • Auditoría completa                │ │
│ └─────────────────────────────────────┘ │
└─────────────────────────────────────────┘
           │ (Controlado)
           ▼
    Private Network
```

#### **🔒 Controles de Seguridad DMZ:**
- **Tráfico de entrada:** Solo puertos 80/443 desde Internet
- **Tráfico de salida:** Solo conexiones autorizadas a subredes internas
- **Monitoreo:** Logging exhaustivo de todas las conexiones
- **Respuesta a incidentes:** Aislamiento rápido si se detecta compromiso

### 🦘 **8. Bastion Hosts & Jump Boxes**

El acceso administrativo seguro es uno de los desafíos más complejos en seguridad de nube.

#### **☁️ Azure Bastion (Solución Cloud-Native)**
```
👨‍💻 Administrator ──► 🌐 Azure Portal ──► 🛡️ Azure Bastion ──► 💻 Target VM
    (Browser)          (HTTPS + Azure AD)    (No Public IP)     (Private IP)
```

**Características de Azure Bastion:**
- ✅ **Sin IPs públicas:** Las VMs no necesitan exposición a Internet
- ✅ **Autenticación Azure AD:** Integración con Conditional Access y MFA
- ✅ **Protocolo seguro:** RDP/SSH sobre HTTPS
- ✅ **Auditoría completa:** Todas las sesiones son registradas
- ✅ **PIM Integration:** Just-in-time access con Privileged Identity Management

#### **🖥️ Custom Jump Boxes (Máximo Control)**
Para casos donde necesitan más control granular:

```
🏢 Corporate Network ──► 🔐 VPN/ExpressRoute ──► 🦘 Jump Box ──► 🎯 Target Systems
    (Secured Connection)     (Private Connectivity)    (Hardened VM)   (Internal Network)
```

**Configuración de Jump Box Seguro:**
- 🔒 **OS Hardening:** Servicios innecesarios deshabilitados
- 🔄 **Auto-patching:** Actualizaciones automáticas de seguridad
- 📝 **Session Recording:** Grabación completa para auditorías
- 🔑 **MFA Obligatorio:** Nunca solo usuario/contraseña
- 🚨 **Threat Detection:** Monitoring de comportamiento anómalo

---

## 📚 Laboratorios de la Sesión

### 🛠️ [Laboratorio 0: Verificación y Configuración del Entorno](./Laboratorio0-VerificacionEntorno/)
- **⏱️ Duración:** 15 minutos
- **🎯 Objetivo:** Preparar entorno completo para laboratorios de infraestructura Azure
- **📋 Contenido:**
  - Instalación de Chocolatey
  - Configuración de .NET 9 SDK y Azure CLI
  - Setup de Visual Studio Code con extensiones
  - Verificación de acceso a Azure

### 🧪 [Laboratorio 1: Creación de Virtual Network Segura](./Laboratorio1-VirtualNetwork/)
- **⏱️ Duración:** 20 minutos
- **🎯 Objetivo:** Crear y configurar una Virtual Network con segmentación apropiada
- **📋 Contenido:**
  - Creación de Resource Group base
  - Planificación de address space (10.1.0.0/16)
  - Implementación de 4 subredes especializadas
  - Configuración de DNS personalizado

### 🧪 [Laboratorio 2: Network Security Groups (NSGs)](./Laboratorio2-NetworkSecurityGroups/)
- **⏱️ Duración:** 25 minutos
- **🎯 Objetivo:** Crear y configurar NSGs con reglas de seguridad granulares
- **📋 Contenido:**
  - NSG para DMZ subnet con reglas de Internet
  - NSG para Private subnet con controles internos
  - NSG para Data subnet con máxima seguridad
  - NSG para Management subnet con acceso administrativo

### 🧪 [Laboratorio 3: Azure Bastion y Jump Box](./Laboratorio3-AzureBastion/)
- **⏱️ Duración:** 20 minutos
- **🎯 Objetivo:** Implementar acceso administrativo seguro
- **📋 Contenido:**
  - Creación de AzureBastionSubnet
  - Implementación de Azure Bastion Host
  - Configuración de Jump Box VM
  - Configuración de acceso seguro sin exposición pública

### 🧪 [Laboratorio 4: Testing y Arquitectura Hub-and-Spoke](./Laboratorio4-TestingArquitectura/)
- **⏱️ Duración:** 10 minutos
- **🎯 Objetivo:** Validar la arquitectura implementada y preparar expansión
- **📋 Contenido:**
  - Testing de conectividad con Network Watcher
  - Documentación de arquitectura implementada
  - Planificación para Hub-and-Spoke
  - Verificación final de principios de seguridad

---

## 🏗️ Arquitectura Final Implementada

```
🌐 INTERNET
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│                    VNET: 10.1.0.0/16                       │
│                                                             │
│  ┌─────────────────┐  ┌─────────────────┐                  │
│  │   DMZ Subnet    │  │  Bastion Subnet │                  │
│  │  10.1.1.0/24    │  │ 10.1.100.0/26   │                  │
│  │                 │  │                 │                  │
│  │ • Load Balancer │  │ • Azure Bastion │                  │
│  │ • WAF           │  │ • Public IP     │                  │
│  │ • Public Apps   │  │ • Secure Access │                  │
│  └─────────────────┘  └─────────────────┘                  │
│                                                             │
│  ┌─────────────────┐  ┌─────────────────┐                  │
│  │ Private Subnet  │  │ Management Sub  │                  │
│  │  10.1.2.0/24    │  │  10.1.10.0/24   │                  │
│  │                 │  │                 │                  │
│  │ • App Servers   │  │ • Jump Box VM   │                  │
│  │ • APIs          │  │ • Admin Tools   │                  │
│  │ • Microservices │  │ • Monitoring    │                  │
│  └─────────────────┘  └─────────────────┘                  │
│                                                             │
│           ┌─────────────────┐                               │
│           │  Data Subnet    │                               │
│           │  10.1.3.0/24    │                               │
│           │                 │                               │
│           │ • Databases     │                               │
│           │ • Storage       │                               │
│           │ • Cache (Redis) │                               │
│           └─────────────────┘                               │
└─────────────────────────────────────────────────────────────┘
```

---

## 🛡️ Principios de Seguridad Implementados

### **🎯 Defense in Depth**
- Múltiples capas de seguridad (Network → Subnet → NSG → Application)
- Controles redundantes en cada nivel
- Fail-safe defaults con deny-by-default

### **🔒 Principle of Least Privilege**
- Acceso mínimo necesario para cada componente
- Segmentación granular por función
- Control de tráfico bidireccional

### **🌐 Network Segmentation**
- Separación lógica por niveles de confianza
- Aislamiento de cargas de trabajo críticas
- Prevención de lateral movement

### **🦘 Secure Administrative Access**
- Eliminación de RDP/SSH directo desde Internet
- Azure Bastion para acceso controlado
- Jump Box para administración interna

### **📊 Zero Trust Networking**
- Never trust, always verify
- Continuous verification de identidad y contexto
- Micro-segmentation implementada

---

## ✅ Checklist de Completación

### **Laboratorio 0: Entorno**
- [ ] Chocolatey instalado y funcionando
- [ ] .NET 9 SDK configurado
- [ ] Azure CLI autenticado
- [ ] VS Code con extensiones instaladas

### **Laboratorio 1: Virtual Network**
- [ ] Resource Group creado
- [ ] VNET con address space 10.1.0.0/16
- [ ] 4 subredes segmentadas correctamente
- [ ] DNS configurado (opcional)

### **Laboratorio 2: Network Security Groups**
- [ ] 4 NSGs creados con reglas específicas
- [ ] NSGs asociados con subredes correspondientes
- [ ] Principio de least privilege aplicado
- [ ] Matriz de conectividad documentada

### **Laboratorio 3: Acceso Administrativo**
- [ ] AzureBastionSubnet creada (10.1.100.0/26)
- [ ] Azure Bastion deployado (si presupuesto permite)
- [ ] Jump Box VM configurada
- [ ] Acceso seguro sin exposición directa

### **Laboratorio 4: Testing y Documentación**
- [ ] Conectividad validada con Network Watcher
- [ ] Arquitectura completamente documentada
- [ ] Plan de expansión Hub-and-Spoke creado
- [ ] Verificación final de security rules

---

## 🚨 Troubleshooting Común

### **Error: "Cannot create resources in Azure"**
- Verificar permisos de Contributor en la suscripción
- Confirmar quota limits no excedidos
- Revisar políticas organizacionales

### **Error: "NSG rules not working"**
- Esperar 2-3 minutos para propagación
- Verificar prioridades de reglas (no duplicadas)
- Revisar effective security rules

### **Error: "Cannot connect via Bastion"**
- Confirmar AzureBastionSubnet es exactamente /26
- Verificar que VM está en estado Running
- Revisar credenciales y NSG rules

### **Error: "Network Watcher tests fail"**
- Registrar provider: `az provider register --namespace Microsoft.Network`
- Verificar que Network Watcher está habilitado
- Confirmar permisos de Network Watcher Contributor

---

## 🔗 Recursos de Referencia

### **📖 Documentación Oficial**
- [Azure Virtual Network](https://docs.microsoft.com/en-us/azure/virtual-network/)
- [Network Security Groups](https://docs.microsoft.com/en-us/azure/virtual-network/network-security-groups-overview)
- [Azure Bastion](https://docs.microsoft.com/en-us/azure/bastion/)
- [Azure Network Watcher](https://docs.microsoft.com/en-us/azure/network-watcher/)

### **🏗️ Arquitecturas de Referencia**
- [Hub-spoke network topology](https://docs.microsoft.com/en-us/azure/architecture/reference-architectures/hybrid-networking/hub-spoke)
- [Network security best practices](https://docs.microsoft.com/en-us/azure/security/fundamentals/network-best-practices)

### **🛠️ Herramientas y Comandos**
- [Azure CLI Network Commands](https://docs.microsoft.com/en-us/cli/azure/network)
- [PowerShell Az.Network Module](https://docs.microsoft.com/en-us/powershell/module/az.network/)

---

## 📋 Preparación para Sesión 7

### **🎯 Próximos Temas (Lunes 21/07):**
- Azure Firewall implementation y configuración avanzada
- DDoS Protection Standard deployment
- Network monitoring y alerting con Azure Monitor
- Hub-and-Spoke architecture completion
- Application deployment en infraestructura segura

### **📚 Lectura Recomendada:**
- Azure Firewall documentation
- DDoS Protection best practices
- Azure Monitor for networks

### **🔧 Preparación Técnica:**
- Mantener la infraestructura de esta sesión funcionando
- Familiarizarse con Azure Firewall concepts
- Revisar Azure Monitor basics

---

## 🎉 Resultados de Aprendizaje

Al completar esta sesión, los estudiantes han demostrado capacidad para:

1. **🏗️ Arquitectura Empresarial:** Diseñar e implementar redes seguras escalables
2. **🛡️ Controles de Seguridad:** Configurar multiple layers de protección
3. **🎯 Best Practices:** Aplicar principios Zero Trust y Defense in Depth
4. **📊 Validation:** Testing y documentación de arquitecturas complejas
5. **🚀 Scalability:** Planificación para crecimiento y expansión futura

---

## 📞 Soporte y Contacto

### **👨‍🏫 Instructor:**
- Email: [instructor@email.com]
- Teams: [Canal del Curso]

### **📚 Repositorio del Curso:**
- GitHub: [URL del repositorio]
- Materiales adicionales y actualizaciones

### **🤝 Comunidad:**
- Foro de estudiantes: [URL]
- Sesiones de Q&A: [Horarios]

---

**¡Excelente trabajo completando la Sesión 06! Han construido una base sólida de infraestructura segura que servirá como foundation para todas las aplicaciones futuras. Nos vemos en la Sesión 7 para continuar expandiendo estas capacidades.** 🚀

---

*📅 Última actualización: Sesión 06 - Infraestructura Azure Segura*  
*🎓 Curso: Diseño Seguro de Aplicaciones (.NET en Azure)* 