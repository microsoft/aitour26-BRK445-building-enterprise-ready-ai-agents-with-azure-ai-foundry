# How to run the demo locally

This file provides step-by-step instructions to build and run the `aspiredemo` (Zava-Aspire) solution locally. It is a split-off from the original `01.Installation.md` to make the run instructions easier to follow.

## Agent Framework Selection

This solution supports two agent frameworks that you can switch between using the **Settings page** in the Store frontend:

1. **Large Language Models (LLM)** - Default usage using directly large language models
1. **Semantic Kernel (SK)** - Default framework using Microsoft.SemanticKernel
2. **Microsoft Agent Framework (AgentFx)** - Uses Microsoft.Agents.AI

### Selecting the Agent Framework

After running the demo:

1. Open the **Store** application in your browser
2. Navigate to **Settings** from the navigation menu (left sidebar)
3. Select your preferred framework
4. Your selection is automatically saved in your browser's localStorage
5. All agent demos will immediately use the selected framework (no restart needed)

**Note:** All the frameworks connect to the same Azure AI Foundry agents. The selection only affects which orchestration framework is used.

## Quick start (terminal)

Open PowerShell / Bash in the solution folder [./src] and run:

```powershell
# Restore dependencies
dotnet restore Zava-Aspire.slnx

# Build the solution
dotnet build Zava-Aspire.slnx

# Trust local dev certs for HTTPS
dotnet dev-certs https --trust

# Run the app host
dotnet run --project ./ZavaAppHost/ZavaAppHost.csproj
```

Or if you installed the .NET Aspire CLI tool, you can run:

```powershell
# From the src folder
aspire run 
```

## Running in CodeSpaces

Check the video here for a walkthrough: **< coming soon >**

## Running in Visual Studio

1. Open `aspiredemo\Zava-Aspire.slnx` in Visual Studio.
2. In Solution Explorer, right-click the project '1 Aspire/ZavaAppHost' to run and select `Set as Startup Project`.
3. Press F5 to run (or Ctrl+F5 to run without debugging).

## Running in Visual Studio Code / Visual Studio Code Insiders

1. Open `aspiredemo` folder in Visual Studio Code / Visual Studio Code Insiders.
2. In Solution Explorer, right-click the project '1 Aspire/ZavaAppHost' to run and select `Set as Startup Project`.
3. Right-Click '1 Aspire/ZavaAppHost', Select 'Debug' -> 'Start New Instance'

The console will display the listening URL(s) (for example `https://localhost:17104/login?t=7040c2fb1bad0ebe1a467bd1ad076f5e`). Open the indicated URL in your browser to access the demo UI.

## 1st run: set secrets

The first time running the solution, the .NET Aspire dashboard will require you to set up the necessary secrets for accessing Azure resources. Complete the values in the form:

- `aifoundry` is the connection string: `Endpoint=https://<your-resource>.cognitiveservices.azure.com/;ApiKey=<your-api-key>`
- `applicationinsights` is the Application Insights connection string from your Application Insights resource
- `aifoundryproject` is the Project endpoint URL from your AI Foundry project settings page
- `customerinformationagentid`, `inventoryagentid` and all the other agent ids are the agent IDs you created in the previous step; see `02.NeededCloudResources.md` for details.

Check the `Save to user secrets` box to save them in your user secrets project for future runs.

![Aspire first run form](./imgs/40-AspireFirstRun.png)

![Set Aspire user secrets](./imgs/45-setAspireUserSecrets.png)

---

## Troubleshooting

### Azure CLI Not Found

**Error**: `bash: az: command not found`

**Solution**: Install Azure CLI in your environment:

```bash
# For Debian/Ubuntu (including dev containers)
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Verify installation
az --version
```

After installation, authenticate with Azure:

```bash
az login --tenant <your-tenant-url> --use-device-code
```

Replace `<your-tenant-url>` with your Azure tenant domain (e.g., `yourcompany.onmicrosoft.com`).

### Build Errors - File Permission Issues

**Error**: `error MSB3374: The last access/last write time on file "obj/Debug/net9.0/*.cache" cannot be set. Access to the path '...' is denied.`

This error commonly occurs in dev containers or when switching between different development environments.

**Solution**: Clean and rebuild the solution:

```bash
cd /workspaces/aitour26-BRK445-building-enterprise-ready-ai-agents-with-azure-ai-foundry/src

# Clean the solution
dotnet clean

# Remove obj and bin folders with elevated permissions
sudo find . -type d -name "obj" -exec rm -rf {} + 2>/dev/null || true
sudo find . -type d -name "bin" -exec rm -rf {} + 2>/dev/null || true

# Rebuild the solution
dotnet build
```

### Missing Aspire Workload

**Error**: `The name 'DistributedApplication' does not exist in the current context` or similar errors in the `ZavaAppHost` project.

**Solution**: Install the .NET Aspire workload:

```bash
# Install Aspire workload
dotnet workload install aspire

# Verify installation
dotnet workload list

# Rebuild the solution
cd /workspaces/aitour26-BRK445-building-enterprise-ready-ai-agents-with-azure-ai-foundry/src
dotnet build
```

### Agent Deployment Issues

**Error**: Authentication failures when running the console application in `/infra` folder.

**Solution**: Ensure you're logged in to Azure CLI before running the agent deployment console application:

```bash
# Login to Azure with device code authentication
az login --tenant <your-tenant-url> --use-device-code

# Verify you're logged in
az account show

# Then run the agent deployment
cd /workspaces/aitour26-BRK445-building-enterprise-ready-ai-agents-with-azure-ai-foundry/infra
dotnet run
```

### Build Warnings

The solution may show numerous nullable reference warnings (CS8604, CS8618, etc.). These are non-critical warnings related to nullable reference types and do not prevent the application from running. They can be safely ignored for development purposes.

### Dev Container Specific Issues

If you're running in a dev container and experience persistent permission issues:

1. **Rebuild the dev container**: Use the VS Code command palette (`Ctrl+Shift+P` or `Cmd+Shift+P`) and select "Dev Containers: Rebuild Container"

2. **Check workspace permissions**: Ensure your workspace folder has appropriate permissions:

   ```bash
   sudo chown -R $(whoami) /workspaces/aitour26-BRK445-building-enterprise-ready-ai-agents-with-azure-ai-foundry
   ```

3. **Clear all build artifacts**: Before rebuilding, ensure all artifacts are removed:

   ```bash
   cd /workspaces/aitour26-BRK445-building-enterprise-ready-ai-agents-with-azure-ai-foundry/src
   git clean -xdf
   dotnet build
   ```

### Additional Help

If you continue experiencing issues:

1. Check that all prerequisites from `Prerequisites.md` are installed
2. Verify your Azure resources are properly configured as described in `02.NeededCloudResources.md`
3. Ensure all user secrets are correctly set as shown in the "1st run: set secrets" section above
4. Review the Application Insights logs for runtime errors
