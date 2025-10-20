#!/bin/bash
set -e

# Deploy Azure AI Foundry Resources Script
# This script deploys Azure AI Foundry resources with gpt-5-mini and text-embedding-ada-002 models
# Compatible with Codespaces and Linux environments

echo "=================================================="
echo "Azure AI Foundry Resource Deployment"
echo "=================================================="
echo ""

# Check for Azure CLI
if ! command -v az &> /dev/null; then
    echo "❌ Error: Azure CLI is not installed"
    echo "Please install Azure CLI: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
fi

echo "✓ Azure CLI found"

# Check if logged in to Azure
if ! az account show &> /dev/null; then
    echo "❌ Error: Not logged in to Azure"
    echo "Please run: az login"
    exit 1
fi

echo "✓ Logged in to Azure"

# Get current subscription
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
SUBSCRIPTION_NAME=$(az account show --query name -o tsv)
echo "📋 Using subscription: $SUBSCRIPTION_NAME ($SUBSCRIPTION_ID)"
echo ""

# Prompt for environment name
read -p "Enter environment name (lowercase, alphanumeric, max 64 chars): " ENV_NAME
if [ -z "$ENV_NAME" ]; then
    echo "❌ Environment name cannot be empty"
    exit 1
fi

# Prompt for location
echo ""
echo "Select Azure region:"
echo "1. East US (eastus)"
echo "2. East US 2 (eastus2)"
echo "3. West US (westus)"
echo "4. West US 2 (westus2)"
echo "5. West US 3 (westus3)"
echo "6. Central US (centralus)"
echo "7. West Europe (westeurope)"
echo "8. North Europe (northeurope)"
echo "9. UK South (uksouth)"
echo "10. Southeast Asia (southeastasia)"
read -p "Enter choice (1-10) or type custom location: " LOCATION_CHOICE

case $LOCATION_CHOICE in
    1) LOCATION="eastus";;
    2) LOCATION="eastus2";;
    3) LOCATION="westus";;
    4) LOCATION="westus2";;
    5) LOCATION="westus3";;
    6) LOCATION="centralus";;
    7) LOCATION="westeurope";;
    8) LOCATION="northeurope";;
    9) LOCATION="uksouth";;
    10) LOCATION="southeastasia";;
    *) LOCATION="$LOCATION_CHOICE";;
esac

echo ""
echo "=================================================="
echo "Deployment Configuration"
echo "=================================================="
echo "Environment Name: $ENV_NAME"
echo "Location: $LOCATION"
echo "Subscription: $SUBSCRIPTION_NAME"
echo "=================================================="
echo ""

read -p "Proceed with deployment? (y/n): " CONFIRM
if [ "$CONFIRM" != "y" ] && [ "$CONFIRM" != "Y" ]; then
    echo "Deployment cancelled"
    exit 0
fi

echo ""
echo "🚀 Starting deployment..."
echo ""

# Determine which bicep files to use (prefer src/ZavaAppHost/infra if it exists)
if [ -d "./src/ZavaAppHost/infra" ]; then
    INFRA_DIR="./src/ZavaAppHost/infra"
    echo "📁 Using infrastructure from: src/ZavaAppHost/infra"
elif [ -d "./infra" ]; then
    INFRA_DIR="./infra"
    echo "📁 Using infrastructure from: infra"
else
    echo "❌ Error: No infrastructure directory found"
    exit 1
fi

RESOURCE_GROUP="rg-${ENV_NAME}"

# Create resource group
echo "📦 Creating resource group: $RESOURCE_GROUP"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --tags "azd-env-name=$ENV_NAME" > /dev/null

# Generate SQL password
SQL_PASSWORD=$(openssl rand -base64 16 | tr -d "=+/" | cut -c1-20)A1!

# Deploy infrastructure
echo "🔧 Deploying Azure AI Foundry resources..."
echo "   This may take 5-10 minutes..."

DEPLOYMENT_OUTPUT=$(az deployment sub create \
    --name "deploy-${ENV_NAME}-$(date +%s)" \
    --location "$LOCATION" \
    --template-file "${INFRA_DIR}/main.bicep" \
    --parameters environmentName="$ENV_NAME" \
    --parameters location="$LOCATION" \
    --parameters aifoundryproject="" \
    --parameters customerinformationagentid="" \
    --parameters inventoryagentid="" \
    --parameters locationserviceagentid="" \
    --parameters navigationagentid="" \
    --parameters photoanalyzeragentid="" \
    --parameters productmatchmakingagentid="" \
    --parameters sql_password="$SQL_PASSWORD" \
    --parameters toolreasoningagentid="" \
    --query properties.outputs -o json 2>&1)

