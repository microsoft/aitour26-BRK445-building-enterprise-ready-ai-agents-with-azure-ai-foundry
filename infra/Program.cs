using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using System.Text;

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

// Path to markdown instructions file (each agent starts with a line: ### Agent Name)
string instructionsFile = Path.Combine(AppContext.BaseDirectory, "AgentInstructions.md");
if (!File.Exists(instructionsFile))
{
    Console.WriteLine($"Instructions file not found at {instructionsFile}. Ensure 'AgentInstructions.md' exists.");
    return;
}

var agentDefinitions = ParseAgentInstructions(instructionsFile);
if (agentDefinitions.Count == 0)
{
    Console.WriteLine("No agent definitions found in instructions file.");
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

static List<(string Name, string Instructions)> ParseAgentInstructions(string filePath)
{
    var lines = File.ReadAllLines(filePath);
    List<(string Name, string Instructions)> list = new();
    string? currentName = null;
    var sb = new StringBuilder();

    foreach (var raw in lines)
    {
        var line = raw.TrimEnd();
        if (line.StartsWith("### "))
        {
            if (currentName != null)
            {
                list.Add((currentName, sb.ToString().Trim()));
                sb.Clear();
            }
            currentName = line.Substring(4).Trim();
        }
        else if (currentName != null)
        {
            sb.AppendLine(line);
        }
    }

    if (currentName != null)
    {
        list.Add((currentName, sb.ToString().Trim()));
    }

    return list;
}