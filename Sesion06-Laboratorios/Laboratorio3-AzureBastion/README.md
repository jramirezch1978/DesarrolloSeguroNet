# 🧪 LABORATORIO 3: IMPLEMENTACIÓN DE AZURE BASTION Y JUMP BOX

## 📋 Información General
- **⏱️ Duración:** 20 minutos
- **🎯 Objetivo:** Implementar acceso administrativo seguro usando Azure Bastion y Jump Box personalizado
- **📚 Curso:** Diseño Seguro de Aplicaciones (.NET en Azure)
- **🔧 Prerequisitos:** Laboratorios 1 y 2 completados (VNET con NSGs)

---

## 📝 Paso 1: Crear Subnet para Azure Bastion (3 minutos)

### Azure Bastion requiere subnet específica:

1. **Agregar AzureBastionSubnet:**
   ```
   VNET: vnet-principal-[sunombre]
   Subnet name: AzureBastionSubnet (nombre obligatorio)
   Subnet address range: 10.1.100.0/26 (mínimo /26 requerido)
   ```

2. **En Azure Portal:**
   - VNET → Subnets → + Subnet
   - Name: `AzureBastionSubnet`
   - Address range: `10.1.100.0/26`

### Con Azure CLI:
```bash
# Crear subnet para Bastion
az network vnet subnet create \
  --resource-group rg-infraestructura-segura-[SuNombre] \
  --vnet-name vnet-principal-[sunombre] \
  --name AzureBastionSubnet \
  --address-prefix 10.1.100.0/26
```

### ⚠️ Consideraciones importantes:
- **Nombre obligatorio:** Debe ser exactamente `AzureBastionSubnet`
- **Tamaño mínimo:** /26 (64 IPs) es el mínimo requerido
- **Sin NSG:** Azure Bastion subnet no debe tener NSG asociado

---

## 📝 Paso 2: Implementar Azure Bastion (8 minutos)

### Crear Azure Bastion Host:

1. **Azure Portal → Bastions → + Create:**
   ```
   Subscription: [Su suscripción]
   Resource group: rg-infraestructura-segura-[SuNombre]
   Name: bastion-[sunombre]
   Region: East US
   Tier: Basic (para laboratorio)
   ```

2. **Virtual network:**
   ```
   Virtual network: vnet-principal-[sunombre]
   Subnet: AzureBastionSubnet (auto-seleccionado)
   ```

3. **Public IP para Bastion:**
   ```
   Public IP address: Create new
   Public IP name: pip-bastion-[sunombre]
   SKU: Standard
   Assignment: Static
   ```

4. **Review + Create → Esperar deployment (toma 5-10 minutos)**

### Con Azure CLI:
```bash
# Crear Public IP para Bastion
az network public-ip create \
  --resource-group rg-infraestructura-segura-[SuNombre] \
  --name pip-bastion-[sunombre] \
  --sku Standard \
  --allocation-method Static

# Crear Azure Bastion
az network bastion create \
  --resource-group rg-infraestructura-segura-[SuNombre] \
  --name bastion-[sunombre] \
  --public-ip-address pip-bastion-[sunombre] \
  --vnet-name vnet-principal-[sunombre] \
  --location eastus
```

### 💰 Consideración de costos:
⚠️ **Nota:** Azure Bastion es un servicio premium (~$140 USD/mes). Para laboratorio, también implementaremos Jump Box como alternativa económica.

---

## 📝 Paso 3: Crear Jump Box VM (7 minutos)

### Crear VM de administración en Management Subnet:

1. **Azure Portal → Virtual machines → + Create:**

#### **Basics:**
```
Subscription: [Su suscripción]
Resource group: rg-infraestructura-segura-[SuNombre]
Virtual machine name: vm-jumpbox-[sunombre]
Region: East US
Image: Windows Server 2022 Datacenter
Size: Standard_B2s (2 vcpus, 4 GB RAM)
```

#### **Administrator account:**
```
Authentication type: Password
Username: azureadmin
Password: [Contraseña segura - ej: JumpBox2024!]
```

2. **Networking:**
   ```
   Virtual network: vnet-principal-[sunombre]
   Subnet: snet-management (10.1.10.0/24)
   Public IP: None (acceso solo vía Bastion)
   NIC network security group: None (usaremos subnet NSG)
   ```

