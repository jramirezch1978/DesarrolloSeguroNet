<#
.SYNOPSIS
Script de verificación completa del entorno para laboratorios de Azure Security

.DESCRIPTION
Valida que todas las herramientas estén correctamente instaladas y configuradas
para proceder con los laboratorios de Network Security Groups y DDoS Protection.

.EXAMPLE
.\verify-setup.ps1
Ejecuta la verificación completa del entorno
#>

# Configuración de colores para output
$colors = @{
    Success = "Green"
    Warning = "Yellow"
    Error = "Red"
    Info = "Cyan"
    Header = "Magenta"
}

function Write-ColorMessage {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $colors[$Color]
}

function Test-ChocolateyInstallation {
    Write-ColorMessage "🍫 Verificando Chocolatey..." "Header"
    
    try {
        $chocoVersion = choco --version 2>$null
        if ($chocoVersion) {
            Write-ColorMessage "✅ Chocolatey instalado: $chocoVersion" "Success"
            return $true
        } else {
            Write-ColorMessage "❌ Chocolatey no encontrado" "Error"
            Write-ColorMessage "💡 Ejecute: .\setup-environment.ps1" "Warning"
            return $false
        }
    }
    catch {
        Write-ColorMessage "❌ Error verificando Chocolatey" "Error"
        return $false
    }
}

function Test-DotNetInstallation {
    Write-ColorMessage "🔧 Verificando .NET SDK..." "Header"
    
    try {
        $dotnetVersion = dotnet --version 2>$null
        if ($dotnetVersion -and $dotnetVersion -match "^9\.") {
            Write-ColorMessage "✅ .NET SDK instalado: $dotnetVersion" "Success"
            
            # Verificar que puede crear proyectos
            $tempDir = Join-Path $env:TEMP "dotnet-test-$(Get-Random)"
            New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
            
            try {
                Push-Location $tempDir
                $output = dotnet new console --name TestApp 2>$null
                if (Test-Path "TestApp\TestApp.csproj") {
                    Write-ColorMessage "✅ .NET SDK funcional - puede crear proyectos" "Success"
                    $result = $true
                } else {
                    Write-ColorMessage "⚠️  .NET SDK instalado pero no puede crear proyectos" "Warning"
                    $result = $false
                }
            }
            finally {
                Pop-Location
                Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
            }
            
            return $result
        } else {
            Write-ColorMessage "❌ .NET 9 SDK no encontrado" "Error"
            if ($dotnetVersion) {
                Write-ColorMessage "🔍 Versión encontrada: $dotnetVersion (se requiere 9.x)" "Warning"
            }
            Write-ColorMessage "💡 Instale con: choco install dotnet-9.0-sdk -y" "Warning"
            return $false
        }
    }
    catch {
        Write-ColorMessage "❌ Error verificando .NET SDK" "Error"
        return $false
    }
}

function Test-AzureCLIInstallation {
    Write-ColorMessage "☁️  Verificando Azure CLI..." "Header"
    
    try {
        $azVersion = az --version 2>$null
        if ($azVersion -and $azVersion -match "azure-cli") {
            # Extraer versión específica
            $versionLine = ($azVersion -split "`n")[0]
            Write-ColorMessage "✅ Azure CLI instalado: $versionLine" "Success"
            
            # Verificar autenticación
            try {
                $account = az account show 2>$null | ConvertFrom-Json
                if ($account) {
                    Write-ColorMessage "✅ Autenticado como: $($account.user.name)" "Success"
                    Write-ColorMessage "📋 Suscripción: $($account.name)" "Info"
                    
                    # Verificar permisos básicos
                    $groups = az group list --query "[].name" 2>$null | ConvertFrom-Json
                    if ($groups -and $groups.Count -gt 0) {
                        Write-ColorMessage "✅ Permisos verificados - puede listar resource groups" "Success"
                        return $true
                    } else {
                        Write-ColorMessage "⚠️  Autenticado pero permisos limitados" "Warning"
                        return $false
                    }
                } else {
                    Write-ColorMessage "⚠️  Azure CLI instalado pero no autenticado" "Warning"
                    Write-ColorMessage "💡 Ejecute: az login" "Warning"
                    return $false
                }
            }
            catch {
                Write-ColorMessage "⚠️  Azure CLI instalado pero error en autenticación" "Warning"
                return $false
            }
        } else {
            Write-ColorMessage "❌ Azure CLI no encontrado" "Error"
            Write-ColorMessage "💡 Instale con: choco install azure-cli -y" "Warning"
            return $false
        }
    }
    catch {
        Write-ColorMessage "❌ Error verificando Azure CLI" "Error"
        return $false
    }
}

