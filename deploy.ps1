<#
.SYNOPSIS
    Deploy Azure AI Foundry Resources Script

.DESCRIPTION
    This script deploys Azure AI Foundry resources with gpt-5-mini and text-embedding-ada-002 models
    Compatible with Windows PowerShell and PowerShell Core

.EXAMPLE
    .\deploy.ps1
#>

$ErrorActionPreference = "Stop"

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Azure AI Foundry Resource Deployment" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Check for Azure CLI
try {
    $null = az version 2>$null
    Write-Host "✓ Azure CLI found" -ForegroundColor Green
}
catch {
    Write-Host "❌ Error: Azure CLI is not installed" -ForegroundColor Red
    Write-Host "Please install Azure CLI: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli" -ForegroundColor Yellow
    exit 1
}

# Check if logged in to Azure
try {
    $null = az account show 2>$null
    Write-Host "✓ Logged in to Azure" -ForegroundColor Green
}
catch {
    Write-Host "❌ Error: Not logged in to Azure" -ForegroundColor Red
    Write-Host "Please run: az login" -ForegroundColor Yellow
    exit 1
}

# Get current subscription
$subscriptionId = az account show --query id -o tsv
$subscriptionName = az account show --query name -o tsv
Write-Host "📋 Using subscription: $subscriptionName ($subscriptionId)" -ForegroundColor Cyan
Write-Host ""

# Prompt for environment name
$envName = Read-Host "Enter environment name (lowercase, alphanumeric, max 64 chars)"
if ([string]::IsNullOrWhiteSpace($envName)) {
    Write-Host "❌ Environment name cannot be empty" -ForegroundColor Red
    exit 1
}

# Prompt for location
Write-Host ""
Write-Host "Select Azure region:" -ForegroundColor Yellow
Write-Host "1. East US (eastus)"
Write-Host "2. East US 2 (eastus2)"
Write-Host "3. West US (westus)"
Write-Host "4. West US 2 (westus2)"
Write-Host "5. West US 3 (westus3)"
Write-Host "6. Central US (centralus)"
Write-Host "7. West Europe (westeurope)"
Write-Host "8. North Europe (northeurope)"
Write-Host "9. UK South (uksouth)"
Write-Host "10. Southeast Asia (southeastasia)"
$locationChoice = Read-Host "Enter choice (1-10) or type custom location"

