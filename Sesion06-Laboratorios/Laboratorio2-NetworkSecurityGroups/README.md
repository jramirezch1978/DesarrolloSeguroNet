# 🧪 LABORATORIO 2: IMPLEMENTACIÓN DE NETWORK SECURITY GROUPS (NSGs)

## 📋 Información General
- **⏱️ Duración:** 25 minutos
- **🎯 Objetivo:** Crear y configurar NSGs con reglas de seguridad granulares para cada subnet
- **📚 Curso:** Diseño Seguro de Aplicaciones (.NET en Azure)
- **🔧 Prerequisitos:** Laboratorio 1 completado (VNET con 4 subredes)

---

## 📝 Paso 1: Crear NSG para DMZ Subnet (6 minutos)

### Crear NSG-DMZ:

1. **Azure Portal → Network security groups → + Create:**
   ```
   Subscription: [Su suscripción]
   Resource group: rg-infraestructura-segura-[SuNombre]
   Name: nsg-dmz-[sunombre]
   Region: East US
   ```

### Configurar reglas de seguridad para DMZ:

#### **Regla 1 - Permitir HTTP/HTTPS desde Internet:**
```
Name: Allow-HTTP-HTTPS-Inbound
Priority: 100
Source: Any
Source port ranges: *
Destination: Any  
Destination port ranges: 80,443
Protocol: TCP
Action: Allow
```

#### **Regla 2 - Permitir SSH para administración:**
```
Name: Allow-SSH-Management
Priority: 110
Source: 10.1.10.0/24 (Management subnet)
Source port ranges: *
Destination: Any
Destination port ranges: 22
Protocol: TCP
Action: Allow
```

#### **Regla 3 - Denegar todo lo demás:**
```
Name: Deny-All-Other-Inbound
Priority: 4000
Source: Any
Source port ranges: *
Destination: Any
Destination port ranges: *
Protocol: Any
Action: Deny
```

### Comando Azure CLI para crear NSG-DMZ:
```bash
# Crear NSG
az network nsg create \
  --resource-group rg-infraestructura-segura-[SuNombre] \
  --name nsg-dmz-[sunombre] \
  --location eastus

# Agregar reglas
az network nsg rule create \
  --resource-group rg-infraestructura-segura-[SuNombre] \
  --nsg-name nsg-dmz-[sunombre] \
  --name Allow-HTTP-HTTPS-Inbound \
  --priority 100 \
  --source-address-prefixes '*' \
  --destination-port-ranges 80 443 \
  --access Allow \
  --protocol Tcp
```

---

## 📝 Paso 2: Crear NSG para Private Subnet (6 minutos)

### Crear NSG-Private:

1. **Crear el NSG:**
   ```
   Name: nsg-private-[sunombre]
   Resource group: rg-infraestructura-segura-[SuNombre]
   ```

### Reglas para Private Subnet:

#### **Regla 1 - Permitir tráfico desde DMZ:**
```
Name: Allow-DMZ-to-Private
Priority: 100
Source: 10.1.1.0/24 (DMZ subnet)
Destination: 10.1.2.0/24 (Private subnet)
Destination port ranges: 80,443,8080,8443
Protocol: TCP
Action: Allow
```

#### **Regla 2 - Permitir comunicación interna:**
```
Name: Allow-Private-Internal
Priority: 110
Source: 10.1.2.0/24
Destination: 10.1.2.0/24
Destination port ranges: *
Protocol: Any
Action: Allow
```

#### **Regla 3 - Permitir acceso a Data subnet:**
```
Name: Allow-Private-to-Data
Priority: 120
Source: 10.1.2.0/24
Destination: 10.1.3.0/24
Destination port ranges: 1433,3306,5432  # SQL Server, MySQL, PostgreSQL
Protocol: TCP
Action: Allow
```

#### **Regla 4 - Denegar Internet directo:**
```
Name: Deny-Internet-Direct
Priority: 4000
Source: Internet
Destination: 10.1.2.0/24
Destination port ranges: *
Protocol: Any
Action: Deny
```

