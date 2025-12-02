using Azure.AI.Projects;
using OpenAI.Assistants;
using Spectre.Console;

namespace Infra.AgentDeployment;

internal interface IAgentCreationService
{
    List<(string Name, string Id)> CreateAgents(IEnumerable<AgentDefinition> definitions, Dictionary<string, UploadedFile> uploadedFiles);
}

internal sealed class AgentCreationService : IAgentCreationService
{
    private readonly AIProjectClient _client;
    private readonly string _modelDeploymentName;
    public AgentCreationService(AIProjectClient client, string modelDeploymentName)
    {
        _client = client;
        _modelDeploymentName = modelDeploymentName;
    }
    public List<(string Name, string Id)> CreateAgents(IEnumerable<AgentDefinition> definitions, Dictionary<string, UploadedFile> uploadedFiles)
    {
        AnsiConsole.MarkupLine("[cyan]Creating agents...[/]\n");
        var created = new List<(string Name, string Id)>();

        var table = new Table()
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[cyan]Agent Name[/]").Centered())
            .AddColumn(new TableColumn("[cyan]Status[/]").Centered())
            .AddColumn(new TableColumn("[cyan]Agent ID[/]").Centered());

        foreach (var def in definitions)
        {
            try
            {
                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .Start($"Creating agent: [cyan]{def.Name}[/]", ctx =>
                    {
                        AnsiConsole.MarkupLine($"[grey]Instructions: {def.Instructions?.Length ?? 0} chars[/]");
                        AnsiConsole.MarkupLine($"[grey]Files referenced: {(def.Files?.Count ?? 0)}[/]");

                        PersistentAgent agent = null;
                        List<string> agentFileIds = new();

                        if (def.Files is { Count: > 0 })
                        {
                            foreach (var fileRef in def.Files)
                            {
                                var resolved = PathResolver.ResolveSourceFilePath(fileRef);
                                if (uploadedFiles.TryGetValue(resolved, out var meta))
                                    agentFileIds.Add(meta.UploadedId);
                                else
                                    AnsiConsole.MarkupLine($"[yellow]⚠[/] File not uploaded: [grey]{fileRef}[/]");
                            }
                        }

                        if (agentFileIds.Count > 0)
                        {
                            ctx.Status($"Creating vector store for {def.Name}...");
                            var fileSearchToolResource = new FileSearchToolResource();
                            var vectorStoreName = $"{def.Name}_vs";
                            PersistentAgentsVectorStore vectorStore = _client.VectorStores.CreateVectorStore(fileIds: agentFileIds, name: vectorStoreName);
                            AnsiConsole.MarkupLine($"[green]✓[/] Vector store created: [grey]{vectorStore.Id}[/]");
                            fileSearchToolResource.VectorStoreIds.Add(vectorStore.Id);

                            ctx.Status($"Creating agent {def.Name} with tools...");
                            agent = _client.CreateAIAgent(
                                model: _modelDeploymentName,
                                name: def.Name,
                                instructions: def.Instructions,
                                tools: new List<ToolDefinition> { new CodeInterpreterToolDefinition(), new FileSearchToolDefinition() },
                                toolResources: new ToolResources { FileSearch = fileSearchToolResource });
                        }

                        if (agent == null)
                        {
                            ctx.Status($"Creating agent {def.Name}...");
                            agent = _client.CreateAIAgent(
                                model: _modelDeploymentName,
                                name: def.Name,
                                instructions: def.Instructions,
                                tools: new List<ToolDefinition> { new CodeInterpreterToolDefinition() });
                        }

                        created.Add((def.Name, agent.Id));
                    });

                table.AddRow($"[cyan]{def.Name}[/]", "[green]✓ Created[/]", $"[grey]{created[^1].Id}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Failed to create agent [cyan]{def.Name}[/]: {ex.Message}");
                table.AddRow($"[cyan]{def.Name}[/]", "[red]✗ Failed[/]", $"[red]{ex.Message}[/]");
            }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        return created;
    }
}
