#Requires -Version 5.1

<#
.SYNOPSIS
Script para desplegar NSGs avanzados con mejores prácticas de seguridad

.DESCRIPTION
Este script despliega NSGs configurados con reglas granulares y Application Security Groups
para implementar una arquitectura de seguridad multicapa.

.PARAMETER ResourceGroupName
Nombre del resource group donde desplegar

.PARAMETER UseASGs
Habilitar Application Security Groups

.PARAMETER EnableFlowLogs
Habilitar Flow Logs para monitoreo

.EXAMPLE
.\script-deploy-nsgs.ps1 -ResourceGroupName "rg-nsg-lab-juan" -UseASGs -EnableFlowLogs
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory = $false)]
    [switch]$UseASGs = $true,
    
    [Parameter(Mandatory = $false)]
    [switch]$EnableFlowLogs = $true,
    
    [Parameter(Mandatory = $false)]
    [switch]$ValidateOnly = $false
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
    $configPath = Join-Path $PSScriptRoot "lab-config.json"
    if (Test-Path $configPath) {
        return Get-Content $configPath | ConvertFrom-Json
    }
    return $null
}

function New-NSGRule {
    param(
        [string]$Name,
        [int]$Priority,
        [string]$Direction,
        [string]$Access,
        [string]$Protocol,
        [string]$SourcePortRange = "*",
        [string]$DestinationPortRange,
        [string]$SourceAddressPrefix,
        [string]$DestinationAddressPrefix,
        [string]$Description
    )
    
    return @{
        Name = $Name
        Priority = $Priority
        Direction = $Direction
        Access = $Access
        Protocol = $Protocol
        SourcePortRange = $SourcePortRange
        DestinationPortRange = $DestinationPortRange
        SourceAddressPrefix = $SourceAddressPrefix
        DestinationAddressPrefix = $DestinationAddressPrefix
        Description = $Description
    }
}

function Deploy-WebTierNSG {
    param(
        [string]$ResourceGroup,
        [string]$Location
    )
    
    Write-ColorMessage "🌐 Desplegando NSG para Web Tier..." "Cyan"
    
    $nsgName = "nsg-web-advanced"
    
    # Crear NSG
    az network nsg create `
        --resource-group $ResourceGroup `
        --name $nsgName `
        --location $Location `
        --tags Tier=Web Environment=Lab SecurityLevel=Enhanced
    
    # Reglas de seguridad avanzadas
    $rules = @(
        @{
            Name = "Allow-HTTPS-Internet"
            Priority = 100
            Direction = "Inbound"
            Access = "Allow"
            Protocol = "Tcp"
            SourcePortRange = "*"
            DestinationPortRange = "443"
            SourceAddressPrefix = "Internet"
            DestinationAddressPrefix = "VirtualNetwork"
            Description = "HTTPS desde Internet - conexiones seguras"
        },
        @{
            Name = "Allow-HTTP-Redirect" 
            Priority = 110
            Direction = "Inbound"
            Access = "Allow"
            Protocol = "Tcp"
            SourcePortRange = "*"
            DestinationPortRange = "80"
            SourceAddressPrefix = "Internet"
            DestinationAddressPrefix = "VirtualNetwork"
            Description = "HTTP para redirección a HTTPS"
        },
        @{
            Name = "Allow-HealthProbes"
            Priority = 120
            Direction = "Inbound"
            Access = "Allow"
            Protocol = "*"
            SourcePortRange = "*"
            DestinationPortRange = "*"
            SourceAddressPrefix = "AzureLoadBalancer"
            DestinationAddressPrefix = "*"
            Description = "Health probes desde Azure Load Balancer"
        },
        @{
            Name = "Deny-Database-Ports"
            Priority = 3900
            Direction = "Inbound"
            Access = "Deny"
            Protocol = "Tcp"
            SourcePortRange = "*"
            DestinationPortRange = "1433,3306,5432,1521"
            SourceAddressPrefix = "*"
            DestinationAddressPrefix = "*"
            Description = "Bloqueo explícito de puertos de base de datos"
        },
        @{
            Name = "Deny-Admin-Protocols"
            Priority = 4000
            Direction = "Inbound"
            Access = "Deny"
            Protocol = "Tcp"
            SourcePortRange = "*"
            DestinationPortRange = "22,23,3389,5985,5986"
            SourceAddressPrefix = "Internet"
            DestinationAddressPrefix = "*"
            Description = "Bloqueo de protocolos administrativos desde Internet"
        }
    )
    
    foreach ($rule in $rules) {
        Write-ColorMessage "  📋 Creando regla: $($rule.Name)" "Yellow"
        
        az network nsg rule create `
            --resource-group $ResourceGroup `
            --nsg-name $nsgName `
            --name $rule.Name `
            --priority $rule.Priority `
            --direction $rule.Direction `
            --access $rule.Access `
            --protocol $rule.Protocol `
            --source-port-ranges $rule.SourcePortRange `
            --destination-port-ranges $rule.DestinationPortRange `
            --source-address-prefixes $rule.SourceAddressPrefix `
            --destination-address-prefixes $rule.DestinationAddressPrefix `
            --description $rule.Description
    }
    
    Write-ColorMessage "✅ NSG Web Tier desplegado exitosamente" "Green"
    return $nsgName
}

