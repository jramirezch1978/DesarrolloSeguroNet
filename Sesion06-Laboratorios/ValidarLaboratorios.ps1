# Script de Validacion Automatica - Sesion 06 Laboratorios
# Curso: Diseno Seguro de Aplicaciones (.NET en Azure)

Write-Host "INICIANDO VALIDACION DE LABORATORIOS - SESION 06" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
Write-Host ""

$laboratorios = @(
    "Laboratorio0-VerificacionEntorno",
    "Laboratorio1-VirtualNetwork", 
    "Laboratorio2-NetworkSecurityGroups",
    "Laboratorio3-AzureBastion",
    "Laboratorio4-TestingArquitectura"
)

$erroresCompilacion = @()
$exitosos = @()

foreach ($lab in $laboratorios) {
    Write-Host "Validando: $lab" -ForegroundColor Cyan
    
    if (Test-Path $lab) {
        Push-Location $lab
        
        try {
            # Verificar que existe el archivo .csproj
            $csprojFiles = Get-ChildItem -Filter "*.csproj"
            if ($csprojFiles.Count -eq 0) {
                Write-Host "   WARNING: No se encontro archivo .csproj en $lab" -ForegroundColor Yellow
                Pop-Location
                continue
            }
            
            # Usar nombres correctos de proyecto para labs simplificados
            $projectFile = switch ($lab) {
                "Laboratorio0-VerificacionEntorno" { "EntornoVerificacion.csproj" }
                "Laboratorio1-VirtualNetwork" { "VNetConfiguration.csproj" }
                "Laboratorio2-NetworkSecurityGroups" { "NSGConfiguration.csproj" }
                "Laboratorio3-AzureBastion" { "BastionConfiguration.csproj" }
                "Laboratorio4-TestingArquitectura" { "TestingValidation.csproj" }
                default { $csprojFiles[0].Name }
            }
            
            Write-Host "   Ejecutando dotnet restore..." -ForegroundColor Gray
            $restoreResult = dotnet restore 2>&1 | Out-String
            
            if ($LASTEXITCODE -ne 0) {
                Write-Host "   ERROR: dotnet restore fallo para $lab" -ForegroundColor Red
                $erroresCompilacion += "$lab - Error en restore"
                Pop-Location
                continue
            }
            
            Write-Host "   Ejecutando dotnet build..." -ForegroundColor Gray
            $buildResult = dotnet build --no-restore 2>&1 | Out-String
            
            if ($LASTEXITCODE -ne 0) {
                Write-Host "   ERROR: dotnet build fallo para $lab" -ForegroundColor Red
                $erroresCompilacion += "$lab - Error en build"
            } else {
                Write-Host "   SUCCESS: $lab compilado exitosamente" -ForegroundColor Green
                $exitosos += $lab
            }
            
        } catch {
            Write-Host "   ERROR: Excepcion durante validacion de $lab" -ForegroundColor Red
            $erroresCompilacion += "$lab - Excepcion: $($_.Exception.Message)"
        } finally {
            Pop-Location
        }
    } else {
        Write-Host "   ERROR: Directorio $lab no encontrado" -ForegroundColor Red
        $erroresCompilacion += "$lab - Directorio no encontrado"
    }
    
    Write-Host ""
}

# Resumen final
Write-Host "RESUMEN DE VALIDACION" -ForegroundColor Yellow
Write-Host "=====================" -ForegroundColor Yellow
Write-Host ""

Write-Host "Laboratorios exitosos ($($exitosos.Count)/$($laboratorios.Count)):" -ForegroundColor Green
foreach ($lab in $exitosos) {
    Write-Host "   + $lab" -ForegroundColor Green
}

if ($erroresCompilacion.Count -gt 0) {
    Write-Host ""
    Write-Host "Errores encontrados ($($erroresCompilacion.Count)):" -ForegroundColor Red
    foreach ($errorItem in $erroresCompilacion) {
        Write-Host "   - $errorItem" -ForegroundColor Red
    }
} else {
    Write-Host ""
    Write-Host "TODOS LOS LABORATORIOS VALIDADOS EXITOSAMENTE!" -ForegroundColor Green
}

Write-Host ""
Write-Host "Informacion adicional:" -ForegroundColor Cyan
Write-Host "   .NET Version: $(dotnet --version)" -ForegroundColor Gray
Write-Host "   Fecha: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host "   Directorio: $(Get-Location)" -ForegroundColor Gray

# Verificar configuraciones criticas
Write-Host ""
Write-Host "Verificando configuraciones criticas..." -ForegroundColor Cyan

$configuracionesCorrectas = 0
$configuracionesTotales = 0

foreach ($lab in $exitosos) {
    # Solo verificar Azure AD para laboratorios que lo requieren (NINGUNO en esta sesión)
    # Todos los laboratorios de Sesión 06 son configuración de Azure, no aplicaciones web
    # Write-Host "   + $lab - No requiere Azure AD (configuración de infraestructura)" -ForegroundColor Green
}

Write-Host ""
Write-Host "Configuraciones Azure AD: $configuracionesCorrectas/$configuracionesTotales correctas" -ForegroundColor Cyan

# Codigo de salida
if ($erroresCompilacion.Count -eq 0) {
    Write-Host ""
    Write-Host "VALIDACION COMPLETADA EXITOSAMENTE" -ForegroundColor Green
    exit 0
} else {
    Write-Host ""
    Write-Host "VALIDACION COMPLETADA CON ERRORES" -ForegroundColor Yellow
    exit 1
} 