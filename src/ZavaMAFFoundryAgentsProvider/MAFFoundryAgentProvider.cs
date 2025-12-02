using Azure.AI.Projects;
using Azure.Identity;

namespace ZavaMAFFoundryAgentsProvider;

/// <summary>
/// Provider for Microsoft Agent Framework using Microsoft Foundry Agents.
/// This provider creates and manages AIAgents backed by Azure Foundry Agents.
/// </summary>
public class MAFFoundryAgentProvider
{
    private readonly string _azureFoundryProjectEndpoint;
    private readonly string _deploymentName;

    public MAFFoundryAgentProvider(
        string azureFoundryProjectEndpoint,
        string deploymentName = "gpt-4o-mini")
    {
        _azureFoundryProjectEndpoint = azureFoundryProjectEndpoint;
        _deploymentName = deploymentName;
    }

    /// <summary>
    /// Gets the Azure Foundry Project endpoint.
    /// </summary>
    public string AzureFoundryProjectEndpoint => _azureFoundryProjectEndpoint;

    /// <summary>
    /// Gets the deployment name used for agent creation.
    /// </summary>
    public string DeploymentName => _deploymentName;

    /// <summary>
    /// Creates an AIProjectClient for interacting with Azure Foundry Agents.
    /// Uses DefaultAzureCredential for flexible authentication across development and production.
    /// </summary>
    /// <returns>A configured AIProjectClient instance.</returns>
    public AIProjectClient CreateAIProjectClient()
    {
        return new AIProjectClient(
            _azureFoundryProjectEndpoint,
            new DefaultAzureCredential());
    }
}
