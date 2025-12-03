using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;

namespace ZavaMAFAgentsProvider;

public class MAFAgentProvider
{
    private readonly AIProjectClient _projectClient;
    private readonly string _agentId;

    public MAFAgentProvider(string microsoftFoundryProjectEndpoint)
    {
        _projectClient = new(
            endpoint: new Uri(microsoftFoundryProjectEndpoint),
            tokenProvider: new DefaultAzureCredential());
    }

    public async Task<AIAgent> GetAIAgentAsync(string agentId = "")
    {
        // validate if agentId  is null use _agentId
        if (string.IsNullOrWhiteSpace(agentId))
        {
            agentId = _agentId;
        }

        var agent = await _projectClient.GetAIAgentAsync(agentId);
        return agent;
    }

    public AIAgent GetAIAgent(string agentId = "")
    {
        // validate if agentId  is null use _agentId
        if (string.IsNullOrWhiteSpace(agentId))
        {
            agentId = _agentId;
        }

        return _projectClient.GetAIAgent(agentId);
    }
}

