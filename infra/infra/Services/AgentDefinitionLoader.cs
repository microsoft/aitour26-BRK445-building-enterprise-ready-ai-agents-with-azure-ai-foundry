using System.Text.Json;
using Spectre.Console;

namespace Infra.AgentDeployment;

internal interface IAgentDefinitionLoader
{
    AgentDefinition[] LoadDefinitions();
}

internal sealed class JsonAgentDefinitionLoader : IAgentDefinitionLoader
{
    private readonly string _configPath;
    public JsonAgentDefinitionLoader(string configPath) => _configPath = configPath;
    public AgentDefinition[] LoadDefinitions()
    {
        if (!File.Exists(_configPath))
        {
            AnsiConsole.MarkupLine($"[red]Error: Agent configuration file not found at {_configPath}.[/]");
            return Array.Empty<AgentDefinition>();
        }
        try
        {
            var json = File.ReadAllText(_configPath);
            var list = JsonSerializer.Deserialize(json, AgentDefinitionJsonContext.Default.ListAgentDefinition) ?? new List<AgentDefinition>();
            AnsiConsole.MarkupLine($"[green]✓[/] Loaded {list.Count} agent definition(s).\n");
            return list.ToArray();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: Failed to parse agent configuration: {ex.Message}[/]");
            return Array.Empty<AgentDefinition>();
        }
    }
}
