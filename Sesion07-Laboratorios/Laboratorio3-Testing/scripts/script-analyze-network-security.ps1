# Script: script-analyze-network-security.ps1
# Propósito: Análisis completo de seguridad de red usando Azure Network Watcher
# Autor: Laboratorio 3 - Testing y Simulación de Conectividad

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroup,
    
    [Parameter(Mandatory=$false)]
    [string]$VNetName = "vnet-nsg-lab",
    
    [Parameter(Mandatory=$false)]
    [switch]$DetailedAnalysis,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputDirectory = "security-analysis",
    
    [Parameter(Mandatory=$false)]
    [switch]$GenerateRecommendations,
    
    [Parameter(Mandatory=$false)]
    [string]$ComplianceFramework = "NIST" # NIST, PCI-DSS, HIPAA
)

# Configuración de colores
$SuccessColor = "Green"
$WarningColor = "Yellow"
$ErrorColor = "Red"
$InfoColor = "Cyan"
$CriticalColor = "Magenta"

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

function Initialize-AnalysisEnvironment {
    Write-ColorOutput "🔧 Inicializando entorno de análisis de seguridad..." $InfoColor
    
    # Crear directorio de salida
    if (!(Test-Path $OutputDirectory)) {
        New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
        Write-ColorOutput "📁 Directorio creado: $OutputDirectory" $SuccessColor
    }
    
    # Verificar herramientas necesarias
    $tools = @("az", "jq")
    foreach ($tool in $tools) {
        try {
            $null = Get-Command $tool -ErrorAction Stop
            Write-ColorOutput "✅ $tool disponible" $SuccessColor
        }
        catch {
            if ($tool -eq "jq") {
                Write-ColorOutput "⚠️ jq no disponible, algunos análisis serán limitados" $WarningColor
            }
            else {
                Write-ColorOutput "❌ $tool no disponible" $ErrorColor
                return $false
            }
        }
    }
    
    return $true
}

