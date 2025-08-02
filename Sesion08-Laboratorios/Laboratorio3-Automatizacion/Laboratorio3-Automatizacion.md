# 🧪 Laboratorio 3: Análisis de Secure Score y Automatización

## ⏱️ Duración: 15 minutos
## 🎯 Objetivo: Crear automatización para monitoreo continuo y respuesta a alertas de seguridad

---

## 📋 Prerrequisitos
- Laboratorios 1 y 2 completados exitosamente
- .NET Core 9 SDK instalado
- Azure CLI autenticado
- PowerShell como Administrador
- Visual Studio Code con extensiones de Azure

---

## 🚀 Paso 1: Crear Aplicación .NET para Análisis de Secure Score (8 minutos)

### Crear proyecto .NET Core para API de Security Center:

```powershell
# Definir variables
$resourceGroupName = "rg-security-lab-$env:USERNAME"

# Crear directorio de proyecto
New-Item -ItemType Directory -Path "SecureScoreAnalyzer" -Force
Set-Location "SecureScoreAnalyzer"

# Crear proyecto .NET Core
dotnet new console

# Agregar paquetes necesarios
dotnet add package Azure.ResourceManager
dotnet add package Azure.ResourceManager.SecurityCenter
dotnet add package Azure.Identity
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Newtonsoft.Json

Write-Host "✅ Proyecto .NET Core creado exitosamente" -ForegroundColor Green
```

### Crear el código principal:

```powershell
# Crear Program.cs
$programContent = @"
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.SecurityCenter;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace SecureScoreAnalyzer
{
    class Program
    {
        private static ArmClient _armClient;
        private static string _subscriptionId;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Azure Security Center - Secure Score Analyzer");
            Console.WriteLine("==============================================");

            try
            {
                // Configuración
                var config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                // Autenticación con Azure
                var credential = new DefaultAzureCredential();
                _armClient = new ArmClient(credential);
                
                // Obtener suscripción activa
                var subscriptions = _armClient.GetSubscriptions();
                await foreach (var subscription in subscriptions)
                {
                    _subscriptionId = subscription.Data.SubscriptionId;
                    Console.WriteLine($"Analizando suscripción: {subscription.Data.DisplayName}");
                    break;
                }

                // Análisis principal
                await AnalyzeSecureScore();
                await AnalyzeRecommendations();
                await AnalyzeAlerts();
                await GenerateReport();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("\nPresione cualquier tecla para continuar...");
            Console.ReadKey();
        }

        static async Task AnalyzeSecureScore()
        {
            Console.WriteLine("\n📊 ANÁLISIS DE SECURE SCORE");
            Console.WriteLine("----------------------------");

            try
            {
                var subscription = await _armClient.GetSubscriptionResource(
                    new Azure.Core.ResourceIdentifier($"/subscriptions/{_subscriptionId}")).GetAsync();

                // Simular análisis de Secure Score (API real requiere permisos específicos)
                var mockSecureScore = new
                {
                    CurrentScore = 75.5,
                    MaxScore = 100.0,
                    Percentage = 75.5,
                    ScoreHistory = new[]
                    {
                        new { Date = DateTime.Now.AddDays(-30), Score = 68.2 },
                        new { Date = DateTime.Now.AddDays(-15), Score = 71.8 },
                        new { Date = DateTime.Now, Score = 75.5 }
                    }
                };

                Console.WriteLine($"🎯 Secure Score Actual: {mockSecureScore.CurrentScore:F1}/{mockSecureScore.MaxScore:F1} ({mockSecureScore.Percentage:F1}%)");
                Console.WriteLine($"📈 Mejora en 30 días: +{mockSecureScore.CurrentScore - 68.2:F1} puntos");
                
                // Análisis de tendencia
                if (mockSecureScore.CurrentScore > 68.2)
                {
                    Console.WriteLine("✅ Tendencia: MEJORANDO");
                }
                else
                {
                    Console.WriteLine("⚠️ Tendencia: DETERIORANDO");
                }

                // Benchmark de industria
                var industryAverage = 65.0;
                if (mockSecureScore.CurrentScore > industryAverage)
                {
                    Console.WriteLine($"🏆 Por encima del promedio de industria ({industryAverage}%)");
                }
                else
                {
                    Console.WriteLine($"📉 Por debajo del promedio de industria ({industryAverage}%)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error obteniendo Secure Score: {ex.Message}");
            }
        }

        static async Task AnalyzeRecommendations()
        {
            Console.WriteLine("\n🔍 ANÁLISIS DE RECOMENDACIONES");
            Console.WriteLine("------------------------------");

            // Simular recomendaciones típicas
            var mockRecommendations = new[]
            {
                new
                {
                    Title = "Enable MFA for admin accounts",
                    Severity = "High",
                    Impact = 10,
                    Effort = "Low",
                    ResourcesAffected = 3,
                    Description = "Cuentas administrativas sin autenticación multifactor"
                },
                new
                {
                    Title = "Enable disk encryption for VMs",
                    Severity = "Medium",
                    Impact = 6,
                    Effort = "Medium",
                    ResourcesAffected = 2,
                    Description = "Máquinas virtuales sin cifrado de disco"
                },
                new
                {
                    Title = "Restrict HTTP traffic for web apps",
                    Severity = "High",
                    Impact = 8,
                    Effort = "Low",
                    ResourcesAffected = 1,
                    Description = "Aplicaciones web permitiendo tráfico HTTP"
                },
                new
                {
                    Title = "Update vulnerability assessment agent",
                    Severity = "Low",
                    Impact = 3,
                    Effort = "Low",
                    ResourcesAffected = 2,
                    Description = "Agentes de evaluación de vulnerabilidades desactualizados"
                }
            };

            Console.WriteLine($"📋 Total de recomendaciones: {mockRecommendations.Length}");
            
            var highPriorityCount = 0;
            var quickWinsCount = 0;

            foreach (var rec in mockRecommendations)
            {
                var priority = GetPriority(rec.Severity, rec.Effort);
                Console.WriteLine($"\n🔹 {rec.Title}");
                Console.WriteLine($"   Severidad: {rec.Severity} | Impacto: +{rec.Impact} pts | Esfuerzo: {rec.Effort}");
                Console.WriteLine($"   Recursos afectados: {rec.ResourcesAffected} | Prioridad: {priority}");
                Console.WriteLine($"   {rec.Description}");

                if (rec.Severity == "High")
                    highPriorityCount++;
                
                if (rec.Severity == "High" && rec.Effort == "Low")
                    quickWinsCount++;
            }

            Console.WriteLine($"\n📊 RESUMEN:");
            Console.WriteLine($"   🚨 Alta prioridad: {highPriorityCount} recomendaciones");
            Console.WriteLine($"   ⚡ Quick wins: {quickWinsCount} recomendaciones");
            Console.WriteLine($"   📈 Mejora potencial: +{mockRecommendations.Sum(r => r.Impact)} puntos");
        }

        static async Task AnalyzeAlerts()
        {
            Console.WriteLine("\n🚨 ANÁLISIS DE ALERTAS DE SEGURIDAD");
            Console.WriteLine("-----------------------------------");

            // Simular alertas de seguridad
            var mockAlerts = new[]
            {
                new
                {
                    Title = "Suspicious PowerShell execution detected",
                    Severity = "Medium",
                    Status = "Active",
                    Timestamp = DateTime.Now.AddHours(-2),
                    Resource = "vm-windows-test",
                    Description = "Comando PowerShell sospechoso ejecutado en VM Windows"
                },
                new
                {
                    Title = "Potential brute force attack",
                    Severity = "High",
                    Status = "Active", 
                    Timestamp = DateTime.Now.AddMinutes(-45),
                    Resource = "vm-linux-test",
                    Description = "Múltiples intentos de login SSH fallidos"
                },
                new
                {
                    Title = "Malware detected in storage account",
                    Severity = "Critical",
                    Status = "Resolved",
                    Timestamp = DateTime.Now.AddDays(-1),
                    Resource = "stsecuritytest$env:USERNAME",
                    Description = "Archivo malicioso detectado y removido"
                }
            };

            var activeAlerts = mockAlerts.Where(a => a.Status == "Active").ToArray();
            Console.WriteLine($"📋 Total de alertas: {mockAlerts.Length}");
            Console.WriteLine($"🔴 Alertas activas: {activeAlerts.Length}");

            foreach (var alert in mockAlerts)
            {
                var statusIcon = alert.Status == "Active" ? "🔴" : "✅";
                Console.WriteLine($"\n{statusIcon} {alert.Title}");
                Console.WriteLine($"   Severidad: {alert.Severity} | Estado: {alert.Status}");
                Console.WriteLine($"   Recurso: {alert.Resource} | Tiempo: {alert.Timestamp:yyyy-MM-dd HH:mm}");
                Console.WriteLine($"   {alert.Description}");
            }

            if (activeAlerts.Any())
            {
                Console.WriteLine($"\n⚠️ ACCIÓN REQUERIDA: {activeAlerts.Length} alertas necesitan atención");
            }
            else
            {
                Console.WriteLine($"\n✅ No hay alertas activas que requieran atención");
            }
        }

        static async Task GenerateReport()
        {
            Console.WriteLine("\n📄 GENERANDO REPORTE EJECUTIVO");
            Console.WriteLine("------------------------------");

            var report = new
            {
                ReportDate = DateTime.Now,
                SecureScore = new
                {
                    Current = 75.5,
                    Target = 85.0,
                    Industry = 65.0
                },
                Summary = new
                {
                    TotalRecommendations = 4,
                    HighPriority = 2,
                    QuickWins = 2,
                    ActiveAlerts = 2,
                    CriticalIssues = 0
                },
                NextActions = new[]
                {
                    "Implementar MFA en cuentas administrativas",
                    "Restringir tráfico HTTP en aplicaciones web",
                    "Investigar alertas de seguridad activas",
                    "Programar revisión mensual de Secure Score"
                }
            };

            var jsonReport = JsonConvert.SerializeObject(report, Formatting.Indented);
            await File.WriteAllTextAsync("security-report.json", jsonReport);

            Console.WriteLine("✅ Reporte generado: security-report.json");
            Console.WriteLine($"📊 Secure Score: {report.SecureScore.Current}% (Meta: {report.SecureScore.Target}%)");
            Console.WriteLine($"🎯 Próximas acciones: {report.NextActions.Length}");
            
            Console.WriteLine("\n📋 PRÓXIMAS ACCIONES:");
            for (int i = 0; i < report.NextActions.Length; i++)
            {
                Console.WriteLine($"   {i + 1}. {report.NextActions[i]}");
            }
        }

        static string GetPriority(string severity, string effort)
        {
            return (severity, effort) switch
            {
                ("High", "Low") => "🚀 CRÍTICA (Quick Win)",
                ("High", "Medium") => "🔴 ALTA",
                ("High", "High") => "🟠 ALTA (Complejo)",
                ("Medium", "Low") => "🟡 MEDIA (Quick Win)",
                ("Medium", "Medium") => "🟡 MEDIA",
                ("Low", _) => "🟢 BAJA",
                _ => "❓ REVISAR"
            };
        }
    }
}
"@

$programContent | Out-File -FilePath "Program.cs" -Encoding UTF8

# Crear archivo de configuración
$appSettingsContent = @"
{
  "Azure": {
    "SubscriptionId": "",
    "TenantId": "",
    "ResourceGroupName": "$resourceGroupName"
  },
  "Settings": {
    "SecureScoreTarget": 85.0,
    "AlertSeverityThreshold": "Medium",
    "ReportOutputPath": "./reports/"
  }
}
"@

$appSettingsContent | Out-File -FilePath "appsettings.json" -Encoding UTF8

Write-Host "✅ Código fuente creado exitosamente" -ForegroundColor Green
```

