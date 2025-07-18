# 🧪 LABORATORIO 1: CREACIÓN DE VIRTUAL NETWORK (VNET) SEGURA

## 📋 Información General
- **⏱️ Duración:** 20 minutos
- **🎯 Objetivo:** Crear y configurar una Virtual Network con segmentación apropiada
- **📚 Curso:** Diseño Seguro de Aplicaciones (.NET en Azure)
- **🔧 Herramientas:** Azure Portal + Azure CLI

---

## 📝 Paso 1: Creación del Resource Group Base (3 minutos)

### Desde Azure Portal:

1. **Navegar a Resource Groups:**
   - Azure Portal → Resource groups → + Create

2. **Configuración del Resource Group:**
   ```
   Subscription: [Su suscripción asignada]
   Resource group: rg-infraestructura-segura-[SuNombre]
   Region: East US
   ```

3. **Tags (etiquetas):**
   ```
   Environment: Development
   Project: InfraestructuraSegura
   Owner: [Su nombre]
   Course: DesarrolloSeguroAzure
   ```

### Comando alternativo con Azure CLI:
```bash
# Crear resource group
az group create \
  --name rg-infraestructura-segura-[SuNombre] \
  --location eastus \
  --tags Environment=Development Project=InfraestructuraSegura
```

---

## 📝 Paso 2: Planificación del Address Space (2 minutos)

### Diseño de la arquitectura de red:

```
VNET Principal: 10.1.0.0/16 (65,534 IPs disponibles)

├── DMZ Subnet: 10.1.1.0/24 (254 IPs)
│   └── Componentes: Load Balancers, WAF, Bastion
│
├── Private Subnet: 10.1.2.0/24 (254 IPs) 
│   └── Componentes: App Servers, APIs internas
│
├── Data Subnet: 10.1.3.0/24 (254 IPs)
│   └── Componentes: Databases, Storage
│
└── Management Subnet: 10.1.10.0/24 (254 IPs)
    └── Componentes: Jump boxes, Management tools
```

### Consideraciones importantes:
- **Address space planning:** Una vez implementado, es difícil cambiar
- **Overlap prevention:** Los rangos no pueden solaparse con redes on-premises
- **Growth planning:** Dimensionar para crecimiento futuro
- **Azure reservations:** Las primeras 5 IPs de cada subnet están reservadas

---

## 📝 Paso 3: Crear Virtual Network Principal (8 minutos)

### Desde Azure Portal:

1. **Navegar a Virtual Networks:**
   - Azure Portal → Virtual networks → + Create

2. **Configuración Básica:**
   ```
   Subscription: [Su suscripción]
   Resource group: rg-infraestructura-segura-[SuNombre]
   Name: vnet-principal-[sunombre]
   Region: East US
   ```

3. **IP Addresses:**
   ```
   IPv4 address space: 10.1.0.0/16
   ```

4. **Crear las 4 subredes:**

   **Subnet 1 - DMZ:**
   ```
   Name: snet-dmz
   Subnet address range: 10.1.1.0/24
   ```

   **Subnet 2 - Private:**
   ```
   Name: snet-private  
   Subnet address range: 10.1.2.0/24
   ```

   **Subnet 3 - Data:**
   ```
   Name: snet-data
   Subnet address range: 10.1.3.0/24
   ```

   **Subnet 4 - Management:**
   ```
   Name: snet-management
   Subnet address range: 10.1.10.0/24
   ```

5. **Security (mantener defaults por ahora):**
   - BastionHost: Disabled (lo configuraremos después)
   - DDoS Protection Standard: Disabled (para laboratorio)
   - Firewall: Disabled (lo configuraremos después)

### Comando alternativo con Azure CLI:
```bash
# Crear VNET con subredes
az network vnet create \
  --resource-group rg-infraestructura-segura-[SuNombre] \
  --name vnet-principal-[sunombre] \
  --address-prefix 10.1.0.0/16 \
  --subnet-name snet-dmz \
  --subnet-prefix 10.1.1.0/24

# Añadir subredes adicionales
az network vnet subnet create \
  --resource-group rg-infraestructura-segura-[SuNombre] \
  --vnet-name vnet-principal-[sunombre] \
  --name snet-private \
  --address-prefix 10.1.2.0/24

az network vnet subnet create \
  --resource-group rg-infraestructura-segura-[SuNombre] \
  --vnet-name vnet-principal-[sunombre] \
  --name snet-data \
  --address-prefix 10.1.3.0/24

az network vnet subnet create \
  --resource-group rg-infraestructura-segura-[SuNombre] \
  --vnet-name vnet-principal-[sunombre] \
  --name snet-management \
  --address-prefix 10.1.10.0/24
```

---

## 📝 Paso 4: Verificación de la VNET (4 minutos)

### Verificar configuración:

1. **En Azure Portal:**
   - Ir a su VNET creada
   - Verificar: Overview → Address space
   - Verificar: Subnets → Ver las 4 subredes creadas

2. **Con Azure CLI:**
   ```bash
   # Listar VNETs
   az network vnet list --resource-group rg-infraestructura-segura-[SuNombre] --output table
   
   # Ver detalles de subredes
   az network vnet subnet list \
     --resource-group rg-infraestructura-segura-[SuNombre] \
     --vnet-name vnet-principal-[sunombre] \
     --output table
   ```

### Verificaciones importantes:
- ✅ VNET creada con address space 10.1.0.0/16
- ✅ 4 subredes con rangos correctos
- ✅ Sin overlapping en address spaces
- ✅ Nombres consistentes con convención

---

## 📝 Paso 5: Configurar DNS Personalizado (Opcional) (3 minutos)

### Para empresas que requieren DNS personalizado:

1. **En su VNET → DNS servers:**
   ```
   DNS servers: Custom
   DNS server addresses: 
   - 8.8.8.8 (Google DNS - para laboratorio)
   - 8.8.4.4 (Google DNS secundario)
   ```

2. **Aplicar configuración y esperar propagación**

### Consideraciones:
- Para laboratorio, Azure default DNS es suficiente
- En producción, considerar Azure Private DNS Zones
- DNS personalizado requiere reinicio de VMs existentes

---

## ✅ Resultado Esperado

Al completar este laboratorio debe tener:

- ✅ **VNET creada** con 4 subredes segmentadas
- ✅ **Address space** apropiadamente planificado (10.1.0.0/16)
- ✅ **Subredes especializadas:**
  - DMZ (10.1.1.0/24)
  - Private (10.1.2.0/24)
  - Data (10.1.3.0/24)
  - Management (10.1.10.0/24)
- ✅ **DNS configurado** (opcional)
- ✅ **Base sólida** para implementar security groups

---

## 🚨 Troubleshooting Común

### Error: "Address space overlaps"
- **Solución:** Verificar que los rangos IP no se solapen
- **Comando:** `az network vnet subnet list --vnet-name [vnet] --resource-group [rg] --output table`

### Error: "Subnet too small"
- **Solución:** Azure requiere mínimo /29 (8 IPs) por subnet
- **Recomendación:** Usar /24 para flexibilidad futura

### Error: "Cannot create subnet"
- **Solución:** 
  - Verificar permisos en la suscripción
  - Confirmar que el resource group existe
  - Validar naming conventions de Azure

### Error: "Region not available"
- **Solución:** 
  - Usar region alternativa (West US, Central US)
  - Verificar quota limits en la suscripción

---

## 📊 Arquitectura Implementada

```
🌐 INTERNET
    │
    ▼
┌─────────────────────────────────────────────┐
│        VNET: 10.1.0.0/16                   │
│                                             │
│  ┌─────────────┐  ┌─────────────────────┐   │
│  │    DMZ      │  │      PRIVATE        │   │
│  │ 10.1.1.0/24 │  │   10.1.2.0/24       │   │
│  │             │  │                     │   │
│  │ • WAF       │  │ • App Servers       │   │
│  │ • LB        │  │ • APIs              │   │
│  │ • Bastion   │  │ • Microservices     │   │
│  └─────────────┘  └─────────────────────┘   │
│                                             │
│  ┌─────────────┐  ┌─────────────────────┐   │
│  │    DATA     │  │    MANAGEMENT       │   │
│  │ 10.1.3.0/24 │  │   10.1.10.0/24      │   │
│  │             │  │                     │   │
│  │ • Databases │  │ • Jump Boxes        │   │
│  │ • Storage   │  │ • Admin Tools       │   │
│  │ • Cache     │  │ • Monitoring        │   │
│  └─────────────┘  └─────────────────────┘   │
└─────────────────────────────────────────────┘
```

---

## 📋 Próximos Pasos

Con la VNET base implementada, procederá a:

1. **Laboratorio 2:** Implementar Network Security Groups (NSGs)
2. **Laboratorio 3:** Configurar Azure Bastion y Jump Box
3. **Laboratorio 4:** Testing y validación de arquitectura

---

## 📚 Recursos Adicionales

- [Azure Virtual Network Documentation](https://docs.microsoft.com/en-us/azure/virtual-network/)
- [Azure Subnet Planning](https://docs.microsoft.com/en-us/azure/virtual-network/virtual-network-vnet-plan-design-arm)
- [Azure DNS Documentation](https://docs.microsoft.com/en-us/azure/dns/)
- [Network Security Best Practices](https://docs.microsoft.com/en-us/azure/security/fundamentals/network-best-practices)

---

**¡Excelente trabajo!** Ha creado la base de red segura para toda la infraestructura. En el próximo laboratorio implementaremos las reglas de seguridad granulares. 🎯
