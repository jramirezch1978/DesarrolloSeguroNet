# 🧪 Laboratorio 1: Implementación de Azure Security Center Avanzado

## ⏱️ Duración: 25 minutos
## 🎯 Objetivo: Configurar y utilizar Azure Security Center para evaluación continua de seguridad

---

## 📋 Prerrequisitos
- Laboratorio 0 completado exitosamente
- Acceso a Azure Portal con permisos de Security Center
- Azure CLI autenticado
- PowerShell como Administrador

---

## 🚀 Paso 1: Verificar Infraestructura Base (5 minutos)

### Si NO completaron los laboratorios de infraestructura de sesiones anteriores, ejecutar:

```powershell
# Definir variables
$resourceGroupName = "rg-security-lab-$env:USERNAME"
$location = "eastus"

# Crear resource group base
az group create `
    --name $resourceGroupName `
    --location $location `
    --tags Environment=Development Project=SecurityLab

# Crear VNET básica para el laboratorio
az network vnet create `
    --resource-group $resourceGroupName `
    --name vnet-security-lab `
    --address-prefix 10.3.0.0/16 `
    --subnet-name snet-web `
    --subnet-prefix 10.3.1.0/24

# Agregar subredes adicionales
az network vnet subnet create `
    --resource-group $resourceGroupName `
    --vnet-name vnet-security-lab `
    --name snet-app `
    --address-prefix 10.3.2.0/24

az network vnet subnet create `
    --resource-group $resourceGroupName `
    --vnet-name vnet-security-lab `
    --name snet-data `
    --address-prefix 10.3.3.0/24

Write-Host "✅ Infraestructura base creada exitosamente" -ForegroundColor Green
```

**✅ Verificación:** Debe ver el resource group y VNET creados en Azure Portal.

---

## 🔧 Paso 2: Configurar Azure Security Center (10 minutos)

### Habilitar Security Center con configuración avanzada:

```powershell
# Habilitar Defender for Servers
az security pricing create `
    --name VirtualMachines `
    --tier Standard

# Habilitar Defender for App Service  
az security pricing create `
    --name AppServices `
    --tier Standard

# Habilitar Defender for Storage
az security pricing create `
    --name StorageAccounts `
    --tier Standard

# Habilitar Defender for SQL
az security pricing create `
    --name SqlServers `
    --tier Standard

# Verificar configuración
az security pricing list --output table
```

### Configurar Auto Provisioning:

```powershell
# Habilitar auto provisioning del Log Analytics agent
az security auto-provisioning-setting update `
    --name default `
    --auto-provision On

# Configurar workspace de Log Analytics
az monitor log-analytics workspace create `
    --resource-group $resourceGroupName `
    --workspace-name "law-security-monitoring-$env:USERNAME" `
    --location $location `
    --sku PerGB2018

Write-Host "✅ Security Center configurado con planes de Defender" -ForegroundColor Green
```

**✅ Verificación:** En Azure Portal → Microsoft Defender for Cloud → Environment settings debe mostrar los planes habilitados.

---

## 🖥️ Paso 3: Crear Recursos de Testing (8 minutos)

### Crear VMs y recursos para evaluación:

```powershell
# VM Windows para testing
az vm create `
    --resource-group $resourceGroupName `
    --name vm-windows-test `
    --image Win2022Datacenter `
    --vnet-name vnet-security-lab `
    --subnet snet-web `
    --size Standard_B2s `
    --admin-username azureuser `
    --admin-password "SecureP@ssw0rd123!" `
    --public-ip-address pip-windows-test `
    --nsg nsg-windows-test

# VM Linux para testing
az vm create `
    --resource-group $resourceGroupName `
    --name vm-linux-test `
    --image Ubuntu2204 `
    --vnet-name vnet-security-lab `
    --subnet snet-app `
    --size Standard_B2s `
    --admin-username azureuser `
    --generate-ssh-keys `
    --public-ip-address pip-linux-test `
    --nsg nsg-linux-test

# App Service con configuración insegura (para testing)
az appservice plan create `
    --name "plan-security-test-$env:USERNAME" `
    --resource-group $resourceGroupName `
    --sku B1 `
    --is-linux

az webapp create `
    --resource-group $resourceGroupName `
    --plan "plan-security-test-$env:USERNAME" `
    --name "webapp-security-test-$env:USERNAME" `
    --runtime "DOTNETCORE:8.0"

# Storage Account con configuración por defecto
az storage account create `
    --name "stsecuritytest$env:USERNAME" `
    --resource-group $resourceGroupName `
    --location $location `
    --sku Standard_LRS `
    --kind StorageV2

