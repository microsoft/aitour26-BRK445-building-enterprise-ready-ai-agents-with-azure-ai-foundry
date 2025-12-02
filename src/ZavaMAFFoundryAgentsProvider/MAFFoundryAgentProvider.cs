using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;

namespace ZavaMAFFoundryAgentsProvider;

/// <summary>
/// Provider for Microsoft Agent Framework using Microsoft Foundry Agents.
/// This provider creates and manages AIAgents backed by Azure Foundry Agents.
/// </summary>
public class MAFFoundryAgentProvider
{
    private readonly AIProjectClient _aiProjectClient;
    private readonly string _deploymentName;

    public MAFFoundryAgentProvider(
        string azureFoundryProjectEndpoint,
        string deploymentName = "gpt-4o-mini")
    {
        _aiProjectClient = new AIProjectClient(
            new Uri(azureFoundryProjectEndpoint),
            new AzureCliCredential());
        _deploymentName = deploymentName;
    }

    /// <summary>
    /// Creates a new AIAgent with the specified name and instructions.
    /// </summary>
    /// <param name="agentName">The name of the agent to create.</param>
    /// <param name="instructions">The instructions for the agent.</param>
    /// <returns>The created AIAgent.</returns>
    public AIAgent CreateAIAgent(string agentName, string instructions)
    {
        return _aiProjectClient.CreateAIAgent(
            name: agentName,
            model: _deploymentName,
            instructions: instructions);
    }

    /// <summary>
    /// Gets an existing AIAgent by name (latest version).
    /// </summary>
    /// <param name="agentName">The name of the agent to retrieve.</param>
    /// <returns>The AIAgent.</returns>
    public AIAgent GetAIAgent(string agentName)
    {
        return _aiProjectClient.GetAIAgent(name: agentName);
    }

    /// <summary>
    /// Gets the AIProjectClient for direct access to Foundry Agents APIs.
    /// </summary>
    public AIProjectClient AIProjectClient => _aiProjectClient;

    /// <summary>
    /// Gets the deployment name used for agent creation.
    /// </summary>
    public string DeploymentName => _deploymentName;
}