if [ $? -ne 0 ]; then
    echo "❌ Deployment failed:"
    echo "$DEPLOYMENT_OUTPUT"
    exit 1
fi

echo "✅ Deployment completed successfully!"
echo ""

# Parse outputs
AIFOUNDRY_CONNECTION=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.AIFOUNDRY_CONNECTIONSTRING.value // empty')
APPINSIGHTS_CONNECTION=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.APPINSIGHTS_APPINSIGHTSCONNECTIONSTRING.value // empty')

echo "=================================================="
echo "🎉 Deployment Complete!"
echo "=================================================="
echo ""
echo "📋 Resource Group: $RESOURCE_GROUP"
echo "📍 Location: $LOCATION"
echo ""
echo "=================================================="
echo "Connection Strings"
echo "=================================================="
echo ""

if [ -n "$AIFOUNDRY_CONNECTION" ]; then
    echo "🔗 Azure AI Foundry (OpenAI):"
    echo "   $AIFOUNDRY_CONNECTION"
    echo ""
    
    # Get the AI Foundry account name and fetch API key
    AIFOUNDRY_NAME=$(az cognitiveservices account list --resource-group "$RESOURCE_GROUP" --query "[?kind=='OpenAI' && contains(name, 'aifoundry')].name | [0]" -o tsv)
    if [ -n "$AIFOUNDRY_NAME" ]; then
        AIFOUNDRY_KEY=$(az cognitiveservices account keys list --resource-group "$RESOURCE_GROUP" --name "$AIFOUNDRY_NAME" --query key1 -o tsv)
        if [ -n "$AIFOUNDRY_KEY" ]; then
            echo "   With API Key:"
            echo "   ${AIFOUNDRY_CONNECTION};ApiKey=${AIFOUNDRY_KEY}"
            echo ""
        fi
    fi
else
    echo "⚠️  AI Foundry connection string not found"
fi

if [ -n "$APPINSIGHTS_CONNECTION" ]; then
    echo "📊 Application Insights:"
    echo "   $APPINSIGHTS_CONNECTION"
    echo ""
fi

echo "=================================================="
echo "Deployed Models"
echo "=================================================="
echo ""
echo "✅ gpt-5-mini (version: 2025-08-07)"
echo "   - Deployment Name: gpt-5-mini"
echo "   - SKU: GlobalStandard"
echo "   - Capacity: 8"
echo ""
echo "✅ text-embedding-ada-002 (version: 2)"
echo "   - Deployment Name: text-embedding-ada-002"
echo "   - SKU: Standard"
echo "   - Capacity: 8"
echo ""

echo "=================================================="
echo "Next Steps"
echo "=================================================="
echo ""
echo "1. Copy the connection strings above to your application configuration"
echo "2. Use the following in your appsettings.json or user secrets:"
echo ""
echo "   {"
echo "     \"ConnectionStrings\": {"
if [ -n "$AIFOUNDRY_CONNECTION" ] && [ -n "$AIFOUNDRY_KEY" ]; then
    echo "       \"aifoundry\": \"${AIFOUNDRY_CONNECTION};ApiKey=${AIFOUNDRY_KEY}\","
fi
if [ -n "$APPINSIGHTS_CONNECTION" ]; then
    echo "       \"applicationinsights\": \"${APPINSIGHTS_CONNECTION}\""
fi
echo "     }"
echo "   }"
echo ""
echo "3. Deploy AI Foundry agents using Azure AI Foundry portal: https://ai.azure.com"
echo ""
echo "=================================================="
echo ""

# Save outputs to file
OUTPUT_FILE="deployment-${ENV_NAME}-$(date +%Y%m%d-%H%M%S).txt"
{
    echo "Deployment Outputs - $(date)"
    echo "=================================="
    echo ""
    echo "Resource Group: $RESOURCE_GROUP"
    echo "Location: $LOCATION"
    echo ""
    if [ -n "$AIFOUNDRY_CONNECTION" ] && [ -n "$AIFOUNDRY_KEY" ]; then
        echo "AI Foundry Connection: ${AIFOUNDRY_CONNECTION};ApiKey=${AIFOUNDRY_KEY}"
    fi
    if [ -n "$APPINSIGHTS_CONNECTION" ]; then
        echo "Application Insights Connection: $APPINSIGHTS_CONNECTION"
    fi
} > "$OUTPUT_FILE"

echo "💾 Connection strings saved to: $OUTPUT_FILE"
echo ""
