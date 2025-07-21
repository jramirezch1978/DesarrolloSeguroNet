# 🧪 LABORATORIO 4: TESTING DE CONECTIVIDAD Y ARQUITECTURA HUB-AND-SPOKE

## 📋 Información General
- **⏱️ Duración:** 10 minutos
- **🎯 Objetivo:** Validar la arquitectura implementada y preparar para Hub-and-Spoke
- **📚 Curso:** Diseño Seguro de Aplicaciones (.NET en Azure)
- **🔧 Prerequisitos:** Laboratorios 1, 2 y 3 completados

---

## 📝 Paso 1: Testing de Conectividad Básica (3 minutos)

### Verificar NSG Rules:

1. **Azure Portal → Network security groups:**
   - Verificar que los 4 NSGs están asociados
   - Review effective security rules para cada subnet

2. **Usar Azure Network Watcher:**
   - Azure Portal → Network Watcher → IP flow verify

### Test 1: Tráfico DMZ → Private (Debe ser PERMITIDO)
```
Source: 10.1.1.10 (DMZ)
Destination: 10.1.2.10 (Private) 
Port: 443
Direction: Outbound
Expected: ✅ Allow
```

### Test 2: Tráfico Internet → Data (Debe ser DENEGADO)
```
Source: Internet
Destination: 10.1.3.10 (Data)
Port: 1433
Direction: Inbound
Expected: ❌ Deny
```

### Test 3: Tráfico Private → Data (Debe ser PERMITIDO)
```
Source: 10.1.2.15 (Private)
Destination: 10.1.3.20 (Data)
Port: 1433
Direction: Outbound
Expected: ✅ Allow
```

### Con Azure CLI:
```bash
# Verificar effective routes
az network nic show-effective-route-table \
  --resource-group rg-infraestructura-segura-[SuNombre] \
  --name [NIC-name] \
  --output table

# Verificar effective NSG rules
az network nic list-effective-nsg \
  --resource-group rg-infraestructura-segura-[SuNombre] \
  --name [NIC-name] \
  --output table
```

---

## 📝 Paso 2: Documentar la Arquitectura Implementada (4 minutos)

### Crear documentación de la infraestructura:

1. **Crear archivo: `arquitectura-implementada.md`**

```markdown
# Infraestructura Segura - [Su Nombre]

## Virtual Network
- **VNET:** vnet-principal-[sunombre] (10.1.0.0/16)
- **Region:** East US
- **DNS:** Azure Default

## Subnets y Segmentación

### DMZ Subnet (10.1.1.0/24)
- **Propósito:** Servicios expuestos a Internet
- **NSG:** nsg-dmz-[sunombre]
- **Reglas:** HTTP/HTTPS permitido desde Internet

### Private Subnet (10.1.2.0/24) 
- **Propósito:** Aplicaciones internas
- **NSG:** nsg-private-[sunombre]
- **Reglas:** Solo acceso desde DMZ

### Data Subnet (10.1.3.0/24)
- **Propósito:** Bases de datos y almacenamiento
- **NSG:** nsg-data-[sunombre]
- **Reglas:** Solo acceso desde Private

### Management Subnet (10.1.10.0/24)
- **Propósito:** Administración y Jump Box
- **NSG:** nsg-management-[sunombre]
- **Recursos:** vm-jumpbox-[sunombre]

### Azure Bastion Subnet (10.1.100.0/26)
- **Propósito:** Acceso administrativo seguro
- **Recurso:** bastion-[sunombre]

## Principios de Seguridad Implementados
- ✅ Defense in Depth
- ✅ Principle of Least Privilege
- ✅ Network Segmentation
- ✅ Secure Administrative Access
```

### 📊 Matriz de Conectividad Validada:

| Origen | Destino | Puerto | Estado | NSG Rule |
|--------|---------|--------|--------|----------|
| Internet | DMZ | 80,443 | ✅ Permitido | Allow-HTTP-HTTPS-Inbound |
| Internet | Private | * | ❌ Denegado | Deny-Internet-Direct |
| Internet | Data | * | ❌ Denegado | Deny-All-Other-Data |
| DMZ | Private | 80,443 | ✅ Permitido | Allow-DMZ-to-Private |
| Private | Data | 1433,3306,5432 | ✅ Permitido | Allow-Private-to-Data |
| Management | All | * | ✅ Permitido | Allow-Management-to-All |

---

## 📝 Paso 3: Preparación para Hub-and-Spoke (3 minutos)

### Planificar expansión Hub-and-Spoke:

1. **La VNET actual se convertirá en Hub VNET**
2. **Planificar Spoke VNETs:**
   ```
   Spoke 1 - Production: 10.2.0.0/16
   Spoke 2 - Development: 10.3.0.0/16
   Spoke 3 - Testing: 10.4.0.0/16
   ```

3. **Servicios compartidos en Hub:**
   - Azure Firewall (próxima sesión)
   - DNS Servers
   - Monitoring y Logging
   - VPN Gateway (para on-premises)

### Crear planificación:

```markdown
# Expansión Hub-and-Spoke Planificada

## Hub VNET (Actual)
- **VNET:** vnet-principal-[sunombre] (10.1.0.0/16)
- **Servicios compartidos:** Bastion, Firewall (futuro)

## Spoke VNETs (Futuros)
- **Prod Spoke:** 10.2.0.0/16
- **Dev Spoke:** 10.3.0.0/16  
- **Test Spoke:** 10.4.0.0/16

## Conectividad
- **VNet Peering:** Hub ↔ Cada Spoke
- **Routing:** Todo tráfico vía Hub
- **Security:** Políticas centralizadas en Hub
```

### 🏗️ Arquitectura Hub-and-Spoke Futura:

```
                🌐 INTERNET
                      │
                      ▼
        ┌─────────────────────────────────┐
        │         HUB VNET                │
        │      (10.1.0.0/16)              │
        │                                 │
        │  ┌─────────┐  ┌─────────────┐   │
        │  │   DMZ   │  │   Bastion   │   │
        │  │         │  │             │   │
        │  └─────────┘  └─────────────┘   │
        │                                 │
        │  ┌─────────┐  ┌─────────────┐   │
        │  │ Firewall│  │ Management  │   │
        │  │ (Future)│  │             │   │
        │  └─────────┘  └─────────────┘   │
        └─────────────────────────────────┘
                      │
        ┌─────────────┼─────────────┐
        │             │             │
        ▼             ▼             ▼
┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│ PROD SPOKE  │ │ DEV SPOKE   │ │ TEST SPOKE  │
│(10.2.0.0/16)│ │(10.3.0.0/16)│ │(10.4.0.0/16)│
│             │ │             │ │             │
│ • App Tier  │ │ • App Tier  │ │ • App Tier  │
│ • Data Tier │ │ • Data Tier │ │ • Data Tier │
└─────────────┘ └─────────────┘ └─────────────┘
```

---

## ✅ Resultado Final del Laboratorio

Al completar este laboratorio debe tener:

- ✅ **VNET segura** con 4 subredes segmentadas
- ✅ **NSGs configurados** con reglas granulares
- ✅ **Azure Bastion y Jump Box** implementados
- ✅ **Arquitectura documentada** y validada
- ✅ **Plan para expansión** Hub-and-Spoke
- ✅ **Base sólida** para próximas sesiones

---

## 📊 Verificación Final y Resumen

### ✅ Checklist de Completación

#### **Laboratorio 1 - Virtual Network:**
- [ ] VNET creada con address space 10.1.0.0/16
- [ ] 4 subredes segmentadas correctamente
- [ ] DNS configurado (opcional)
- [ ] Resource group organizado

#### **Laboratorio 2 - Network Security Groups:**
- [ ] 4 NSGs creados (DMZ, Private, Data, Management)
- [ ] Reglas de seguridad granulares implementadas
- [ ] NSGs asociados con subredes correspondientes
- [ ] Principio de least privilege aplicado

#### **Laboratorio 3 - Acceso Administrativo:**
- [ ] Azure Bastion subnet creada
- [ ] Azure Bastion deployado (si presupuesto permite)
- [ ] Jump Box VM configurada
- [ ] Acceso administrativo seguro sin exposición directa

#### **Laboratorio 4 - Testing y Documentación:**
- [ ] Conectividad validada con Network Watcher
- [ ] Arquitectura documentada
- [ ] Plan de expansión Hub-and-Spoke creado
- [ ] Verificación de security rules

---

## 🎯 Resultados de Aprendizaje Alcanzados

Al completar este laboratorio, ha logrado:

### 1. 🏗️ **Arquitectura de Red Segura:**
- Diseño y implementación de VNET con segmentación apropiada
- Address space planning y subnet design
- DNS configuration y network topology

### 2. 🔒 **Network Security Groups Avanzados:**
- Reglas granulares basadas en principios de seguridad
- Control de tráfico entre subredes
- Implementation del principio de least privilege

### 3. 🦘 **Acceso Administrativo Seguro:** 
- Azure Bastion para acceso sin exposición pública
- Jump Box configurado en subnet de management
- Eliminación de RDP/SSH directo desde Internet

### 4. 🎯 **Defense in Depth Implementation:** 
- Múltiples capas de seguridad implementadas
- Segmentación de red por función y sensibilidad
- Control granular de tráfico entre componentes