3. **Disks:**
   ```
   OS disk type: Premium SSD (mejor para administración)
   ```

4. **Management:**
   ```
   Enable auto-shutdown: Yes
   Shutdown time: 23:00 (para laboratorio)
   ```

### Con Azure CLI:
```bash
# Crear Jump Box VM
az vm create \
  --resource-group rg-infraestructura-segura-[SuNombre] \
  --name vm-jumpbox-[sunombre] \
  --image Win2022Datacenter \
  --size Standard_B2s \
  --subnet snet-management \
  --vnet-name vnet-principal-[sunombre] \
  --public-ip-address "" \
  --nsg "" \
  --admin-username azureadmin \
  --admin-password "JumpBox2024!"
```

---

## 📝 Paso 4: Configurar Acceso Seguro (2 minutos)

### Configurar NSG para permitir Bastion:

1. **Actualizar nsg-management-[sunombre]:**

#### **Nueva regla para Azure Bastion:**
```
Name: Allow-Bastion-Inbound
Priority: 90
Source: 10.1.100.0/26 (AzureBastionSubnet)
Destination: 10.1.10.0/24 (Management subnet)
Destination port ranges: 3389,22
Protocol: TCP
Action: Allow
```

### Con Azure CLI:
```bash
# Agregar regla para Bastion
az network nsg rule create \
  --resource-group rg-infraestructura-segura-[SuNombre] \
  --nsg-name nsg-management-[sunombre] \
  --name Allow-Bastion-Inbound \
  --priority 90 \
  --source-address-prefixes 10.1.100.0/26 \
  --destination-address-prefixes 10.1.10.0/24 \
  --destination-port-ranges 3389 22 \
  --access Allow \
  --protocol Tcp
```

---

## 🔗 Cómo Usar Azure Bastion

### Conectarse a Jump Box vía Bastion:

1. **En Azure Portal:**
   - Ir a: Virtual machines → vm-jumpbox-[sunombre]
   - Click en "Connect" → "Bastion"
   - Ingresar credenciales:
     - Username: `azureadmin`
     - Password: `JumpBox2024!`
   - Click "Connect"

2. **Desde Jump Box, acceder a otras VMs:**
   - Use RDP/SSH interno desde Jump Box
   - Todas las VMs internas deben estar sin IP pública
   - Jump Box actúa como "puente seguro"

### 🔒 Características de seguridad:
- ✅ **Sin RDP/SSH directo:** No hay puertos 3389/22 expuestos a Internet
- ✅ **Autenticación Azure AD:** Integrado con identidades corporativas
- ✅ **Conditional Access:** Políticas de acceso basadas en ubicación/dispositivo
- ✅ **Session recording:** Posibilidad de grabar sesiones para auditoría
- ✅ **Just-in-time access:** Permisos temporales con PIM

---

## ✅ Resultado Esperado

Al completar este laboratorio debe tener:

- ✅ **AzureBastionSubnet creada** (10.1.100.0/26)
- ✅ **Azure Bastion deployado** (si el presupuesto lo permite)
- ✅ **Jump Box VM configurada** en Management subnet
- ✅ **Acceso administrativo seguro** sin exposición directa a Internet
- ✅ **NSG rules actualizadas** para permitir tráfico de Bastion
- ✅ **Base para administración segura** de toda la infraestructura

---

## 🛡️ Arquitectura de Seguridad Implementada

```
🌐 INTERNET
    │
    ▼
┌─────────────────────────────────────────────┐
│  Azure Bastion Subnet (10.1.100.0/26)      │
│  ┌─────────────────────────────────────────┐ │
│  │        🛡️ Azure Bastion              │ │
│  │    • No direct SSH/RDP               │ │
│  │    • Azure AD integration            │ │
│  │    • Conditional Access              │ │
│  └─────────────────────────────────────────┘ │
│                    │                        │
│                    ▼                        │
│  Management Subnet (10.1.10.0/24)          │
│  ┌─────────────────────────────────────────┐ │
│  │        💻 Jump Box VM                 │ │
│  │    • No public IP                    │ │
│  │    • Access via Bastion only         │ │
│  │    • Admin tools installed           │ │
│  └─────────────────────────────────────────┘ │
│                    │                        │
│         ┌──────────┼──────────┐             │
│         ▼          ▼          ▼             │
│     DMZ Subnet  Private   Data Subnet       │
│    (10.1.1.0/24) Subnet  (10.1.3.0/24)     │
│                (10.1.2.0/24)               │
└─────────────────────────────────────────────┘
```

