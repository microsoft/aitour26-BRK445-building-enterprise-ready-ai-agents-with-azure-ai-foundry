#!/usr/bin/env pwsh

Write-Host "Building and validating all .NET projects..." -ForegroundColor Green

$projects = Get-ChildItem -Path .\src -Recurse -Filter *.csproj
$infraProjects = Get-ChildItem -Path .\infra -Recurse -Filter *.csproj
$allProjects = $projects + $infraProjects

$successCount = 0
$failureCount = 0
$failures = @()

foreach ($project in $allProjects) {
    Write-Host "`nBuilding: $($project.Name)" -ForegroundColor Cyan
    
    $output = dotnet build $project.FullName --no-restore 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ Success" -ForegroundColor Green
        $successCount++
    }
    else {
        Write-Host "  ✗ Failed" -ForegroundColor Red
        $failureCount++
        $failures += $project.Name
    }
}

Write-Host "`n========================================" -ForegroundColor Yellow
Write-Host "Build Summary:" -ForegroundColor Yellow
Write-Host "  Successful: $successCount" -ForegroundColor Green
Write-Host "  Failed: $failureCount" -ForegroundColor Red

if ($failureCount -gt 0) {
    Write-Host "`nFailed projects:" -ForegroundColor Red
    foreach ($failure in $failures) {
        Write-Host "  - $failure" -ForegroundColor Red
    }
    exit 1
}
else {
    Write-Host "`nAll projects built successfully! ✓" -ForegroundColor Green
    exit 0
}
