# Apply EF Core migrations to PostgreSQL

Write-Host "🔄 Applying database migrations..." -ForegroundColor Green
Write-Host ""

$BackendPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $BackendPath

# Navigate to API project
Set-Location MonProfAI.Api

# Apply migrations
Write-Host "Running: dotnet ef database update" -ForegroundColor Cyan
dotnet ef database update -p ../MonProfAI.Infrastructure

Write-Host ""
Write-Host "✅ Migrations applied successfully!" -ForegroundColor Green
Write-Host ""
