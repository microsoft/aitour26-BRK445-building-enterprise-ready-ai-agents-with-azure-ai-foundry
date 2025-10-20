using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Agents.AI;

namespace ZavaAgentFxAgentsProvider;

public class AgentFxAgentProvider
{
    private readonly PersistentAgentsClient _agentsClient;
    private readonly string _agentId;

    public AgentFxAgentProvider(
        string azureAIFoundryProjectEndpoint)
    {
        _agentsClient = new PersistentAgentsClient(
            azureAIFoundryProjectEndpoint!,
            new AzureCliCredential());
    }

    public async Task<AIAgent> GetAzureAIAgent(string agentId = "")
    {
        // validate if agentId  is null use _agentId
        if (string.IsNullOrWhiteSpace(agentId))
        {
            agentId = _agentId;
        }

        var agent = await _agentsClient.GetAIAgentAsync(agentId);
        return agent;
    }
}

