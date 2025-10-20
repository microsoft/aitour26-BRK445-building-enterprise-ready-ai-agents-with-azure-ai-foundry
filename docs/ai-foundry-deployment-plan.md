# Azure AI Foundry Deployment Migration Plan

This document captures the actionable steps required to evolve `deploy.ps1` and the associated infrastructure templates from provisioning a classic Azure OpenAI resource to Azure AI Foundry (workspace + project). Carry out the steps in order, checking off each task once complete.

---

## 1. Research & Prerequisites

- [ ] Review current Azure AI Foundry ARM/Bicep schemas to understand required resource types (workspace, project, model deployments) and GA API versions.
- [ ] Identify required RBAC role definition IDs that grant project-level access to managed identities or applications.
- [ ] Confirm the desired model SKUs, capacity, and region support within Azure AI Foundry.
- [ ] Collect sample outputs (endpoint URLs, project IDs, connection strings, and keys) to mirror in our deployment outputs.

## 2. Update Infrastructure Modules

### 2.1 `infra/openai/openai.module.bicep`

- [ ] Replace the existing `Microsoft.CognitiveServices/accounts` resource with Azure AI Foundry equivalents (workspace and project resources).
- [ ] Provision model deployments under the AI Foundry project using the appropriate resource types/API versions.
- [ ] Surface outputs for
  - Workspace and project names/IDs
  - Project endpoint URI
  - Connection string pattern (if applicable)
- [ ] Remove or refactor any legacy OpenAI-only properties (e.g., `kind: 'OpenAI'`, `customSubDomainName`).

### 2.2 `infra/openai-roles/openai-roles.module.bicep`

- [ ] Update role assignment targets to the new AI Foundry workspace/project resources.
- [ ] Swap role definition IDs to ones that grant the desired permissions in AI Foundry (e.g., **Azure AI Foundry Project Reader/Contributor** roles).
- [ ] Validate template parameters/outputs align with the new resource names.

### 2.3 `infra/main.bicep`

- [ ] Adjust module references to capture new outputs from the AI Foundry module.
- [ ] Rename any existing outputs (e.g., `OPENAI_CONNECTIONSTRING`) to AI Foundry terminology (`AIFOUNDRY_CONNECTIONSTRING`, `AIFOUNDRY_ENDPOINT`, etc.).
- [ ] Ensure downstream modules still receive required parameters (managed identity IDs, tagging, etc.).

## 3. Script Enhancements (`deploy.ps1`)

- [ ] Update the deployment output parsing to consume the new AI Foundry output names.
- [ ] Construct the connection details (endpoint + key) using the AI Foundry format.
- [ ] Refresh console messaging and saved file content to reference "Azure AI Foundry" terminology.
- [ ] Optionally add validation to ensure required outputs are present; surface helpful errors if missing.
- [ ] Persist the final connection details to a local file and echo them back to the user, ensuring the following entries contain the concrete values returned by the deployment:
  - `ConnectionStrings:openai` → `Endpoint=https://<resource>.cognitiveservices.azure.com/;Key=<apikey>`
  - `ConnectionStrings:appinsights` → `InstrumentationKey=<key>;IngestionEndpoint=https://eastus2-3.in.applicationinsights.azure.com/;LiveEndpoint=https://eastus2.livediagnostics.monitor.azure.com/;ApplicationId=<appid>`
  - `ConnectionStrings:aifoundryproject` → `https://<resource>.services.ai.azure.com/api/projects/<project>`
  - `ConnectionStrings:aifoundry` → `Endpoint=https://<resource>.cognitiveservices.azure.com/;Key=<apikey>;`
- [ ] Review and adapt the PowerShell samples in [Inspect outputs and retrieve keys](https://github.com/microsoft/aitour26-BRK447-agentic-use-of-github-copilot-within-visual-studio/blob/main/session-delivery-resources/docs/01-InitialSetup.md#inspect-outputs-and-retrieve-keys) to automate retrieving secrets and formatting the connection strings.

## 4. Validation & Testing

- [ ] Run `az deployment sub what-if` or a dry-run deployment to verify Bicep changes compile and deploy successfully.
- [ ] Execute `deploy.ps1` end-to-end in a test subscription. Capture the console transcript and the generated `deployment-*.txt` file.
- [ ] Confirm the script prints and saves the correct AI Foundry connection string and API key.
- [ ] Validate role assignments by performing a simple API call using the managed identity or captured key.
- [ ] Clean up test resources after validation (`az group delete --name rg-<env> --yes --no-wait`).

## 5. Documentation & Follow-up

- [ ] Update README or onboarding docs to reflect the new AI Foundry deployment workflow.
- [ ] Highlight any new prerequisites (e.g., required Azure CLI versions or preview flags).
- [ ] Share the updated deployment instructions with stakeholders.
- [ ] Track outstanding items or known limitations (e.g., regional availability, quota constraints).

---

Once all checkboxes are complete, the project will consistently deploy Azure AI Foundry resources and furnish the correct connection details for downstream services.