---

## 🚨 Troubleshooting Común

### Error: "Cannot create subnet - address space overlaps"
- **Solución:** Verificar que 10.1.100.0/26 no está en uso
- **Comando:** `az network vnet subnet list --vnet-name [vnet] --resource-group [rg] --output table`

### Error: "Azure Bastion deployment failed"
- **Solución:** 
  - Verificar que AzureBastionSubnet es exactamente /26 o mayor
  - Confirmar que el nombre de subnet es exactamente "AzureBastionSubnet"
  - Verificar permisos en la suscripción
  - Revisar quota limits para Public IPs

### Error: "Cannot connect to Jump Box via Bastion"
- **Solución:** 
  - Verificar NSG rules permiten tráfico desde AzureBastionSubnet
  - Confirmar que VM está en estado "Running"
  - Revisar effective security rules
  - Validar credenciales de VM

### Error: "Bastion connection timeout"
- **Solución:**
  - Verificar que no hay conflictos de NSG
  - Confirmar que Windows Firewall en Jump Box permite RDP
  - Revisar que VM tiene suficientes recursos (CPU/Memory)

### Error: "VM creation failed in Management subnet"
- **Solución:**
  - Verificar que la subnet tiene IPs disponibles
  - Confirmar que no hay políticas que bloqueen creación de VMs
  - Revisar quota limits de la suscripción

---

## 📊 Mejores Prácticas Implementadas

### **🔐 Zero Trust Access:**
- Sin exposición directa a Internet
- Autenticación multi-factor obligatoria
- Conditional access policies
- Principio de least privilege

### **🎯 Just-in-Time Administration:**
- Acceso administrativo solo cuando se necesita
- Sesiones con tiempo limitado
- Auditoría completa de actividades
- Escalation de privilegios controlada

### **🛡️ Defense in Depth:**
- Múltiples capas de autenticación
- Network segmentation + application security
- Monitoring y alerting integrados
- Incident response preparado

### **📋 Compliance Ready:**
- Session recording capabilities
- Audit logs centralizados
- Access review processes
- Change management integration

---

## 🎯 Alternativas por Presupuesto

### **💰 Opción Económica: Solo Jump Box**
- Crear Jump Box con IP pública temporal
- Configurar NSG para permitir solo su IP
- Remover IP pública después de configuración inicial
- Costo: ~$50-80 USD/mes

### **💎 Opción Premium: Azure Bastion + Jump Box**
- Azure Bastion para acceso seguro sin IPs públicas
- Jump Box para administración avanzada
- Integración completa con Azure AD
- Costo: ~$180-220 USD/mes

### **🏢 Opción Empresarial: Bastion + PAW**
- Azure Bastion Standard tier
- Privileged Access Workstations dedicadas
- Azure AD PIM integration
- Microsoft Defender for Cloud
- Costo: ~$300-500 USD/mes

---

## 📋 Próximos Pasos

Con el acceso administrativo seguro implementado, procederá a:

1. **Laboratorio 4:** Testing de conectividad y validación de arquitectura
2. **Sesión 7:** Azure Firewall y DDoS Protection Advanced
3. **Deployment:** Aplicaciones .NET en la infraestructura segura

---

## 📚 Recursos Adicionales

- [Azure Bastion Documentation](https://docs.microsoft.com/en-us/azure/bastion/)
- [Jump Box Security Best Practices](https://docs.microsoft.com/en-us/azure/security/fundamentals/paas-deployments)
- [Azure AD Conditional Access](https://docs.microsoft.com/en-us/azure/active-directory/conditional-access/)
- [Privileged Identity Management](https://docs.microsoft.com/en-us/azure/active-directory/privileged-identity-management/)

---

**¡Excelente trabajo!** Ha implementado un sistema de acceso administrativo de nivel empresarial. Su infraestructura ahora tiene acceso seguro sin exposición directa a Internet, siguiendo las mejores prácticas de Zero Trust. 🦘