function Test-GitInstallation {
    Write-ColorMessage "📝 Verificando Git..." "Header"
    
    try {
        $gitVersion = git --version 2>$null
        if ($gitVersion -and $gitVersion -match "git version") {
            Write-ColorMessage "✅ Git instalado: $gitVersion" "Success"
            
            # Verificar configuración básica
            $userName = git config --global user.name 2>$null
            $userEmail = git config --global user.email 2>$null
            
            if ($userName -and $userEmail) {
                Write-ColorMessage "✅ Git configurado: $userName <$userEmail>" "Success"
            } else {
                Write-ColorMessage "⚠️  Git instalado pero no configurado" "Warning"
                Write-ColorMessage "💡 Configure con: git config --global user.name 'Su Nombre'" "Warning"
                Write-ColorMessage "💡 Configure con: git config --global user.email 'su@email.com'" "Warning"
            }
            
            return $true
        } else {
            Write-ColorMessage "❌ Git no encontrado" "Error"
            Write-ColorMessage "💡 Instale con: choco install git -y" "Warning"
            return $false
        }
    }
    catch {
        Write-ColorMessage "❌ Error verificando Git" "Error"
        return $false
    }
}

function Test-VSCodeInstallation {
    Write-ColorMessage "💻 Verificando Visual Studio Code..." "Header"
    
    try {
        $codeVersion = code --version 2>$null
        if ($codeVersion) {
            $version = ($codeVersion -split "`n")[0]
            Write-ColorMessage "✅ VS Code instalado: $version" "Success"
            
            # Verificar extensiones importantes
            $extensions = code --list-extensions 2>$null
            $requiredExtensions = @(
                "ms-dotnettools.csdevkit",
                "ms-vscode.azure-account",
                "ms-azuretools.vscode-azureresourcegroups",
                "ms-vscode.azurecli"
            )
            
            $installedCount = 0
            foreach ($required in $requiredExtensions) {
                if ($extensions -contains $required) {
                    $installedCount++
                }
            }
            
            if ($installedCount -eq $requiredExtensions.Count) {
                Write-ColorMessage "✅ Todas las extensiones requeridas están instaladas" "Success"
            } else {
                Write-ColorMessage "⚠️  Faltan extensiones: $($requiredExtensions.Count - $installedCount) de $($requiredExtensions.Count)" "Warning"
                Write-ColorMessage "💡 Ejecute setup-environment.ps1 para instalar extensiones" "Warning"
            }
            
            return $true
        } else {
            Write-ColorMessage "❌ VS Code no encontrado" "Error"
            Write-ColorMessage "💡 Instale con: choco install vscode -y" "Warning"
            return $false
        }
    }
    catch {
        Write-ColorMessage "❌ Error verificando VS Code" "Error"
        return $false
    }
}

function Test-PowerShellVersion {
    Write-ColorMessage "⚡ Verificando PowerShell..." "Header"
    
    $psVersion = $PSVersionTable.PSVersion
    Write-ColorMessage "✅ PowerShell: $($psVersion.ToString())" "Success"
    
    if ($psVersion.Major -ge 5) {
        Write-ColorMessage "✅ Versión compatible para laboratorios" "Success"
        return $true
    } else {
        Write-ColorMessage "⚠️  Versión antigua de PowerShell detectada" "Warning"
        Write-ColorMessage "💡 Considere actualizar a PowerShell 7+" "Warning"
        return $false
    }
}

function Test-NetworkConnectivity {
    Write-ColorMessage "🌐 Verificando conectividad de red..." "Header"
    
    $testUrls = @(
        @{Name = "Azure Portal"; Url = "https://portal.azure.com"},
        @{Name = "GitHub"; Url = "https://github.com"},
        @{Name = "Chocolatey"; Url = "https://chocolatey.org"},
        @{Name = "NuGet"; Url = "https://api.nuget.org"}
    )
    
    $allConnected = $true
    
    foreach ($test in $testUrls) {
        try {
            $response = Invoke-WebRequest -Uri $test.Url -TimeoutSec 10 -UseBasicParsing 2>$null
            if ($response.StatusCode -eq 200) {
                Write-ColorMessage "✅ $($test.Name): Conectado" "Success"
            } else {
                Write-ColorMessage "⚠️  $($test.Name): Respuesta inusual ($($response.StatusCode))" "Warning"
                $allConnected = $false
            }
        }
        catch {
            Write-ColorMessage "❌ $($test.Name): Sin conexión" "Error"
            $allConnected = $false
        }
    }
    
    return $allConnected
}

