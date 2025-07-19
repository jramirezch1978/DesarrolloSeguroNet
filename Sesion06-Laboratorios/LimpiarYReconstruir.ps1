# Script para Limpiar y Reconstruir todos los Laboratorios
# Curso: Diseno Seguro de Aplicaciones (.NET en Azure)

Write-Host "LIMPIANDO Y RECONSTRUYENDO LABORATORIOS - SESION 06" -ForegroundColor Green
Write-Host "====================================================" -ForegroundColor Green
Write-Host ""

$laboratorios = @(
    "Laboratorio0-VerificacionEntorno",
    "Laboratorio1-VirtualNetwork", 
    "Laboratorio2-NetworkSecurityGroups",
    "Laboratorio3-AzureBastion",
    "Laboratorio4-TestingArquitectura"
)

foreach ($lab in $laboratorios) {
    Write-Host "Procesando: $lab" -ForegroundColor Cyan
    
    if (Test-Path $lab) {
        Push-Location $lab
        
        try {
            Write-Host "   Eliminando directorios bin y obj..." -ForegroundColor Gray
            Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
            Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue
            
            Write-Host "   Ejecutando dotnet restore..." -ForegroundColor Gray
            dotnet restore | Out-Null
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "   SUCCESS: $lab limpiado y restaurado" -ForegroundColor Green
            } else {
                Write-Host "   ERROR: Fallo en restore de $lab" -ForegroundColor Red
            }
            
        } catch {
            Write-Host "   ERROR: Excepcion en $lab - $($_.Exception.Message)" -ForegroundColor Red
        } finally {
            Pop-Location
        }
    } else {
        Write-Host "   ERROR: Directorio $lab no encontrado" -ForegroundColor Red
    }
    
    Write-Host ""
}

Write-Host "LIMPIEZA COMPLETADA" -ForegroundColor Green
Write-Host "Ejecute ValidarLaboratorios.ps1 para verificar compilacion" -ForegroundColor Cyan 