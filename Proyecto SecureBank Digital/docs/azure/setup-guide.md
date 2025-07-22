# Guía de Configuración Azure Monitor - SecureBank Digital

## 🎯 Objetivo

Esta guía proporciona instrucciones paso a paso para configurar **Azure Monitor**, **Application Insights** y **Log Analytics** para el proyecto SecureBank Digital, incluyendo la configuración para Machine Learning.

## 📋 Prerrequisitos

- **Suscripción de Azure** activa
- **Azure CLI** instalado
- **Terraform** (opcional, para infraestructura como código)
- **Visual Studio 2022** o **VS Code**
- **.NET 9 SDK**
- **Permisos de Contributor** en la suscripción de Azure

## 🏗️ Paso 1: Configuración de Recursos en Azure

### 1.1 Crear Resource Group

```bash
# Login en Azure
az login

# Crear resource group
az group create \
  --name "securebank-rg" \
  --location "East US"
```

### 1.2 Crear Log Analytics Workspace

```bash
# Crear Log Analytics Workspace
az monitor log-analytics workspace create \
  --resource-group "securebank-rg" \
  --workspace-name "securebank-logs" \
  --location "East US" \
  --sku "PerGB2018" \
  --retention-time 730
```

### 1.3 Crear Application Insights

```bash
# Crear Application Insights
az extension add --name application-insights

az monitor app-insights component create \
  --app "securebank-appinsights" \
  --location "East US" \
  --resource-group "securebank-rg" \
  --application-type "web" \
  --retention-time 90
```

### 1.4 Obtener Connection Strings

```bash
# Obtener connection string de Application Insights
az monitor app-insights component show \
  --app "securebank-appinsights" \
  --resource-group "securebank-rg" \
  --query "connectionString" \
  --output tsv

# Obtener workspace ID de Log Analytics
az monitor log-analytics workspace show \
  --resource-group "securebank-rg" \
  --workspace-name "securebank-logs" \
  --query "customerId" \
  --output tsv

# Obtener primary key de Log Analytics
az monitor log-analytics workspace get-shared-keys \
  --resource-group "securebank-rg" \
  --workspace-name "securebank-logs" \
  --query "primarySharedKey" \
  --output tsv
```

## 🔧 Paso 2: Configuración en la Aplicación .NET

### 2.1 Configurar appsettings.json

```json
{
  "ConnectionStrings": {
    "ApplicationInsights": "InstrumentationKey=your-key;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/;LiveEndpoint=https://eastus.livediagnostics.monitor.azure.com/"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    },
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft": "Warning"
      }
    }
  },
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=your-key;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/",
    "EnableAdaptiveSampling": true,
    "EnableQuickPulseMetricStream": true,
    "EnableAuthenticationTrackingJavaScript": true,
    "SamplingRate": 100.0,
    "MaxTelemetryItemsPerSecond": 20,
    "EnableSqlCommandTextInstrumentation": false
  },
  "AzureMonitor": {
    "WorkspaceId": "your-workspace-id",
    "SharedKey": "your-shared-key",
    "LogType": "SecureBankLogs",
    "TimeStampField": "timestamp"
  }
}
```

### 2.2 Configurar Secrets en Development

```bash
# Configurar User Secrets para desarrollo
cd src/Services/SecureBank.AuthAPI
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:ApplicationInsights" "InstrumentationKey=your-dev-key;..."
dotnet user-secrets set "AzureMonitor:WorkspaceId" "your-workspace-id"
dotnet user-secrets set "AzureMonitor:SharedKey" "your-shared-key"
```

### 2.3 Variables de Entorno para Producción

```bash
# Azure App Service - Application Settings
APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=..."
AZURE_CLIENT_ID="your-managed-identity-client-id"
AZURE_CLIENT_SECRET="" # Vacío para Managed Identity
AZURE_TENANT_ID="your-tenant-id"
LOG_ANALYTICS_WORKSPACE_ID="your-workspace-id"
LOG_ANALYTICS_SHARED_KEY="your-shared-key"
```

## 🚀 Paso 3: Configurar Azure Key Vault

### 3.1 Crear Key Vault

```bash
# Crear Key Vault
az keyvault create \
  --name "securebank-kv" \
  --resource-group "securebank-rg" \
  --location "East US" \
  --sku "Premium" \
  --enable-rbac-authorization false
```

### 3.2 Agregar Secrets

```bash
# Agregar connection string a Key Vault
az keyvault secret set \
  --vault-name "securebank-kv" \
  --name "ApplicationInsights-ConnectionString" \
  --value "InstrumentationKey=..."

az keyvault secret set \
  --vault-name "securebank-kv" \
  --name "LogAnalytics-WorkspaceId" \
  --value "your-workspace-id"

az keyvault secret set \
  --vault-name "securebank-kv" \
  --name "LogAnalytics-SharedKey" \
  --value "your-shared-key"
```

