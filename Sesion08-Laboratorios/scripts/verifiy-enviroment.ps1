$verificationScript = @"
Write-Host "=== VERIFICACIÓN DEL ENTORNO ===" -ForegroundColor Green

# Verificar .NET
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET Core: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ .NET Core no encontrado" -ForegroundColor Red
}

# Verificar Azure CLI
try {
    $azVersion = az version --output json | ConvertFrom-Json
    Write-Host "✅ Azure CLI: $($azVersion.'azure-cli')" -ForegroundColor Green
} catch {
    Write-Host "❌ Azure CLI no encontrado" -ForegroundColor Red
}

# Verificar Git
try {
    $gitVersion = git --version
    Write-Host "✅ Git: $gitVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Git no encontrado" -ForegroundColor Red
}

# Verificar Nmap
try {
    $nmapVersion = nmap --version | Select-Object -First 1
    Write-Host "✅ Nmap: $nmapVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Nmap no encontrado" -ForegroundColor Red
}

# Verificar VS Code
try {
    $vscodeVersion = code --version | Select-Object -First 1
    Write-Host "✅ VS Code: $vscodeVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ VS Code no encontrado" -ForegroundColor Red
}

# Verificar conexión Azure
try {
    $account = az account show --output json | ConvertFrom-Json
    Write-Host "✅ Azure: Conectado como $($account.user.name)" -ForegroundColor Green
    Write-Host "   Suscripción: $($account.name)" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Azure: No conectado" -ForegroundColor Red
}

Write-Host "`n=== VERIFICACIÓN COMPLETADA ===" -ForegroundColor Green
"@