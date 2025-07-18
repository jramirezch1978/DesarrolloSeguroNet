# 🔑 Laboratorio 2: Integración Completa con Azure Key Vault

## 🎯 Objetivo

Implementar gestión completa de secretos con Azure Key Vault usando Managed Identity y integración con Data Protection API.

## 🚀 Características Implementadas

### ✅ **Data Protection API con Azure Storage**

- Mismo sistema exitoso del Laboratorio 1
- Persistencia en Azure Blob Storage
- Switch dinámico entre Local/Azure Storage
- Logging detallado y manejo de errores

### 🔑 **Azure Key Vault Integration**

- Configuration Provider seamless
- Gestión completa de secretos (CRUD)
- Autenticación con DefaultAzureCredential
- Protección adicional de claves Data Protection

### 🎛️ **Switch Dinámico de Storage**

- Toggle visual para cambiar entre Local/Azure
- Persistencia de preferencias en archivo JSON
- Reinicio automático para aplicar cambios
- Logging claro del tipo de almacenamiento usado

### 🧪 **Testing Completo**

- Interface web interactiva
- Testing de Data Protection API
- Gestión de secrets de Key Vault
- Diagnósticos del sistema
- Verificación de conectividad

## 📋 **Requisitos Previos**

1. **.NET 9 SDK**

   ```bash
   dotnet --version  # Debe mostrar 9.0.x
   ```
2. **Azure CLI autenticado**

   ```bash
   az login
   az account show
   ```
3. **Azure Key Vault configurado**

   - Key Vault creado en Azure Portal
   - Permisos RBAC asignados
   - URI configurado en appsettings.json

## 🔐 **Configuración de Permisos Azure Key Vault**

### **Paso 1: Verificar Suscripción y Usuario**

```bash
# Verificar suscripción actual
az account show --query "{name:name, id:id, user:user.name, state:state}"

# Si no es la suscripción correcta, cambiar a NH25
az account set --subscription "Suscripción NH25"
```

**Resultado esperado:**
```json
{
  "id": "43af7d34-ddbe-4c04-a5d0-97b370408e8d",
  "name": "Suscripción NH25",
  "state": "Enabled",
  "user": "azure.nh2025@newhorizons.edu.pe"
}
```

### **Paso 2: Verificar Configuración del Key Vault**

```bash
# Obtener información del Key Vault
az keyvault show --name kv-devsgro-jarch-001 --query "{name:name, resourceGroup:resourceGroup, enableRbacAuthorization:properties.enableRbacAuthorization, tenantId:properties.tenantId}"
```

**Puntos importantes:**
- `enableRbacAuthorization: true` → Usa RBAC (recomendado)
- `enableRbacAuthorization: false` → Usa Access Policies (legacy)

### **Paso 3: Configurar Permisos RBAC**

El Key Vault usa **RBAC (Role-Based Access Control)**. Necesitas asignar roles específicos:

```bash
# Asignar rol para leer/escribir secretos
az role assignment create \
    --role "Key Vault Secrets User" \
    --assignee azure.nh2025@newhorizons.edu.pe \
    --scope "/subscriptions/43af7d34-ddbe-4c04-a5d0-97b370408e8d/resourceGroups/rg-desarrollo-seguro/providers/Microsoft.KeyVault/vaults/kv-devsgro-jarch-001"

# Para operaciones administrativas (opcional)
az role assignment create \
    --role "Key Vault Secrets Officer" \
    --assignee azure.nh2025@newhorizons.edu.pe \
    --scope "/subscriptions/43af7d34-ddbe-4c04-a5d0-97b370408e8d/resourceGroups/rg-desarrollo-seguro/providers/Microsoft.KeyVault/vaults/kv-devsgro-jarch-001"
```

### **Paso 4: Verificar Permisos**

```bash
# Probar acceso a secretos
az keyvault secret list --vault-name kv-devsgro-jarch-001 -o table

# Obtener un secreto específico
az keyvault secret show --vault-name kv-devsgro-jarch-001 --name secreto01 --query "value" -o tsv
```

### **Troubleshooting Permisos**

#### **Error: "Caller is not authorized"**

```
(Forbidden) Caller is not authorized to perform action on resource.
Action: 'Microsoft.KeyVault/vaults/secrets/readMetadata/action'
```

**Solución:**
1. Verificar que estés en la suscripción correcta
2. Asignar rol RBAC adecuado
3. Esperar 5-10 minutos para propagación de permisos

#### **Error: "Vault not found"**

**Verificar:**
- Nombre del Key Vault correcto
- Key Vault en la misma suscripción
- Permisos de lectura en el Resource Group

### **Roles RBAC Disponibles**

| Rol | Permisos | Uso Recomendado |
|-----|----------|-----------------|
| **Key Vault Secrets User** | Leer secretos | Aplicaciones, usuarios finales |
| **Key Vault Secrets Officer** | CRUD completo de secretos | Desarrolladores, DevOps |
| **Key Vault Administrator** | Todos los permisos | Administradores del sistema |
| **Key Vault Reader** | Solo metadatos | Auditoría, monitoreo |