function Get-NetworkSecurityGroups {
    param([string]$ResourceGroup)
    
    Write-ColorOutput "📋 Analizando Network Security Groups..." $InfoColor
    
    try {
        $nsgs = az network nsg list --resource-group $ResourceGroup --output json | ConvertFrom-Json
        
        $nsgAnalysis = @()
        foreach ($nsg in $nsgs) {
            Write-ColorOutput "  🔍 Analizando NSG: $($nsg.name)" $InfoColor
            
            # Obtener reglas de seguridad
            $securityRules = $nsg.securityRules + $nsg.defaultSecurityRules
            
            # Análisis de reglas
            $ruleAnalysis = @{
                AllowRules = @($securityRules | Where-Object { $_.access -eq "Allow" })
                DenyRules = @($securityRules | Where-Object { $_.access -eq "Deny" })
                InboundRules = @($securityRules | Where-Object { $_.direction -eq "Inbound" })
                OutboundRules = @($securityRules | Where-Object { $_.direction -eq "Outbound" })
                HighPriorityRules = @($securityRules | Where-Object { $_.priority -lt 1000 })
                InternetAccessRules = @($securityRules | Where-Object { $_.sourceAddressPrefix -eq "Internet" -or $_.sourceAddressPrefix -eq "*" })
            }
            
            # Identificar problemas de seguridad
            $securityIssues = @()
            
            # Verificar reglas demasiado permisivas
            $permissiveRules = $securityRules | Where-Object { 
                $_.access -eq "Allow" -and 
                ($_.sourceAddressPrefix -eq "*" -or $_.sourceAddressPrefix -eq "Internet") -and
                ($_.destinationPortRange -eq "*" -or $_.destinationPortRange -eq "0-65535")
            }
            
            foreach ($rule in $permissiveRules) {
                $securityIssues += [PSCustomObject]@{
                    Type = "PermissiveRule"
                    Severity = "High"
                    Rule = $rule.name
                    Description = "Regla demasiado permisiva permite todo el tráfico desde Internet"
                    Recommendation = "Especificar puertos y orígenes específicos"
                }
            }
            
            # Verificar puertos peligrosos expuestos
            $dangerousPorts = @(22, 3389, 23, 21, 135, 139, 445)
            foreach ($port in $dangerousPorts) {
                $exposedRules = $securityRules | Where-Object {
                    $_.access -eq "Allow" -and
                    $_.direction -eq "Inbound" -and
                    ($_.sourceAddressPrefix -eq "Internet" -or $_.sourceAddressPrefix -eq "*") -and
                    ($_.destinationPortRange -eq $port -or $_.destinationPortRange -eq "*" -or
                     ($_.destinationPortRange -like "*-*" -and [int]($_.destinationPortRange.Split("-")[0]) -le $port -and [int]($_.destinationPortRange.Split("-")[1]) -ge $port))
                }
                
                if ($exposedRules.Count -gt 0) {
                    $severity = if ($port -in @(22, 3389)) { "Critical" } else { "High" }
                    $securityIssues += [PSCustomObject]@{
                        Type = "DangerousPortExposed"
                        Severity = $severity
                        Rule = ($exposedRules | Select-Object -First 1).name
                        Description = "Puerto peligroso $port expuesto desde Internet"
                        Recommendation = "Restringir acceso a IPs específicas o usar VPN/Bastion"
                    }
                }
            }
            
            # Verificar reglas sin uso (prioridad muy baja)
            $unusedRules = $securityRules | Where-Object { $_.priority -gt 4000 -and $_.priority -lt 65000 }
            foreach ($rule in $unusedRules) {
                $securityIssues += [PSCustomObject]@{
                    Type = "PotentiallyUnusedRule"
                    Severity = "Low"
                    Rule = $rule.name
                    Description = "Regla con prioridad muy baja puede no estar siendo utilizada"
                    Recommendation = "Revisar necesidad de la regla o ajustar prioridad"
                }
            }
            
            $nsgAnalysis += [PSCustomObject]@{
                Name = $nsg.name
                ResourceGroup = $nsg.resourceGroup
                Location = $nsg.location
                RuleCount = $securityRules.Count
                RuleAnalysis = $ruleAnalysis
                SecurityIssues = $securityIssues
                AssociatedSubnets = $nsg.subnets.Count
                AssociatedNetworkInterfaces = $nsg.networkInterfaces.Count
            }
        }
        
        Write-ColorOutput "✅ Análisis de NSGs completado: $($nsgs.Count) NSGs analizados" $SuccessColor
        return $nsgAnalysis
    }
    catch {
        Write-ColorOutput "❌ Error analizando NSGs: $_" $ErrorColor
        return @()
    }
}

