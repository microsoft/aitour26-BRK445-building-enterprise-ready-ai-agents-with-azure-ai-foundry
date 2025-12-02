#pragma warning disable IDE0017, OPENAI001

using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.VectorStores;
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
    private readonly TaskTracker? _taskTracker;

    public AgentCreationService(AIProjectClient client, string modelDeploymentName, TaskTracker? taskTracker = null)
    {
        _client = client;
        _modelDeploymentName = modelDeploymentName;
        _taskTracker = taskTracker;
    }
    public List<(string Name, string Id)> CreateAgents(IEnumerable<AgentDefinition> definitions, Dictionary<string, UploadedFile> uploadedFiles)
    {
        if (_taskTracker != null)
            _taskTracker.AddLog("[cyan]Creating agents...[/]");
        else
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
                if (_taskTracker != null)
                {
                    _taskTracker.AddLog($"[grey]Creating agent: [cyan]{def.Name}[/][/]");
                    _taskTracker.AddLog($"[grey]Instructions: {def.Instructions?.Length ?? 0} chars[/]");
                    _taskTracker.AddLog($"[grey]Files referenced: {(def.Files?.Count ?? 0)}[/]");

                    AIAgent agent = null;
                    List<string> agentFileIds = new();

                    if (def.Files is { Count: > 0 })
                    {
                        foreach (var fileRef in def.Files)
                        {
                            var resolved = PathResolver.ResolveSourceFilePath(fileRef);
                            if (uploadedFiles.TryGetValue(resolved, out var meta))
                                agentFileIds.Add(meta.UploadedId);
                            else
                                _taskTracker.AddLog($"[yellow]⚠[/] File not uploaded: [grey]{fileRef}[/]");
                        }
                    }

                    if (agentFileIds.Count > 0)
                    {
                        _taskTracker.AddLog($"[grey]Creating vector store for {def.Name}...[/]");
                        var vectorStoreName = $"{def.Name}_vs";
                        var openAIClient = _client.GetProjectOpenAIClient();
                        var vectorStoreClient = openAIClient.GetVectorStoreClient();

                        var vectorStoreOptions = new VectorStoreCreationOptions()
                        {
                            Name = vectorStoreName
                        };
                        foreach (var fileId in agentFileIds)
                        {
                            vectorStoreOptions.FileIds.Add(fileId);
                        }

                        var vectorStoreResult = vectorStoreClient.CreateVectorStore(options: vectorStoreOptions);
                        var vectorStore = vectorStoreResult.Value;
                        _taskTracker.AddLog($"[green]✓[/] Vector store created: [grey]{vectorStore.Id}[/]");

                        _taskTracker.AddLog($"[grey]Creating agent {def.Name} with tools...[/]");
                        var fileSearchTool = new HostedFileSearchTool() { Inputs = [new HostedVectorStoreContent(vectorStore.Id)] };

                        agent = _client.CreateAIAgent(
                            model: _modelDeploymentName,
                            name: def.Name,
                            instructions: def.Instructions,
                            tools: [
                                new HostedCodeInterpreterTool() { Inputs = [] },
                                fileSearchTool
                            ]);

                        _taskTracker.CompleteSubTask("Creating", "Indexes");
                    }

                    if (agent == null)
                    {
                        _taskTracker.AddLog($"[grey]Creating agent {def.Name}...[/]");
                        agent = _client.CreateAIAgent(
                            model: _modelDeploymentName,
                            name: def.Name,
                            instructions: def.Instructions,
                            tools: [new HostedCodeInterpreterTool() { Inputs = [] }]);
                    }

                    created.Add((def.Name, agent.Id));
                    _taskTracker.AddLog($"[green]✓[/] Created agent: [cyan]{def.Name}[/] ({agent.Id})");
                }
                else
                {
                    AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .Start($"Creating agent: [cyan]{def.Name}[/]", ctx =>
                        {
                            AnsiConsole.MarkupLine($"[grey]Instructions: {def.Instructions?.Length ?? 0} chars[/]");
                            AnsiConsole.MarkupLine($"[grey]Files referenced: {(def.Files?.Count ?? 0)}[/]");

                            AIAgent agent = null;
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
                                var vectorStoreName = $"{def.Name}_vs";
                                var openAIClient = _client.GetProjectOpenAIClient();
                                var vectorStoreClient = openAIClient.GetVectorStoreClient();

                                var vectorStoreOptions = new VectorStoreCreationOptions()
                                {
                                    Name = vectorStoreName
                                };
                                foreach (var fileId in agentFileIds)
                                {
                                    vectorStoreOptions.FileIds.Add(fileId);
                                }

                                var vectorStoreResult = vectorStoreClient.CreateVectorStore(options: vectorStoreOptions);
                                var vectorStore = vectorStoreResult.Value;
                                AnsiConsole.MarkupLine($"[green]✓[/] Vector store created: [grey]{vectorStore.Id}[/]");

                                ctx.Status($"Creating agent {def.Name} with tools...");
                                var fileSearchTool = new HostedFileSearchTool() { Inputs = [new HostedVectorStoreContent(vectorStore.Id)] };

                                agent = _client.CreateAIAgent(
                                    model: _modelDeploymentName,
                                    name: def.Name,
                                    instructions: def.Instructions,
                                    tools: [
                                        new HostedCodeInterpreterTool() { Inputs = [] },
                                        fileSearchTool
                                    ]);
                            }

                            if (agent == null)
                            {
                                ctx.Status($"Creating agent {def.Name}...");
                                agent = _client.CreateAIAgent(
                                    model: _modelDeploymentName,
                                    name: def.Name,
                                    instructions: def.Instructions,
                                    tools: [new HostedCodeInterpreterTool() { Inputs = [] }]);
                            }

                            created.Add((def.Name, agent.Id));
                        });

                    table.AddRow($"[cyan]{def.Name}[/]", "[green]✓ Created[/]", $"[grey]{created[^1].Id}[/]");
                }
            }
            catch (Exception ex)
            {
                if (_taskTracker != null)
                    _taskTracker.AddLog($"[red]✗[/] Failed to create agent [cyan]{def.Name}[/]: {ex.Message}");
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to create agent [cyan]{def.Name}[/]: {ex.Message}");
                    table.AddRow($"[cyan]{def.Name}[/]", "[red]✗ Failed[/]", $"[red]{ex.Message}[/]");
                }
            }
        }

        if (_taskTracker != null)
        {
            _taskTracker.AddLog($"[green]✓[/] Successfully created {created.Count} agent(s).");
            _taskTracker.CompleteSubTask("Creating", "Agents");
        }
        else
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }

        return created;
    }
}