function Show-VerificationSummary {
    param(
        [hashtable]$Results
    )
    
    Write-ColorMessage "`n📊 RESUMEN DE VERIFICACIÓN" "Header"
    Write-ColorMessage "════════════════════════════════════════" "Header"
    
    $totalTests = $Results.Count
    $passedTests = ($Results.Values | Where-Object { $_ -eq $true }).Count
    $failedTests = $totalTests - $passedTests
    
    Write-ColorMessage "📈 Estadísticas:" "Info"
    Write-ColorMessage "  • Total de verificaciones: $totalTests" "Info"
    Write-ColorMessage "  • Exitosas: $passedTests" "Success"
    Write-ColorMessage "  • Fallidas: $failedTests" "Error"
    Write-ColorMessage "  • Porcentaje de éxito: $([math]::Round(($passedTests / $totalTests) * 100, 1))%" "Info"
    
    Write-ColorMessage "`n🔍 Detalles:" "Info"
    foreach ($test in $Results.GetEnumerator()) {
        $status = if ($test.Value) { "✅ PASS" } else { "❌ FAIL" }
        $color = if ($test.Value) { "Success" } else { "Error" }
        Write-ColorMessage "  • $($test.Key): $status" $color
    }
    
    if ($passedTests -eq $totalTests) {
        Write-ColorMessage "`n🎉 ¡VERIFICACIÓN COMPLETA EXITOSA!" "Success"
        Write-ColorMessage "El entorno está perfectamente configurado para los laboratorios" "Success"
        Write-ColorMessage "`n🚀 Próximo paso: Proceder al Laboratorio 1 - Network Security Groups" "Header"
    } elseif ($passedTests -ge ($totalTests * 0.8)) {
        Write-ColorMessage "`n⚠️  VERIFICACIÓN PARCIALMENTE EXITOSA" "Warning"
        Write-ColorMessage "La mayoría de componentes están configurados correctamente" "Warning"
        Write-ColorMessage "Puede proceder con los laboratorios, pero revise las fallas" "Warning"
    } else {
        Write-ColorMessage "`n❌ VERIFICACIÓN FALLIDA" "Error"
        Write-ColorMessage "Múltiples componentes requieren atención" "Error"
        Write-ColorMessage "Ejecute setup-environment.ps1 para corregir problemas" "Warning"
    }
}

# ========================================
# EJECUCIÓN PRINCIPAL
# ========================================

try {
    Write-ColorMessage "🔍 VERIFICACIÓN DEL ENTORNO PARA LABORATORIOS AZURE" "Header"
    Write-ColorMessage "══════════════════════════════════════════════════════" "Header"
    
    # Ejecutar todas las verificaciones
    $verificationResults = @{
        "Chocolatey" = Test-ChocolateyInstallation
        ".NET SDK 9" = Test-DotNetInstallation
        "Azure CLI" = Test-AzureCLIInstallation
        "Git" = Test-GitInstallation
        "VS Code" = Test-VSCodeInstallation
        "PowerShell" = Test-PowerShellVersion
        "Conectividad" = Test-NetworkConnectivity
    }
    
    # Mostrar resumen
    Show-VerificationSummary -Results $verificationResults
    
    # Información adicional
    Write-ColorMessage "`n💡 INFORMACIÓN ADICIONAL" "Header"
    Write-ColorMessage "════════════════════════════════════════" "Header"
    Write-ColorMessage "📁 Directorio actual: $(Get-Location)" "Info"
    Write-ColorMessage "👤 Usuario actual: $env:USERNAME" "Info"
    Write-ColorMessage "💻 Computadora: $env:COMPUTERNAME" "Info"
    Write-ColorMessage "📅 Fecha/Hora: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" "Info"
    
    # Enlaces útiles
    Write-ColorMessage "`n🔗 RECURSOS ÚTILES" "Header"
    Write-ColorMessage "• Azure Portal: https://portal.azure.com" "Info"
    Write-ColorMessage "• Documentación Azure: https://docs.microsoft.com/azure" "Info"
    Write-ColorMessage "• VS Code: code ." "Info"
    Write-ColorMessage "• PowerShell ISE: ise" "Info"
}
catch {
    Write-ColorMessage "`n❌ ERROR EN VERIFICACIÓN" "Error"
    Write-ColorMessage "Error: $($_.Exception.Message)" "Error"
}
finally {
    Write-ColorMessage "`n⏸️  Presione cualquier tecla para continuar..." "Info"
    Read-Host
} 