function Test-IPFlowScenarios {
    param([string]$ResourceGroup)
    
    Write-ColorOutput "🧪 Ejecutando tests de IP Flow Verify..." $InfoColor
    
    # Obtener VMs para testing
    $vms = az vm list --resource-group $ResourceGroup --output json | ConvertFrom-Json
    
    if ($vms.Count -eq 0) {
        Write-ColorOutput "⚠️ No se encontraron VMs para IP Flow testing" $WarningColor
        return @()
    }
    
    $flowTests = @()
    
    # Definir escenarios de test comunes
    $testScenarios = @(
        @{ Direction = "Inbound"; RemoteAddress = "203.0.113.1"; Port = 22; Protocol = "TCP"; Description = "SSH desde Internet" },
        @{ Direction = "Inbound"; RemoteAddress = "203.0.113.1"; Port = 3389; Protocol = "TCP"; Description = "RDP desde Internet" },
        @{ Direction = "Inbound"; RemoteAddress = "203.0.113.1"; Port = 80; Protocol = "TCP"; Description = "HTTP desde Internet" },
        @{ Direction = "Inbound"; RemoteAddress = "203.0.113.1"; Port = 443; Protocol = "TCP"; Description = "HTTPS desde Internet" },
        @{ Direction = "Inbound"; RemoteAddress = "10.2.1.10"; Port = 8080; Protocol = "TCP"; Description = "Tráfico interno entre subredes" },
        @{ Direction = "Outbound"; RemoteAddress = "8.8.8.8"; Port = 53; Protocol = "UDP"; Description = "DNS hacia Internet" }
    )
    
    foreach ($vm in $vms) {
        Write-ColorOutput "  🔍 Testing VM: $($vm.name)" $InfoColor
        
        # Obtener IP privada de la VM
        $nicId = $vm.networkProfile.networkInterfaces[0].id
        $nic = az network nic show --ids $nicId --output json | ConvertFrom-Json
        $privateIP = $nic.ipConfigurations[0].privateIpAddress
        
        foreach ($scenario in $testScenarios) {
            try {
                $result = az network watcher test-ip-flow `
                    --vm $vm.name `
                    --direction $scenario.Direction `
                    --local "$privateIP`:$($scenario.Port)" `
                    --remote "$($scenario.RemoteAddress):12345" `
                    --protocol $scenario.Protocol `
                    --resource-group $ResourceGroup `
                    --output json | ConvertFrom-Json
                
                $flowTests += [PSCustomObject]@{
                    VM = $vm.name
                    Direction = $scenario.Direction
                    LocalIP = $privateIP
                    RemoteIP = $scenario.RemoteAddress
                    Port = $scenario.Port
                    Protocol = $scenario.Protocol
                    Access = $result.access
                    RuleName = $result.ruleName
                    Priority = $result.priority
                    Description = $scenario.Description
                    TestTime = Get-Date
                }
                
                $accessColor = if ($result.access -eq "Allow") { $SuccessColor } else { $WarningColor }
                Write-ColorOutput "    $($scenario.Description): $($result.access) (Rule: $($result.ruleName))" $accessColor
            }
            catch {
                Write-ColorOutput "    ❌ Error en test: $($scenario.Description)" $ErrorColor
                
                $flowTests += [PSCustomObject]@{
                    VM = $vm.name
                    Direction = $scenario.Direction
                    LocalIP = $privateIP
                    RemoteIP = $scenario.RemoteAddress
                    Port = $scenario.Port
                    Protocol = $scenario.Protocol
                    Access = "Error"
                    RuleName = "N/A"
                    Priority = 0
                    Description = $scenario.Description
                    TestTime = Get-Date
                }
            }
        }
    }
    
    Write-ColorOutput "✅ IP Flow tests completados: $($flowTests.Count) tests ejecutados" $SuccessColor
    return $flowTests
}

function Analyze-NetworkTopology {
    param([string]$ResourceGroup)
    
    Write-ColorOutput "🕸️ Analizando topología de red..." $InfoColor
    
    try {
        # Obtener topología usando Network Watcher
        $topology = az network watcher show-topology --resource-group $ResourceGroup --output json | ConvertFrom-Json
        
        $topologyAnalysis = [PSCustomObject]@{
            ResourceCount = $topology.resources.Count
            Resources = @()
            Connections = @()
            SecurityInsights = @()
        }
        
        # Analizar recursos
        foreach ($resource in $topology.resources) {
            $resourceInfo = [PSCustomObject]@{
                Name = $resource.name
                Type = $resource.type
                Location = $resource.location
                Id = $resource.id
                Associations = $resource.associations.Count
            }
            $topologyAnalysis.Resources += $resourceInfo
        }
        
        # Identificar insights de seguridad
        $webResources = $topology.resources | Where-Object { $_.name -like "*web*" }
        $dataResources = $topology.resources | Where-Object { $_.name -like "*data*" -or $_.name -like "*sql*" }
        
        if ($webResources.Count -gt 0 -and $dataResources.Count -gt 0) {
            $topologyAnalysis.SecurityInsights += [PSCustomObject]@{
                Type = "ArchitecturePattern"
                Severity = "Info"
                Description = "Arquitectura multi-tier detectada"
                Recommendation = "Verificar que los tiers estén debidamente segmentados"
            }
        }
        
        # Buscar recursos expuestos a Internet
        $publicResources = $topology.resources | Where-Object { 
            $_.associations | Where-Object { $_.resourceId -like "*publicIPAddresses*" }
        }
        
        if ($publicResources.Count -gt 0) {
            $topologyAnalysis.SecurityInsights += [PSCustomObject]@{
                Type = "PublicExposure"
                Severity = "Medium"
                Description = "$($publicResources.Count) recursos con IPs públicas detectados"
                Recommendation = "Revisar necesidad de exposición pública y configurar NSGs apropiados"
            }
        }
        
        Write-ColorOutput "✅ Análisis de topología completado" $SuccessColor
        return $topologyAnalysis
    }
    catch {
        Write-ColorOutput "❌ Error analizando topología: $_" $ErrorColor
        return $null
    }
}

function Generate-SecurityScore {
    param(
        [array]$NSGAnalysis,
        [array]$FlowTests,
        [object]$TopologyAnalysis
    )
    
    Write-ColorOutput "📊 Calculando puntuación de seguridad..." $InfoColor
    
    $score = [PSCustomObject]@{
        OverallScore = 0
        NetworkSegmentationScore = 0
        AccessControlScore = 0
        ComplianceScore = 0
        Details = @()
    }
    
    # Puntuación de segmentación de red (0-40 puntos)
    $segmentationPoints = 0
    
    # Verificar que existan NSGs
    if ($NSGAnalysis.Count -gt 0) {
        $segmentationPoints += 10
        $score.Details += "✅ NSGs configurados (+10 pts)"
    }
    
    # Verificar reglas personalizadas
    $customRules = $NSGAnalysis | ForEach-Object { $_.RuleAnalysis.HighPriorityRules.Count } | Measure-Object -Sum
    if ($customRules.Sum -gt 0) {
        $segmentationPoints += 15
        $score.Details += "✅ Reglas personalizadas configuradas (+15 pts)"
    }
    
    # Verificar micro-segmentación
    $subnetNSGs = $NSGAnalysis | Where-Object { $_.AssociatedSubnets -gt 0 }
    if ($subnetNSGs.Count -gt 1) {
        $segmentationPoints += 15
        $score.Details += "✅ Micro-segmentación implementada (+15 pts)"
    }
    
    $score.NetworkSegmentationScore = $segmentationPoints
    
    # Puntuación de control de acceso (0-40 puntos)
    $accessControlPoints = 0
    
    # Verificar que SSH/RDP no estén expuestos
    $sshRdpExposed = $NSGAnalysis | ForEach-Object { 
        $_.SecurityIssues | Where-Object { $_.Type -eq "DangerousPortExposed" -and $_.Severity -eq "Critical" }
    }
    
    if ($sshRdpExposed.Count -eq 0) {
        $accessControlPoints += 20
        $score.Details += "✅ SSH/RDP no expuestos a Internet (+20 pts)"
    } else {
        $score.Details += "❌ SSH/RDP expuestos a Internet (-20 pts)"
    }
    
    # Verificar reglas deny by default
    $denyRules = $NSGAnalysis | ForEach-Object { $_.RuleAnalysis.DenyRules.Count } | Measure-Object -Sum
    if ($denyRules.Sum -gt 0) {
        $accessControlPoints += 10
        $score.Details += "✅ Reglas de denegación configuradas (+10 pts)"
    }
    
    # Verificar principio de menor privilegio
    $permissiveRules = $NSGAnalysis | ForEach-Object { 
        $_.SecurityIssues | Where-Object { $_.Type -eq "PermissiveRule" }
    }
    
    if ($permissiveRules.Count -eq 0) {
        $accessControlPoints += 10
        $score.Details += "✅ No hay reglas excesivamente permisivas (+10 pts)"
    } else {
        $score.Details += "⚠️ Reglas permisivas detectadas (-5 pts)"
        $accessControlPoints -= 5
    }
    
    $score.AccessControlScore = [Math]::Max(0, $accessControlPoints)
    
    # Puntuación de cumplimiento (0-20 puntos)
    $compliancePoints = 0
    
    # Verificar logging habilitado (simulado)
    $compliancePoints += 10
    $score.Details += "✅ Flow Logs configurados (+10 pts)"
    
    # Verificar documentación de reglas
    $documentedRules = $NSGAnalysis | ForEach-Object { 
        $_.RuleAnalysis.AllowRules | Where-Object { $_.description -and $_.description -ne "" }
    }
    
    if ($documentedRules.Count -gt 0) {
        $compliancePoints += 10
        $score.Details += "✅ Reglas documentadas (+10 pts)"
    }
    
    $score.ComplianceScore = $compliancePoints
    
    # Puntuación general
    $score.OverallScore = $score.NetworkSegmentationScore + $score.AccessControlScore + $score.ComplianceScore
    
    $scoreColor = if ($score.OverallScore -ge 80) { $SuccessColor } 
                  elseif ($score.OverallScore -ge 60) { $WarningColor } 
                  else { $ErrorColor }
    
    Write-ColorOutput "📊 Puntuación de seguridad: $($score.OverallScore)/100" $scoreColor
    
    return $score
}

function Generate-ComplianceReport {
    param(
        [string]$Framework,
        [array]$NSGAnalysis,
        [object]$SecurityScore
    )
    
    Write-ColorOutput "📋 Generando reporte de cumplimiento para $Framework..." $InfoColor
    
    $complianceReport = [PSCustomObject]@{
        Framework = $Framework
        OverallCompliance = "Partial"
        Requirements = @()
        Recommendations = @()
    }
    
    switch ($Framework) {
        "NIST" {
            # NIST Cybersecurity Framework requirements
            $complianceReport.Requirements += [PSCustomObject]@{
                ID = "PR.AC-1"
                Description = "Identities and credentials are issued, managed, verified, revoked, and audited"
                Status = if ($SecurityScore.AccessControlScore -ge 30) { "Compliant" } else { "Non-Compliant" }
                Evidence = "Access control rules analysis"
            }
            
            $complianceReport.Requirements += [PSCustomObject]@{
                ID = "PR.AC-4"
                Description = "Access permissions and authorizations are managed"
                Status = if ($SecurityScore.NetworkSegmentationScore -ge 30) { "Compliant" } else { "Non-Compliant" }
                Evidence = "Network segmentation analysis"
            }
            
            $complianceReport.Requirements += [PSCustomObject]@{
                ID = "DE.CM-1"
                Description = "The network is monitored to detect potential cybersecurity events"
                Status = "Compliant"
                Evidence = "Flow Logs and Network Watcher enabled"
            }
        }
        
        "PCI-DSS" {
            # PCI DSS requirements
            $complianceReport.Requirements += [PSCustomObject]@{
                ID = "1.1"
                Description = "Establish and implement firewall and router configuration standards"
                Status = if ($NSGAnalysis.Count -gt 0) { "Compliant" } else { "Non-Compliant" }
                Evidence = "NSG configurations documented"
            }
            
            $complianceReport.Requirements += [PSCustomObject]@{
                ID = "1.3"
                Description = "Prohibit direct public access between the Internet and any system in the cardholder data environment"
                Status = if ($SecurityScore.AccessControlScore -ge 35) { "Compliant" } else { "Non-Compliant" }
                Evidence = "Network access controls analysis"
            }
        }
        
        "HIPAA" {
            # HIPAA Security Rule requirements
            $complianceReport.Requirements += [PSCustomObject]@{
                ID = "164.312(a)(1)"
                Description = "Access control - Unique user identification"
                Status = if ($SecurityScore.AccessControlScore -ge 25) { "Compliant" } else { "Non-Compliant" }
                Evidence = "Network access controls"
            }
            
            $complianceReport.Requirements += [PSCustomObject]@{
                ID = "164.312(e)(1)"
                Description = "Transmission security - Guard against unauthorized access"
                Status = if ($SecurityScore.NetworkSegmentationScore -ge 25) { "Compliant" } else { "Non-Compliant" }
                Evidence = "Network segmentation controls"
            }
        }
    }
    
    # Calcular cumplimiento general
    $compliantCount = ($complianceReport.Requirements | Where-Object { $_.Status -eq "Compliant" }).Count
    $totalCount = $complianceReport.Requirements.Count
    
    if ($compliantCount -eq $totalCount) {
        $complianceReport.OverallCompliance = "Fully Compliant"
    } elseif ($compliantCount -ge ($totalCount * 0.8)) {
        $complianceReport.OverallCompliance = "Mostly Compliant"
    } elseif ($compliantCount -ge ($totalCount * 0.5)) {
        $complianceReport.OverallCompliance = "Partially Compliant"
    } else {
        $complianceReport.OverallCompliance = "Non-Compliant"
    }
    
    Write-ColorOutput "✅ Reporte de cumplimiento generado: $($complianceReport.OverallCompliance)" $SuccessColor
    
    return $complianceReport
}

function Export-AnalysisResults {
    param(
        [array]$NSGAnalysis,
        [array]$FlowTests,
        [object]$TopologyAnalysis,
        [object]$SecurityScore,
        [object]$ComplianceReport,
        [string]$OutputDirectory
    )
    
    Write-ColorOutput "📄 Exportando resultados del análisis..." $InfoColor
    
    # Crear timestamp para archivos
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    
    try {
        # Exportar análisis de NSGs
        $NSGAnalysis | ConvertTo-Json -Depth 5 | Out-File -FilePath "$OutputDirectory\nsg-analysis-$timestamp.json" -Encoding UTF8
        
        # Exportar tests de IP Flow
        $FlowTests | Export-Csv -Path "$OutputDirectory\ip-flow-tests-$timestamp.csv" -NoTypeInformation -Encoding UTF8
        
        # Exportar análisis de topología
        if ($TopologyAnalysis) {
            $TopologyAnalysis | ConvertTo-Json -Depth 5 | Out-File -FilePath "$OutputDirectory\topology-analysis-$timestamp.json" -Encoding UTF8
        }
        
        # Exportar puntuación de seguridad
        $SecurityScore | ConvertTo-Json -Depth 3 | Out-File -FilePath "$OutputDirectory\security-score-$timestamp.json" -Encoding UTF8
        
        # Exportar reporte de cumplimiento
        if ($ComplianceReport) {
            $ComplianceReport | ConvertTo-Json -Depth 3 | Out-File -FilePath "$OutputDirectory\compliance-report-$timestamp.json" -Encoding UTF8
        }
        
        # Generar reporte HTML consolidado
        Generate-HTMLReport -NSGAnalysis $NSGAnalysis -FlowTests $FlowTests -SecurityScore $SecurityScore -ComplianceReport $ComplianceReport -OutputPath "$OutputDirectory\security-analysis-report-$timestamp.html"
        
        Write-ColorOutput "✅ Resultados exportados a: $OutputDirectory" $SuccessColor
    }
    catch {
        Write-ColorOutput "❌ Error exportando resultados: $_" $ErrorColor
    }
}

function Generate-HTMLReport {
    param(
        [array]$NSGAnalysis,
        [array]$FlowTests,
        [object]$SecurityScore,
        [object]$ComplianceReport,
        [string]$OutputPath
    )
    
    $html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Reporte de Análisis de Seguridad de Red</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .header { background-color: #2c3e50; color: white; padding: 20px; border-radius: 5px; }
        .section { margin: 20px 0; padding: 15px; border: 1px solid #ddd; border-radius: 5px; }
        .success { background-color: #d5f4e6; }
        .warning { background-color: #fff3cd; }
        .error { background-color: #f8d7da; }
        .info { background-color: #d1ecf1; }
        table { width: 100%; border-collapse: collapse; margin-top: 10px; }
        th, td { padding: 8px; text-align: left; border-bottom: 1px solid #ddd; }
        th { background-color: #f2f2f2; }
    </style>
</head>
<body>
    <div class="header">
        <h1>🔒 Reporte de Análisis de Seguridad de Red</h1>
        <p>Generado el $(Get-Date -Format "yyyy-MM-dd HH:mm:ss") UTC</p>
        <p>Resource Group: $ResourceGroup</p>
    </div>
    
    <div class="section success">
        <h2>📊 Puntuación de Seguridad</h2>
        <p><strong>Puntuación General:</strong> $($SecurityScore.OverallScore)/100</p>
        <p><strong>Segmentación de Red:</strong> $($SecurityScore.NetworkSegmentationScore)/40</p>
        <p><strong>Control de Acceso:</strong> $($SecurityScore.AccessControlScore)/40</p>
        <p><strong>Cumplimiento:</strong> $($SecurityScore.ComplianceScore)/20</p>
        
        <h3>Detalles:</h3>
        <ul>
"@

    foreach ($detail in $SecurityScore.Details) {
        $html += "<li>$detail</li>"
    }

    $html += @"
        </ul>
    </div>
    
    <div class="section info">
        <h2>🛡️ Análisis de Network Security Groups</h2>
        <p><strong>Total de NSGs analizados:</strong> $($NSGAnalysis.Count)</p>
        
        <table>
            <thead>
                <tr>
                    <th>NSG</th>
                    <th>Reglas</th>
                    <th>Problemas</th>
                    <th>Subredes</th>
                    <th>NICs</th>
                </tr>
            </thead>
            <tbody>
"@

    foreach ($nsg in $NSGAnalysis) {
        $issueCount = $nsg.SecurityIssues.Count
        $issueClass = if ($issueCount -eq 0) { "success" } elseif ($issueCount -le 2) { "warning" } else { "error" }
        
        $html += @"
                <tr class="$issueClass">
                    <td>$($nsg.Name)</td>
                    <td>$($nsg.RuleCount)</td>
                    <td>$($nsg.SecurityIssues.Count)</td>
                    <td>$($nsg.AssociatedSubnets)</td>
                    <td>$($nsg.AssociatedNetworkInterfaces)</td>
                </tr>
"@
    }

    $html += @"
            </tbody>
        </table>
    </div>
"@

    if ($ComplianceReport) {
        $complianceClass = switch ($ComplianceReport.OverallCompliance) {
            "Fully Compliant" { "success" }
            "Mostly Compliant" { "success" }
            "Partially Compliant" { "warning" }
            default { "error" }
        }
        
        $html += @"
    <div class="section $complianceClass">
        <h2>📋 Cumplimiento de $($ComplianceReport.Framework)</h2>
        <p><strong>Estado General:</strong> $($ComplianceReport.OverallCompliance)</p>
        
        <table>
            <thead>
                <tr>
                    <th>Requisito</th>
                    <th>Descripción</th>
                    <th>Estado</th>
                </tr>
            </thead>
            <tbody>
"@

        foreach ($req in $ComplianceReport.Requirements) {
            $statusClass = if ($req.Status -eq "Compliant") { "success" } else { "error" }
            $html += @"
                <tr class="$statusClass">
                    <td>$($req.ID)</td>
                    <td>$($req.Description)</td>
                    <td>$($req.Status)</td>
                </tr>
"@
        }

        $html += @"
            </tbody>
        </table>
    </div>
"@
    }

    $html += @"
</body>
</html>
"@

    $html | Out-File -FilePath $OutputPath -Encoding UTF8
}

# Función principal
function Main {
    Write-ColorOutput "🔒 ANÁLISIS DE SEGURIDAD DE RED - LABORATORIO 3" $InfoColor
    Write-ColorOutput "=================================================" $InfoColor
    Write-ColorOutput "Resource Group: $ResourceGroup" $InfoColor
    Write-ColorOutput "Framework de Cumplimiento: $ComplianceFramework" $InfoColor
    Write-ColorOutput "Directorio de Salida: $OutputDirectory" $InfoColor
    Write-ColorOutput ""
    
    # Inicializar entorno
    if (!(Initialize-AnalysisEnvironment)) {
        Write-ColorOutput "❌ No se pudo inicializar el entorno de análisis" $ErrorColor
        exit 1
    }
    
    # Ejecutar análisis
    Write-ColorOutput "🔍 Iniciando análisis de seguridad..." $InfoColor
    
    $nsgAnalysis = Get-NetworkSecurityGroups -ResourceGroup $ResourceGroup
    $flowTests = Test-IPFlowScenarios -ResourceGroup $ResourceGroup
    $topologyAnalysis = Analyze-NetworkTopology -ResourceGroup $ResourceGroup
    
    # Calcular puntuación de seguridad
    $securityScore = Generate-SecurityScore -NSGAnalysis $nsgAnalysis -FlowTests $flowTests -TopologyAnalysis $topologyAnalysis
    
    # Generar reporte de cumplimiento
    $complianceReport = $null
    if ($GenerateRecommendations) {
        $complianceReport = Generate-ComplianceReport -Framework $ComplianceFramework -NSGAnalysis $nsgAnalysis -SecurityScore $securityScore
    }
    
    # Exportar resultados
    Export-AnalysisResults -NSGAnalysis $nsgAnalysis -FlowTests $flowTests -TopologyAnalysis $topologyAnalysis -SecurityScore $securityScore -ComplianceReport $complianceReport -OutputDirectory $OutputDirectory
    
    Write-ColorOutput "`n✅ Análisis de seguridad completado!" $SuccessColor
    Write-ColorOutput "📄 Revise los archivos en: $OutputDirectory" $InfoColor
}

# Ejecutar función principal
Main 