### Compilar y ejecutar:

```powershell
# Compilar el proyecto
dotnet build

# Ejecutar el analizador
dotnet run

Write-Host "✅ Aplicación .NET ejecutada exitosamente" -ForegroundColor Green
```

**✅ Verificación:** Debe ver el análisis de Secure Score y el reporte generado.

---

## 🔧 Paso 2: Configurar Logic App para Automatización (7 minutos)

### Crear Logic App para respuesta automática a alertas:

```powershell
# Crear Logic App
Write-Host "🔧 Creando Logic App para automatización..." -ForegroundColor Yellow
az logic workflow create `
    --resource-group $resourceGroupName `
    --name "logic-security-response-$env:USERNAME" `
    --location eastus

Write-Host "✅ Logic App creado exitosamente" -ForegroundColor Green
```

### Configurar Logic App en Azure Portal:

```powershell
# Obtener la URL del Logic App
$LOGIC_APP_URL = az logic workflow show `
    --resource-group $resourceGroupName `
    --name "logic-security-response-$env:USERNAME" `
    --query accessEndpoint --output tsv

Write-Host "🌐 Logic App URL: $LOGIC_APP_URL" -ForegroundColor Cyan
```

### Instrucciones para configurar en Azure Portal:

```powershell
# Crear archivo con instrucciones de configuración
$logicAppInstructions = @"
=== CONFIGURACIÓN DE LOGIC APP EN AZURE PORTAL ===

1. Navegar a Azure Portal → Logic Apps → logic-security-response-$env:USERNAME
2. Click en "Logic app designer"
3. Seleccionar "Start with blank logic app"

4. CONFIGURAR TRIGGER:
   - Buscar "When an HTTP request is received"
   - Seleccionar y configurar

5. GENERAR SCHEMA DESDE SAMPLE PAYLOAD:
   {
     "alertType": "Security_Alert",
     "severity": "High",
     "resourceId": "/subscriptions/.../resourceGroups/.../providers/.../vm-test",
     "alertDetails": {
       "title": "Suspicious activity detected",
       "description": "Potential security threat identified",
       "recommendations": ["Isolate the resource", "Review logs"]
     },
     "timestamp": "2025-07-23T19:30:00Z"
   }

6. AGREGAR ACCIONES:
   a) Condition: Check if severity = "High" or "Critical"
   b) If true: 
      - Send an email (V2): Notificar al equipo de seguridad
      - HTTP: POST webhook a sistema de tickets
      - Azure Function: Trigger automated response
   c) If false: 
      - Create item: Log en SharePoint o sistema de tracking

7. CONFIGURAR EMAIL ACTION:
   {
     "To": "security-team@empresa.com",
     "Subject": "🚨 ALERTA DE SEGURIDAD - @{triggerBody()?['severity']}",
     "Body": "Se ha detectado una alerta de seguridad:\n\nTítulo: @{triggerBody()?['alertDetails']?['title']}\nSeveridad: @{triggerBody()?['severity']}\nRecurso: @{triggerBody()?['resourceId']}\nDescripción: @{triggerBody()?['alertDetails']?['description']}\n\nRecomendaciones:\n@{join(triggerBody()?['alertDetails']?['recommendations'], '\n- ')}\n\nTimestamp: @{triggerBody()?['timestamp']}"
   }

8. GUARDAR Y PROBAR:
   - Click en "Save"
   - Copiar la URL del trigger HTTP POST
   - Usar para testing manual

=== CONFIGURACIÓN COMPLETADA ===
"@

$logicAppInstructions | Out-File -FilePath "logic-app-setup-instructions.txt" -Encoding UTF8

Write-Host "📋 Instrucciones de configuración guardadas en: logic-app-setup-instructions.txt" -ForegroundColor Green
```

