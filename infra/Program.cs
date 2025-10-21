using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

// Load configuration (expects user-secrets or other providers to supply these keys)
var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var projectEndpoint = config["ProjectEndpoint"];
var modelDeploymentName = config["ModelDeploymentName"];
var tenantId = config["TenantId"];

if (string.IsNullOrWhiteSpace(projectEndpoint) || string.IsNullOrWhiteSpace(modelDeploymentName) || string.IsNullOrWhiteSpace(tenantId))
{
    Console.WriteLine("Missing one or more required configuration settings: ProjectEndpoint, ModelDeploymentName, TenantId");
    return;
}

var options = new DefaultAzureCredentialOptions { TenantId = tenantId };
var defaultAzureCredential = new DefaultAzureCredential(options: options);
PersistentAgentsClient client = new(projectEndpoint, defaultAzureCredential);

// Path to JSON configuration file containing agent metadata and optional knowledge files
string agentConfigPath = Path.Combine(AppContext.BaseDirectory, "agents.json");
if (!File.Exists(agentConfigPath))
{
    Console.WriteLine($"Agent configuration file not found at {agentConfigPath}. Ensure 'agents.json' exists.");
    return;
}

List<AgentDefinition>? agentDefinitions;
try
{
    var json = File.ReadAllText(agentConfigPath);
    agentDefinitions = JsonSerializer.Deserialize(json, AgentDefinitionJsonContext.Default.ListAgentDefinition);
}
catch (JsonException jex)
{
    Console.WriteLine($"Failed to parse agent configuration: {jex.Message}");
    return;
}

if (agentDefinitions is null || agentDefinitions.Count == 0)
{
    Console.WriteLine("No agent definitions found in agents.json.");
    return;
}

Console.WriteLine($"Found {agentDefinitions.Count} agent definitions. Creating agents...\n");

List<(string Name, string Id)> createdAgents = new();
foreach (var def in agentDefinitions)
{
    try
    {
        PersistentAgent agent = client.Administration.CreateAgent(
            model: modelDeploymentName,
            name: def.Name,
            instructions: def.Instructions,
            tools: new[] { new CodeInterpreterToolDefinition() } // Adjust tools as needed per agent later
        );
        createdAgents.Add((def.Name, agent.Id));
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

// Persist created agent IDs into a markdown table
if (createdAgents.Count > 0)
{
    string outputPath = Path.Combine(AppContext.BaseDirectory, "CreatedAgents.md");
    using var sw = new StreamWriter(outputPath, false, Encoding.UTF8);
    sw.WriteLine("# Created Agents\n");
    sw.WriteLine("| Name | AgentId |");
    sw.WriteLine("| ---- | ------- |");
    foreach (var a in createdAgents)
    {
        sw.WriteLine($"| {a.Name} | {a.Id} |");
    }
    Console.WriteLine($"\nAgent table written to: {outputPath}");
}
else
{
    Console.WriteLine("No agents were created.");
}

internal sealed record AgentDefinition(string Name, string Instructions, List<string>? Files);

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true)]
[JsonSerializable(typeof(List<AgentDefinition>))]
internal partial class AgentDefinitionJsonContext : JsonSerializerContext;