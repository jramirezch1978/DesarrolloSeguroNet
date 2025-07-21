#Requires -Version 5.1

<#
.SYNOPSIS
Script para configurar Azure DDoS Protection Standard con monitoreo avanzado

.DESCRIPTION
Este script configura DDoS Protection Standard, crea alertas, dashboards y 
herramientas de monitoreo para protección completa contra ataques DDoS.

.PARAMETER ResourceGroupName
Nombre del resource group donde configurar DDoS Protection

.PARAMETER VNetName
Nombre de la Virtual Network a proteger

.PARAMETER PublicIpName
Nombre del Public IP a proteger

.PARAMETER CreateTestResources
Crear recursos de testing (Application Gateway + Public IP)

.PARAMETER EnableMonitoring
Habilitar alertas y dashboards de monitoreo

.EXAMPLE
.\script-setup-ddos-protection.ps1 -ResourceGroupName "rg-lab" -VNetName "vnet-lab" -CreateTestResources -EnableMonitoring
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory = $false)]
    [string]$VNetName = "vnet-nsg-lab",
    
    [Parameter(Mandatory = $false)]
    [string]$PublicIpName = "pip-ddos-test",
    
    [Parameter(Mandatory = $false)]
    [switch]$CreateTestResources = $true,
    
    [Parameter(Mandatory = $false)]
    [switch]$EnableMonitoring = $true,
    
    [Parameter(Mandatory = $false)]
    [string]$AlertEmail = "",
    
    [Parameter(Mandatory = $false)]
    [switch]$DryRun = $false
)