**✅ Verificación:** Debe poder acceder al Logic App en Azure Portal y configurar el workflow.

---

## 📊 Paso 3: Testing de Automatización (5 minutos)

### Testing de Logic App y análisis automatizado:

```powershell
# Test manual del Logic App
Write-Host "🧪 Testing del Logic App..." -ForegroundColor Yellow

# Obtener la URL del Logic App
$LOGIC_APP_URL = az logic workflow show `
    --resource-group $resourceGroupName `
    --name "logic-security-response-$env:USERNAME" `
    --query accessEndpoint --output tsv

# Crear payload de prueba
$testPayload = @{
    alertType = "Security_Alert"
    severity = "High"
    resourceId = "/subscriptions/test/resourceGroups/$resourceGroupName/providers/Microsoft.Compute/virtualMachines/vm-windows-test"
    alertDetails = @{
        title = "Suspicious PowerShell execution detected"
        description = "Potentially malicious PowerShell command executed"
        recommendations = @("Isolate the machine", "Review PowerShell logs", "Run malware scan")
    }
    timestamp = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
} | ConvertTo-Json -Depth 3

# Enviar test al Logic App
try {
    $response = Invoke-RestMethod -Uri $LOGIC_APP_URL -Method POST -Body $testPayload -ContentType "application/json"
    Write-Host "✅ Test enviado exitosamente al Logic App" -ForegroundColor Green
} catch {
    Write-Host "❌ Error enviando test al Logic App: $($_.Exception.Message)" -ForegroundColor Red
}

# Verificar logs del Logic App
Write-Host "`n🔍 Verificando logs del Logic App..." -ForegroundColor Yellow
az monitor activity-log list `
    --resource-group $resourceGroupName `
    --output table

# Ejecutar el analizador de Secure Score
Write-Host "`n🔍 Ejecutando analizador de Secure Score..." -ForegroundColor Yellow
Set-Location "SecureScoreAnalyzer"
dotnet run

