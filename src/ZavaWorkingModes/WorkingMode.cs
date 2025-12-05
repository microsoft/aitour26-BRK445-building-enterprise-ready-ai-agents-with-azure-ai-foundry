namespace ZavaWorkingModes;

/// <summary>
/// Defines the available working modes for the Zava Store agent framework.
/// </summary>
public enum WorkingMode
{
    /// <summary>
    /// Direct HTTP calls to business services without AI orchestration.
    /// </summary>
    DirectCall,

    /// <summary>
    /// LLM Direct Call using Microsoft.Extensions.AI IChatClient.
    /// </summary>
    Llm,

    /// <summary>
    /// Microsoft Agent Framework using Agents hosted in Microsoft Foundry.
    /// </summary>
    MafFoundry,

    /// <summary>
    /// Microsoft Agent Framework using locally created agents with gpt-5-mini model.
    /// </summary>
    MafLocal
}

/// <summary>
/// Provides metadata and utilities for working with WorkingMode enum.
/// </summary>
public static class WorkingModeProvider
{
    /// <summary>
    /// Gets the short name (used in URLs and localStorage) for a working mode.
    /// </summary>
    public static string GetShortName(WorkingMode mode) => mode switch
    {
        WorkingMode.DirectCall => "directcall",
        WorkingMode.Llm => "llm",
        WorkingMode.MafFoundry => "maf_foundry",
        WorkingMode.MafLocal => "maf_local",
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    /// <summary>
    /// Gets the display name (user-friendly name) for a working mode.
    /// </summary>
    public static string GetDisplayName(WorkingMode mode) => mode switch
    {
        WorkingMode.DirectCall => "Direct HTTP Call",
        WorkingMode.Llm => "LLM Direct Call",
        WorkingMode.MafFoundry => "MAF - Microsoft Foundry Agents",
        WorkingMode.MafLocal => "MAF - Local Agents",
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    /// <summary>
    /// Gets a detailed description for a working mode.
    /// </summary>
    public static string GetDescription(WorkingMode mode) => mode switch
    {
        WorkingMode.DirectCall => "Direct HTTP calls to business services without AI orchestration. Uses hardcoded responses from the underlying HTTP services.",
        WorkingMode.Llm => "LLM Direct Call using Microsoft.Extensions.AI IChatClient. Direct interaction with language models for agent-like behavior.",
        WorkingMode.MafFoundry => "Microsoft Agent Framework using Agents deployed and hosted in Microsoft Foundry. Production-ready with cloud-managed agents.",
        WorkingMode.MafLocal => "Microsoft Agent Framework using locally created agents with gpt-5-mini model. Agents are created with instructions and tools configured locally.",
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    /// <summary>
    /// Gets the default working mode.
    /// </summary>
    public static WorkingMode DefaultMode => WorkingMode.MafFoundry;

    /// <summary>
    /// Gets all available working modes.
    /// </summary>
    public static IReadOnlyList<WorkingMode> AllModes => 
        [WorkingMode.DirectCall, WorkingMode.Llm, WorkingMode.MafFoundry, WorkingMode.MafLocal];

    /// <summary>
    /// Parses a short name string to a WorkingMode.
    /// Returns the default mode if the value cannot be parsed.
    /// </summary>
    public static WorkingMode Parse(string? shortName)
    {
        if (string.IsNullOrWhiteSpace(shortName))
            return DefaultMode;

        return shortName.ToLowerInvariant() switch
        {
            "directcall" => WorkingMode.DirectCall,
            "llm" => WorkingMode.Llm,
            "maf_foundry" => WorkingMode.MafFoundry,
            "maf" => WorkingMode.MafFoundry, // backward compatibility
            "maf_local" => WorkingMode.MafLocal,
            _ => DefaultMode
        };
    }

    /// <summary>
    /// Tries to parse a short name string to a WorkingMode.
    /// </summary>
    public static bool TryParse(string? shortName, out WorkingMode mode)
    {
        mode = DefaultMode;
        
        if (string.IsNullOrWhiteSpace(shortName))
            return false;

        var parsed = shortName.ToLowerInvariant() switch
        {
            "directcall" => (WorkingMode?)WorkingMode.DirectCall,
            "llm" => WorkingMode.Llm,
            "maf_foundry" => WorkingMode.MafFoundry,
            "maf" => WorkingMode.MafFoundry,
            "maf_local" => WorkingMode.MafLocal,
            _ => null
        };

        if (parsed.HasValue)
        {
            mode = parsed.Value;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets working mode metadata as a collection of tuples.
    /// </summary>
    public static IEnumerable<(WorkingMode Mode, string ShortName, string DisplayName, string Description)> GetAllModeMetadata()
    {
        foreach (var mode in AllModes)
        {
            yield return (mode, GetShortName(mode), GetDisplayName(mode), GetDescription(mode));
        }
    }
}