# Funciones auxiliares
function Write-ColorMessage {
    param(
        [string]$Message,
        [ValidateSet("Green", "Yellow", "Red", "Cyan", "Magenta", "White")]
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Get-LabConfig {
    $configPath = Join-Path $PSScriptRoot "../scripts/lab-config.json"
    if (Test-Path $configPath) {
        return Get-Content $configPath | ConvertFrom-Json
    }
    return $null
}

function Test-ResourceExists {
    param(
        [string]$ResourceType,
        [string]$ResourceName,
        [string]$ResourceGroup
    )
    
    $exists = switch ($ResourceType) {
        "vnet" { az network vnet show --name $ResourceName --resource-group $ResourceGroup 2>$null }
        "publicip" { az network public-ip show --name $ResourceName --resource-group $ResourceGroup 2>$null }
        "ddos-plan" { az network ddos-protection show --name $ResourceName --resource-group $ResourceGroup 2>$null }
        default { $false }
    }
    
    return $null -ne $exists
}

function Show-CostWarning {
    Write-ColorMessage "`n💰 ADVERTENCIA DE COSTOS - AZURE DDOS PROTECTION STANDARD" "Yellow"
    Write-ColorMessage "════════════════════════════════════════════════════════════" "Yellow"
    Write-ColorMessage "• Costo mensual: ~$2,944 USD/mes" "Red"
    Write-ColorMessage "• Facturación: Inmediata al habilitar" "Red"
    Write-ColorMessage "• Para laboratorio: Se recomienda deshabilitar después del testing" "Yellow"
    Write-ColorMessage "• Incluye: Protección hasta 2+ Tbps, DRR Team, SLA 99.99%" "Green"
    Write-ColorMessage "════════════════════════════════════════════════════════════" "Yellow"
    
    if (-not $DryRun) {
        $confirmation = Read-Host "`n¿Desea continuar con la configuración de DDoS Protection Standard? (y/N)"
        if ($confirmation -ne "y" -and $confirmation -ne "Y" -and $confirmation -ne "yes") {
            Write-ColorMessage "❌ Operación cancelada por el usuario" "Yellow"
            exit 0
        }
    }
}

function Create-TestResources {
    param(
        [string]$ResourceGroup,
        [string]$Location,
        [string]$PublicIpName,
        [string]$VNetName
    )
    
    if (-not $CreateTestResources) {
        Write-ColorMessage "⏭️ Omitiendo creación de recursos de testing" "Yellow"
        return
    }
    
    Write-ColorMessage "🎯 Creando recursos de testing para DDoS..." "Cyan"
    
    # Crear Public IP Standard (requerido para DDoS Protection)
    if (-not (Test-ResourceExists "publicip" $PublicIpName $ResourceGroup)) {
        Write-ColorMessage "📍 Creando Public IP Standard: $PublicIpName" "Yellow"
        
        if (-not $DryRun) {
            az network public-ip create `
                --resource-group $ResourceGroup `
                --name $PublicIpName `
                --allocation-method Static `
                --sku Standard `
                --location $Location `
                --tags Purpose=DDoSTest Environment=Lab CreatedAt=$(Get-Date -Format "yyyy-MM-dd")
            
            if ($LASTEXITCODE -ne 0) {
                throw "Error creando Public IP"
            }
        }
        
        Write-ColorMessage "✅ Public IP creado exitosamente" "Green"
    } else {
        Write-ColorMessage "ℹ️ Public IP $PublicIpName ya existe" "Yellow"
    }
    
    # Crear Application Gateway para testing
    $appGwName = "appgw-ddos-test"
    Write-ColorMessage "⚖️ Creando Application Gateway: $appGwName" "Yellow"
    
    if (-not $DryRun) {
        # Verificar que existe la subred para AppGW
        $subnetExists = az network vnet subnet show --resource-group $ResourceGroup --vnet-name $VNetName --name snet-appgw 2>$null
        if (-not $subnetExists) {
            Write-ColorMessage "📡 Creando subred para Application Gateway..." "Yellow"
            az network vnet subnet create `
                --resource-group $ResourceGroup `
                --vnet-name $VNetName `
                --name snet-appgw `
                --address-prefix 10.2.4.0/24
        }
        
        # Crear Application Gateway
        az network application-gateway create `
            --name $appGwName `
            --location $Location `
            --resource-group $ResourceGroup `
            --vnet-name $VNetName `
            --subnet snet-appgw `
            --capacity 2 `
            --sku Standard_v2 `
            --http-settings-cookie-based-affinity Disabled `
            --frontend-port 80 `
            --http-settings-port 80 `
            --http-settings-protocol Http `
            --public-ip-address $PublicIpName `
            --tags Purpose=DDoSTest Environment=Lab
        
        if ($LASTEXITCODE -ne 0) {
            Write-ColorMessage "⚠️ Application Gateway creation failed - continuando..." "Yellow"
        } else {
            Write-ColorMessage "✅ Application Gateway creado exitosamente" "Green"
        }
    } else {
        Write-ColorMessage "[DRY RUN] Application Gateway creation command prepared" "Cyan"
    }
}

function Setup-DDoSProtection {
    param(
        [string]$ResourceGroup,
        [string]$Location,
        [string]$VNetName
    )
    
    Write-ColorMessage "🛡️ Configurando Azure DDoS Protection Standard..." "Cyan"
    
    $ddosPlanName = "ddos-plan-lab"
    
    # Crear DDoS Protection Plan
    if (-not (Test-ResourceExists "ddos-plan" $ddosPlanName $ResourceGroup)) {
        Write-ColorMessage "🛡️ Creando DDoS Protection Plan: $ddosPlanName" "Yellow"
        
        if (-not $DryRun) {
            az network ddos-protection create `
                --resource-group $ResourceGroup `
                --name $ddosPlanName `
                --location $Location `
                --tags Environment=Lab Purpose=Testing CreatedAt=$(Get-Date -Format "yyyy-MM-dd")
            
            if ($LASTEXITCODE -ne 0) {
                throw "Error creando DDoS Protection Plan"
            }
        }
        
        Write-ColorMessage "✅ DDoS Protection Plan creado exitosamente" "Green"
    } else {
        Write-ColorMessage "ℹ️ DDoS Protection Plan $ddosPlanName ya existe" "Yellow"
    }
    
    # Obtener ID del DDoS Protection Plan
    if (-not $DryRun) {
        $ddosPlanId = az network ddos-protection show `
            --resource-group $ResourceGroup `
            --name $ddosPlanName `
            --query id --output tsv
        
        if (-not $ddosPlanId) {
            throw "No se pudo obtener el ID del DDoS Protection Plan"
        }
    } else {
        $ddosPlanId = "/subscriptions/SUBSCRIPTION_ID/resourceGroups/$ResourceGroup/providers/Microsoft.Network/ddosProtectionPlans/$ddosPlanName"
    }
    
    # Habilitar DDoS Protection en la VNET
    Write-ColorMessage "🌐 Habilitando DDoS Protection en VNET: $VNetName" "Yellow"
    
    if (-not $DryRun) {
        az network vnet update `
            --resource-group $ResourceGroup `
            --name $VNetName `
            --ddos-protection true `
            --ddos-protection-plan $ddosPlanId
        
        if ($LASTEXITCODE -ne 0) {
            throw "Error habilitando DDoS Protection en VNET"
        }
    }
    
    Write-ColorMessage "✅ DDoS Protection habilitado en VNET" "Green"
    
    # Verificar configuración
    if (-not $DryRun) {
        Write-ColorMessage "🔍 Verificando configuración de DDoS Protection..." "Yellow"
        
        $vnetDDoSConfig = az network vnet show `
            --resource-group $ResourceGroup `
            --name $VNetName `
            --query "{DDoSEnabled:enableDdosProtection, DDoSPlan:ddosProtectionPlan.id}" | ConvertFrom-Json
        
        if ($vnetDDoSConfig.DDoSEnabled -eq $true) {
            Write-ColorMessage "✅ DDoS Protection verificado - ACTIVO" "Green"
        } else {
            Write-ColorMessage "⚠️ DDoS Protection no está activo" "Yellow"
        }
    }
}

function Setup-Monitoring {
    param(
        [string]$ResourceGroup,
        [string]$PublicIpName,
        [string]$AlertEmail
    )
    
    if (-not $EnableMonitoring) {
        Write-ColorMessage "⏭️ Omitiendo configuración de monitoreo" "Yellow"
        return
    }
    
    Write-ColorMessage "📊 Configurando monitoreo y alertas DDoS..." "Cyan"
    
    # Crear Action Group
    $actionGroupName = "ag-ddos-alerts"
    Write-ColorMessage "🔔 Creando Action Group: $actionGroupName" "Yellow"
    
    if (-not $DryRun) {
        $actionGroupCmd = "az monitor action-group create --resource-group $ResourceGroup --name $actionGroupName --short-name ddosalert"
        
        # Agregar email si se proporcionó
        if (-not [string]::IsNullOrEmpty($AlertEmail)) {
            $actionGroupCmd += " --email-receiver name=admin email=$AlertEmail"
        }
        
        Invoke-Expression $actionGroupCmd
        
        if ($LASTEXITCODE -ne 0) {
            Write-ColorMessage "⚠️ Error creando Action Group - continuando..." "Yellow"
        } else {
            Write-ColorMessage "✅ Action Group creado exitosamente" "Green"
        }
    }
    
    # Obtener ID del Public IP
    if (-not $DryRun) {
        $pipId = az network public-ip show `
            --resource-group $ResourceGroup `
            --name $PublicIpName `
            --query id --output tsv
        
        if (-not $pipId) {
            Write-ColorMessage "⚠️ No se pudo obtener ID del Public IP" "Yellow"
            return
        }
    } else {
        $pipId = "/subscriptions/SUBSCRIPTION_ID/resourceGroups/$ResourceGroup/providers/Microsoft.Network/publicIPAddresses/$PublicIpName"
    }
    
    # Crear alertas DDoS
    $alerts = @(
        @{
            Name = "alert-ddos-attack"
            Condition = "max 'UnderDDoSAttack' > 0"
            Description = "Alerta cuando está bajo ataque DDoS"
            Severity = 0
        },
        @{
            Name = "alert-ddos-packets-dropped"
            Condition = "max 'PacketsDroppedDDoS' > 1000"
            Description = "Alerta cuando se bloquean >1000 paquetes DDoS"
            Severity = 1
        },
        @{
            Name = "alert-ddos-bytes-dropped"
            Condition = "max 'BytesDroppedDDoS' > 1000000"
            Description = "Alerta cuando se bloquean >1MB de datos DDoS"
            Severity = 1
        }
    )
    
    foreach ($alert in $alerts) {
        Write-ColorMessage "🚨 Creando alerta: $($alert.Name)" "Yellow"
        
        if (-not $DryRun) {
            az monitor metrics alert create `
                --name $alert.Name `
                --resource-group $ResourceGroup `
                --scopes $pipId `
                --condition $alert.Condition `
                --description $alert.Description `
                --evaluation-frequency PT1M `
                --window-size PT5M `
                --severity $alert.Severity `
                --action $actionGroupName 2>$null
            
            if ($LASTEXITCODE -eq 0) {
                Write-ColorMessage "  ✅ $($alert.Name) creada" "Green"
            } else {
                Write-ColorMessage "  ⚠️ Error creando $($alert.Name)" "Yellow"
            }
        }
    }
    
    Write-ColorMessage "✅ Monitoreo configurado exitosamente" "Green"
}

function Create-MonitoringDashboard {
    param(
        [string]$ResourceGroup,
        [string]$PublicIpName
    )
    
    if (-not $EnableMonitoring -or $DryRun) {
        return
    }
    
    Write-ColorMessage "📈 Creando dashboard de monitoreo..." "Yellow"
    
    $dashboardName = "DDoS-Protection-Dashboard"
    $dashboardPath = Join-Path $PSScriptRoot "../monitoring/ddos-dashboard.json"
    
    # Crear template de dashboard si no existe
    if (-not (Test-Path $dashboardPath)) {
        $dashboardTemplate = @{
            "lenses" = @{
                "0" = @{
                    "order" = 0
                    "parts" = @(
                        @{
                            "position" = @{ "x" = 0; "y" = 0; "colSpan" = 6; "rowSpan" = 4 }
                            "metadata" = @{
                                "inputs" = @(
                                    @{
                                        "name" = "options"
                                        "value" = @{
                                            "chart" = @{
                                                "metrics" = @(
                                                    @{
                                                        "resourceMetadata" = @{
                                                            "id" = "/subscriptions/SUBSCRIPTION_ID/resourceGroups/$ResourceGroup/providers/Microsoft.Network/publicIPAddresses/$PublicIpName"
                                                        }
                                                        "name" = "UnderDDoSAttack"
                                                        "aggregationType" = 4
                                                        "namespace" = "Microsoft.Network/publicIPAddresses"
                                                        "metricVisualization" = @{
                                                            "displayName" = "Under DDoS Attack"
                                                            "color" = "#FF0000"
                                                        }
                                                    }
                                                )
                                                "title" = "DDoS Attack Status"
                                                "titleKind" = 1
                                                "visualization" = @{
                                                    "chartType" = 2
                                                    "legendVisualization" = @{
                                                        "isVisible" = $true
                                                        "position" = 2
                                                        "hideSubtitle" = $false
                                                    }
                                                    "axisVisualization" = @{
                                                        "x" = @{ "isVisible" = $true; "axisType" = 2 }
                                                        "y" = @{ "isVisible" = $true; "axisType" = 1 }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                )
                                "type" = "Extension/HubsExtension/PartType/MonitorChartPart"
                            }
                        }
                    )
                }
            }
            "metadata" = @{
                "model" = @{
                    "timeRange" = @{
                        "value" = @{
                            "relative" = @{
                                "duration" = 24
                                "timeUnit" = 1
                            }
                        }
                        "type" = "MsPortalFx.Composition.Configuration.ValueTypes.TimeRange"
                    }
                }
            }
        }
        
        $dashboardTemplate | ConvertTo-Json -Depth 20 | Set-Content $dashboardPath
    }
    
    Write-ColorMessage "✅ Template de dashboard creado en: $dashboardPath" "Green"
    Write-ColorMessage "💡 Configure el dashboard manualmente en Azure Portal para personalizar" "Cyan"
}

function Show-NextSteps {
    param(
        [string]$ResourceGroup,
        [string]$PublicIpName
    )
    
    Write-ColorMessage "`n🚀 PRÓXIMOS PASOS" "Magenta"
    Write-ColorMessage "════════════════════════════════════════" "Magenta"
    
    Write-ColorMessage "1. 📊 Monitorear métricas en Azure Portal:" "Yellow"
    Write-ColorMessage "   • Portal → Monitor → Metrics" "Cyan"
    Write-ColorMessage "   • Resource: $PublicIpName" "Cyan"
    Write-ColorMessage "   • Metrics: UnderDDoSAttack, PacketsDroppedDDoS" "Cyan"
    
    Write-ColorMessage "`n2. 🔍 Usar DDoS Monitor para tiempo real:" "Yellow"
    Write-ColorMessage "   cd ../src/DDoSMonitor" "Cyan"
    Write-ColorMessage "   dotnet run -- monitor --resource-group $ResourceGroup --public-ip $PublicIpName" "Cyan"
    
    Write-ColorMessage "`n3. ⚡ Testing ético (SOLO en recursos propios):" "Yellow"
    Write-ColorMessage "   dotnet run -- simulate --target-url http://YOUR-IP/ --i-own-this-resource" "Cyan"
    
    Write-ColorMessage "`n4. 📄 Generar reportes:" "Yellow"
    Write-ColorMessage "   dotnet run -- report --format html --output ddos-report.html" "Cyan"
    
    Write-ColorMessage "`n⚠️  RECORDATORIO IMPORTANTE:" "Red"
    Write-ColorMessage "   💰 DDoS Protection Standard cuesta ~$2,944/mes" "Red"
    Write-ColorMessage "   🧹 Ejecute script-cleanup-ddos.ps1 después del laboratorio" "Red"
}

function Show-Summary {
    param(
        [hashtable]$Results
    )
    
    Write-ColorMessage "`n📋 RESUMEN DE CONFIGURACIÓN" "Magenta"
    Write-ColorMessage "════════════════════════════════════════" "Magenta"
    
    $totalSteps = $Results.Count
    $successfulSteps = ($Results.Values | Where-Object { $_ -eq $true }).Count
    
    Write-ColorMessage "📊 Estadísticas:" "Cyan"
    Write-ColorMessage "  • Pasos totales: $totalSteps" "White"
    Write-ColorMessage "  • Exitosos: $successfulSteps" "Green"
    Write-ColorMessage "  • Fallidos: $($totalSteps - $successfulSteps)" "Red"
    
    Write-ColorMessage "`n📋 Detalles:" "Cyan"
    foreach ($result in $Results.GetEnumerator()) {
        $status = if ($result.Value) { "✅ COMPLETADO" } else { "❌ FALLIDO" }
        $color = if ($result.Value) { "Green" } else { "Red" }
        Write-ColorMessage "  • $($result.Key): $status" $color
    }
    
    if ($successfulSteps -eq $totalSteps) {
        Write-ColorMessage "`n🎉 ¡CONFIGURACIÓN COMPLETAMENTE EXITOSA!" "Green"
    } elseif ($successfulSteps -ge $totalSteps * 0.7) {
        Write-ColorMessage "`n⚠️ CONFIGURACIÓN PARCIALMENTE EXITOSA" "Yellow"
    } else {
        Write-ColorMessage "`n❌ CONFIGURACIÓN CON PROBLEMAS SIGNIFICATIVOS" "Red"
    }
}

# Script principal
try {
    Write-ColorMessage "🛡️ CONFIGURACIÓN DE AZURE DDOS PROTECTION" "Magenta"
    Write-ColorMessage "════════════════════════════════════════════" "Magenta"
    
    if ($DryRun) {
        Write-ColorMessage "🔍 MODO DRY RUN - No se realizarán cambios reales" "Yellow"
        Write-ColorMessage "════════════════════════════════════════════" "Yellow"
    }
    
    # Cargar configuración del laboratorio
    $config = Get-LabConfig
    if ($null -eq $config) {
        Write-ColorMessage "⚠️ No se encontró configuración de laboratorio previa" "Yellow"
        Write-ColorMessage "Continuando con valores por defecto..." "Yellow"
        $location = "eastus"
    } else {
        $location = $config.Location
        Write-ColorMessage "📋 Configuración cargada: $($config.ResourceGroupName) en $location" "Cyan"
    }
    
    # Mostrar advertencia de costos
    Show-CostWarning
    
    # Ejecutar pasos de configuración
    $configurationResults = @{}
    
    try {
        Create-TestResources -ResourceGroup $ResourceGroupName -Location $location -PublicIpName $PublicIpName -VNetName $VNetName
        $configurationResults["Recursos de Testing"] = $true
    } catch {
        Write-ColorMessage "❌ Error en recursos de testing: $($_.Exception.Message)" "Red"
        $configurationResults["Recursos de Testing"] = $false
    }
    
    try {
        Setup-DDoSProtection -ResourceGroup $ResourceGroupName -Location $location -VNetName $VNetName
        $configurationResults["DDoS Protection"] = $true
    } catch {
        Write-ColorMessage "❌ Error en DDoS Protection: $($_.Exception.Message)" "Red"
        $configurationResults["DDoS Protection"] = $false
    }
    
    try {
        Setup-Monitoring -ResourceGroup $ResourceGroupName -PublicIpName $PublicIpName -AlertEmail $AlertEmail
        $configurationResults["Monitoreo y Alertas"] = $true
    } catch {
        Write-ColorMessage "❌ Error en monitoreo: $($_.Exception.Message)" "Red"
        $configurationResults["Monitoreo y Alertas"] = $false
    }
    
    try {
        Create-MonitoringDashboard -ResourceGroup $ResourceGroupName -PublicIpName $PublicIpName
        $configurationResults["Dashboard"] = $true
    } catch {
        Write-ColorMessage "❌ Error en dashboard: $($_.Exception.Message)" "Red"
        $configurationResults["Dashboard"] = $false
    }
    
    # Mostrar resumen
    Show-Summary -Results $configurationResults
    
    # Mostrar próximos pasos
    if (-not $DryRun) {
        Show-NextSteps -ResourceGroup $ResourceGroupName -PublicIpName $PublicIpName
    }
    
    Write-ColorMessage "`n🎉 ¡CONFIGURACIÓN DE DDOS PROTECTION COMPLETADA!" "Green"
}
catch {
    Write-ColorMessage "`n❌ ERROR EN CONFIGURACIÓN DE DDOS PROTECTION" "Red"
    Write-ColorMessage "Error: $($_.Exception.Message)" "Red"
    
    Write-ColorMessage "`n🔧 TROUBLESHOOTING:" "Yellow"
    Write-ColorMessage "1. Verificar autenticación: az account show" "Cyan"
    Write-ColorMessage "2. Verificar permisos para DDoS Protection en la suscripción" "Cyan"
    Write-ColorMessage "3. Verificar que la VNET existe y es accesible" "Cyan"
    Write-ColorMessage "4. Verificar límites de la suscripción para DDoS Protection Plans" "Cyan"
    
    exit 1
}
finally {
    Write-ColorMessage "`n⏸️ Presione cualquier tecla para continuar..." "Yellow"
    Read-Host
} 