$location = switch ($locationChoice) {
    "1" { "eastus" }
    "2" { "eastus2" }
    "3" { "westus" }
    "4" { "westus2" }
    "5" { "westus3" }
    "6" { "centralus" }
    "7" { "westeurope" }
    "8" { "northeurope" }
    "9" { "uksouth" }
    "10" { "southeastasia" }
    default { $locationChoice }
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Deployment Configuration" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Environment Name: $envName"
Write-Host "Location: $location"
Write-Host "Subscription: $subscriptionName"
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

$confirm = Read-Host "Proceed with deployment? (y/n)"
if ($confirm -ne "y" -and $confirm -ne "Y") {
    Write-Host "Deployment cancelled" -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "🚀 Starting deployment..." -ForegroundColor Green
Write-Host ""

# Determine which bicep files to use
$infraDir = $null
if (Test-Path "./src/ZavaAppHost/infra") {
    $infraDir = "./src/ZavaAppHost/infra"
    Write-Host "📁 Using infrastructure from: src/ZavaAppHost/infra" -ForegroundColor Cyan
}
elseif (Test-Path "./infra") {
    $infraDir = "./infra"
    Write-Host "📁 Using infrastructure from: infra" -ForegroundColor Cyan
}
else {
    Write-Host "❌ Error: No infrastructure directory found" -ForegroundColor Red
    exit 1
}

$resourceGroup = "rg-$envName"

# Create resource group
Write-Host "📦 Creating resource group: $resourceGroup" -ForegroundColor Cyan
az group create --name $resourceGroup --location $location --tags "azd-env-name=$envName" | Out-Null

# Generate SQL password
$sqlPassword = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 16 | ForEach-Object {[char]$_}) + "A1!"

# Deploy infrastructure
Write-Host "🔧 Deploying Azure AI Foundry resources..." -ForegroundColor Cyan
Write-Host "   This may take 5-10 minutes..." -ForegroundColor Yellow

try {
    $deploymentOutput = az deployment sub create `
        --name "deploy-$envName-$(Get-Date -Format 'yyyyMMddHHmmss')" `
        --location $location `
        --template-file "$infraDir/main.bicep" `
        --parameters environmentName=$envName `
        --parameters location=$location `
        --parameters aifoundryproject="" `
        --parameters customerinformationagentid="" `
        --parameters inventoryagentid="" `
        --parameters locationserviceagentid="" `
        --parameters navigationagentid="" `
        --parameters photoanalyzeragentid="" `
        --parameters productmatchmakingagentid="" `
        --parameters sql_password=$sqlPassword `
        --parameters toolreasoningagentid="" `
        --query properties.outputs `
        -o json 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        throw "Deployment failed: $deploymentOutput"
    }
    
    $outputs = $deploymentOutput | ConvertFrom-Json
    
    Write-Host "✅ Deployment completed successfully!" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "❌ Deployment failed:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# Parse outputs
$aifoundryConnection = $outputs.AIFOUNDRY_CONNECTIONSTRING.value
$appInsightsConnection = $outputs.APPINSIGHTS_APPINSIGHTSCONNECTIONSTRING.value

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "🎉 Deployment Complete!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📋 Resource Group: $resourceGroup" -ForegroundColor Cyan
Write-Host "📍 Location: $location" -ForegroundColor Cyan
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Connection Strings" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

if ($aifoundryConnection) {
    Write-Host "🔗 Azure AI Foundry (OpenAI):" -ForegroundColor Yellow
    Write-Host "   $aifoundryConnection" -ForegroundColor White
    Write-Host ""
    
    # Get the AI Foundry account name and fetch API key
    $aifoundryName = az cognitiveservices account list --resource-group $resourceGroup --query "[?kind=='OpenAI' && contains(name, 'aifoundry')].name | [0]" -o tsv
    if ($aifoundryName) {
        $aifoundryKey = az cognitiveservices account keys list --resource-group $resourceGroup --name $aifoundryName --query key1 -o tsv
        if ($aifoundryKey) {
            Write-Host "   With API Key:" -ForegroundColor Yellow
            Write-Host "   $aifoundryConnection;ApiKey=$aifoundryKey" -ForegroundColor White
            Write-Host ""
        }
    }
}
else {
    Write-Host "⚠️  AI Foundry connection string not found" -ForegroundColor Yellow
}

if ($appInsightsConnection) {
    Write-Host "📊 Application Insights:" -ForegroundColor Yellow
    Write-Host "   $appInsightsConnection" -ForegroundColor White
    Write-Host ""
}

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Deployed Models" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ gpt-5-mini (version: 2025-08-07)" -ForegroundColor Green
Write-Host "   - Deployment Name: gpt-5-mini"
Write-Host "   - SKU: GlobalStandard"
Write-Host "   - Capacity: 8"
Write-Host ""
Write-Host "✅ text-embedding-ada-002 (version: 2)" -ForegroundColor Green
Write-Host "   - Deployment Name: text-embedding-ada-002"
Write-Host "   - SKU: Standard"
Write-Host "   - Capacity: 8"
Write-Host ""

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Next Steps" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Copy the connection strings above to your application configuration"
Write-Host "2. Use the following in your appsettings.json or user secrets:"
Write-Host ""
Write-Host "   {" -ForegroundColor Gray
Write-Host "     `"ConnectionStrings`": {" -ForegroundColor Gray

if ($aifoundryConnection -and $aifoundryKey) {
    Write-Host "       `"aifoundry`": `"$aifoundryConnection;ApiKey=$aifoundryKey`"," -ForegroundColor Gray
}
if ($appInsightsConnection) {
    Write-Host "       `"applicationinsights`": `"$appInsightsConnection`"" -ForegroundColor Gray
}

Write-Host "     }" -ForegroundColor Gray
Write-Host "   }" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Deploy AI Foundry agents using Azure AI Foundry portal: https://ai.azure.com"
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Save outputs to file
$outputFile = "deployment-$envName-$(Get-Date -Format 'yyyyMMdd-HHmmss').txt"
$outputContent = @"
Deployment Outputs - $(Get-Date)
==================================

Resource Group: $resourceGroup
Location: $location

"@

if ($aifoundryConnection -and $aifoundryKey) {
    $outputContent += "AI Foundry Connection: $aifoundryConnection;ApiKey=$aifoundryKey`n"
}
if ($appInsightsConnection) {
    $outputContent += "Application Insights Connection: $appInsightsConnection`n"
}

$outputContent | Out-File -FilePath $outputFile -Encoding UTF8

Write-Host "💾 Connection strings saved to: $outputFile" -ForegroundColor Green
Write-Host ""