---

## 📝 Paso 3: Crear NSG para Data Subnet (6 minutos)

### Crear NSG-Data (Máxima Seguridad):

1. **Crear el NSG:**
   ```
   Name: nsg-data-[sunombre]
   ```

### Reglas restrictivas para Data:

#### **Regla 1 - Solo Private subnet puede acceder:**
```
Name: Allow-Private-to-Data-DB
Priority: 100
Source: 10.1.2.0/24 (Private subnet only)
Destination: 10.1.3.0/24
Destination port ranges: 1433,3306,5432,6379  # Databases + Redis
Protocol: TCP
Action: Allow
```

#### **Regla 2 - Permitir backup desde Management:**
```
Name: Allow-Management-Backup
Priority: 110
Source: 10.1.10.0/24
Destination: 10.1.3.0/24
Destination port ranges: 22,3389  # SSH, RDP para backups
Protocol: TCP
Action: Allow
```

#### **Regla 3 - Denegar todo lo demás:**
```
Name: Deny-All-Other-Data
Priority: 4000
Source: Any
Destination: 10.1.3.0/24
Destination port ranges: *
Protocol: Any
Action: Deny
```

---

## 📝 Paso 4: Crear NSG para Management Subnet (4 minutos)

### Crear NSG-Management:

1. **Reglas para administración segura:**

#### **Regla 1 - SSH/RDP desde ubicaciones específicas:**
```
Name: Allow-Admin-Access
Priority: 100
Source: [Su IP pública actual]  # Usar whatismyip.com
Destination: 10.1.10.0/24
Destination port ranges: 22,3389
Protocol: TCP
Action: Allow
```

#### **Regla 2 - Acceso a todas las subredes para administración:**
```
Name: Allow-Management-to-All
Priority: 110
Source: 10.1.10.0/24
Destination: 10.1.0.0/16  # Toda la VNET
Destination port ranges: *
Protocol: Any
Action: Allow
```

### 🔍 Obtener su IP pública:
```bash
# Desde PowerShell/CMD
curl ifconfig.me

# O visitar: https://whatismyip.com
```

---

## 📝 Paso 5: Asociar NSGs con Subredes (3 minutos)

### Asociar cada NSG con su subnet correspondiente:

1. **DMZ Subnet:**
   - VNET → Subnets → snet-dmz → Network security group → nsg-dmz-[sunombre]

2. **Private Subnet:**
   - VNET → Subnets → snet-private → Network security group → nsg-private-[sunombre]

3. **Data Subnet:**
   - VNET → Subnets → snet-data → Network security group → nsg-data-[sunombre]

4. **Management Subnet:**
   - VNET → Subnets → snet-management → Network security group → nsg-management-[sunombre]

### Con Azure CLI:
```bash
# Asociar NSGs con subredes
az network vnet subnet update \
  --resource-group rg-infraestructura-segura-[SuNombre] \
  --vnet-name vnet-principal-[sunombre] \
  --name snet-dmz \
  --network-security-group nsg-dmz-[sunombre]

az network vnet subnet update \
  --resource-group rg-infraestructura-segura-[SuNombre] \
  --vnet-name vnet-principal-[sunombre] \
  --name snet-private \
  --network-security-group nsg-private-[sunombre]

az network vnet subnet update \
  --resource-group rg-infraestructura-segura-[SuNombre] \
  --vnet-name vnet-principal-[sunombre] \
  --name snet-data \
  --network-security-group nsg-data-[sunombre]

az network vnet subnet update \
  --resource-group rg-infraestructura-segura-[SuNombre] \
  --vnet-name vnet-principal-[sunombre] \
  --name snet-management \
  --network-security-group nsg-management-[sunombre]
```

---

## ✅ Resultado Esperado

Al completar este laboratorio debe tener:

- ✅ **4 NSGs creados** con reglas específicas:
  - `nsg-dmz-[sunombre]`
  - `nsg-private-[sunombre]`
  - `nsg-data-[sunombre]`
  - `nsg-management-[sunombre]`
