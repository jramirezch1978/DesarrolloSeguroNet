# Script: script-monitor-flow-logs.ps1
# Propósito: Monitoreo y análisis en tiempo real de NSG Flow Logs
# Autor: Laboratorio 3 - Testing y Simulación de Conectividad

param(
    [Parameter(Mandatory=$true)]
    [string]$StorageAccountName,
    
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroup,
    
    [Parameter(Mandatory=$false)]
    [string]$ContainerName = "insights-logs-networksecuritygroupflowevent",
    
    [Parameter(Mandatory=$false)]
    [int]$AnalysisHours = 24,
    
    [Parameter(Mandatory=$false)]
    [switch]$RealTimeMonitoring,
    
    [Parameter(Mandatory=$false)]
    [int]$RefreshIntervalSeconds = 300,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputDirectory = "flow-analysis",
    
    [Parameter(Mandatory=$false)]
    [switch]$DetectAnomalies,
    
    [Parameter(Mandatory=$false)]
    [int]$SuspiciousThreshold = 50
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

function Initialize-FlowLogEnvironment {
    Write-ColorOutput "🔧 Inicializando entorno de análisis de Flow Logs..." $InfoColor
    
    # Crear directorio de salida
    if (!(Test-Path $OutputDirectory)) {
        New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
        Write-ColorOutput "📁 Directorio creado: $OutputDirectory" $SuccessColor
    }
    
    # Verificar acceso a Storage Account
    try {
        $storageAccount = az storage account show --name $StorageAccountName --output json | ConvertFrom-Json
        Write-ColorOutput "✅ Storage Account accesible: $($storageAccount.name)" $SuccessColor
        
        # Verificar que el contenedor existe
        $containers = az storage container list --account-name $StorageAccountName --output json | ConvertFrom-Json
        $targetContainer = $containers | Where-Object { $_.name -eq $ContainerName }
        
        if ($targetContainer) {
            Write-ColorOutput "✅ Contenedor de Flow Logs encontrado: $ContainerName" $SuccessColor
        } else {
            Write-ColorOutput "⚠️ Contenedor $ContainerName no encontrado" $WarningColor
            Write-ColorOutput "Contenedores disponibles:" $InfoColor
            $containers | ForEach-Object { Write-ColorOutput "  - $($_.name)" $InfoColor }
        }
        
        return $true
    }
    catch {
        Write-ColorOutput "❌ Error accediendo Storage Account $StorageAccountName : $_" $ErrorColor
        return $false
    }
}

function Get-FlowLogBlobs {
    param(
        [string]$StorageAccount,
        [string]$Container,
        [datetime]$StartTime,
        [datetime]$EndTime
    )
    
    Write-ColorOutput "📋 Obteniendo Flow Log blobs desde $StartTime hasta $EndTime..." $InfoColor
    
    try {
        # Listar blobs en el contenedor
        $blobs = az storage blob list --account-name $StorageAccount --container-name $Container --output json | ConvertFrom-Json
        
        # Filtrar blobs por fecha
        $filteredBlobs = $blobs | Where-Object {
            $blobDate = [datetime]::ParseExact($_.properties.lastModified.Substring(0, 19), "yyyy-MM-ddTHH:mm:ss", $null)
            $blobDate -ge $StartTime -and $blobDate -le $EndTime
        }
        
        Write-ColorOutput "✅ Encontrados $($filteredBlobs.Count) blobs en el rango de tiempo especificado" $SuccessColor
        return $filteredBlobs
    }
    catch {
        Write-ColorOutput "❌ Error obteniendo blobs: $_" $ErrorColor
        return @()
    }
}

function Download-FlowLogBlob {
    param(
        [string]$StorageAccount,
        [string]$Container,
        [string]$BlobName,
        [string]$LocalPath
    )
    
    try {
        az storage blob download --account-name $StorageAccount --container-name $Container --name $BlobName --file $LocalPath --output none
        return $true
    }
    catch {
        Write-ColorOutput "❌ Error descargando blob $BlobName : $_" $ErrorColor
        return $false
    }
}

function Parse-FlowLogFile {
    param([string]$FilePath)
    
    try {
        $content = Get-Content $FilePath -Raw | ConvertFrom-Json
        $flows = @()
        
        foreach ($record in $content.records) {
            if ($record.properties -and $record.properties.flows) {
                foreach ($flow in $record.properties.flows) {
                    foreach ($flowGroup in $flow.flows) {
                        foreach ($tuple in $flowGroup.flowTuples) {
                            $parts = $tuple.Split(',')
                            if ($parts.Length -ge 7) {
                                $flowEntry = [PSCustomObject]@{
                                    TimeStamp = [DateTimeOffset]::FromUnixTimeSeconds([long]$parts[0]).DateTime
                                    SourceIP = $parts[1]
                                    DestIP = $parts[2]
                                    SourcePort = [int]$parts[3]
                                    DestPort = [int]$parts[4]
                                    Protocol = if ($parts[5] -eq "T") { "TCP" } elseif ($parts[5] -eq "U") { "UDP" } else { $parts[5] }
                                    Direction = if ($parts[6] -eq "I") { "Inbound" } elseif ($parts[6] -eq "O") { "Outbound" } else { $parts[6] }
                                    Decision = if ($parts[7] -eq "A") { "Allow" } elseif ($parts[7] -eq "D") { "Deny" } else { $parts[7] }
                                    FlowState = if ($parts.Length -gt 8) { $parts[8] } else { "" }
                                    PacketsToSource = if ($parts.Length -gt 9) { [long]$parts[9] } else { 0 }
                                    BytesToSource = if ($parts.Length -gt 10) { [long]$parts[10] } else { 0 }
                                    PacketsToDest = if ($parts.Length -gt 11) { [long]$parts[11] } else { 0 }
                                    BytesToDest = if ($parts.Length -gt 12) { [long]$parts[12] } else { 0 }
                                    Rule = $flow.rule
                                }
                                $flows += $flowEntry
                            }
                        }
                    }
                }
            }
        }
        
        return $flows
    }
    catch {
        Write-ColorOutput "❌ Error parsing flow log file $FilePath : $_" $ErrorColor
        return @()
    }
}

function Analyze-FlowData {
    param([array]$Flows)
    
    Write-ColorOutput "📊 Analizando datos de Flow Logs..." $InfoColor
    
    $analysis = [PSCustomObject]@{
        TotalFlows = $Flows.Count
        AllowedFlows = ($Flows | Where-Object { $_.Decision -eq "Allow" }).Count
        DeniedFlows = ($Flows | Where-Object { $_.Decision -eq "Deny" }).Count
        InboundFlows = ($Flows | Where-Object { $_.Direction -eq "Inbound" }).Count
        OutboundFlows = ($Flows | Where-Object { $_.Direction -eq "Outbound" }).Count
        TopSourceIPs = @()
        TopDestIPs = @()
        TopPorts = @()
        ProtocolDistribution = @()
        SuspiciousActivities = @()
        TrafficTrends = @()
    }
    
    if ($Flows.Count -eq 0) {
        Write-ColorOutput "⚠️ No se encontraron flows para analizar" $WarningColor
        return $analysis
    }
    
    # Top Source IPs
    $analysis.TopSourceIPs = $Flows | Group-Object SourceIP | 
        Sort-Object Count -Descending | 
        Select-Object -First 10 | 
        ForEach-Object { 
            [PSCustomObject]@{
                IP = $_.Name
                FlowCount = $_.Count
                UniqueDestinations = ($_.Group | Select-Object DestIP -Unique).Count
                TotalBytes = ($_.Group | Measure-Object BytesToDest -Sum).Sum
            }
        }
    
    # Top Destination IPs
    $analysis.TopDestIPs = $Flows | Group-Object DestIP | 
        Sort-Object Count -Descending | 
        Select-Object -First 10 | 
        ForEach-Object { 
            [PSCustomObject]@{
                IP = $_.Name
                FlowCount = $_.Count
                UniqueSources = ($_.Group | Select-Object SourceIP -Unique).Count
                TotalBytes = ($_.Group | Measure-Object BytesToDest -Sum).Sum
            }
        }
    
    # Top Destination Ports
    $analysis.TopPorts = $Flows | Group-Object DestPort | 
        Sort-Object Count -Descending | 
        Select-Object -First 15 | 
        ForEach-Object { 
            [PSCustomObject]@{
                Port = [int]$_.Name
                FlowCount = $_.Count
                Protocol = ($_.Group | Select-Object Protocol -Unique | Select-Object -First 1).Protocol
                ServiceName = Get-PortServiceName -Port ([int]$_.Name)
            }
        }
    
    # Distribución de protocolos
    $analysis.ProtocolDistribution = $Flows | Group-Object Protocol | 
        ForEach-Object { 
            [PSCustomObject]@{
                Protocol = $_.Name
                FlowCount = $_.Count
                Percentage = [math]::Round(($_.Count * 100) / $Flows.Count, 2)
                TotalBytes = ($_.Group | Measure-Object BytesToDest -Sum).Sum
            }
        }
    
    Write-ColorOutput "✅ Análisis básico completado" $SuccessColor
    Write-ColorOutput "  Total flows: $($analysis.TotalFlows)" $InfoColor
    Write-ColorOutput "  Allowed: $($analysis.AllowedFlows) ($([math]::Round(($analysis.AllowedFlows * 100) / $analysis.TotalFlows, 1))%)" $SuccessColor
    Write-ColorOutput "  Denied: $($analysis.DeniedFlows) ($([math]::Round(($analysis.DeniedFlows * 100) / $analysis.TotalFlows, 1))%)" $WarningColor
    
    return $analysis
}

function Detect-SuspiciousActivities {
    param([array]$Flows, [int]$Threshold = 50)
    
    Write-ColorOutput "🔍 Detectando actividades sospechosas..." $InfoColor
    
    $suspiciousActivities = @()
    
    # Detectar port scanning
    $portScanners = $Flows | Group-Object SourceIP | Where-Object { 
        ($_.Group | Select-Object DestPort -Unique).Count -gt $Threshold 
    }
    
    foreach ($scanner in $portScanners) {
        $uniquePorts = ($scanner.Group | Select-Object DestPort -Unique).Count
        $deniedConnections = ($scanner.Group | Where-Object { $_.Decision -eq "Deny" }).Count
        
        $suspiciousActivities += [PSCustomObject]@{
            Type = "PortScanning"
            Severity = if ($uniquePorts -gt 100) { "High" } else { "Medium" }
            SourceIP = $scanner.Name
            Description = "Potential port scan detected - $uniquePorts unique ports accessed"
            Evidence = "Unique ports: $uniquePorts, Denied connections: $deniedConnections"
            Recommendation = "Investigate source IP and consider blocking if malicious"
            FirstSeen = ($scanner.Group | Sort-Object TimeStamp | Select-Object -First 1).TimeStamp
            LastSeen = ($scanner.Group | Sort-Object TimeStamp | Select-Object -Last 1).TimeStamp
            FlowCount = $scanner.Count
        }
    }
    
    # Detectar brute force attacks
    $bruteForceTargets = $Flows | Where-Object { $_.Decision -eq "Deny" } | 
        Group-Object SourceIP, DestIP, DestPort | 
        Where-Object { $_.Count -gt ($Threshold / 2) }
    
    foreach ($target in $bruteForceTargets) {
        $nameParts = $target.Name.Split(', ')
        $suspiciousActivities += [PSCustomObject]@{
            Type = "BruteForce"
            Severity = "High"
            SourceIP = $nameParts[0]
            Description = "Potential brute force attack detected"
            Evidence = "Failed attempts: $($target.Count) to $($nameParts[1]):$($nameParts[2])"
            Recommendation = "Block source IP and review authentication logs"
            FirstSeen = ($target.Group | Sort-Object TimeStamp | Select-Object -First 1).TimeStamp
            LastSeen = ($target.Group | Sort-Object TimeStamp | Select-Object -Last 1).TimeStamp
            FlowCount = $target.Count
        }
    }
    
    # Detectar data exfiltration (alto volumen de datos salientes)
    $highVolumeOutbound = $Flows | Where-Object { $_.Direction -eq "Outbound" -and $_.BytesToDest -gt 1000000 } | 
        Group-Object SourceIP | 
        Where-Object { ($_.Group | Measure-Object BytesToDest -Sum).Sum -gt 100000000 } # 100MB threshold
    
    foreach ($source in $highVolumeOutbound) {
        $totalBytes = ($source.Group | Measure-Object BytesToDest -Sum).Sum
        $suspiciousActivities += [PSCustomObject]@{
            Type = "DataExfiltration"
            Severity = "Critical"
            SourceIP = $source.Name
            Description = "High volume outbound traffic detected"
            Evidence = "Total bytes transferred: $([math]::Round($totalBytes / 1048576, 2)) MB"
            Recommendation = "Investigate data transfers and verify legitimacy"
            FirstSeen = ($source.Group | Sort-Object TimeStamp | Select-Object -First 1).TimeStamp
            LastSeen = ($source.Group | Sort-Object TimeStamp | Select-Object -Last 1).TimeStamp
            FlowCount = $source.Count
        }
    }
    
    # Detectar comunicación con IPs sospechosas (rangos privados desde Internet)
    $suspiciousIPs = $Flows | Where-Object { 
        $_.Direction -eq "Inbound" -and 
        ($_.SourceIP -match "^192\.168\." -or $_.SourceIP -match "^10\." -or $_.SourceIP -match "^172\.(1[6-9]|2[0-9]|3[01])\.")
    }
    
    if ($suspiciousIPs.Count -gt 0) {
        $uniqueSuspiciousIPs = ($suspiciousIPs | Select-Object SourceIP -Unique).Count
        $suspiciousActivities += [PSCustomObject]@{
            Type = "SpoofedPrivateIP"
            Severity = "Medium"
            SourceIP = "Multiple"
            Description = "Traffic from private IP ranges detected as inbound"
            Evidence = "Unique private IPs: $uniqueSuspiciousIPs, Total flows: $($suspiciousIPs.Count)"
            Recommendation = "Review network configuration and firewall rules"
            FirstSeen = ($suspiciousIPs | Sort-Object TimeStamp | Select-Object -First 1).TimeStamp
            LastSeen = ($suspiciousIPs | Sort-Object TimeStamp | Select-Object -Last 1).TimeStamp
            FlowCount = $suspiciousIPs.Count
        }
    }
    
    Write-ColorOutput "✅ Detección de anomalías completada: $($suspiciousActivities.Count) actividades sospechosas encontradas" $SuccessColor
    
    foreach ($activity in $suspiciousActivities) {
        $severityColor = switch ($activity.Severity) {
            "Critical" { $CriticalColor }
            "High" { $ErrorColor }
            "Medium" { $WarningColor }
            default { $InfoColor }
        }
        Write-ColorOutput "  🚨 $($activity.Type) - $($activity.Severity): $($activity.Description)" $severityColor
    }
    
    return $suspiciousActivities
}

function Get-PortServiceName {
    param([int]$Port)
    
    $commonPorts = @{
        20 = "FTP-Data"
        21 = "FTP"
        22 = "SSH"
        23 = "Telnet"
        25 = "SMTP"
        53 = "DNS"
        80 = "HTTP"
        110 = "POP3"
        143 = "IMAP"
        443 = "HTTPS"
        993 = "IMAPS"
        995 = "POP3S"
        1433 = "SQL Server"
        3306 = "MySQL"
        3389 = "RDP"
        5432 = "PostgreSQL"
        8080 = "HTTP-Alt"
        8443 = "HTTPS-Alt"
    }
    
    return if ($commonPorts.ContainsKey($Port)) { $commonPorts[$Port] } else { "Unknown" }
}

function Generate-FlowLogReport {
    param(
        [object]$Analysis,
        [array]$SuspiciousActivities,
        [string]$OutputPath
    )
    
    Write-ColorOutput "📄 Generando reporte HTML..." $InfoColor
    
    $html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Reporte de Análisis de Flow Logs</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .header { background-color: #2c3e50; color: white; padding: 20px; border-radius: 5px; }
        .section { margin: 20px 0; padding: 15px; border: 1px solid #ddd; border-radius: 5px; }
        .success { background-color: #d5f4e6; }
        .warning { background-color: #fff3cd; }
        .error { background-color: #f8d7da; }
        .critical { background-color: #f1c0c7; }
        .info { background-color: #d1ecf1; }
        table { width: 100%; border-collapse: collapse; margin-top: 10px; }
        th, td { padding: 8px; text-align: left; border-bottom: 1px solid #ddd; }
        th { background-color: #f2f2f2; }
        .chart { margin: 10px 0; }
    </style>
</head>
<body>
    <div class="header">
        <h1>📊 Reporte de Análisis de Flow Logs</h1>
        <p>Generado el $(Get-Date -Format "yyyy-MM-dd HH:mm:ss") UTC</p>
        <p>Storage Account: $StorageAccountName</p>
        <p>Período analizado: Últimas $AnalysisHours horas</p>
    </div>
    
    <div class="section info">
        <h2>📈 Estadísticas Generales</h2>
        <p><strong>Total de flows:</strong> $($Analysis.TotalFlows)</p>
        <p><strong>Flows permitidos:</strong> $($Analysis.AllowedFlows) ($([math]::Round(($Analysis.AllowedFlows * 100) / $Analysis.TotalFlows, 1))%)</p>
        <p><strong>Flows denegados:</strong> $($Analysis.DeniedFlows) ($([math]::Round(($Analysis.DeniedFlows * 100) / $Analysis.TotalFlows, 1))%)</p>
        <p><strong>Tráfico entrante:</strong> $($Analysis.InboundFlows)</p>
        <p><strong>Tráfico saliente:</strong> $($Analysis.OutboundFlows)</p>
    </div>
    
    <div class="section">
        <h2>🌐 Top Source IPs</h2>
        <table>
            <thead>
                <tr>
                    <th>IP Address</th>
                    <th>Flow Count</th>
                    <th>Unique Destinations</th>
                    <th>Total Bytes</th>
                </tr>
            </thead>
            <tbody>
"@

    foreach ($sourceIP in $Analysis.TopSourceIPs) {
        $html += @"
                <tr>
                    <td>$($sourceIP.IP)</td>
                    <td>$($sourceIP.FlowCount)</td>
                    <td>$($sourceIP.UniqueDestinations)</td>
                    <td>$([math]::Round($sourceIP.TotalBytes / 1048576, 2)) MB</td>
                </tr>
"@
    }

    $html += @"
            </tbody>
        </table>
    </div>
    
    <div class="section">
        <h2>🔌 Top Destination Ports</h2>
        <table>
            <thead>
                <tr>
                    <th>Port</th>
                    <th>Service</th>
                    <th>Protocol</th>
                    <th>Flow Count</th>
                </tr>
            </thead>
            <tbody>
"@

    foreach ($port in $Analysis.TopPorts) {
        $html += @"
                <tr>
                    <td>$($port.Port)</td>
                    <td>$($port.ServiceName)</td>
                    <td>$($port.Protocol)</td>
                    <td>$($port.FlowCount)</td>
                </tr>
"@
    }

    $html += @"
            </tbody>
        </table>
    </div>
"@

    if ($SuspiciousActivities.Count -gt 0) {
        $html += @"
    <div class="section error">
        <h2>🚨 Actividades Sospechosas</h2>
        <table>
            <thead>
                <tr>
                    <th>Tipo</th>
                    <th>Severidad</th>
                    <th>IP Origen</th>
                    <th>Descripción</th>
                    <th>Recomendación</th>
                </tr>
            </thead>
            <tbody>
"@

        foreach ($activity in $SuspiciousActivities) {
            $severityClass = switch ($activity.Severity) {
                "Critical" { "critical" }
                "High" { "error" }
                "Medium" { "warning" }
                default { "info" }
            }
            
            $html += @"
                <tr class="$severityClass">
                    <td>$($activity.Type)</td>
                    <td>$($activity.Severity)</td>
                    <td>$($activity.SourceIP)</td>
                    <td>$($activity.Description)</td>
                    <td>$($activity.Recommendation)</td>
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
    Write-ColorOutput "✅ Reporte HTML generado: $OutputPath" $SuccessColor
}

function Start-RealTimeMonitoring {
    param(
        [string]$StorageAccount,
        [string]$Container,
        [int]$RefreshInterval
    )
    
    Write-ColorOutput "🔄 Iniciando monitoreo en tiempo real..." $InfoColor
    Write-ColorOutput "Presione Ctrl+C para detener el monitoreo" $WarningColor
    
    $lastProcessedTime = (Get-Date).AddMinutes(-5)
    
    while ($true) {
        try {
            $currentTime = Get-Date
            
            # Obtener nuevos blobs
            $newBlobs = Get-FlowLogBlobs -StorageAccount $StorageAccount -Container $Container -StartTime $lastProcessedTime -EndTime $currentTime
            
            if ($newBlobs.Count -gt 0) {
                Write-ColorOutput "📥 Procesando $($newBlobs.Count) nuevos blobs..." $InfoColor
                
                $allFlows = @()
                foreach ($blob in $newBlobs) {
                    $tempFile = Join-Path $env:TEMP "flowlog_$([guid]::NewGuid().ToString()).json"
                    if (Download-FlowLogBlob -StorageAccount $StorageAccount -Container $Container -BlobName $blob.name -LocalPath $tempFile) {
                        $flows = Parse-FlowLogFile -FilePath $tempFile
                        $allFlows += $flows
                        Remove-Item $tempFile -Force
                    }
                }
                
                if ($allFlows.Count -gt 0) {
                    $analysis = Analyze-FlowData -Flows $allFlows
                    
                    if ($DetectAnomalies) {
                        $suspiciousActivities = Detect-SuspiciousActivities -Flows $allFlows -Threshold $SuspiciousThreshold
                        
                        if ($suspiciousActivities.Count -gt 0) {
                            Write-ColorOutput "🚨 ALERTA: $($suspiciousActivities.Count) actividades sospechosas detectadas!" $CriticalColor
                        }
                    }
                }
            }
            
            $lastProcessedTime = $currentTime
            Write-ColorOutput "⏰ Próxima verificación en $RefreshInterval segundos..." $InfoColor
            Start-Sleep -Seconds $RefreshInterval
        }
        catch {
            Write-ColorOutput "❌ Error en monitoreo en tiempo real: $_" $ErrorColor
            Start-Sleep -Seconds 30
        }
    }
}

# Función principal
function Main {
    Write-ColorOutput "📊 MONITOREO Y ANÁLISIS DE FLOW LOGS - LABORATORIO 3" $InfoColor
    Write-ColorOutput "======================================================" $InfoColor
    Write-ColorOutput "Storage Account: $StorageAccountName" $InfoColor
    Write-ColorOutput "Container: $ContainerName" $InfoColor
    Write-ColorOutput "Período de análisis: $AnalysisHours horas" $InfoColor
    Write-ColorOutput ""
    
    # Inicializar entorno
    if (!(Initialize-FlowLogEnvironment)) {
        Write-ColorOutput "❌ No se pudo inicializar el entorno de análisis" $ErrorColor
        exit 1
    }
    
    if ($RealTimeMonitoring) {
        Start-RealTimeMonitoring -StorageAccount $StorageAccountName -Container $ContainerName -RefreshInterval $RefreshIntervalSeconds
        return
    }
    
    # Análisis histórico
    $endTime = Get-Date
    $startTime = $endTime.AddHours(-$AnalysisHours)
    
    Write-ColorOutput "🔍 Obteniendo Flow Logs desde $startTime hasta $endTime..." $InfoColor
    
    $blobs = Get-FlowLogBlobs -StorageAccount $StorageAccountName -Container $ContainerName -StartTime $startTime -EndTime $endTime
    
    if ($blobs.Count -eq 0) {
        Write-ColorOutput "⚠️ No se encontraron Flow Logs en el período especificado" $WarningColor
        exit 0
    }
    
    # Procesar blobs
    Write-ColorOutput "📥 Descargando y procesando $($blobs.Count) archivos de Flow Logs..." $InfoColor
    
    $allFlows = @()
    $processedCount = 0
    
    foreach ($blob in $blobs) {
        $processedCount++
        $progress = [math]::Round(($processedCount * 100) / $blobs.Count, 1)
        Write-ColorOutput "[$progress%] Procesando: $($blob.name)" $InfoColor
        
        $tempFile = Join-Path $env:TEMP "flowlog_$([guid]::NewGuid().ToString()).json"
        if (Download-FlowLogBlob -StorageAccount $StorageAccountName -Container $ContainerName -BlobName $blob.name -LocalPath $tempFile) {
            $flows = Parse-FlowLogFile -FilePath $tempFile
            $allFlows += $flows
            Remove-Item $tempFile -Force
        }
    }
    
    if ($allFlows.Count -eq 0) {
        Write-ColorOutput "⚠️ No se pudieron procesar flows de los archivos descargados" $WarningColor
        exit 0
    }
    
    # Analizar datos
    Write-ColorOutput "📊 Analizando $($allFlows.Count) flows..." $InfoColor
    $analysis = Analyze-FlowData -Flows $allFlows
    
    # Detectar actividades sospechosas
    $suspiciousActivities = @()
    if ($DetectAnomalies) {
        $suspiciousActivities = Detect-SuspiciousActivities -Flows $allFlows -Threshold $SuspiciousThreshold
    }
    
    # Exportar resultados
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    
    # Exportar análisis en JSON
    $analysis | ConvertTo-Json -Depth 5 | Out-File -FilePath "$OutputDirectory\flow-analysis-$timestamp.json" -Encoding UTF8
    
    # Exportar actividades sospechosas
    if ($suspiciousActivities.Count -gt 0) {
        $suspiciousActivities | ConvertTo-Json -Depth 3 | Out-File -FilePath "$OutputDirectory\suspicious-activities-$timestamp.json" -Encoding UTF8
        $suspiciousActivities | Export-Csv -Path "$OutputDirectory\suspicious-activities-$timestamp.csv" -NoTypeInformation -Encoding UTF8
    }
    
    # Generar reporte HTML
    $reportPath = "$OutputDirectory\flow-log-report-$timestamp.html"
    Generate-FlowLogReport -Analysis $analysis -SuspiciousActivities $suspiciousActivities -OutputPath $reportPath
    
    Write-ColorOutput "`n✅ Análisis de Flow Logs completado!" $SuccessColor
    Write-ColorOutput "📄 Archivos generados en: $OutputDirectory" $InfoColor
    Write-ColorOutput "🌐 Reporte HTML: $reportPath" $InfoColor
    
    if ($suspiciousActivities.Count -gt 0) {
        Write-ColorOutput "`n🚨 RESUMEN DE SEGURIDAD:" $CriticalColor
        Write-ColorOutput "  Actividades sospechosas encontradas: $($suspiciousActivities.Count)" $ErrorColor
        Write-ColorOutput "  Revise el reporte detallado para más información" $WarningColor
    }
}

# Ejecutar función principal
Main 