### 5. 📋 **Documentation y Planning:** 
- Arquitectura documentada apropiadamente
- Planificación para escalabilidad futura
- Foundation para Hub-and-Spoke architecture

---

## 🚨 Troubleshooting de Validación

### Error: "Network Watcher IP flow verify fails"
- **Solución:** 
  - Registrar Network Watcher provider: `az provider register --namespace Microsoft.Network`
  - Verificar que Network Watcher está habilitado en la región
  - Confirmar permisos de Network Watcher Contributor

### Error: "Effective security rules not showing"
- **Solución:**
  - Esperar 2-3 minutos para propagación de reglas
  - Refrescar la vista en Azure Portal
  - Verificar que NSGs están asociados correctamente

### Error: "Cannot validate connectivity"
- **Solución:**
  - Verificar que las VMs están en estado "Running"
  - Confirmar que las IPs de prueba están en los rangos correctos
  - Revisar que no hay reglas conflictivas

---

## 📈 Métricas de Éxito

### Indicadores de Implementación Exitosa:
- ✅ **Network Segmentation:** 4 subredes con funciones claramente definidas
- ✅ **Security Controls:** NSGs con reglas deny-by-default implementadas
- ✅ **Administrative Access:** Zero direct Internet exposure para management
- ✅ **Scalability:** Arquitectura lista para Hub-and-Spoke expansion
- ✅ **Documentation:** Clear architecture documentation maintained

### Criterios de Calidad:
- Address space planning sin overlaps
- NSG rules siguiendo principio de least privilege
- Proper subnet sizing para crecimiento futuro
- Security-first approach en todas las decisiones de diseño

---

## 🔗 Preparación para Sesión 7

Para la próxima sesión (Lunes 21/07), tendrá lista:

1. **Infraestructura base:** VNET con segmentación apropiada
2. **Security baseline:** NSGs configurados y validados
3. **Administrative access:** Bastion y Jump Box funcionales
4. **Documentation:** Arquitectura documentada y planificada

### La Sesión 7 expandirá:
- Azure Firewall implementation
- DDoS Protection Standard
- Network monitoring y alerting
- Hub-and-Spoke architecture completion
- Application deployment en la infraestructura segura

---

## 🌟 Conceptos Avanzados Aplicados

Este laboratorio implementa:

### 1. **Zero Trust Networking:** 
- Never trust, always verify
- Micro-segmentation implemented
- Least privilege access controls

### 2. **Defense in Depth:** 
- Multiple security layers
- Fail-safe defaults
- Comprehensive monitoring points

### 3. **Secure by Design:** 
- Security considerations first
- Scalable architecture patterns
- Compliance-ready structure

### 4. **Cloud Native Security:** 
- Azure-native security services
- Infrastructure as Code ready
- Automation-friendly design

---

## 📚 Recursos Adicionales

### Para profundizar conocimientos:

1. **Microsoft Documentation:** 
   - [Azure Virtual Network documentation](https://docs.microsoft.com/en-us/azure/virtual-network/)
   - [Network Security Groups](https://docs.microsoft.com/en-us/azure/virtual-network/network-security-groups-overview)
   - [Azure Bastion](https://docs.microsoft.com/en-us/azure/bastion/)

2. **Best Practices:** 
   - [Azure Network Security Best Practices](https://docs.microsoft.com/en-us/azure/security/fundamentals/network-best-practices)
   - [Hub-spoke network topology](https://docs.microsoft.com/en-us/azure/architecture/reference-architectures/hybrid-networking/hub-spoke)

3. **Tools para práctica:** 
   - [Azure Network Watcher](https://docs.microsoft.com/en-us/azure/network-watcher/)
   - [Azure CLI network commands](https://docs.microsoft.com/en-us/cli/azure/network)
   - [PowerShell Az.Network module](https://docs.microsoft.com/en-us/powershell/module/az.network/)

---

## 🎉 ¡Felicitaciones!

Ha completado exitosamente la implementación de una infraestructura de red segura en Azure. Esta base sólida le permitirá:

- ✅ **Deployar aplicaciones** de manera segura
- ✅ **Escalar horizontalmente** agregando nuevos componentes
- ✅ **Mantener compliance** con frameworks de seguridad
- ✅ **Monitorear y auditar** toda la actividad de red
- ✅ **Responder eficientemente** a incidentes de seguridad

La arquitectura que ha construido es **production-ready** y sigue las mejores prácticas de la industria para seguridad de infraestructura en la nube.

**¡Nos vemos en la Sesión 7 para continuar construyendo sobre esta base sólida!** 🚀