### 3.3 Configurar Managed Identity

```bash
# Crear Managed Identity
az identity create \
  --name "securebank-identity" \
  --resource-group "securebank-rg"

# Obtener principal ID
PRINCIPAL_ID=$(az identity show \
  --name "securebank-identity" \
  --resource-group "securebank-rg" \
  --query "principalId" \
  --output tsv)

# Asignar permisos al Key Vault
az keyvault set-policy \
  --name "securebank-kv" \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

## 🔍 Paso 4: Configurar Alertas y Monitoring

### 4.1 Crear Action Group

```bash
# Crear Action Group para notificaciones
az monitor action-group create \
  --name "securebank-alerts" \
  --resource-group "securebank-rg" \
  --short-name "sbalerts" \
  --email-receivers \
    name="SecurityTeam" email="security@securebankdigital.pe" \
  --sms-receivers \
    name="OnCall" country-code="51" phone-number="999999999"
```

### 4.2 Crear Alertas de Seguridad

```bash
# Alerta para intentos de login fallidos
az monitor scheduled-query create \
  --name "FailedLoginAttempts" \
  --resource-group "securebank-rg" \
  --scopes "/subscriptions/{subscription-id}/resourceGroups/securebank-rg/providers/Microsoft.Insights/components/securebank-appinsights" \
  --condition "count > 10" \
  --condition-query "customEvents | where name == 'SecurityEvent' and tostring(customDimensions.EventName) == 'LoginFailed' | summarize count()" \
  --condition-time-aggregation "Total" \
  --condition-threshold 10 \
  --condition-operator "GreaterThan" \
  --evaluation-frequency "PT5M" \
  --window-size "PT15M" \
  --severity 2 \
  --action-groups "/subscriptions/{subscription-id}/resourceGroups/securebank-rg/providers/microsoft.insights/actionGroups/securebank-alerts"
```

### 4.3 Crear Alertas de Performance

```bash
# Alerta para latencia alta
az monitor scheduled-query create \
  --name "HighLatency" \
  --resource-group "securebank-rg" \
  --scopes "/subscriptions/{subscription-id}/resourceGroups/securebank-rg/providers/Microsoft.Insights/components/securebank-appinsights" \
  --condition "average > 5000" \
  --condition-query "requests | summarize avg(duration)" \
  --condition-time-aggregation "Average" \
  --condition-threshold 5000 \
  --condition-operator "GreaterThan" \
  --evaluation-frequency "PT5M" \
  --window-size "PT15M" \
  --severity 3 \
  --action-groups "/subscriptions/{subscription-id}/resourceGroups/securebank-rg/providers/microsoft.insights/actionGroups/securebank-alerts"
```

## 📊 Paso 5: Configurar Dashboards

### 5.1 Crear Dashboard de Seguridad

```json
{
  "lenses": {
    "0": {
      "order": 0,
      "parts": {
        "0": {
          "position": {
            "x": 0,
            "y": 0,
            "colSpan": 6,
            "rowSpan": 4
          },
          "metadata": {
            "inputs": [
              {
                "name": "query",
                "value": "customEvents\n| where name == \"SecurityEvent\"\n| where tostring(customDimensions.EventName) == \"LoginFailed\"\n| summarize count() by bin(timestamp, 1h)\n| render timechart"
              }
            ],
            "type": "Extension/Microsoft_OperationsManagementSuite_Workspace/PartType/LogsDashboardPart"
          }
        }
      }
    }
  },
  "metadata": {
    "model": {
      "timeRange": {
        "value": {
          "relative": {
            "duration": 24,
            "timeUnit": 1
          }
        },
        "type": "MsPortalFx.Composition.Configuration.ValueTypes.TimeRange"
      }
    }
  }
}
```

### 5.2 Guardar Dashboard

```bash
# Crear dashboard desde archivo JSON
az portal dashboard create \
  --resource-group "securebank-rg" \
  --name "securebank-security-dashboard" \
  --input-path "./dashboard-security.json"
```

## 🤖 Paso 6: Configurar Machine Learning Workspace

### 6.1 Crear ML Workspace

```bash
# Crear storage account para ML
az storage account create \
  --name "securebankmlstorage" \
  --resource-group "securebank-rg" \
  --location "East US" \
  --sku "Standard_LRS"

# Crear ML Workspace
az ml workspace create \
  --name "securebank-ml" \
  --resource-group "securebank-rg" \
  --location "East US" \
  --storage-account "securebankmlstorage" \
  --key-vault "securebank-kv" \
  --application-insights "securebank-appinsights"