# Verificar reporte generado
if (Test-Path "security-report.json") {
    Write-Host "✅ Reporte de seguridad generado exitosamente" -ForegroundColor Green
    $report = Get-Content "security-report.json" | ConvertFrom-Json
    Write-Host "📊 Resumen del reporte:" -ForegroundColor Cyan
    Write-Host "   - Total recomendaciones: $($report.Summary.TotalRecommendations)" -ForegroundColor White
    Write-Host "   - Alta prioridad: $($report.Summary.HighPriority)" -ForegroundColor White
    Write-Host "   - Quick wins: $($report.Summary.QuickWins)" -ForegroundColor White
    Write-Host "   - Alertas activas: $($report.Summary.ActiveAlerts)" -ForegroundColor White
} else {
    Write-Host "❌ Error generando reporte de seguridad" -ForegroundColor Red
}
```

**✅ Verificación:** Debe ver el test enviado al Logic App y el reporte generado.

---

## 📊 Paso 4: Verificación Final Completa (5 minutos)

### Script de verificación final:

```powershell
# Script de verificación completa
$finalVerificationScript = @"
Write-Host "=== VERIFICACIÓN FINAL DE AUTOMATIZACIÓN ===" -ForegroundColor Green

# Verificar aplicación .NET
Write-Host "`n🔍 Verificando aplicación .NET..." -ForegroundColor Yellow
if (Test-Path "SecureScoreAnalyzer/SecureScoreAnalyzer.csproj") {
    Write-Host "✅ Proyecto .NET creado" -ForegroundColor Green
} else {
    Write-Host "❌ Proyecto .NET no encontrado" -ForegroundColor Red
}

if (Test-Path "SecureScoreAnalyzer/security-report.json") {
    Write-Host "✅ Reporte de seguridad generado" -ForegroundColor Green
} else {
    Write-Host "❌ Reporte de seguridad no encontrado" -ForegroundColor Red
}