### **Verificación Final**

```bash
# Listar secretos disponibles
az keyvault secret list --vault-name kv-devsgro-jarch-001 -o table

# Resultado esperado:
# Name       Id                                                              ContentType    Enabled
# ---------  --------------------------------------------------------------  -------------  ---------
# secreto01  https://kv-devsgro-jarch-001.vault.azure.net/secrets/secreto01                 True
```

## ⚙️ **Configuración**

### 1. **Configuración de Permisos Azure Key Vault** 🔐

#### **Verificar Suscripción y Usuario**

```bash
# Verificar suscripción actual
az account show --query "{name:name, id:id, user:user.name, state:state}"

# Si no es la suscripción correcta, cambiar a NH25
az account set --subscription "Suscripción NH25"
```

#### **Verificar Configuración del Key Vault**

```bash
# Obtener información del Key Vault
az keyvault show --name kv-devsgro-jarch-001 --query "{name:name, resourceGroup:resourceGroup, enableRbacAuthorization:properties.enableRbacAuthorization}"
```

#### **Configurar Permisos RBAC**

El Key Vault usa **RBAC (Role-Based Access Control)**. Necesitas asignar roles específicos:

```bash
# Asignar rol para leer/escribir secretos
az role assignment create \
    --role "Key Vault Secrets User" \
    --assignee tu-email@newhorizons.edu.pe \
    --scope "/subscriptions/43af7d34-ddbe-4c04-a5d0-97b370408e8d/resourceGroups/rg-desarrollo-seguro/providers/Microsoft.KeyVault/vaults/kv-devsgro-jarch-001"

# Para operaciones administrativas (opcional)
az role assignment create \
    --role "Key Vault Secrets Officer" \
    --assignee tu-email@newhorizons.edu.pe \
    --scope "/subscriptions/43af7d34-ddbe-4c04-a5d0-97b370408e8d/resourceGroups/rg-desarrollo-seguro/providers/Microsoft.KeyVault/vaults/kv-devsgro-jarch-001"
```

#### **Verificar Permisos**

```bash
# Probar acceso a secretos
az keyvault secret list --vault-name kv-devsgro-jarch-001 -o table

# Si aparece "Forbidden", revisar que:
# 1. Estés en la suscripción correcta (NH25)
# 2. Tengas los roles RBAC asignados
# 3. El Key Vault tenga enableRbacAuthorization: true
```

#### **Roles RBAC Disponibles**

| Rol | Permisos | Uso |
|-----|----------|-----|
| **Key Vault Secrets User** | Leer secretos | Aplicaciones y usuarios |
| **Key Vault Secrets Officer** | Leer, crear, actualizar, eliminar secretos | Administradores |
| **Key Vault Administrator** | Todos los permisos | Administradores completos |

### 2. **Actualizar appsettings.json**

```json
{
  "KeyVault": {
    "VaultUri": "https://kv-devsgro-jarch-001.vault.azure.net/"
  },
  "DataProtection": {
    "ApplicationName": "DevSeguroApp-KeyVault",
    "StorageConnectionString": "[TU-STORAGE-CONNECTION-STRING]",
    "KeyLifetime": "90.00:00:00"
  }
}
```

### 3. **Crear Secrets en Key Vault**

```bash
# Una vez configurados los permisos, crear secrets de prueba
az keyvault secret set --vault-name "kv-devsgro-jarch-001" --name "DatabaseConnectionString" --value "Server=localhost;Database=DevSeguroApp;Integrated Security=true;"
az keyvault secret set --vault-name "kv-devsgro-jarch-001" --name "ExternalApiKey" --value "sk-test-123456789abcdef-external-api-key"
az keyvault secret set --vault-name "kv-devsgro-jarch-001" --name "EncryptionKey" --value "MyVerySecretEncryptionKey2024!"
az keyvault secret set --vault-name "kv-devsgro-jarch-001" --name "SmtpPassword" --value "smtp-password-super-secret-123"
```

## 🏃‍♂️ **Ejecución**

```bash
cd Laboratorio2-KeyVault
dotnet restore
dotnet run
```

La aplicación estará disponible en: `https://localhost:7001`

## 🧪 **Testing**

### **1. Verificar Estado del Sistema**

- Navegar a `https://localhost:7001`
- Click en "KeyVaultTest"
- Verificar que muestre:
  - ✅ Data Protection: Configurado
  - ✅ Key Vault: Configurado
  - ✅ Azure Storage: Conectado

### **2. Testing Data Protection**

```
Datos de prueba: "Información personal: Juan Pérez, CC: 1234567890"
Propósito: "Información Personal"
```

1. Pegar datos en "Datos a Proteger"
2. Click "Probar Protección"
3. Verificar que aparecen datos encriptados
4. Verificar que se recupera el texto original

### **3. Testing Key Vault**

1. Click "Actualizar" en sección Key Vault
2. Verificar que carga los 4 secrets creados
3. Crear nuevo secret:
   - Nombre: "TestSecret"
   - Valor: "Este es un valor de prueba"
