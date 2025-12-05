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
    public static IServiceCollection RegisterMAFAgentsLocal(this IServiceCollection services)
    {
        // Register the local MAF agent provider as singleton
        services.AddSingleton<MAFLocalAgentProvider>();
        
        return services;
    }

    /// <summary>
    /// Registers Azure AI Foundry-based Microsoft Agent Framework agents.
    /// </summary>
    public static IServiceCollection RegisterMAFAgentsFoundry(this IServiceCollection services)
    {
        // Register the Foundry MAF agent provider as singleton
        services.AddSingleton<MAFAgentProvider>();
        
        return services;
    }
}