function Deploy-AppTierNSG {
    param(
        [string]$ResourceGroup,
        [string]$Location
    )
    
    Write-ColorMessage "⚙️ Desplegando NSG para App Tier..." "Cyan"
    
    $nsgName = "nsg-app-advanced"
    
    # Crear NSG
    az network nsg create `
        --resource-group $ResourceGroup `
        --name $nsgName `
        --location $Location `
        --tags Tier=Application Environment=Lab SecurityLevel=Enhanced
    
    # Reglas específicas para tier de aplicación
    $rules = @(
        @{
            Name = "Allow-Web-to-App"
            Priority = 100
            Direction = "Inbound"
            Access = "Allow"
            Protocol = "Tcp"
            SourcePortRange = "*"
            DestinationPortRange = "8080,8443,8000,9000"
            SourceAddressPrefix = "10.2.1.0/24"  # Web subnet
            DestinationAddressPrefix = "10.2.2.0/24"  # App subnet
            Description = "Comunicación desde tier web"
        },
        @{
            Name = "Allow-Internal-API"
            Priority = 110
            Direction = "Inbound"
            Access = "Allow"
            Protocol = "Tcp"
            SourcePortRange = "*"
            DestinationPortRange = "80,443"
            SourceAddressPrefix = "VirtualNetwork"
            DestinationAddressPrefix = "10.2.2.0/24"
            Description = "APIs internas desde VNET"
        },
        @{
            Name = "Deny-Direct-Internet"
            Priority = 4000
            Direction = "Inbound"
            Access = "Deny"
            Protocol = "*"
            SourcePortRange = "*"
            DestinationPortRange = "*"
            SourceAddressPrefix = "Internet"
            DestinationAddressPrefix = "*"
            Description = "Bloqueo de acceso directo desde Internet"
        }
    )
    
    foreach ($rule in $rules) {
        Write-ColorMessage "  📋 Creando regla: $($rule.Name)" "Yellow"
        
        az network nsg rule create `
            --resource-group $ResourceGroup `
            --nsg-name $nsgName `
            --name $rule.Name `
            --priority $rule.Priority `
            --direction $rule.Direction `
            --access $rule.Access `
            --protocol $rule.Protocol `
            --source-port-ranges $rule.SourcePortRange `
            --destination-port-ranges $rule.DestinationPortRange `
            --source-address-prefixes $rule.SourceAddressPrefix `
            --destination-address-prefixes $rule.DestinationAddressPrefix `
            --description $rule.Description
    }
    
    Write-ColorMessage "✅ NSG App Tier desplegado exitosamente" "Green"
    return $nsgName
}

