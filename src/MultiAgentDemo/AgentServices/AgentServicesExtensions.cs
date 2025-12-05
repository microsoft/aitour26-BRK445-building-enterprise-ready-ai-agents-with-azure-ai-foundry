using Microsoft.Extensions.DependencyInjection;
using ZavaMAFLocalAgentsProvider;
using ZavaMAFAgentsProvider;

namespace MultiAgentDemo.AgentServices;

/// <summary>
/// Extension methods for registering agent services in dependency injection.
/// </summary>
public static class AgentServicesExtensions
{
    /// <summary>
    /// Registers local Microsoft Agent Framework agents.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="endpoint">The Azure AI project endpoint.</param>
    /// <param name="deploymentName">The deployment name for the model (default: "gpt-5-mini").</param>
    public static IServiceCollection RegisterMAFAgentsLocal(
        this IServiceCollection services,
        string endpoint,
        string deploymentName = "gpt-5-mini")
    {
        // Register the local MAF agent provider as singleton with configuration
        services.AddSingleton(_ => new MAFLocalAgentProvider(endpoint, deploymentName));
        
        return services;
    }

    /// <summary>
    /// Registers Azure AI Foundry-based Microsoft Agent Framework agents.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="projectEndpoint">The Microsoft Foundry project endpoint.</param>
    public static IServiceCollection RegisterMAFAgentsFoundry(
        this IServiceCollection services,
        string projectEndpoint)
    {
        // Register the Foundry MAF agent provider as singleton with configuration
        services.AddSingleton(_ => new MAFAgentProvider(projectEndpoint));
        
        return services;
    }
}
