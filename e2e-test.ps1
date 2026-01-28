# Script para automatizar pruebas E2E con Bruno
Write-Host "🚀 Ejecutando pruebas E2E con Bruno..." -ForegroundColor Green

# Cambiamos al directorio de la colección para asegurar que Bruno encuentre el bruno.json
Push-Location "BrunoTest-FunkoApi"

try {
    # Ejecutamos bruno indicando la ruta actual (.)
    # El reporte se guarda en ../e2e-results.html para que quede en la raíz del proyecto
    $command = "npx @usebruno/cli run . -r --env dev --insecure --reporter-html ../e2e-results.html"
    Invoke-Expression $command
}
finally {
    # Volvemos al directorio original pase lo que pase
    Pop-Location
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Pruebas completadas exitosamente." -ForegroundColor Green
} else {
    Write-Host "❌ Hubo fallos en las pruebas." -ForegroundColor Red
}

Write-Host "📄 Reporte generado en: e2e-results.html" -ForegroundColor Cyan
Write-Host "🌐 Abriendo reporte en el navegador..." -ForegroundColor Cyan
Start-Process "e2e-results.html"
