# 🧪 Laboratorio 4: Testing Completo y Verificación

## 📋 Información del Laboratorio
- **Duración**: 10 minutos
- **Objetivo**: Realizar testing end-to-end de todas las funcionalidades implementadas
- **Prerrequisitos**: Laboratorios 1, 2 y 3 completados

## 🎯 Objetivos Específicos
- Configurar autenticación con Azure CLI para Key Vault
- Realizar testing completo de la aplicación
- Verificar diferentes propósitos de protección
- Validar logs y resultados de operaciones

## 🔑 Paso 1: Autenticación con Azure CLI para Key Vault (3 minutos)

### Instalar Azure CLI (si no está instalado)
```bash
# Con chocolatey (Windows)
choco install azure-cli -y

# Con winget (Windows)
winget install Microsoft.AzureCLI

# Con apt (Ubuntu/Debian)
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
```

### Login con Azure CLI
```bash
# Login interactivo
az login

# Seleccionar el tenant correcto si tiene múltiples
az account set --subscription [SUBSCRIPTION-ID]

# Verificar login
az account show
```

### Verificar acceso a Key Vault
```bash
# Listar secrets en Key Vault
az keyvault secret list --vault-name kv-devsgro-[sunombre]-[numero]

# Verificar permisos
az keyvault show --name kv-devsgro-[sunombre]-[numero] --query "properties.accessPolicies"
```

## 🧪 Paso 2: Testing Completo de la Aplicación (5 minutos)

### Ejecutar la aplicación
```bash
cd DevSeguroApp/DevSeguroWebApp
dotnet run
```

### Secuencia de Testing Completa

#### 1. 🏠 Verificar Página de Inicio
- **Navegar a**: https://localhost:7001
- **Verificar que carga correctamente**
- **Confirmar estado de autenticación**

#### 2. 🔑 Autenticación
- **Si no está autenticado**: Click "Iniciar Sesión"
- **Completar flujo OAuth** con Azure AD
- **Verificar redirección exitosa**

#### 3. 🔐 Testing de Datos Seguros
- **Click en "Datos Seguros"** en el menú
- **Verificar que la página carga**
- **Confirmar que "Estado del Sistema"** muestra configuraciones correctas

#### 4. 🛡️ Testing Data Protection API

**Datos de Prueba:**
```
Información personal: Juan Pérez, CC: 1234567890, Tarjeta: 4111-1111-1111-1111
```

**Pasos:**
1. **Pegar texto** en "Datos a Proteger"
2. **Click "Proteger Datos"**
3. **Verificar que aparecen datos encriptados**
4. **Click "Desproteger Datos"**
5. **Verificar que recupera texto original**

#### 5. 🔑 Testing Key Vault Integration

**Pasos:**
1. **Click "Actualizar"** en sección Key Vault
2. **Verificar que carga los 4 secrets** creados
3. **Crear nuevo secret**:
   - Nombre: "TestSecret"
   - Valor: "Este es un valor de prueba"
4. **Click "Crear Secret"**
5. **Verificar mensaje de éxito**
6. **Click "Actualizar"** nuevamente
7. **Confirmar que aparece el nuevo secret**

#### 6. 📊 Verificar Logs y Resultados
- **Observar la sección "Resultados de Testing y Logs"**
- **Confirmar que aparecen mensajes** con timestamps
- **Verificar diferentes tipos de alertas** (éxito, error, info)

## 🧪 Paso 3: Testing de Diferentes Propósitos de Protección (2 minutos)

### Probar diferentes protectores:

#### 1. Datos Financieros
- **Datos**: `Cuenta bancaria: 123-456-789, Saldo: $50,000`
- **Propósito**: "Datos Financieros"

#### 2. Información Médica
- **Datos**: `Paciente: María López, Diagnóstico: Diabetes Tipo 2`
- **Propósito**: "Información Médica"

#### 3. Propósito Personalizado
- **Datos**: `API Key: sk-live-123456789abcdef`
- **Propósito**: "Propósito Personalizado"

### Verificar que:
- ✅ **Cada propósito genera datos protegidos diferentes**
- ✅ **No se puede desproteger datos de un propósito con otro propósito**
- ✅ **Los mensajes de error son apropiados**

## 📊 Verificación Final y Resumen

### ✅ Checklist de Completación

