# Script: script-test-connectivity.ps1
# Propósito: Testing automatizado de conectividad de red usando Azure Network Watcher
# Autor: Laboratorio 3 - Testing y Simulación de Conectividad

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroup,
    
    [Parameter(Mandatory=$false)]
    [string]$VNetName = "vnet-nsg-lab",
    
    [Parameter(Mandatory=$false)]
    [switch]$Verbose,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputFile,
    
    [Parameter(Mandatory=$false)]
    [switch]$GenerateReport,
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "eastus"
)

# Configuración de colores para output
$SuccessColor = "Green"
$WarningColor = "Yellow"
$ErrorColor = "Red"
$InfoColor = "Cyan"

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

function Test-Prerequisites {
    Write-ColorOutput "🔍 Verificando prerrequisitos..." $InfoColor
    
    # Verificar Azure CLI
    try {
        $azVersion = az version --output json | ConvertFrom-Json
        Write-ColorOutput "✅ Azure CLI versión: $($azVersion.'azure-cli')" $SuccessColor
    }
    catch {
        Write-ColorOutput "❌ Azure CLI no está instalado o configurado" $ErrorColor
        return $false
    }
    
    # Verificar autenticación
    try {
        $account = az account show --output json | ConvertFrom-Json
        Write-ColorOutput "✅ Autenticado como: $($account.user.name)" $SuccessColor
    }
    catch {
        Write-ColorOutput "❌ No está autenticado en Azure. Ejecute 'az login'" $ErrorColor
        return $false
    }
    
    # Verificar Network Watcher
    try {
        $networkWatcher = az network watcher list --output json | ConvertFrom-Json
        if ($networkWatcher.Count -eq 0) {
            Write-ColorOutput "⚠️ Network Watcher no encontrado, habilitando..." $WarningColor
            az network watcher configure --resource-group NetworkWatcherRG --location $Location --enabled true
        }
        Write-ColorOutput "✅ Network Watcher disponible" $SuccessColor
    }
    catch {
        Write-ColorOutput "❌ Error verificando Network Watcher" $ErrorColor
        return $false
    }
    
    return $true
}

function Get-VirtualMachines {
    param([string]$ResourceGroup)
    
    Write-ColorOutput "📋 Obteniendo lista de VMs en $ResourceGroup..." $InfoColor
    
    try {
        $vms = az vm list --resource-group $ResourceGroup --output json | ConvertFrom-Json
        
        if ($vms.Count -eq 0) {
            Write-ColorOutput "⚠️ No se encontraron VMs en el resource group $ResourceGroup" $WarningColor
            return @()
        }
        
        $vmList = @()
        foreach ($vm in $vms) {
            # Obtener información de red de la VM
            $nicId = $vm.networkProfile.networkInterfaces[0].id
            $nic = az network nic show --ids $nicId --output json | ConvertFrom-Json
            $privateIP = $nic.ipConfigurations[0].privateIpAddress
            
            $vmInfo = [PSCustomObject]@{
                Name = $vm.name
                ResourceGroup = $vm.resourceGroup
                Location = $vm.location
                PrivateIP = $privateIP
                PowerState = (az vm get-instance-view --name $vm.name --resource-group $ResourceGroup --query "instanceView.statuses[?code=='PowerState/running']" --output tsv).Length -gt 0
                Tier = if ($vm.name -like "*web*") { "Web" } elseif ($vm.name -like "*app*") { "App" } elseif ($vm.name -like "*data*") { "Data" } else { "Other" }
            }
            $vmList += $vmInfo
        }
        
        Write-ColorOutput "✅ Encontradas $($vmList.Count) VMs" $SuccessColor
        return $vmList
    }
    catch {
        Write-ColorOutput "❌ Error obteniendo VMs: $_" $ErrorColor
        return @()
    }
}