```

### 6.2 Configurar Data Pipeline

```python
# requirements.txt para ML pipeline
azure-ai-ml==1.11.1
azure-kusto-data==4.3.1
azure-kusto-ingest==4.3.1
pandas==2.1.4
scikit-learn==1.3.2
numpy==1.24.3
```

```python
# ml_pipeline.py
from azure.ai.ml import MLClient
from azure.identity import DefaultAzureCredential
from azure.kusto.data import KustoClient, KustoConnectionStringBuilder

# Configurar cliente ML
credential = DefaultAzureCredential()
ml_client = MLClient(
    credential=credential,
    subscription_id="your-subscription-id",
    resource_group_name="securebank-rg",
    workspace_name="securebank-ml"
)

# Configurar Kusto client para Log Analytics
kcsb = KustoConnectionStringBuilder.with_aad_managed_service_identity_authentication(
    f"https://ade.loganalytics.io/subscriptions/your-subscription-id/resourcegroups/securebank-rg/providers/microsoft.operationalinsights/workspaces/securebank-logs"
)
kusto_client = KustoClient(kcsb)

# Query para obtener datos de fraude
fraud_query = """
customEvents
| where name in ("SecurityEvent", "TransactionEvent", "FraudDetection")
| where timestamp > ago(30d)
| project timestamp,
          userId = tostring(customDimensions.UserId),
          eventName = tostring(customDimensions.EventName),
          amount = todouble(customDimensions.ML_TransactionAmount),
          riskScore = toint(customDimensions.ML_RiskScore),
          deviceFingerprint = tostring(customDimensions.ML_DeviceFingerprint),
          ipAddress = tostring(customDimensions.ML_IpAddress),
          timeOfDay = toint(customDimensions.ML_TimeOfDay),
          dayOfWeek = tostring(customDimensions.ML_DayOfWeek)
"""

# Ejecutar query y procesar datos
response = kusto_client.execute_query("securebank-logs", fraud_query)
```

## 🔒 Paso 7: Configurar Seguridad y Compliance

### 7.1 Configurar RBAC

```bash
# Crear rol personalizado para lectura de logs
az role definition create --role-definition '{
  "Name": "SecureBank Log Reader",
  "Description": "Can read SecureBank logs and metrics",
  "Actions": [
    "Microsoft.Insights/components/api/read",
    "Microsoft.Insights/components/query/read",
    "Microsoft.OperationalInsights/workspaces/query/read"
  ],
  "NotActions": [],
  "AssignableScopes": ["/subscriptions/your-subscription-id/resourceGroups/securebank-rg"]
}'

# Asignar rol a grupo de desarrolladores
az role assignment create \
  --role "SecureBank Log Reader" \
  --assignee-object-id "developer-group-object-id" \
  --scope "/subscriptions/your-subscription-id/resourceGroups/securebank-rg"
```

### 7.2 Configurar Data Retention

```bash
# Configurar retención de Application Insights
az monitor app-insights component update \
  --app "securebank-appinsights" \
  --resource-group "securebank-rg" \
  --retention-time 90

# Configurar retención de Log Analytics
az monitor log-analytics workspace update \
  --resource-group "securebank-rg" \
  --workspace-name "securebank-logs" \
  --retention-time 730
```

### 7.3 Configurar Data Export

```bash
# Crear storage account para export
az storage account create \
  --name "securebanklogexport" \
  --resource-group "securebank-rg" \
  --location "East US" \
  --sku "Standard_LRS"

# Crear continuous export
az monitor app-insights component continues-export create \
  --app "securebank-appinsights" \
  --resource-group "securebank-rg" \
  --record-types "Event,Metric,Exception,PageView,Request" \
  --dest-account "securebanklogexport" \
  --dest-container "appinsights-export" \
  --dest-sub-id "your-subscription-id" \
  --dest-sas "your-sas-token"