function Deploy-DataTierNSG {
    param(
        [string]$ResourceGroup,
        [string]$Location
    )
    
    Write-ColorMessage "🗄️ Desplegando NSG para Data Tier..." "Cyan"
    
    $nsgName = "nsg-data-advanced"
    
    # Crear NSG
    az network nsg create `
        --resource-group $ResourceGroup `
        --name $nsgName `
        --location $Location `
        --tags Tier=Data Environment=Lab SecurityLevel=Maximum
    
    # Reglas máxima seguridad para datos
    $rules = @(
        @{
            Name = "Allow-App-to-SQL"
            Priority = 100
            Direction = "Inbound"
            Access = "Allow"
            Protocol = "Tcp"
            SourcePortRange = "*"
            DestinationPortRange = "1433"
            SourceAddressPrefix = "10.2.2.0/24"  # App subnet
            DestinationAddressPrefix = "10.2.3.0/24"  # Data subnet
            Description = "SQL Server desde tier aplicación"
        },
        @{
            Name = "Allow-App-to-MySQL"
            Priority = 110
            Direction = "Inbound"
            Access = "Allow"
            Protocol = "Tcp"
            SourcePortRange = "*"
            DestinationPortRange = "3306"
            SourceAddressPrefix = "10.2.2.0/24"
            DestinationAddressPrefix = "10.2.3.0/24"
            Description = "MySQL desde tier aplicación"
        },
        @{
            Name = "Allow-App-to-PostgreSQL"
            Priority = 120
            Direction = "Inbound"
            Access = "Allow"
            Protocol = "Tcp"
            SourcePortRange = "*"
            DestinationPortRange = "5432"
            SourceAddressPrefix = "10.2.2.0/24"
            DestinationAddressPrefix = "10.2.3.0/24"
            Description = "PostgreSQL desde tier aplicación"
        },
        @{
            Name = "Deny-Web-to-DB"
            Priority = 200
            Direction = "Inbound"
            Access = "Deny"
            Protocol = "*"
            SourcePortRange = "*"
            DestinationPortRange = "*"
            SourceAddressPrefix = "10.2.1.0/24"  # Web subnet
            DestinationAddressPrefix = "*"
            Description = "Bloqueo explícito desde tier web"
        },
        @{
            Name = "Deny-All-Internet"
            Priority = 4000
            Direction = "Inbound"
            Access = "Deny"
            Protocol = "*"
            SourcePortRange = "*"
            DestinationPortRange = "*"
            SourceAddressPrefix = "Internet"
            DestinationAddressPrefix = "*"
            Description = "Bloqueo total desde Internet"
        }
    )
    
    foreach ($rule in $rules) {
        Write-ColorMessage "  📋 Creando regla: $($rule.Name)" "Yellow"
        
        az network nsg rule create `
            --resource-group $ResourceGroup `
            --nsg-name $nsgName `
            --name $rule.Name `
            --priority $rule.Priority `
            --direction $rule.Direction `
            --access $rule.Access `
            --protocol $rule.Protocol `
            --source-port-ranges $rule.SourcePortRange `
            --destination-port-ranges $rule.DestinationPortRange `
            --source-address-prefixes $rule.SourceAddressPrefix `
            --destination-address-prefixes $rule.DestinationAddressPrefix `
            --description $rule.Description
    }
    
    Write-ColorMessage "✅ NSG Data Tier desplegado exitosamente" "Green"
    return $nsgName
}

function Deploy-ApplicationSecurityGroups {
    param(
        [string]$ResourceGroup,
        [string]$Location
    )
    
    if (-not $UseASGs) {
        Write-ColorMessage "⏭️ ASGs omitidos según parámetros" "Yellow"
        return @()
    }
    
    Write-ColorMessage "📋 Desplegando Application Security Groups..." "Cyan"
    
    $asgs = @(
        @{ Name = "asg-webservers"; Purpose = "WebServers"; Description = "Servidores web públicos" },
        @{ Name = "asg-appservers"; Purpose = "AppServers"; Description = "Servidores de aplicación" },
        @{ Name = "asg-dbservers"; Purpose = "DBServers"; Description = "Servidores de base de datos" },
        @{ Name = "asg-mgmtservers"; Purpose = "ManagementServers"; Description = "Servidores de gestión" }
    )
    
    $createdASGs = @()
    
    foreach ($asg in $asgs) {
        Write-ColorMessage "  📋 Creando ASG: $($asg.Name)" "Yellow"
        
        az network asg create `
            --resource-group $ResourceGroup `
            --name $asg.Name `
            --location $Location `
            --tags Purpose=$($asg.Purpose) Environment=Lab CreatedBy=PowerShell Description=$($asg.Description)
        
        $createdASGs += $asg.Name
    }
    
    Write-ColorMessage "✅ Application Security Groups desplegados exitosamente" "Green"
    return $createdASGs
}