function Build-ConnectivityTestMatrix {
    param([array]$VMs)
    
    Write-ColorOutput "🧪 Construyendo matriz de tests de conectividad..." $InfoColor
    
    $testMatrix = @()
    
    # Categorizar VMs por tier
    $webVMs = $VMs | Where-Object { $_.Tier -eq "Web" }
    $appVMs = $VMs | Where-Object { $_.Tier -eq "App" }
    $dataVMs = $VMs | Where-Object { $_.Tier -eq "Data" }
    
    Write-ColorOutput "📊 VMs por tier: Web=$($webVMs.Count), App=$($appVMs.Count), Data=$($dataVMs.Count)" $InfoColor
    
    # Tests Web -> App (deben pasar)
    foreach ($webVM in $webVMs) {
        foreach ($appVM in $appVMs) {
            $testMatrix += [PSCustomObject]@{
                Source = $webVM.Name
                SourceIP = $webVM.PrivateIP
                Target = $appVM.PrivateIP
                TargetName = $appVM.Name
                Port = 8080
                Protocol = "TCP"
                Expected = "Reachable"
                Category = "Web-to-App"
                Description = "Comunicación desde tier web a tier aplicación"
            }
            
            $testMatrix += [PSCustomObject]@{
                Source = $webVM.Name
                SourceIP = $webVM.PrivateIP
                Target = $appVM.PrivateIP
                TargetName = $appVM.Name
                Port = 8443
                Protocol = "TCP"
                Expected = "Reachable"
                Category = "Web-to-App"
                Description = "Comunicación HTTPS desde tier web a tier aplicación"
            }
        }
    }
    
    # Tests App -> Data (deben pasar)
    foreach ($appVM in $appVMs) {
        foreach ($dataVM in $dataVMs) {
            $testMatrix += [PSCustomObject]@{
                Source = $appVM.Name
                SourceIP = $appVM.PrivateIP
                Target = $dataVM.PrivateIP
                TargetName = $dataVM.Name
                Port = 1433
                Protocol = "TCP"
                Expected = "Reachable"
                Category = "App-to-Data"
                Description = "Conexión SQL Server desde tier aplicación a tier datos"
            }
            
            $testMatrix += [PSCustomObject]@{
                Source = $appVM.Name
                SourceIP = $appVM.PrivateIP
                Target = $dataVM.PrivateIP
                TargetName = $dataVM.Name
                Port = 3306
                Protocol = "TCP"
                Expected = "Reachable"
                Category = "App-to-Data"
                Description = "Conexión MySQL desde tier aplicación a tier datos"
            }
        }
    }
    
    # Tests Web -> Data (deben fallar - validación de seguridad)
    foreach ($webVM in $webVMs) {
        foreach ($dataVM in $dataVMs) {
            $testMatrix += [PSCustomObject]@{
                Source = $webVM.Name
                SourceIP = $webVM.PrivateIP
                Target = $dataVM.PrivateIP
                TargetName = $dataVM.Name
                Port = 1433
                Protocol = "TCP"
                Expected = "Unreachable"
                Category = "Security-Validation"
                Description = "Validación: Web tier NO debe acceder directamente a Data tier"
            }
        }
    }
    
    # Tests hacia Internet desde Web tier
    foreach ($webVM in $webVMs) {
        $testMatrix += [PSCustomObject]@{
            Source = $webVM.Name
            SourceIP = $webVM.PrivateIP
            Target = "www.microsoft.com"
            TargetName = "Microsoft.com"
            Port = 443
            Protocol = "TCP"
            Expected = "Reachable"
            Category = "Internet-Access"
            Description = "Acceso HTTPS a Internet desde tier web"
        }
        
        $testMatrix += [PSCustomObject]@{
            Source = $webVM.Name
            SourceIP = $webVM.PrivateIP
            Target = "8.8.8.8"
            TargetName = "Google DNS"
            Port = 53
            Protocol = "UDP"
            Expected = "Reachable"
            Category = "Internet-Access"
            Description = "Consulta DNS a Internet desde tier web"
        }
    }
    
    # Tests de servicios no autorizados (deben fallar)
    foreach ($vm in $VMs) {
        $testMatrix += [PSCustomObject]@{
            Source = $vm.Name
            SourceIP = $vm.PrivateIP
            Target = $vm.PrivateIP
            TargetName = $vm.Name
            Port = 22
            Protocol = "TCP"
            Expected = "Unreachable"
            Category = "Security-Validation"
            Description = "Validación: SSH desde Internet debe estar bloqueado"
        }
    }
    
    Write-ColorOutput "✅ Matriz de tests construida: $($testMatrix.Count) tests planificados" $SuccessColor
    return $testMatrix
}