4. Verificar que aparece en la lista

### **4. Testing Storage Switch**

1. Cambiar toggle de Azure Storage a Local
2. Verificar mensaje de reinicio
3. Reiniciar aplicación con `Ctrl+C` y `dotnet run`
4. Verificar que ahora usa almacenamiento local

## 📊 **Arquitectura**

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Frontend      │    │   ASP.NET Core  │    │   Azure         │
│   (Razor/JS)    │◄──►│   Controllers   │◄──►│   Key Vault     │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                │                     
                                ▼                     
                       ┌─────────────────┐            
                       │ Data Protection │            
                       │    Services     │            
                       └─────────────────┘            
                                │                     
                                ▼                     
                       ┌─────────────────┐            
                       │ Azure Storage   │            
                       │ (Blob Storage)  │            
                       └─────────────────┘            
```

## 🔐 **Servicios Implementados**

### **ISecureDataService**

- `ProtectSensitiveData()` - Protección con propósito personalizado
- `UnprotectSensitiveData<T>()` - Desprotección tipada
- `ProtectPersonalInfo()` - Protección de datos personales
- `ProtectFinancialData()` - Protección de datos financieros

### **IKeyVaultService**

- `GetSecretAsync()` - Obtener secret por nombre
- `SetSecretAsync()` - Crear/actualizar secret
- `GetAllSecretsAsync()` - Listar todos los secrets
- `IsConnectedAsync()` - Verificar conectividad
- `EncryptDataAsync()` - Cifrado con claves de Key Vault
- `DecryptDataAsync()` - Descifrado con claves de Key Vault

## 🛠️ **Características Técnicas**

### **Data Protection**

- Integración con Azure Blob Storage
- Protección adicional con Key Vault
- Múltiples protectores para diferentes tipos de datos
- Rotación automática de claves
- Fallback a almacenamiento local

### **Key Vault**

- Configuration Provider integrado
- Autenticación con DefaultAzureCredential
- Manejo de secretos con enmascaramiento
- Operaciones asíncronas
- Manejo robusto de errores

### **UI/UX**

- Interface responsive con Bootstrap
- Toggle switch para cambio de storage
- Actualizaciones en tiempo real
- Logging detallado de operaciones
- Manejo visual de errores y éxitos

## 🔧 **Troubleshooting**

### **Error: "Access denied to Key Vault"**

**Síntomas:**
```
(Forbidden) Caller is not authorized to perform action on resource.
Action: 'Microsoft.KeyVault/vaults/secrets/readMetadata/action'
```

**Causa:** Permisos RBAC insuficientes en Key Vault

**Solución paso a paso:**

1. **Verificar suscripción correcta:**
   ```bash
   az account show --query "{name:name, user:user.name}"
   # Debe mostrar "Suscripción NH25" y tu email correcto
   ```

2. **Verificar que el Key Vault usa RBAC:**
   ```bash
   az keyvault show --name kv-devsgro-jarch-001 --query "properties.enableRbacAuthorization"
   # Debe mostrar: true
   ```

3. **Asignar permisos RBAC:**
   ```bash
   az role assignment create \
       --role "Key Vault Secrets User" \
       --assignee azure.nh2025@newhorizons.edu.pe \
       --scope "/subscriptions/43af7d34-ddbe-4c04-a5d0-97b370408e8d/resourceGroups/rg-desarrollo-seguro/providers/Microsoft.KeyVault/vaults/kv-devsgro-jarch-001"
   ```

4. **Verificar asignación:**
   ```bash
   az keyvault secret list --vault-name kv-devsgro-jarch-001 -o table
   ```

### **Error: "Could not connect to Azure Storage"**

- Verificar connection string en appsettings.json
- Verificar que el Storage Account existe
- Verificar permisos de acceso

### **Error: "Key Vault URI not found"**

- Verificar URI en appsettings.json: `https://kv-devsgro-jarch-001.vault.azure.net/`
- Verificar que el Key Vault existe en la suscripción correcta
- Verificar permisos RBAC (ver sección anterior)

### **Error: "DefaultAzureCredential failed"**

**En desarrollo local:**
```bash
az login
az account set --subscription "Suscripción NH25"
```

**En Azure App Service:**
- Habilitar System Assigned Managed Identity
- Asignar roles RBAC a la Managed Identity

### **Error: "Secrets not visible in Azure Portal"**

**Causa:** Usuario sin permisos para ver secretos en el portal

**Solución:**
```bash
# Asignar rol para ver secretos en el portal
az role assignment create \
    --role "Key Vault Secrets Officer" \
    --assignee tu-email@newhorizons.edu.pe \
    --scope "/subscriptions/43af7d34-ddbe-4c04-a5d0-97b370408e8d/resourceGroups/rg-desarrollo-seguro/providers/Microsoft.KeyVault/vaults/kv-devsgro-jarch-001"
```

## 📈 **Logs de Ejemplo**

```