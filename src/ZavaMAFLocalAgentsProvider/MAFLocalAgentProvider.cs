using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ZavaFoundryAgentsProvider;

namespace ZavaMAFLocalAgentsProvider;

/// <summary>
/// Provides locally-created agents using the Microsoft Agent Framework with gpt-5-mini model.
/// Agents are created with instructions and tools configured locally using Azure.AI.Projects.
/// </summary>
public class MAFLocalAgentProvider
{
    private readonly AIProjectClient _projectClient;
    private readonly string _deploymentName;
    private readonly Dictionary<string, AIAgent> _cachedAgents = new();

    /// <summary>
    /// Initializes a new instance of the MAFLocalAgentProvider.
    /// </summary>
    /// <param name="endpoint">The Azure AI project endpoint.</param>
    /// <param name="deploymentName">The deployment name for the model (e.g., "gpt-5-mini").</param>
    public MAFLocalAgentProvider(string endpoint, string deploymentName = "gpt-5-mini")
    {
        _projectClient = new AIProjectClient(
            endpoint: new Uri(endpoint),
            tokenProvider: new DefaultAzureCredential());
        _deploymentName = deploymentName;
    }

    /// <summary>
    /// Creates an inventory agent with product knowledge.
    /// </summary>
    public AIAgent CreateInventoryAgent()
    {
        return GetOrCreateAgent("InventoryAgent", AgentInstructions.InventoryAgent);
    }

    /// <summary>
    /// Creates a customer information agent.
    /// </summary>
    public AIAgent CreateCustomerInformationAgent()
    {
        return GetOrCreateAgent("CustomerInformationAgent", AgentInstructions.CustomerInformationAgent);
    }

    /// <summary>
    /// Creates a navigation agent for store navigation.
    /// </summary>
    public AIAgent CreateNavigationAgent()
    {
        return GetOrCreateAgent("NavigationAgent", AgentInstructions.NavigationAgent);
    }

    /// <summary>
    /// Creates a location service agent.
    /// </summary>
    public AIAgent CreateLocationServiceAgent()
    {
        return GetOrCreateAgent("LocationServiceAgent", AgentInstructions.LocationServiceAgent);
    }

    /// <summary>
    /// Creates a photo analyzer agent.
    /// </summary>
    public AIAgent CreatePhotoAnalyzerAgent()
    {
        return GetOrCreateAgent("PhotoAnalyzerAgent", AgentInstructions.PhotoAnalyzerAgent);
    }

    /// <summary>
    /// Creates a product matchmaking agent.
    /// </summary>
    public AIAgent CreateProductMatchmakingAgent()
    {
        return GetOrCreateAgent("ProductMatchmakingAgent", AgentInstructions.ProductMatchmakingAgent);
    }

    /// <summary>
    /// Creates a product search agent.
    /// </summary>
    public AIAgent CreateProductSearchAgent()
    {
        return GetOrCreateAgent("ProductSearchAgent", AgentInstructions.ProductSearchAgent);
    }

    /// <summary>
    /// Creates a tool reasoning agent.
    /// </summary>
    public AIAgent CreateToolReasoningAgent()
    {
        return GetOrCreateAgent("ToolReasoningAgent", AgentInstructions.ToolReasoningAgent);
    }

    /// <summary>
    /// Gets or creates an agent with the specified name and instructions.
    /// </summary>
    private AIAgent GetOrCreateAgent(string name, string instructions)
    {
        var cacheKey = $"local_{name}";
        if (_cachedAgents.TryGetValue(cacheKey, out var existingAgent))
        {
            return existingAgent;
        }

        // Create a new local agent using the Azure.AI.Projects client
        var agent = _projectClient.CreateAIAgent(
            model: _deploymentName,
            name: $"Local_{name}",
            instructions: instructions,
            tools: [new HostedCodeInterpreterTool() { Inputs = [] }]);

        _cachedAgents[cacheKey] = agent;
        return agent;
    }

    /// <summary>
    /// Gets an agent by name from the AgentName enum.
    /// </summary>
    public AIAgent GetAgentByName(AgentNamesProvider.AgentName agentName)
    {
        return agentName switch
        {
            AgentNamesProvider.AgentName.InventoryAgent => CreateInventoryAgent(),
            AgentNamesProvider.AgentName.CustomerInformationAgent => CreateCustomerInformationAgent(),
            AgentNamesProvider.AgentName.NavigationAgent => CreateNavigationAgent(),
            AgentNamesProvider.AgentName.LocationServiceAgent => CreateLocationServiceAgent(),
            AgentNamesProvider.AgentName.PhotoAnalyzerAgent => CreatePhotoAnalyzerAgent(),
            AgentNamesProvider.AgentName.ProductMatchmakingAgent => CreateProductMatchmakingAgent(),
            AgentNamesProvider.AgentName.ProductSearchAgent => CreateProductSearchAgent(),
            AgentNamesProvider.AgentName.ToolReasoningAgent => CreateToolReasoningAgent(),
            _ => throw new ArgumentOutOfRangeException(nameof(agentName), $"Unknown agent: {agentName}")
        };
    }
}

/// <summary>
/// Extension methods for registering local MAF agents in dependency injection.
/// </summary>
public static class MAFLocalAgentServiceExtensions
{
    /// <summary>
    /// Adds all local MAF agents to the service collection as keyed services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="endpoint">The Azure AI project endpoint.</param>
    /// <param name="deploymentName">The deployment name for the model (e.g., "gpt-5-mini").</param>
    public static IServiceCollection AddLocalMAFAgents(
        this IServiceCollection services,
        string endpoint,
        string deploymentName = "gpt-5-mini")
    {
        var provider = new MAFLocalAgentProvider(endpoint, deploymentName);

        // Register each agent as a keyed singleton
        services.AddKeyedSingleton(
            AgentNamesProvider.GetAgentName(AgentNamesProvider.AgentName.InventoryAgent),
            (_, _) => provider.CreateInventoryAgent());

        services.AddKeyedSingleton(
            AgentNamesProvider.GetAgentName(AgentNamesProvider.AgentName.CustomerInformationAgent),
            (_, _) => provider.CreateCustomerInformationAgent());

        services.AddKeyedSingleton(
            AgentNamesProvider.GetAgentName(AgentNamesProvider.AgentName.NavigationAgent),
            (_, _) => provider.CreateNavigationAgent());

        services.AddKeyedSingleton(
            AgentNamesProvider.GetAgentName(AgentNamesProvider.AgentName.LocationServiceAgent),
            (_, _) => provider.CreateLocationServiceAgent());

        services.AddKeyedSingleton(
            AgentNamesProvider.GetAgentName(AgentNamesProvider.AgentName.PhotoAnalyzerAgent),
            (_, _) => provider.CreatePhotoAnalyzerAgent());

        services.AddKeyedSingleton(
            AgentNamesProvider.GetAgentName(AgentNamesProvider.AgentName.ProductMatchmakingAgent),
            (_, _) => provider.CreateProductMatchmakingAgent());

        services.AddKeyedSingleton(
            AgentNamesProvider.GetAgentName(AgentNamesProvider.AgentName.ProductSearchAgent),
            (_, _) => provider.CreateProductSearchAgent());

        services.AddKeyedSingleton(
            AgentNamesProvider.GetAgentName(AgentNamesProvider.AgentName.ToolReasoningAgent),
            (_, _) => provider.CreateToolReasoningAgent());

        return services;
    }
}