function Invoke-ConnectivityTest {
    param(
        [PSCustomObject]$Test,
        [int]$TestNumber,
        [int]$TotalTests
    )
    
    $progress = [math]::Round(($TestNumber * 100) / $TotalTests, 1)
    
    if ($Verbose) {
        Write-ColorOutput "🔍 [$progress%] Testing: $($Test.Source) -> $($Test.Target):$($Test.Port) ($($Test.Protocol))" $InfoColor
    }
    
    try {
        # Ejecutar test de conectividad usando Azure Network Watcher
        $connectivityResult = az network watcher test-connectivity `
            --source-resource $Test.Source `
            --dest-address $Test.Target `
            --dest-port $Test.Port `
            --protocol $Test.Protocol `
            --resource-group $ResourceGroup `
            --output json | ConvertFrom-Json
        
        $actualStatus = if ($connectivityResult.connectionStatus -eq "Reachable") { "Reachable" } else { "Unreachable" }
        $success = ($actualStatus -eq $Test.Expected)
        
        $result = [PSCustomObject]@{
            Source = $Test.Source
            Target = $Test.Target
            TargetName = $Test.TargetName
            Port = $Test.Port
            Protocol = $Test.Protocol
            Expected = $Test.Expected
            Actual = $actualStatus
            Success = $success
            Category = $Test.Category
            Description = $Test.Description
            Latency = if ($connectivityResult.avgLatencyInMs) { $connectivityResult.avgLatencyInMs } else { 0 }
            StatusReason = if ($connectivityResult.connectionStatus) { $connectivityResult.connectionStatus } else { "Unknown" }
            TestTime = Get-Date
        }
        
        # Mostrar resultado
        $statusIcon = if ($success) { "✅" } else { "❌" }
        $statusColor = if ($success) { $SuccessColor } else { $ErrorColor }
        
        $message = "[$progress%] $statusIcon $($Test.Source) -> $($Test.TargetName):$($Test.Port) [$actualStatus]"
        if ($Verbose -and $result.Latency -gt 0) {
            $message += " - $($result.Latency)ms"
        }
        
        Write-ColorOutput $message $statusColor
        
        return $result
    }
    catch {
        Write-ColorOutput "❌ Error en test $($Test.Source) -> $($Test.Target):$($Test.Port): $_" $ErrorColor
        
        return [PSCustomObject]@{
            Source = $Test.Source
            Target = $Test.Target
            TargetName = $Test.TargetName
            Port = $Test.Port
            Protocol = $Test.Protocol
            Expected = $Test.Expected
            Actual = "Error"
            Success = $false
            Category = $Test.Category
            Description = $Test.Description
            Latency = 0
            StatusReason = "Test execution failed: $_"
            TestTime = Get-Date
        }
    }
}

function Show-TestSummary {
    param([array]$Results)
    
    Write-ColorOutput "`n📊 RESUMEN DE TESTS DE CONECTIVIDAD" $InfoColor
    Write-ColorOutput "==========================================" $InfoColor
    
    $total = $Results.Count
    $successful = ($Results | Where-Object { $_.Success -eq $true }).Count
    $failed = $total - $successful
    
    $successRate = if ($total -gt 0) { [math]::Round(($successful * 100) / $total, 1) } else { 0 }
    
    Write-ColorOutput "✅ Total de tests: $total" $InfoColor
    Write-ColorOutput "✅ Exitosos: $successful ($successRate%)" $SuccessColor
    Write-ColorOutput "❌ Fallidos: $failed ($([math]::Round(100 - $successRate, 1))%)" $ErrorColor
    
    if ($successful -gt 0) {
        $avgLatency = ($Results | Where-Object { $_.Success -eq $true -and $_.Latency -gt 0 } | Measure-Object -Property Latency -Average).Average
        if ($avgLatency) {
            Write-ColorOutput "⏱️ Latencia promedio: $([math]::Round($avgLatency, 2))ms" $InfoColor
        }
    }
    
    # Resumen por categoría
    Write-ColorOutput "`n📋 RESUMEN POR CATEGORÍA:" $InfoColor
    $categories = $Results | Group-Object Category
    foreach ($category in $categories) {
        $catSuccessful = ($category.Group | Where-Object { $_.Success -eq $true }).Count
        $catTotal = $category.Group.Count
        $catRate = if ($catTotal -gt 0) { [math]::Round(($catSuccessful * 100) / $catTotal, 1) } else { 0 }
        
        $categoryColor = if ($catRate -ge 80) { $SuccessColor } elseif ($catRate -ge 60) { $WarningColor } else { $ErrorColor }
        Write-ColorOutput "  $($category.Name): $catSuccessful/$catTotal ($catRate%)" $categoryColor
    }
    
    # Mostrar fallos de seguridad esperados
    $securityTests = $Results | Where-Object { $_.Category -eq "Security-Validation" -and $_.Success -eq $true }
    if ($securityTests.Count -gt 0) {
        Write-ColorOutput "`n🔒 VALIDACIONES DE SEGURIDAD EXITOSAS:" $SuccessColor
        Write-ColorOutput "  Tests de seguridad que fallaron correctamente: $($securityTests.Count)" $SuccessColor
        Write-ColorOutput "  (Esto significa que los controles de seguridad están funcionando)" $SuccessColor
    }
    
    # Mostrar fallos inesperados
    $unexpectedFailures = $Results | Where-Object { $_.Success -eq $false -and $_.Category -ne "Security-Validation" }
    if ($unexpectedFailures.Count -gt 0) {
        Write-ColorOutput "`n⚠️ FALLOS INESPERADOS:" $WarningColor
        foreach ($failure in $unexpectedFailures) {
            Write-ColorOutput "  $($failure.Source) -> $($failure.TargetName):$($failure.Port) - $($failure.StatusReason)" $WarningColor
        }
    }
}