function Associate-NSGsToSubnets {
    param(
        [string]$ResourceGroup,
        [string]$VNetName,
        [array]$NSGNames
    )
    
    Write-ColorMessage "🔗 Asociando NSGs a subredes..." "Cyan"
    
    # Asociaciones subnet -> NSG
    $associations = @{
        "snet-web" = "nsg-web-advanced"
        "snet-app" = "nsg-app-advanced" 
        "snet-data" = "nsg-data-advanced"
    }
    
    foreach ($association in $associations.GetEnumerator()) {
        $subnetName = $association.Key
        $nsgName = $association.Value
        
        if ($nsgName -in $NSGNames) {
            Write-ColorMessage "  🔗 Asociando $nsgName → $subnetName" "Yellow"
            
            az network vnet subnet update `
                --resource-group $ResourceGroup `
                --vnet-name $VNetName `
                --name $subnetName `
                --network-security-group $nsgName
        }
    }
    
    Write-ColorMessage "✅ NSGs asociados a subredes exitosamente" "Green"
}

function Enable-FlowLogsForNSGs {
    param(
        [string]$ResourceGroup,
        [array]$NSGNames,
        [string]$StorageAccountName
    )
    
    if (-not $EnableFlowLogs) {
        Write-ColorMessage "⏭️ Flow Logs omitidos según parámetros" "Yellow"
        return
    }
    
    Write-ColorMessage "📊 Habilitando Flow Logs..." "Cyan"
    
    foreach ($nsgName in $NSGNames) {
        Write-ColorMessage "  📈 Configurando Flow Logs para $nsgName" "Yellow"
        
        # En un entorno real, aquí se configuraría Flow Logs con Network Watcher
        # Por ahora mostramos el comando que se ejecutaría
        Write-ColorMessage "    (Flow Logs se configurarían con Network Watcher)" "White"
    }
    
    Write-ColorMessage "✅ Flow Logs configurados (simulación)" "Green"
}

function Test-NSGConfiguration {
    param(
        [string]$ResourceGroup,
        [array]$NSGNames
    )
    
    Write-ColorMessage "🔍 Validando configuración de NSGs..." "Cyan"
    
    $validationErrors = @()
    
    foreach ($nsgName in $NSGNames) {
        Write-ColorMessage "  🔍 Validando $nsgName" "Yellow"
        
        # Obtener reglas del NSG
        $rules = az network nsg rule list --resource-group $ResourceGroup --nsg-name $nsgName --query "[].{Name:name, Priority:priority, Access:access, Direction:direction}" | ConvertFrom-Json
        
        # Validaciones básicas
        $allowRules = $rules | Where-Object { $_.Access -eq "Allow" }
        $denyRules = $rules | Where-Object { $_.Access -eq "Deny" }
        
        if ($allowRules.Count -eq 0) {
            $validationErrors += "$nsgName: No tiene reglas Allow (puede bloquear todo el tráfico)"
        }
        
        if ($denyRules.Count -eq 0 -and $nsgName -eq "nsg-web-advanced") {
            $validationErrors += "$nsgName: No tiene reglas Deny explícitas para tier web"
        }
        
        # Verificar prioridades duplicadas
        $duplicatePriorities = $rules | Group-Object Priority | Where-Object { $_.Count -gt 1 }
        if ($duplicatePriorities) {
            $validationErrors += "$nsgName: Tiene prioridades duplicadas"
        }
    }
    
    if ($validationErrors.Count -eq 0) {
        Write-ColorMessage "✅ Validación exitosa - Configuración NSG correcta" "Green"
    } else {
        Write-ColorMessage "⚠️ Advertencias encontradas en validación:" "Yellow"
        foreach ($error in $validationErrors) {
            Write-ColorMessage "    • $error" "Yellow"
        }
    }
}

