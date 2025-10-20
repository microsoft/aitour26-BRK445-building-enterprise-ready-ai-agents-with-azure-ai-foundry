# Azure AI Foundry Deployment Guide

This guide explains how to deploy Azure AI Foundry resources with the required models for this project.

## Overview

The deployment scripts automatically create:
- ✅ Azure AI Foundry (Cognitive Services OpenAI) resource
- ✅ **gpt-5-mini** model deployment (version: 2025-08-07)
- ✅ **text-embedding-ada-002** model deployment (version: 2)
- ✅ Application Insights for monitoring
- ✅ All necessary Azure infrastructure

## Prerequisites

### Required Tools
- **Azure CLI** - [Install Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- **Active Azure Subscription** - [Create free account](https://azure.microsoft.com/free/)

### For Codespaces/Linux:
- Bash shell
- `jq` (JSON processor) - usually pre-installed in Codespaces

### For Windows:
- PowerShell 5.1 or PowerShell Core 7+

## Quick Start

### Option 1: Codespaces/Linux/Mac

1. **Login to Azure:**
   ```bash
   az login
   ```

2. **Run the deployment script:**
   ```bash
   ./deploy.sh
   ```

3. **Follow the prompts:**
   - Enter environment name (e.g., `myproject`)
   - Select Azure region
   - Confirm deployment

4. **Wait for completion** (5-10 minutes)

5. **Copy the connection strings** displayed at the end

### Option 2: Windows

1. **Login to Azure:**
   ```powershell
   az login
   ```

2. **Run the deployment script:**
   ```powershell
   .\deploy.ps1
   ```

3. **Follow the prompts:**
   - Enter environment name (e.g., `myproject`)
   - Select Azure region
   - Confirm deployment

4. **Wait for completion** (5-10 minutes)

5. **Copy the connection strings** displayed at the end

## What Gets Deployed

### Azure Resources

The deployment creates the following resources in a new resource group named `rg-{your-environment-name}`:

1. **Azure AI Foundry (OpenAI) Account**
   - Type: Cognitive Services OpenAI
   - SKU: S0 (Standard)
   - Public network access enabled
   - Custom subdomain configured

2. **Model Deployments**
   - **gpt-5-mini**
     - Version: 2025-08-07
     - SKU: GlobalStandard
     - Capacity: 8 units
   - **text-embedding-ada-002**
     - Version: 2
     - SKU: Standard
     - Capacity: 8 units

3. **Application Insights**
   - For application monitoring and telemetry

4. **Supporting Infrastructure**
   - Container Registry
   - Container Apps Environment
   - Log Analytics Workspace
   - Storage Account (for volumes)
   - Managed Identity (for secure access)

### Connection Strings

After deployment, you'll receive connection strings in the format:

```
Azure AI Foundry:
Endpoint=https://aifoundry-xxxxxxxxx.cognitiveservices.azure.com/;ApiKey=xxxxxxxxxxxxxxxxxxxxx

Application Insights:
InstrumentationKey=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx;...
```

## Using the Connection Strings

### Method 1: User Secrets (Recommended for Development)

1. Navigate to your project directory:
   ```bash
   cd src/ZavaAppHost
   ```

2. Set user secrets:
   ```bash
   dotnet user-secrets set "ConnectionStrings:aifoundry" "Endpoint=...;ApiKey=..."
   dotnet user-secrets set "ConnectionStrings:applicationinsights" "InstrumentationKey=..."
   ```

### Method 2: appsettings.json (Not recommended for production)

Add to `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "aifoundry": "Endpoint=https://...;ApiKey=...",
    "applicationinsights": "InstrumentationKey=..."
  }
}
```

⚠️ **Warning:** Never commit API keys to source control!

### Method 3: Environment Variables

```bash
# Linux/Mac
export ConnectionStrings__aifoundry="Endpoint=...;ApiKey=..."
export ConnectionStrings__applicationinsights="InstrumentationKey=..."

# Windows PowerShell
$env:ConnectionStrings__aifoundry="Endpoint=...;ApiKey=..."
$env:ConnectionStrings__applicationinsights="InstrumentationKey=..."
```

## Azure AI Foundry Agents Setup

After deploying the infrastructure, you need to create the AI agents in Azure AI Foundry:

1. **Navigate to Azure AI Foundry Portal:**
   - Visit [https://ai.azure.com](https://ai.azure.com)
   - Sign in with your Azure account

2. **Create a New Project:**
   - Click "New project"
   - Select the OpenAI resource you just deployed
   - Configure project settings

3. **Create Required Agents:**
   
   According to the project requirements, create these agents:
   - `customerinformationagentid` - Customer information agent
   - `inventoryagentid` - Inventory management agent
   - `locationserviceagentid` - Location service agent
   - `navigationagentid` - Navigation agent
   - `photoanalyzeragentid` - Photo analyzer agent
   - `productmatchmakingagentid` - Product matchmaking agent
   - `toolreasoningagentid` - Tool reasoning agent

4. **Copy Agent IDs:**
   - After creating each agent, copy its ID
   - Store these IDs in your application configuration

5. **Update Configuration:**
   ```bash
   dotnet user-secrets set "aifoundryproject" "your-project-endpoint"
   dotnet user-secrets set "customerinformationagentid" "agent-id-1"
   dotnet user-secrets set "inventoryagentid" "agent-id-2"
   # ... and so on for all agents
   ```

## Troubleshooting

### Login Issues
```bash
# Clear Azure CLI cache
az account clear
az login
```

### Deployment Failures

**Quota Exceeded:**
- Check your subscription's OpenAI quota
- Request quota increase in Azure Portal

**Location Not Available:**
- Try a different Azure region
- Check [Azure Products by Region](https://azure.microsoft.com/global-infrastructure/services/?products=cognitive-services)

**Permission Errors:**
- Ensure you have "Contributor" or "Owner" role on the subscription
- Contact your Azure administrator

### Model Deployment Issues

**Model Not Available:**
- The gpt-5-mini model requires specific regions
- Try: East US, West US, or West Europe

**Version Mismatch:**
- The scripts use the latest versions
- Check [Azure OpenAI Model Versions](https://learn.microsoft.com/azure/ai-services/openai/concepts/models)

## Cleanup

To delete all deployed resources:

```bash
# List your resource groups
az group list --query "[?tags.\"azd-env-name\"].name" -o table

# Delete the resource group
az group delete --name rg-{your-environment-name} --yes --no-wait
```

⚠️ **Warning:** This permanently deletes all resources in the resource group!

## Cost Estimation

Approximate monthly costs (as of October 2025):

- **OpenAI S0 SKU:** Base charge + usage-based pricing
- **gpt-5-mini:** ~$0.40 per 1M input tokens, ~$1.20 per 1M output tokens
- **text-embedding-ada-002:** ~$0.10 per 1M tokens
- **Application Insights:** ~$2.30/GB for data ingestion
- **Container Infrastructure:** Variable based on usage

💡 **Tip:** Use Azure Cost Management to monitor actual costs

## Additional Resources

- [Azure AI Foundry Documentation](https://learn.microsoft.com/azure/ai-services/openai/)
- [Azure OpenAI Pricing](https://azure.microsoft.com/pricing/details/cognitive-services/openai-service/)
- [Model Availability](https://learn.microsoft.com/azure/ai-services/openai/concepts/models)
- [Responsible AI Guidelines](https://learn.microsoft.com/azure/ai-services/openai/concepts/responsible-ai)

## Support

For issues with:
- **Deployment scripts:** Create an issue in this repository
- **Azure services:** [Azure Support](https://azure.microsoft.com/support/)
- **Azure AI Foundry:** [Azure AI Foundry Support](https://learn.microsoft.com/answers/tags/387/azure-openai)