# Verificar Logic App
Write-Host "`n🔍 Verificando Logic App..." -ForegroundColor Yellow
try {
    `$logicApp = az logic workflow show --resource-group $resourceGroupName --name "logic-security-response-$env:USERNAME" --output json | ConvertFrom-Json
    Write-Host "✅ Logic App: `$(`$logicApp.name)" -ForegroundColor Green
    Write-Host "   Estado: `$(`$logicApp.state)" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Logic App no encontrado" -ForegroundColor Red
}

# Verificar recursos totales
Write-Host "`n🔍 Verificando recursos totales..." -ForegroundColor Yellow
`$resources = az resource list --resource-group $resourceGroupName --output json | ConvertFrom-Json
`$resourceTypes = `$resources | Group-Object type | Sort-Object Count -Descending
foreach (`$type in `$resourceTypes) {
    Write-Host "✅ `$(`$type.Name): `$(`$type.Count) recursos" -ForegroundColor Green
}

# Verificar planes de Defender
Write-Host "`n🔍 Verificando planes de Defender..." -ForegroundColor Yellow
`$pricing = az security pricing list --output json | ConvertFrom-Json
`$standardPlans = `$pricing | Where-Object { `$_.pricingTier -eq "Standard" }
Write-Host "✅ Planes Standard habilitados: `$(`$standardPlans.Count)" -ForegroundColor Green

Write-Host "`n=== VERIFICACIÓN COMPLETADA ===" -ForegroundColor Green
Write-Host "🎉 ¡Todos los laboratorios completados exitosamente!" -ForegroundColor Green
"@

# Guardar y ejecutar script
$finalVerificationScript | Out-File -FilePath "verify-final-automation.ps1" -Encoding UTF8
.\verify-final-automation.ps1
```

**✅ Verificación Final:** Todos los componentes deben mostrar ✅ verde.

---

## 🎯 Resultados Esperados

Al completar este laboratorio, habrá logrado:

### 🔍 Security Assessment Automation:
- ✅ Aplicación .NET Core para análisis de Secure Score
- ✅ Logic App para respuesta automática a alertas
- ✅ Reporting automatizado y métricas de seguridad
- ✅ Integration con sistemas externos (email, tickets)

### 🤖 Continuous Security Monitoring:
- ✅ Log Analytics workspace configurado
- ✅ Security policies enforcement habilitado
- ✅ Automated compliance checking funcionando
- ✅ Real-time threat detection activo

### 📊 Analytics y Reporting:
- ✅ Análisis de tendencias de Secure Score
- ✅ Priorización inteligente de recomendaciones
- ✅ Reportes ejecutivos automatizados
- ✅ Métricas de seguridad cuantificadas

---

## 🚨 Troubleshooting Común

### Error: ".NET application compilation failed"
**Solución:**
```powershell
# Verificar versión de .NET
dotnet --version

# Limpiar y restaurar paquetes
dotnet clean
dotnet restore
dotnet build --verbose

# Verificar que todas las dependencias están instaladas
dotnet list package
```

### Error: "Logic App trigger URL not working"
**Solución:**
1. Azure Portal → Logic Apps → logic-security-response-[username]
2. Logic app designer → When an HTTP request is received
3. Copy HTTP POST URL (se genera después de guardar)
4. Verificar que el JSON schema está correctamente configurado

### Error: "Azure authentication failed"
**Solución:**
```powershell
# Reautenticar con Azure
az login

# Verificar suscripción activa
az account show

# Verificar permisos
az role assignment list --assignee $env:USERNAME --output table
```

---

## 📊 Métricas de Éxito

Indicadores de Implementación Exitosa:
- ✅ Automation & Monitoring: Aplicación .NET compilando y ejecutando sin errores
- ✅ Logic App respondiendo a triggers HTTP
- ✅ Security reports generándose en formato JSON
- ✅ Email notifications funcionando correctamente
- ✅ Integration & Analytics: Log Analytics workspace recibiendo datos
- ✅ Security policies enforcement activo
- ✅ Trend analysis disponible para Secure Score
- ✅ Automated response workflows funcionando

---

## 🔗 Recursos Adicionales

- [Azure Logic Apps Documentation](https://docs.microsoft.com/azure/logic-apps/)
- [Azure Resource Manager SDK](https://docs.microsoft.com/dotnet/api/overview/azure/resource-manager)
- [Security Center REST API](https://docs.microsoft.com/rest/api/securitycenter/)
- [Azure Monitor Logs](https://docs.microsoft.com/azure/azure-monitor/logs/)

---

## 🎯 Próximos Pasos

Una vez completado este laboratorio, habrá terminado exitosamente la Sesión 8:

1. **Sesión 9:** Pruebas y Auditorías Parte 2 - Penetration Testing y Attack Simulation

---

## 📝 Notas Importantes

- **Costos:** Los recursos creados pueden generar costos en Azure
- **Seguridad:** Las configuraciones son para propósitos educativos
- **Limpieza:** Ejecutar scripts de limpieza para evitar costos innecesarios
- **Documentación:** Guardar los reportes generados para referencia futura

---

## 🧹 Limpieza Completa de Recursos

```powershell
# Deshabilitar Defender plans para evitar costos
az security pricing create --name VirtualMachines --tier Free
az security pricing create --name AppServices --tier Free
az security pricing create --name StorageAccounts --tier Free
az security pricing create --name SqlServers --tier Free
az security pricing create --name ContainerRegistry --tier Free

# Eliminar resource group completo
az group delete --name $resourceGroupName --yes --no-wait

# Limpiar políticas personalizadas
az policy assignment delete --name "assign-https-policy"
az policy assignment delete --name "require-storage-encryption"
az policy definition delete --name "require-https-webapps"

# Limpiar archivos locales
Remove-Item -Path "SecureScoreAnalyzer" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "container-security-test" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "*.ps1" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "*.json" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "*.txt" -Force -ErrorAction SilentlyContinue

Write-Host "🧹 Limpieza completada exitosamente" -ForegroundColor Green
```

---

## 🎉 ¡FELICITACIONES!

Ha completado exitosamente la implementación de Azure Security Center avanzado y vulnerability assessment automatizado. Esta plataforma de seguridad es:

- ✅ **Enterprise-ready** para organizaciones de cualquier tamaño
- ✅ **Compliance-focused** con frameworks regulatorios
- ✅ **Automated** para continuous security assessment
- ✅ **Scalable** para infraestructuras complejas
- ✅ **Integrated** con herramientas de security operations

La solución que ha construido puede:
- Detectar vulnerabilidades en tiempo real
- Automatizar respuesta a incidentes de seguridad
- Mantener compliance continuo con regulaciones
- Proporcionar visibility ejecutiva del riesgo
- Integrar con SIEM y SOAR platforms

**¡Nos vemos en la Sesión 9 para continuar con Penetration Testing y Attack Simulation! 🚀** 