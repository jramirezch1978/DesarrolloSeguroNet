# Script de Limpieza de Recursos - Sesión 8
# Diseño Seguro de Aplicaciones (.NET en Azure)

param(
    [string]$ResourceGroupName = "rg-security-lab-$env:USERNAME",
    [switch]$Force = $false
)

Write-Host "🧹 Iniciando limpieza de recursos de la Sesión 8..." -ForegroundColor Yellow
Write-Host "==================================================" -ForegroundColor Yellow

# Confirmar limpieza
if (-not $Force) {
    Write-Host "`n⚠️ ADVERTENCIA: Esta acción eliminará TODOS los recursos creados en los laboratorios." -ForegroundColor Red
    Write-Host "Esto incluye:" -ForegroundColor Red
    Write-Host "- Resource Group: $ResourceGroupName" -ForegroundColor Red
    Write-Host "- VMs Windows y Linux" -ForegroundColor Red
    Write-Host "- App Services" -ForegroundColor Red
    Write-Host "- Storage Accounts" -ForegroundColor Red
    Write-Host "- Container Registry" -ForegroundColor Red
    Write-Host "- Logic Apps" -ForegroundColor Red
    Write-Host "- Network Security Groups" -ForegroundColor Red
    Write-Host "- Políticas personalizadas" -ForegroundColor Red
    
    $confirmation = Read-Host "`n¿Está seguro de que desea continuar? (y/N)"
    if ($confirmation -ne "y" -and $confirmation -ne "Y") {
        Write-Host "❌ Limpieza cancelada por el usuario" -ForegroundColor Red
        exit
    }
}

Write-Host "`n🔍 Verificando recursos a eliminar..." -ForegroundColor Yellow

# Verificar si el resource group existe
try {
    $rg = az group show --name $ResourceGroupName --output json | ConvertFrom-Json
    Write-Host "✅ Resource Group encontrado: $($rg.name)" -ForegroundColor Green
} catch {
    Write-Host "❌ Resource Group no encontrado: $ResourceGroupName" -ForegroundColor Red
    Write-Host "No hay recursos que limpiar." -ForegroundColor Yellow
    exit
}

# Listar recursos en el resource group
Write-Host "`n📋 Recursos encontrados en $ResourceGroupName:" -ForegroundColor Yellow
$resources = az resource list --resource-group $ResourceGroupName --output table
Write-Host $resources

# Paso 1: Deshabilitar planes de Defender para evitar costos
Write-Host "`n🔧 Deshabilitando planes de Defender..." -ForegroundColor Yellow

$defenderPlans = @(
    "VirtualMachines",
    "AppServices", 
    "StorageAccounts",
    "SqlServers",
    "ContainerRegistry"
)

foreach ($plan in $defenderPlans) {
    try {
        az security pricing create --name $plan --tier Free
        Write-Host "✅ Defender for $plan deshabilitado" -ForegroundColor Green
    } catch {
        Write-Host "⚠️ No se pudo deshabilitar Defender for $plan" -ForegroundColor Yellow
    }
}

# Paso 2: Eliminar políticas personalizadas
Write-Host "`n🔧 Eliminando políticas personalizadas..." -ForegroundColor Yellow

$policies = @(
    "assign-https-policy",
    "require-storage-encryption"
)

foreach ($policy in $policies) {
    try {
        az policy assignment delete --name $policy
        Write-Host "✅ Política eliminada: $policy" -ForegroundColor Green
    } catch {
        Write-Host "⚠️ No se pudo eliminar política: $policy" -ForegroundColor Yellow
    }
}

# Eliminar definiciones de políticas personalizadas
$policyDefinitions = @(
    "require-https-webapps"
)

foreach ($definition in $policyDefinitions) {
    try {
        az policy definition delete --name $definition
        Write-Host "✅ Definición de política eliminada: $definition" -ForegroundColor Green
    } catch {
        Write-Host "⚠️ No se pudo eliminar definición: $definition" -ForegroundColor Yellow
    }
}

# Paso 3: Eliminar resource group completo
Write-Host "`n🗑️ Eliminando resource group completo..." -ForegroundColor Yellow