#### Laboratorio 1 - Data Protection API:
- [ ] Azure Storage Account creado y configurado
- [ ] Data Protection API integrada con Blob Storage
- [ ] Servicio SecureDataService implementado
- [ ] Múltiples protectores funcionando (Personal, Financial, Medical)

#### Laboratorio 2 - Azure Key Vault:
- [ ] Key Vault creado con RBAC habilitado
- [ ] 4 secrets almacenados en Key Vault
- [ ] Configuration Provider integrado
- [ ] Servicio KeyVaultService implementado
- [ ] Azure CLI autenticado para desarrollo local

#### Laboratorio 3 - Interface y Testing:
- [ ] Controller SecureDataController creado
- [ ] Vista avanzada con JavaScript interactivo
- [ ] Menú actualizado con enlace "Datos Seguros"
- [ ] Testing end-to-end completado exitosamente

### Funcionalidades Verificadas:
- [ ] Protección/desprotección de datos funciona
- [ ] Diferentes propósitos generan diferentes encriptaciones
- [ ] Key Vault secrets se cargan correctamente
- [ ] Creación de nuevos secrets funciona
- [ ] Logs y mensajes de estado aparecen
- [ ] Manejo de errores apropiado

## 🎯 Resultados de Aprendizaje Alcanzados

Al completar este laboratorio, los estudiantes han logrado:

### 1. 🔐 Implementación Enterprise de Data Protection:
- Configuración avanzada con Azure Storage
- Múltiples protectores para diferentes tipos de datos
- Rotación automática de claves
- Logging y auditoría integrados

### 2. 🔑 Gestión Completa de Secretos:
- Azure Key Vault con RBAC
- Configuration Provider seamless
- Managed Identity para autenticación
- Operaciones CRUD de secrets

### 3. 🛡️ Seguridad End-to-End:
- Separación de responsabilidades entre tipos de datos
- Protección en reposo y en tránsito
- Auditoría completa de operaciones
- Manejo seguro de errores

### 4. ⚙️ Patterns de Desarrollo Seguro:
- Dependency injection apropiada
- Configuración centralizada
- Logging estructurado
- Testing automatizado

## 🚨 Troubleshooting Común

### Error: "Could not load file or assembly 'Azure.Identity'"
**Solución:**
```bash
dotnet clean
dotnet restore
dotnet build
```

### Error: "Access denied to Key Vault"
**Solución:**
```bash
az login
az account set --subscription [subscription-id]
# Verificar permisos RBAC en Azure Portal
```

### Error: "DataProtection keys not found"
**Solución:**
- Verificar connection string de Storage Account
- Confirmar que container "dataprotection-keys" existe
- Revisar logs en consola para detalles específicos

### Error: "Cannot connect to Key Vault"
**Solución:**
- Verificar URL de Key Vault en appsettings.json
- Confirmar que Azure CLI está autenticado
- Revisar permisos de red en Key Vault

## 📈 Métricas de Éxito

### Indicadores de Implementación Exitosa:
- ✅ **Tiempo de respuesta**: Operaciones de protección/desprotección < 100ms
- ✅ **Disponibilidad**: Key Vault accesible 100% del tiempo durante testing
- ✅ **Seguridad**: Datos nunca aparecen en logs sin protección
- ✅ **Usabilidad**: Interface responsive y intuitiva
- ✅ **Escalabilidad**: Configuración lista para múltiples entornos

### Criterios de Calidad:
- Código limpio y bien estructurado
- Manejo apropiado de excepciones
- Logging comprensivo pero no verbose
- Configuración centralizada y flexible
- Documentación inline clara

## 🎓 Preparación para Sesión Final

Para la próxima sesión (Proyecto Integrador), asegúrense de tener:

1. **Proyecto completamente funcional** con todas las características implementadas
2. **Azure resources creados y funcionando** (Key Vault, Storage Account)
3. **Credenciales Azure configuradas** para desarrollo local
4. **Entendimiento claro de los patterns** implementados

### La Sesión Final integrará:
- Autenticación OAuth 2.0/OpenID Connect (Sesión 4)
- Protección de datos y Key Vault (Sesión 5)
- Deployment a Azure App Service
- Monitoring y alertas
- Performance testing
- Security scanning

¡Excelente trabajo completando estos laboratorios avanzados! 🚀 