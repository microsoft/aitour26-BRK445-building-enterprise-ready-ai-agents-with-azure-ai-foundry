using Azure.AI.Agents.Persistent;
using System.Text;
using System.Text.Json;
using Infra.AgentDeployment;

namespace Infra.AgentDeployment;

/// <summary>
/// Orchestrates deletion and creation of Persistent Agents from a JSON file.
/// </summary>
public class AgentDeploymentRunner
{
    private readonly PersistentAgentsClient _client;
    private readonly string _modelDeploymentName;
    private readonly string _configPath;

    public AgentDeploymentRunner(PersistentAgentsClient client, string modelDeploymentName, string configPath)
    {
        _client = client;
        _modelDeploymentName = modelDeploymentName;
        _configPath = configPath;
    }

    public void Run(bool? deleteFlag)
    {
        var definitions = LoadDefinitions();
        if (definitions.Length == 0)
        {
            Console.WriteLine("No agent definitions to process.");
            return;
        }

        bool deleteRequested = ShouldDelete(deleteFlag);
        if (deleteRequested)
        {
            DeleteExisting(definitions);
        }
        else
        {
            Console.WriteLine("Skipping deletion of existing agents.\n");
        }

        CreateAgents(definitions);
    }

    private AgentDefinition[] LoadDefinitions()
    {
        if (!File.Exists(_configPath))
        {
            Console.WriteLine($"Agent configuration file not found at {_configPath}.");
            return Array.Empty<AgentDefinition>();
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            var list = JsonSerializer.Deserialize(json, AgentDefinitionJsonContext.Default.ListAgentDefinition) ?? new List<AgentDefinition>();
            Console.WriteLine($"Loaded {list.Count} agent definition(s).\n");
            return list.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse agent configuration: {ex.Message}");
            return Array.Empty<AgentDefinition>();
        }
    }

    private bool ShouldDelete(bool? flag)
    {
        if (flag.HasValue) return flag.Value; // command line override

        Console.Write("Delete existing agents matching definitions? (y/N): ");
        var input = Console.ReadLine();
        return !string.IsNullOrWhiteSpace(input) && input.Trim().Equals("y", StringComparison.OrdinalIgnoreCase);
    }

    private void DeleteExisting(IEnumerable<AgentDefinition> definitions)
    {
        try
        {
            Console.WriteLine("Starting deletion of matching agents...");
            var namesToDelete = new HashSet<string>(definitions.Select(d => d.Name), StringComparer.OrdinalIgnoreCase);
            int deletedCount = 0;

            foreach (var existing in _client.Administration.GetAgents())
            {
                if (namesToDelete.Contains(existing.Name))
                {
                    try
                    {
                        _client.Administration.DeleteAgent(existing.Id);
                        Console.WriteLine($"Deleted agent: {existing.Name} ({existing.Id})");
                        deletedCount++;
                    }
                    catch (Exception exDel)
                    {
                        Console.WriteLine($"Failed to delete agent '{existing.Name}' ({existing.Id}): {exDel.Message}");
                    }
                }
            }

            Console.WriteLine($"Deletion phase complete. Deleted {deletedCount} agent(s).\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error during deletion phase: {ex.Message}");
            Console.WriteLine("Continuing with agent creation...\n");
        }
    }

    private void CreateAgents(IEnumerable<AgentDefinition> definitions)
    {
        Console.WriteLine("Creating agents...\n");
        var created = new List<(string Name, string Id)>();

        foreach (var def in definitions)
        {
            try
            {
                // The CreateAgent method returns Response<PersistentAgent>. Access Value for instance.
                var response = _client.Administration.CreateAgent(
                    model: _modelDeploymentName,
                    name: def.Name,
                    instructions: def.Instructions,
                    tools: new[] { new CodeInterpreterToolDefinition() }
                );
                var agent = response.Value;

                created.Add((def.Name, agent.Id));
                Console.WriteLine($"Created agent: {def.Name} => {agent.Id}");

                if (def.Files is { Count: > 0 })
                {
                    Console.WriteLine($"  - {def.Files.Count} knowledge file(s) referenced (attach via vector store as needed).");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create agent '{def.Name}': {ex.Message}");
            }
        }

        PersistCreated(created);
    }

    private void PersistCreated(List<(string Name, string Id)> created)
    {
        if (created.Count == 0)
        {
            Console.WriteLine("No agents were created.");
            return;
        }

        string outputPath = Path.Combine(AppContext.BaseDirectory, "CreatedAgents.md");
        using var sw = new StreamWriter(outputPath, false, Encoding.UTF8);
        sw.WriteLine("# Created Agents\n");
        sw.WriteLine("| Name | AgentId |");
        sw.WriteLine("| ---- | ------- |");
        foreach (var a in created)
        {
            sw.WriteLine($"| {a.Name} | {a.Id} |");
        }
        Console.WriteLine($"\nAgent table written to: {outputPath}");
    }
}
