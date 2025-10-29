namespace SharedEntities;

/// <summary>
/// Defines the different types of agent orchestration patterns available.
/// </summary>
public enum OrchestrationType
{
    /// <summary>
    /// Default orchestration - uses a predefined orchestration type if none is specified.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Sequential orchestration - passes result from one agent to the next in a defined order.
    /// Use Case: Step-by-step workflows, pipelines, multi-stage processing.
    /// </summary>
    Sequential = 1,

    /// <summary>
    /// Concurrent orchestration - broadcasts a task to all agents, collects results independently.
    /// Use Case: Parallel analysis, independent subtasks, ensemble decision making.
    /// </summary>
    Concurrent = 2,

    /// <summary>
    /// Handoff orchestration - dynamically passes control between agents based on context or rules.
    /// Use Case: Dynamic workflows, escalation, fallback, or expert handoff scenarios.
    /// </summary>
    Handoff = 3,

    /// <summary>
    /// Group Chat orchestration - all agents participate in a group conversation, coordinated by a group manager.
    /// Use Case: Brainstorming, collaborative problem solving, consensus building.
    /// </summary>
    GroupChat = 4,

    /// <summary>
    /// Magentic orchestration - group chat-like orchestration inspired by MagenticOne.
    /// Use Case: Complex, generalist multi-agent collaboration.
    /// </summary>
    Magentic = 5
}