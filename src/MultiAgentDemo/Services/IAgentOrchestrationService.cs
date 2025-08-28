using SharedEntities;

namespace MultiAgentDemo.Services;

/// <summary>
/// Interface for agent orchestration strategies.
/// </summary>
public interface IAgentOrchestrationService
{
    /// <summary>
    /// Executes the orchestration strategy for the given request.
    /// </summary>
    /// <param name="request">The multi-agent request</param>
    /// <returns>The orchestration response</returns>
    Task<MultiAgentResponse> ExecuteAsync(MultiAgentRequest request);
}