- ✅ **Cada subnet protegida** por su NSG correspondiente
- ✅ **Tráfico controlado** según principios de seguridad
- ✅ **Principio de least privilege** implementado
- ✅ **Base lista** para deployment de recursos

---

## 🔒 Principios de Seguridad Implementados

### **🛡️ Defense in Depth:**
- Múltiples capas de seguridad (DMZ → Private → Data)
- Controles granulares en cada nivel
- Fail-safe defaults (deny by default)

### **🎯 Principle of Least Privilege:**
- Solo el tráfico necesario es permitido
- Acceso basado en origen y destino específicos
- Puertos limitados a los requeridos

### **🔐 Network Segmentation:**
- Aislamiento por función (DMZ, Private, Data, Management)
- Control de flujo de tráfico bidireccional
- Prevención de lateral movement

### **📊 Auditability:**
- Reglas nombradas descriptivamente
- Prioridades organizadas lógicamente
- Trazabilidad de decisiones de seguridad

---

## 🚨 Troubleshooting Común

### Error: "NSG rule priority conflict"
- **Solución:** Usar prioridades únicas (100, 110, 120, etc.)
- **Verificación:** Azure Portal → NSG → Security rules

### Error: "Cannot associate NSG with subnet"
- **Solución:** 
  - Verificar que el NSG y subnet están en la misma región
  - Confirmar permisos de Network Contributor
  - Revisar que no hay conflictos con otros NSGs

### Error: "Rule validation failed"
- **Solución:**
  - Verificar formato de IP addresses (usar CIDR notation)
  - Confirmar que port ranges son válidos
  - Revisar que source/destination no estén vacíos

### Error: "Effective security rules not updating"
- **Solución:**
  - Esperar 2-3 minutos para propagación
  - Refrescar la vista en Azure Portal
  - Verificar con Azure CLI: `az network nic list-effective-nsg`

---

## 📊 Matriz de Tráfico Implementada

| Origen | Destino | Puertos | Protocolo | Acción | NSG Rule |
|--------|---------|---------|-----------|--------|----------|
| Internet | DMZ | 80,443 | TCP | ✅ Allow | Allow-HTTP-HTTPS-Inbound |
| Management | DMZ | 22 | TCP | ✅ Allow | Allow-SSH-Management |
| DMZ | Private | 80,443,8080,8443 | TCP | ✅ Allow | Allow-DMZ-to-Private |
| Private | Private | * | Any | ✅ Allow | Allow-Private-Internal |
| Private | Data | 1433,3306,5432 | TCP | ✅ Allow | Allow-Private-to-Data |
| Management | Data | 22,3389 | TCP | ✅ Allow | Allow-Management-Backup |
| Management | All | * | Any | ✅ Allow | Allow-Management-to-All |
| Internet | Private | * | Any | ❌ Deny | Deny-Internet-Direct |
| Any | Data | * | Any | ❌ Deny | Deny-All-Other-Data |

---

## 📋 Próximos Pasos

Con los NSGs implementados, procederá a:

1. **Laboratorio 3:** Implementar Azure Bastion y Jump Box
2. **Laboratorio 4:** Testing de conectividad y validación
3. **Sesión 7:** Azure Firewall y DDoS Protection

---

## 📚 Recursos Adicionales

- [Azure NSG Documentation](https://docs.microsoft.com/en-us/azure/virtual-network/network-security-groups-overview)
- [Network Security Best Practices](https://docs.microsoft.com/en-us/azure/security/fundamentals/network-best-practices)
- [Azure Network Watcher](https://docs.microsoft.com/en-us/azure/network-watcher/)
- [NSG Flow Logs](https://docs.microsoft.com/en-us/azure/network-watcher/network-watcher-nsg-flow-logging-overview)

---

**¡Excelente trabajo!** Ha implementado una arquitectura de seguridad robusta con controles granulares. Su red ahora está protegida por múltiples capas de seguridad siguiendo las mejores prácticas de la industria. 🛡️
