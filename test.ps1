# Script para generar reporte de cobertura excluyendo clases marcadas con [ExcludeFromCodeCoverage]
# Ejecutar desde la raíz del proyecto: .\test.ps1

Write-Host "Limpiando carpetas de reportes anteriores..." -ForegroundColor Yellow

# Eliminar carpeta TestResults si existe
$testResultsPath = "C:\Users\angel\RiderProjects\FunkoApi\FunkoApi.Tests\TestResults"
if (Test-Path $testResultsPath) {
    Write-Host "  Eliminando: $testResultsPath" -ForegroundColor Gray
    Remove-Item -Path $testResultsPath -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  Carpeta TestResults eliminada" -ForegroundColor Green
} else {
    Write-Host "  Carpeta TestResults no existe (OK)" -ForegroundColor Gray
}

# Eliminar carpeta coverage si existe
$coveragePath = "C:\Users\angel\RiderProjects\FunkoApi\coverage"
if (Test-Path $coveragePath) {
    Write-Host "  Eliminando: $coveragePath" -ForegroundColor Gray
    Remove-Item -Path $coveragePath -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  Carpeta coverage eliminada" -ForegroundColor Green
} else {
    Write-Host "  Carpeta coverage no existe (OK)" -ForegroundColor Gray
}

Write-Host ""

# Ejecutar los tests con cobertura
Write-Host "Ejecutando tests con cobertura..." -ForegroundColor Cyan
dotnet test --collect:"XPlat Code Coverage"

# Generar el reporte HTML excluyendo clases específicas
Write-Host "`nGenerando reporte HTML..." -ForegroundColor Cyan

reportgenerator `
    -reports:**/coverage.cobertura.xml `
    -targetdir:coverage `
    -reporttypes:Html `
    -classfilters:"-FunkoApi.Migrations.*;-FunkoApi.DTOs.*;-FunkoApi.Configuration.*;-FunkoApi.GraphQL.*;-FunkoApi.Models.*;-FunkoApi.Mappers.*;-FunkoApi.Errors.*;-FunkoApi.Storage.*;-FunkoApi.WebSockets.*;-FunkoApi.Repositories.*;-FunkoApi.Controllers.AuthController;-FunkoApi.Program;-FunkoApi.Data.AppSeeder;-FunkoApi.Data.FunkoDbContext;-Program"

Write-Host "`nReporte generado en: coverage\index.html" -ForegroundColor Green
Write-Host "Abriendo reporte..." -ForegroundColor Cyan

# Abrir el reporte en el navegador
Start-Process "coverage\index.html"