```

## 📝 Paso 8: Testing y Validación

### 8.1 Test de Conectividad

```csharp
// TestController.cs para validar configuración
[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly TelemetryClient _telemetryClient;
    private readonly IAzureMonitorService _azureMonitor;

    public TestController(TelemetryClient telemetryClient, IAzureMonitorService azureMonitor)
    {
        _telemetryClient = telemetryClient;
        _azureMonitor = azureMonitor;
    }

    [HttpGet("appinsights")]
    public IActionResult TestApplicationInsights()
    {
        _telemetryClient.TrackEvent("TestEvent", new Dictionary<string, string>
        {
            ["TestProperty"] = "TestValue",
            ["Timestamp"] = DateTime.UtcNow.ToString()
        });

        return Ok("Event sent to Application Insights");
    }

    [HttpGet("azuremonitor")]
    public async Task<IActionResult> TestAzureMonitor()
    {
        await _azureMonitor.LogSecurityEventAsync("TestSecurityEvent", new
        {
            TestProperty = "TestValue",
            Timestamp = DateTime.UtcNow
        });

        return Ok("Event sent to Azure Monitor");
    }
}
```

### 8.2 Validar Logs en Azure

```kusto
// Query para validar datos en Log Analytics
customEvents
| where name == "TestEvent" or name == "SecurityEvent"
| where timestamp > ago(1h)
| project timestamp, name, customDimensions
| order by timestamp desc
| limit 10
```

### 8.3 Test de Performance

```bash
# Usar Azure CLI para enviar métrica de test
az monitor metrics alert create \
  --name "TestMetric" \
  --resource-group "securebank-rg" \
  --condition "avg customMetrics/TestMetric > 100" \
  --description "Test metric alert" \
  --evaluation-frequency "PT1M" \
  --window-size "PT5M" \
  --severity 4
```

## 🚀 Paso 9: Despliegue en Producción

### 9.1 Azure App Service Configuration

```bash
# Crear App Service Plan
az appservice plan create \
  --name "securebank-plan" \
  --resource-group "securebank-rg" \
  --location "East US" \
  --sku "P1V3" \
  --is-linux

# Crear Web App
az webapp create \
  --name "securebank-authapi" \
  --resource-group "securebank-rg" \
  --plan "securebank-plan" \
  --runtime "DOTNETCORE:9.0"

# Configurar Application Settings
az webapp config appsettings set \
  --name "securebank-authapi" \
  --resource-group "securebank-rg" \
  --settings \
    APPLICATIONINSIGHTS_CONNECTION_STRING="your-connection-string" \
    AZURE_CLIENT_ID="your-managed-identity-id" \
    LOG_ANALYTICS_WORKSPACE_ID="your-workspace-id"
```

### 9.2 Configurar Managed Identity

```bash
# Habilitar system-assigned managed identity
az webapp identity assign \
  --name "securebank-authapi" \
  --resource-group "securebank-rg"

# Obtener principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --name "securebank-authapi" \
  --resource-group "securebank-rg" \
  --query "principalId" \
  --output tsv)

# Asignar permisos a Key Vault
az keyvault set-policy \
  --name "securebank-kv" \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list

# Asignar rol de Monitoring Contributor
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Monitoring Contributor" \
  --scope "/subscriptions/your-subscription-id/resourceGroups/securebank-rg"
```

## 📋 Checklist de Configuración

### ✅ Infraestructura
- [ ] Resource Group creado
- [ ] Application Insights configurado
- [ ] Log Analytics Workspace creado
- [ ] Key Vault configurado
- [ ] Managed Identity asignada
- [ ] ML Workspace creado

### ✅ Aplicación
- [ ] NuGet packages instalados
- [ ] Connection strings configurados
- [ ] Secrets almacenados en Key Vault
- [ ] Logging configurado correctamente
- [ ] Azure Monitor Service implementado

### ✅ Seguridad
- [ ] RBAC configurado
- [ ] Data retention policies aplicadas
- [ ] Sensitive data masking implementado
- [ ] Compliance policies habilitadas

### ✅ Monitoring
- [ ] Alertas de seguridad configuradas
- [ ] Alertas de performance configuradas
- [ ] Dashboards creados
- [ ] Action groups configurados

### ✅ Testing
- [ ] Conectividad validada
- [ ] Logs aparecen en Log Analytics
- [ ] Métricas funcionando
- [ ] Alertas probadas

---

## 🆘 Troubleshooting

### Problema: No aparecen logs en Application Insights

**Solución**:
```bash
# Verificar connection string
az monitor app-insights component show \
  --app "securebank-appinsights" \
  --resource-group "securebank-rg" \
  --query "connectionString"

# Verificar sampling configuration
# En appsettings.json, asegurar que SamplingRate sea 100.0 para testing
```

### Problema: Errores de autenticación con Key Vault

**Solución**:
```bash
# Verificar managed identity
az webapp identity show \
  --name "securebank-authapi" \
  --resource-group "securebank-rg"

# Verificar permisos en Key Vault
az keyvault show \
  --name "securebank-kv" \
  --query "properties.accessPolicies"
```

### Problema: Alta latencia en logs

**Solución**:
- Verificar sampling rate
- Implementar batching en AzureMonitorService
- Usar async/await en todos los métodos de logging

---

Esta guía asegura una configuración completa y robusta de Azure Monitor para SecureBank Digital, manteniendo la filosofía de **"Security First, Innovation Always"**. 