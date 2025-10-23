#pragma warning disable SKEXP0110 

using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.SemanticKernel.Agents.AzureAI;

namespace ZavaAIFoundrySKAgentsProvider;

public class AIFoundryAgentProvider
{
    private readonly PersistentAgentsClient _agentsClient;
    private readonly string _agentId;

    public AIFoundryAgentProvider(
        string azureAIFoundryEndpoint,
        string agentId)
    {
        _agentId = agentId;
        _agentsClient = AzureAIAgent.CreateAgentsClient(
            endpoint: azureAIFoundryEndpoint, 
            new AzureCliCredential());
    }

    public async Task<AzureAIAgent> CreateAzureAIAgentAsync(string agentId = "") 
    {
        // validate if agentId  is null use _agentId
        if (string.IsNullOrWhiteSpace(agentId))
        {
            agentId = _agentId;
        }

        PersistentAgent definition = await _agentsClient.Administration.GetAgentAsync(
    agentId);
        AzureAIAgent agent = new(definition, _agentsClient);
        return agent;
    }

    public AzureAIAgent CreateAzureAIAgent(string agentId = "")
    {
        // validate if agentId  is null use _agentId
        if (string.IsNullOrWhiteSpace(agentId))
        {
            agentId = _agentId;
        }

        PersistentAgent definition = _agentsClient.Administration.GetAgent(
    agentId);
        AzureAIAgent agent = new(definition, _agentsClient);
        return agent;
    }
}
