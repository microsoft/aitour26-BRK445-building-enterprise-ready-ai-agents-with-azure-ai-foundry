using SharedEntities;

namespace AgentsCatalogService;

public static class AgentCatalog
{
    public static readonly List<AvailableAgent> Agents = new List<AvailableAgent>();

    public static void InitAgentList(IConfiguration config)
    {
        Agents.Add(new AvailableAgent
        {
            AgentId = "customerinformationagentid",
            AgentCnnStringId = "customerinformationagentid",
            AgentName = "Customer Information Agent",
            Description = "Manages customer information and provides personalized recommendations"
        });
        Agents.Add(new AvailableAgent
        {
            AgentId = "inventoryagentid",
            AgentCnnStringId = "inventoryagentid",
            AgentName = "Inventory Agent",
            Description = "Searches product inventory and provides availability information"
        });
        Agents.Add(new AvailableAgent
        {
            AgentId = "locationserviceagentid",
            AgentCnnStringId = "locationserviceagentid",
            AgentName = "Location Service Agent",
            Description = "Provides location-based services and store information"
        });
        Agents.Add(new AvailableAgent
        {
            AgentId = "navigationagentid",
            AgentCnnStringId = "navigationagentid",
            AgentName = "Navigation Agent",
            Description = "Provides navigation and routing assistance within stores"
        });
        Agents.Add(new AvailableAgent
        {
            AgentId = "photoanalyzeragentid",
            AgentCnnStringId = "photoanalyzeragentid",
            AgentName = "Photo Analysis Agent",
            Description = "Analyzes photos and identifies materials and project requirements"
        });
        Agents.Add(new AvailableAgent
        {
            AgentId = "productmatchmakingagentid",
            AgentCnnStringId = "productmatchmakingagentid",
            AgentName = "Product Matchmaking Agent",
            Description = "Matches products to customer needs and project requirements"
        });
        Agents.Add(new AvailableAgent
        {
            AgentId = "toolreasoningagentid",
            AgentCnnStringId = "toolreasoningagentid",
            AgentName = "Tool Reasoning Agent",
            Description = "Provides reasoning for tool recommendations based on DIY projects"
        });
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