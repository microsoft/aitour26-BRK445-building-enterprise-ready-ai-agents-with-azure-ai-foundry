using SharedEntities;
using ZavaFoundryAgentsProvider;

namespace AgentsCatalogService;

public static class AgentCatalog
{
    public static readonly List<AvailableAgent> Agents = new List<AvailableAgent>();

    public static void InitAgentList(IConfiguration config)
    {
        Agents.Clear();
        foreach (AgentNamesProvider.AgentName agentEnum in System.Enum.GetValues(typeof(AgentNamesProvider.AgentName)))
        {
            var agentName = AgentNamesProvider.GetAgentName(agentEnum);
            Agents.Add(new AvailableAgent
            {
                AgentId = agentName.ToLowerInvariant() + "id",
                AgentCnnStringId = agentName.ToLowerInvariant() + "id",
                AgentName = agentName.Replace("Agent", " Agent"),
                Description = $"Agent for {agentName.Replace("Agent", " agent").Replace("Matchmaking", "matchmaking").Replace("Reasoning", "reasoning").Replace("Information", "information").Replace("PhotoAnalyzer", "photo analysis").Replace("ProductSearch", "product search").Replace("LocationService", "location service").Replace("Navigation", "navigation").Replace("Inventory", "inventory").ToLower()}"
            });
        }
        foreach (var agent in Agents)
        {
            var agentId = config.GetConnectionString(agent.AgentCnnStringId);
            if (!string.IsNullOrEmpty(agentId))
            {
                agent.AgentId = agentId;
            }
        }
    }

    public static AvailableAgent? GetAgent(string agentId)
    {
        return Agents.FirstOrDefault(a => a.AgentId.Equals(agentId, StringComparison.OrdinalIgnoreCase));
    }

    public static string GetAgentName(string agentId)
    {
        var agent = GetAgent(agentId);
        return agent?.AgentName ?? "AI Assistant";
    }

}