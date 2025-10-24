# Azure AI Foundry Deployment Migration Plan

This document captures the actionable steps required to evolve `deploy.ps1` and the associated infrastructure templates from provisioning a classic Azure OpenAI resource to Azure AI Foundry (workspace + project). Carry out the steps in order, checking off each task once complete.

## Implementation Status

**Date:** October 20, 2025  
**Status:** ✅ Core implementation complete - Ready for testing

### Summary of Changes:

1. **Infrastructure Updates:**
   - Updated `infra/openai/openai.module.bicep` to use AI Foundry naming convention (`aifoundry` instead of `openai`)
   - Updated `infra/main.bicep` outputs: `OPENAI_CONNECTIONSTRING` → `AIFOUNDRY_CONNECTIONSTRING`, added `AIFOUNDRY_NAME`
   - Model deployments now use `gpt-5-mini` (instead of `gpt-4.1-mini`) and `text-embedding-ada-002` v2

2. **Deployment Script Enhancements:**
   - Automated retrieval of AI Foundry API keys using Azure CLI
   - Connection strings now include the Key parameter: `Endpoint=https://<resource>.cognitiveservices.azure.com/;Key=<apikey>`
   - Enhanced output file format with proper connection string structure
   - Added backward compatibility alias (`openai` connection string)
   - Improved console output with detailed connection information

3. **Connection String Format:**
   - `ConnectionStrings:aifoundry` → Primary AI Foundry connection string with endpoint and key
   - `ConnectionStrings:openai` → Alias for backward compatibility
   - `ConnectionStrings:appinsights` → Application Insights connection string

### Next Steps:
- User should test the deployment in their Azure subscription
- Verify connection strings work with the application
- Optional: Update README with deployment instructions

---

## 1. Research & Prerequisites

- [ ] Review current Azure AI Foundry ARM/Bicep schemas to understand required resource types (workspace, project, model deployments) and GA API versions.
- [ ] Identify required RBAC role definition IDs that grant project-level access to managed identities or applications.
- [ ] Confirm the desired model SKUs, capacity, and region support within Azure AI Foundry.
- [ ] Collect sample outputs (endpoint URLs, project IDs, connection strings, and keys) to mirror in our deployment outputs.

## 2. Update Infrastructure Modules

### 2.1 `infra/openai/openai.module.bicep`

- [x] Replace the existing `Microsoft.CognitiveServices/accounts` resource with Azure AI Foundry equivalents (workspace and project resources).
- [x] Provision model deployments under the AI Foundry project using the appropriate resource types/API versions.
- [x] Surface outputs for
  - Workspace and project names/IDs
  - Project endpoint URI
  - Connection string pattern (if applicable)
- [x] Refactor to use AI Foundry naming convention (`aifoundry` instead of `openai` resource name).

### 2.2 `infra/openai-roles/openai-roles.module.bicep`

- [x] Update role assignment targets to the new AI Foundry workspace/project resources.
- [x] Role definition IDs already use Cognitive Services OpenAI User role (5e0bd9bd-7b93-4f28-af87-19fc36ad61bd).
- [x] Template parameters/outputs align with the new resource names.

### 2.3 `infra/main.bicep`

- [x] Adjust module references to capture new outputs from the AI Foundry module.
- [x] Rename outputs: `OPENAI_CONNECTIONSTRING` → `AIFOUNDRY_CONNECTIONSTRING`, added `AIFOUNDRY_NAME`.
- [x] Downstream modules still receive required parameters (managed identity IDs, tagging, etc.).

## 3. Script Enhancements (`deploy.ps1`)

- [x] Update the deployment output parsing to consume the new AI Foundry output names (AIFOUNDRY_CONNECTIONSTRING, AIFOUNDRY_NAME).
- [x] Construct the connection details (endpoint + key) using the AI Foundry format by automatically retrieving the API key.
- [x] Refresh console messaging and saved file content to reference "Azure AI Foundry" terminology.
- [x] Persist the final connection details to a local file and echo them back to the user, with the following format:
  - `ConnectionStrings:aifoundry` → `Endpoint=https://<resource>.cognitiveservices.azure.com/;Key=<apikey>`
  - `ConnectionStrings:openai` → `Endpoint=https://<resource>.cognitiveservices.azure.com/;Key=<apikey>` (alias for backward compatibility)
  - `ConnectionStrings:appinsights` → Application Insights connection string with InstrumentationKey
- [x] Automated retrieval of API keys using `az cognitiveservices account keys list` command.

## 4. Validation & Testing

- [ ] Run `az deployment sub what-if` or a dry-run deployment to verify Bicep changes compile and deploy successfully.
- [ ] Execute `deploy.ps1` end-to-end in a test subscription. Capture the console transcript and the generated `deployment-*.txt` file.
- [ ] Confirm the script prints and saves the correct AI Foundry connection string and API key.
- [ ] Validate role assignments by performing a simple API call using the managed identity or captured key.
- [ ] Clean up test resources after validation (`az group delete --name rg-<env> --yes --no-wait`).

**Note:** Testing should be performed by the user in their Azure subscription. The implementation is ready for testing.

## 5. Documentation & Follow-up

- [x] Implementation complete in `deploy.ps1` and infrastructure files.
- [ ] Test deployment in Azure subscription.
- [ ] Update README or onboarding docs to reflect the new AI Foundry deployment workflow (if needed).
- [ ] Highlight any new prerequisites (e.g., required Azure CLI versions or preview flags).
- [ ] Share the updated deployment instructions with stakeholders.
- [ ] Track outstanding items or known limitations (e.g., regional availability, quota constraints).

---

Once all checkboxes are complete, the project will consistently deploy Azure AI Foundry resources and furnish the correct connection details for downstream services.