Write-Host "✅ Recursos de testing creados exitosamente" -ForegroundColor Green
```

**✅ Verificación:** Debe ver las VMs, App Service y Storage Account en el resource group.

---

## 📋 Paso 4: Configurar Custom Security Policies (7 minutos)

### Crear política para requerir HTTPS:

```powershell
# Crear archivo policy-https.json
$policyContent = @"
{
  "displayName": "Require HTTPS for Web Apps - Security Lab",
  "policyType": "Custom",
  "mode": "All",
  "description": "Ensures all web applications use HTTPS only",
  "policyRule": {
    "if": {
      "allOf": [
        {
          "field": "type",
          "equals": "Microsoft.Web/sites"
        },
        {
          "field": "Microsoft.Web/sites/httpsOnly",
          "equals": false
        }
      ]
    },
    "then": {
      "effect": "audit"
    }
  }
}
"@

$policyContent | Out-File -FilePath "policy-https.json" -Encoding UTF8

# Crear la política personalizada
az policy definition create `
    --name "require-https-webapps" `
    --display-name "Require HTTPS for Web Apps - Security Lab" `
    --description "Ensures all web applications use HTTPS only" `
    --rules policy-https.json `
    --mode All

# Asignar la política al resource group
az policy assignment create `
    --name "assign-https-policy" `
    --policy "require-https-webapps" `
    --scope "/subscriptions/$(az account show --query id -o tsv)/resourceGroups/$resourceGroupName"
```

### Crear política para Storage Account encryption:

```powershell
# Crear política para requerir encryption en Storage Accounts
az policy assignment create `
    --name "require-storage-encryption" `
    --policy "/providers/Microsoft.Authorization/policyDefinitions/404c3081-a854-4457-ae30-26a93ef643f9" `
    --scope "/subscriptions/$(az account show --query id -o tsv)/resourceGroups/$resourceGroupName"

Write-Host "✅ Políticas personalizadas creadas y asignadas" -ForegroundColor Green
```

**✅ Verificación:** En Azure Portal → Policy → Assignments debe mostrar las políticas asignadas.

---

## 📊 Paso 5: Verificar Configuración Completa (5 minutos)

### Script de verificación final:

```powershell
# Script de verificación completa
$verificationScript = @"
Write-Host "=== VERIFICACIÓN DE SECURITY CENTER ===" -ForegroundColor Green

# Verificar planes de Defender habilitados
Write-Host "`n🔍 Verificando planes de Defender..." -ForegroundColor Yellow
$pricing = az security pricing list --output json | ConvertFrom-Json
foreach ($plan in $pricing) {
    $status = if ($plan.pricingTier -eq "Standard") { "✅" } else { "❌" }
    Write-Host "$status $($plan.name): $($plan.pricingTier)" -ForegroundColor $(if ($plan.pricingTier -eq "Standard") { "Green" } else { "Red" })
}

# Verificar políticas asignadas
Write-Host "`n🔍 Verificando políticas asignadas..." -ForegroundColor Yellow
$policies = az policy assignment list --scope "/subscriptions/$(az account show --query id -o tsv)/resourceGroups/$resourceGroupName" --output json | ConvertFrom-Json
foreach ($policy in $policies) {
    Write-Host "✅ $($policy.displayName)" -ForegroundColor Green
}

# Verificar recursos creados
Write-Host "`n🔍 Verificando recursos creados..." -ForegroundColor Yellow
$resources = az resource list --resource-group $resourceGroupName --output json | ConvertFrom-Json
$resourceTypes = $resources | Group-Object type | Sort-Object Count -Descending
foreach ($type in $resourceTypes) {
    Write-Host "✅ $($type.Name): $($type.Count) recursos" -ForegroundColor Green
}

