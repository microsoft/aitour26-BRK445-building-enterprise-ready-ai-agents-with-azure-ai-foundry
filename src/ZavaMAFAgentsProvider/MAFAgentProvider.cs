using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Agents.AI;

namespace ZavaMAFAgentsProvider;

public class MAFAgentProvider
{
    private readonly PersistentAgentsClient _agentsClient;
    private readonly string _agentId;

    public MAFAgentProvider(
        string azureAIFoundryProjectEndpoint)
    {
        _agentsClient = new PersistentAgentsClient(
            azureAIFoundryProjectEndpoint!,
            new AzureCliCredential());
    }

    public async Task<AIAgent> GetAIAgentAsync(string agentId = "")
    {
        // validate if agentId  is null use _agentId
        if (string.IsNullOrWhiteSpace(agentId))
        {
            agentId = _agentId;
        }

        var agent = await _agentsClient.GetAIAgentAsync(agentId);
        return agent;
    }

    public AIAgent GetAIAgent(string agentId = "")
    {
        // validate if agentId  is null use _agentId
        if (string.IsNullOrWhiteSpace(agentId))
        {
            agentId = _agentId;
        }

        return _agentsClient.GetAIAgent(agentId);
    }
}