function Export-Results {
    param([array]$Results, [string]$OutputFile)
    
    if ([string]::IsNullOrEmpty($OutputFile)) {
        $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
        $OutputFile = "connectivity-test-results-$timestamp.csv"
    }
    
    try {
        $Results | Export-Csv -Path $OutputFile -NoTypeInformation -Encoding UTF8
        Write-ColorOutput "📄 Resultados exportados a: $OutputFile" $SuccessColor
    }
    catch {
        Write-ColorOutput "❌ Error exportando resultados: $_" $ErrorColor
    }
}

function Invoke-NetworkTesterApp {
    param([string]$ResourceGroup, [string]$OutputFile)
    
    $networkTesterPath = Join-Path $PSScriptRoot "..\src\NetworkTester"
    
    if (Test-Path $networkTesterPath) {
        Write-ColorOutput "🚀 Ejecutando NetworkTester .NET application..." $InfoColor
        
        try {
            Push-Location $networkTesterPath
            
            # Compilar si es necesario
            if (!(Test-Path "bin\Debug\net9.0\NetworkTester.exe")) {
                Write-ColorOutput "🔨 Compilando NetworkTester..." $InfoColor
                dotnet build
            }
            
            # Ejecutar test completo
            $appArgs = @("test-all", "--resource-group", $ResourceGroup, "--verbose")
            if ($OutputFile) {
                $appArgs += @("--output", $OutputFile)
            }
            
            dotnet run -- $appArgs
            
            Pop-Location
        }
        catch {
            Write-ColorOutput "❌ Error ejecutando NetworkTester: $_" $ErrorColor
            Pop-Location
        }
    }
    else {
        Write-ColorOutput "⚠️ NetworkTester application no encontrada en $networkTesterPath" $WarningColor
    }
}

# Función principal
function Main {
    Write-ColorOutput "🧪 LABORATORIO 3: TESTING Y SIMULACIÓN DE CONECTIVIDAD" $InfoColor
    Write-ColorOutput "=======================================================" $InfoColor
    Write-ColorOutput "Resource Group: $ResourceGroup" $InfoColor
    Write-ColorOutput "Virtual Network: $VNetName" $InfoColor
    Write-ColorOutput "Fecha/Hora: $(Get-Date)" $InfoColor
    Write-ColorOutput ""
    
    # Verificar prerrequisitos
    if (!(Test-Prerequisites)) {
        Write-ColorOutput "❌ No se cumplieron los prerrequisitos. Abortando." $ErrorColor
        exit 1
    }
    
    # Obtener VMs
    $vms = Get-VirtualMachines -ResourceGroup $ResourceGroup
    if ($vms.Count -eq 0) {
        Write-ColorOutput "❌ No se encontraron VMs para testear. Abortando." $ErrorColor
        exit 1
    }
    
    # Construir matriz de tests
    $testMatrix = Build-ConnectivityTestMatrix -VMs $vms
    if ($testMatrix.Count -eq 0) {
        Write-ColorOutput "❌ No se pudieron construir tests de conectividad. Abortando." $ErrorColor
        exit 1
    }
    
    # Ejecutar tests
    Write-ColorOutput "🔍 Ejecutando $($testMatrix.Count) tests de conectividad..." $InfoColor
    Write-ColorOutput ""
    
    $results = @()
    for ($i = 0; $i -lt $testMatrix.Count; $i++) {
        $result = Invoke-ConnectivityTest -Test $testMatrix[$i] -TestNumber ($i + 1) -TotalTests $testMatrix.Count
        $results += $result
        
        # Pequeña pausa para evitar rate limiting
        Start-Sleep -Milliseconds 500
    }
    
    # Mostrar resumen
    Show-TestSummary -Results $results
    
    # Exportar resultados
    if ($OutputFile -or $PSBoundParameters.ContainsKey('OutputFile')) {
        Export-Results -Results $results -OutputFile $OutputFile
    }
    
    # Generar reporte con NetworkTester
    if ($GenerateReport) {
        Write-ColorOutput "`n📊 Generando reporte detallado..." $InfoColor
        Invoke-NetworkTesterApp -ResourceGroup $ResourceGroup -OutputFile $OutputFile
    }
    
    Write-ColorOutput "`n✅ Testing de conectividad completado!" $SuccessColor
}

# Ejecutar función principal
Main 