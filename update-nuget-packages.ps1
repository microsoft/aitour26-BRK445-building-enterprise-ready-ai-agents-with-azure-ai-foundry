#!/usr/bin/env pwsh

Write-Host "Updating NuGet packages to latest versions..." -ForegroundColor Green

$projects = Get-ChildItem -Path . -Recurse -Filter *.csproj

foreach ($project in $projects) {
    Write-Host "`nUpdating packages in: $($project.FullName)" -ForegroundColor Cyan
    
    try {
        # Get list of packages
        $packages = dotnet list $project.FullName package --outdated 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            # Update packages
            dotnet list $project.FullName package --outdated | 
                Select-String -Pattern '^\s*>\s+(\S+)' | 
                ForEach-Object {
                    $packageName = $_.Matches.Groups[1].Value
                    Write-Host "  Updating package: $packageName" -ForegroundColor Yellow
                    dotnet add $project.FullName package $packageName
                }
        }
    }
    catch {
        Write-Host "  Error updating packages: $_" -ForegroundColor Red
    }
}

Write-Host "`nNuGet package update complete!" -ForegroundColor Green
