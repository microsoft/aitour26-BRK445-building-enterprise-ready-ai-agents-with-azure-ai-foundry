namespace ZavaFoundryAgentsProvider
{
    public class AgentNamesProvider
    {
        public enum AgentName
        {
            ToolReasoningAgent,
            ProductSearchAgent,
            ProductMatchmakingAgent,
            PhotoAnalyzerAgent,
            LocationServiceAgent,
            NavigationAgent,
            CustomerInformationAgent,
            InventoryAgent
        }

        public static string GetAgentName(AgentName agent)
        {
            return agent switch
            {
                AgentName.ToolReasoningAgent => "ToolReasoningAgent",
                AgentName.ProductSearchAgent => "ProductSearchAgent",
                AgentName.ProductMatchmakingAgent => "ProductMatchmakingAgent",
                AgentName.PhotoAnalyzerAgent => "PhotoAnalyzerAgent",
                AgentName.LocationServiceAgent => "LocationServiceAgent",
                AgentName.NavigationAgent => "NavigationAgent",
                AgentName.CustomerInformationAgent => "CustomerInformationAgent",
                AgentName.InventoryAgent => "InventoryAgent",
                _ => string.Empty
            };
        }
    }
}