# Verificar auto provisioning
Write-Host "`n🔍 Verificando auto provisioning..." -ForegroundColor Yellow
$autoProvision = az security auto-provisioning-setting show --name default --output json | ConvertFrom-Json
$status = if ($autoProvision.autoProvision -eq "On") { "✅" } else { "❌" }
Write-Host "$status Auto Provisioning: $($autoProvision.autoProvision)" -ForegroundColor $(if ($autoProvision.autoProvision -eq "On") { "Green" } else { "Red" })

Write-Host "`n=== VERIFICACIÓN COMPLETADA ===" -ForegroundColor Green
"@

# Guardar y ejecutar script
$verificationScript | Out-File -FilePath "verify-security-center.ps1" -Encoding UTF8
.\verify-security-center.ps1
```

**✅ Verificación Final:** Todos los componentes deben mostrar ✅ verde.

---

## 🎯 Resultados Esperados

Al completar este laboratorio, habrá logrado:

### 🔒 Azure Security Center Avanzado:
- ✅ Microsoft Defender for Cloud configurado con planes específicos
- ✅ Secure Score monitoring y análisis de tendencias
- ✅ Custom security policies implementadas y asignadas
- ✅ Regulatory compliance dashboard configurado

### 🛡️ Infraestructura de Testing:
- ✅ VMs Windows y Linux para vulnerability assessment
- ✅ App Service con configuración para testing
- ✅ Storage Account para evaluación de seguridad
- ✅ VNET segmentada para diferentes tipos de recursos

### 📋 Políticas de Seguridad:
- ✅ Política personalizada para requerir HTTPS en web apps
- ✅ Política para encryption en Storage Accounts
- ✅ Políticas asignadas al resource group específico

---

## 🚨 Troubleshooting Común

### Error: "Insufficient permissions for Security Center"
**Solución:**
```powershell
# Verificar roles asignados
az role assignment list --assignee $env:USERNAME --output table

# El usuario necesita rol "Security Reader" o "Security Admin"
# Contactar al administrador de la suscripción si es necesario
```

### Error: "Resource group not found"
**Solución:**
```powershell
# Verificar que el resource group existe
az group list --output table

# Si no existe, ejecutar el Paso 1 completo
```

### Error: "Policy definition creation failed"
**Solución:**
```powershell
# Verificar que el archivo JSON es válido
Get-Content policy-https.json | ConvertFrom-Json

# Si hay errores, recrear el archivo manualmente
```

---

## 📊 Métricas de Éxito

Indicadores de Implementación Exitosa:
- ✅ Security Center Configuration: Microsoft Defender plans habilitados para todos los servicios
- ✅ Secure Score visible y reportando métricas
- ✅ Custom policies creadas y asignadas correctamente
- ✅ Compliance dashboard mostrando estado actual

---

## 🔗 Recursos Adicionales

- [Azure Security Center Documentation](https://docs.microsoft.com/azure/security-center/)
- [Microsoft Defender for Cloud](https://docs.microsoft.com/azure/defender-for-cloud/)
- [Azure Policy Documentation](https://docs.microsoft.com/azure/governance/policy/)
- [Security Center Pricing](https://azure.microsoft.com/pricing/details/security-center/)

---

## 🎯 Próximos Pasos

Una vez completado este laboratorio, estará listo para:

1. **Laboratorio 2:** Vulnerability Assessment y Scanning Automatizado
2. **Laboratorio 3:** Análisis de Secure Score y Automatización

---

## 📝 Notas Importantes

- **Costos:** Los planes de Defender Standard generan costos adicionales
- **Tiempo de Propagación:** Los cambios pueden tardar hasta 24 horas en reflejarse completamente
- **Permisos:** Asegúrese de tener permisos adecuados para Security Center
- **Limpieza:** Recuerde ejecutar los scripts de limpieza al final de todos los laboratorios

---

**✅ ¡Azure Security Center configurado exitosamente! Está listo para el siguiente laboratorio.** 