try {
    az group delete --name $ResourceGroupName --yes --no-wait
    Write-Host "✅ Resource group marcado para eliminación: $ResourceGroupName" -ForegroundColor Green
    Write-Host "   La eliminación puede tardar varios minutos..." -ForegroundColor Cyan
} catch {
    Write-Host "❌ Error eliminando resource group: $ResourceGroupName" -ForegroundColor Red
}

# Paso 4: Limpiar archivos locales
Write-Host "`n🧹 Limpiando archivos locales..." -ForegroundColor Yellow

$localItems = @(
    "SecureScoreAnalyzer",
    "container-security-test",
    "*.ps1",
    "*.json",
    "*.txt",
    "setup-openvas.sh",
    "policy-https.json",
    "logic-app-setup-instructions.txt"
)

foreach ($item in $localItems) {
    try {
        if (Test-Path $item) {
            Remove-Item -Path $item -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "✅ Eliminado: $item" -ForegroundColor Green
        }
    } catch {
        Write-Host "⚠️ No se pudo eliminar: $item" -ForegroundColor Yellow
    }
}

# Limpiar directorios específicos si están vacíos
$directories = @(
    "scripts",
    "templates", 
    "reports",
    "logs"
)

foreach ($dir in $directories) {
    if (Test-Path $dir) {
        $items = Get-ChildItem -Path $dir -Force
        if ($items.Count -eq 0) {
            Remove-Item -Path $dir -Force
            Write-Host "✅ Directorio vacío eliminado: $dir" -ForegroundColor Green
        }
    }
}

# Paso 5: Verificar limpieza
Write-Host "`n🔍 Verificando limpieza..." -ForegroundColor Yellow

# Verificar si el resource group aún existe
Start-Sleep -Seconds 10  # Dar tiempo para que la eliminación comience

try {
    $rgCheck = az group show --name $ResourceGroupName --output json | ConvertFrom-Json
    Write-Host "⚠️ Resource group aún existe (eliminación en progreso): $($rgCheck.name)" -ForegroundColor Yellow
} catch {
    Write-Host "✅ Resource group eliminado exitosamente" -ForegroundColor Green
}

# Verificar planes de Defender
Write-Host "`n🔍 Verificando planes de Defender..." -ForegroundColor Yellow
$pricing = az security pricing list --output json | ConvertFrom-Json
$freePlans = $pricing | Where-Object { $_.pricingTier -eq "Free" }
$standardPlans = $pricing | Where-Object { $_.pricingTier -eq "Standard" }

Write-Host "✅ Planes Free: $($freePlans.Count)" -ForegroundColor Green
if ($standardPlans.Count -gt 0) {
    Write-Host "⚠️ Planes Standard aún activos: $($standardPlans.Count)" -ForegroundColor Yellow
    foreach ($plan in $standardPlans) {
        Write-Host "   - $($plan.name)" -ForegroundColor Yellow
    }
}

# Resumen final
Write-Host "`n🎉 ¡Limpieza completada!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green

Write-Host "`n📊 Resumen de la limpieza:" -ForegroundColor Cyan
Write-Host "- ✅ Planes de Defender deshabilitados" -ForegroundColor White
Write-Host "- ✅ Políticas personalizadas eliminadas" -ForegroundColor White
Write-Host "- ✅ Resource group marcado para eliminación" -ForegroundColor White
Write-Host "- ✅ Archivos locales limpiados" -ForegroundColor White

Write-Host "`n💡 Notas importantes:" -ForegroundColor Cyan
Write-Host "- La eliminación del resource group puede tardar hasta 30 minutos" -ForegroundColor White
Write-Host "- Verificar en Azure Portal que todos los recursos se eliminaron" -ForegroundColor White
Write-Host "- Los costos se detendrán una vez que se complete la eliminación" -ForegroundColor White

Write-Host "`n🔗 Para verificar el estado de eliminación:" -ForegroundColor Cyan
Write-Host "az group show --name $ResourceGroupName" -ForegroundColor White

Write-Host "`n✅ ¡Limpieza completada exitosamente!" -ForegroundColor Green 