# Script principal
try {
    Write-ColorMessage "🛡️ DEPLOYMENT DE NSGS AVANZADOS" "Magenta"
    Write-ColorMessage "════════════════════════════════════════" "Magenta"
    
    # Cargar configuración del laboratorio
    $config = Get-LabConfig
    if ($null -eq $config) {
        Write-ColorMessage "⚠️ No se encontró configuración de laboratorio" "Yellow"
        Write-ColorMessage "Ejecute script-create-infrastructure.ps1 primero" "Yellow"
        exit 1
    }
    
    $location = $config.Location
    $vnetName = $config.VNetName
    $storageAccountName = $config.StorageAccountName
    
    Write-ColorMessage "📋 Configuración cargada:" "Cyan"
    Write-ColorMessage "  Resource Group: $ResourceGroupName" "White"
    Write-ColorMessage "  Ubicación: $location" "White"
    Write-ColorMessage "  VNET: $vnetName" "White"
    Write-ColorMessage "  Storage Account: $storageAccountName" "White"
    
    if ($ValidateOnly) {
        Write-ColorMessage "🔍 MODO VALIDACIÓN ÚNICAMENTE" "Yellow"
        
        # Obtener NSGs existentes
        $existingNSGs = az network nsg list --resource-group $ResourceGroupName --query "[].name" -o tsv
        
        if ($existingNSGs) {
            Test-NSGConfiguration -ResourceGroup $ResourceGroupName -NSGNames $existingNSGs
        } else {
            Write-ColorMessage "❌ No se encontraron NSGs para validar" "Red"
        }
        
        exit 0
    }
    
    # Desplegar NSGs por tier
    $deployedNSGs = @()
    
    $deployedNSGs += Deploy-WebTierNSG -ResourceGroup $ResourceGroupName -Location $location
    $deployedNSGs += Deploy-AppTierNSG -ResourceGroup $ResourceGroupName -Location $location  
    $deployedNSGs += Deploy-DataTierNSG -ResourceGroup $ResourceGroupName -Location $location
    
    # Desplegar Application Security Groups
    $deployedASGs = Deploy-ApplicationSecurityGroups -ResourceGroup $ResourceGroupName -Location $location
    
    # Asociar NSGs a subredes
    Associate-NSGsToSubnets -ResourceGroup $ResourceGroupName -VNetName $vnetName -NSGNames $deployedNSGs
    
    # Habilitar Flow Logs
    Enable-FlowLogsForNSGs -ResourceGroup $ResourceGroupName -NSGNames $deployedNSGs -StorageAccountName $storageAccountName
    
    # Validar configuración
    Test-NSGConfiguration -ResourceGroup $ResourceGroupName -NSGNames $deployedNSGs
    
    # Mostrar resumen final
    Write-ColorMessage "`n📊 RESUMEN DEL DEPLOYMENT" "Magenta"
    Write-ColorMessage "════════════════════════════════════════" "Magenta"
    Write-ColorMessage "NSGs Desplegados: $($deployedNSGs.Count)" "Green"
    foreach ($nsg in $deployedNSGs) {
        Write-ColorMessage "  ✅ $nsg" "Green"
    }
    
    if ($deployedASGs.Count -gt 0) {
        Write-ColorMessage "ASGs Desplegados: $($deployedASGs.Count)" "Green"
        foreach ($asg in $deployedASGs) {
            Write-ColorMessage "  ✅ $asg" "Green"
        }
    }
    
    Write-ColorMessage "`n🚀 PRÓXIMOS PASOS" "Magenta"
    Write-ColorMessage "1. Probar conectividad: .\script-test-connectivity.ps1" "Cyan"
    Write-ColorMessage "2. Auditar seguridad: .\script-security-audit.ps1" "Cyan"
    Write-ColorMessage "3. Usar NSGManager para análisis avanzado" "Cyan"
    
    Write-ColorMessage "`n🎉 ¡DEPLOYMENT COMPLETADO EXITOSAMENTE!" "Green"
}
catch {
    Write-ColorMessage "`n❌ ERROR EN DEPLOYMENT" "Red"
    Write-ColorMessage "Error: $($_.Exception.Message)" "Red"
    exit 1
}
finally {
    Write-ColorMessage "`n⏸️ Presione cualquier tecla para continuar..." "Yellow"
    Read-Host
} 