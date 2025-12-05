using SharedEntities;

namespace MultiAgentDemo.Services;

/// <summary>
/// Interface for agent orchestration strategies.
/// Defines the contract for different multi-agent orchestration patterns.
/// </summary>
public interface IAgentOrchestrationService
{
    /// <summary>
    /// Executes the orchestration strategy for the given request.
    /// </summary>
    /// <param name="request">The multi-agent request containing query and configuration.</param>
    /// <returns>The orchestration response with steps and results.</returns>
    Task<MultiAgentResponse> ExecuteAsync(MultiAgentRequest request);
}