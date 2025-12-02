#pragma warning disable IDE0017, OPENAI001

using Azure.AI.Projects;
using OpenAI;
using OpenAI.Files;
using OpenAI.VectorStores;
using System.IO;
using Spectre.Console;

namespace Infra.AgentDeployment;

internal interface IAgentDeletionService
{
    void DeleteExisting(IEnumerable<AgentDefinition> definitions, bool deleteAgents, bool deleteIndexes, bool deleteDatasets);
}

internal sealed class AgentDeletionService : IAgentDeletionService
{
    private readonly AIProjectClient _client;
    private readonly TaskTracker? _taskTracker;

    public AgentDeletionService(AIProjectClient client, TaskTracker? taskTracker = null)
    {
        _client = client;
        _taskTracker = taskTracker;
    }

    public void DeleteExisting(IEnumerable<AgentDefinition> definitions, bool deleteAgents, bool deleteIndexes, bool deleteDatasets)
    {
        try
        {
            if (deleteAgents)
            {
                DeleteAgents(definitions);
                _taskTracker?.CompleteSubTask("Deleting", "Agents");
            }
            else
            {
                if (_taskTracker != null)
                    _taskTracker.AddLog("[yellow]Skipping agent deletion.[/]");
                else
                    AnsiConsole.MarkupLine("[yellow]Skipping agent deletion.[/]");
            }

            if (deleteDatasets)
            {
                DeleteReferencedFiles(definitions);
                _taskTracker?.CompleteSubTask("Deleting", "DataSets");
            }
            else
            {
                if (_taskTracker != null)
                    _taskTracker.AddLog("[yellow]Skipping dataset (file) deletion.[/]");
                else
                    AnsiConsole.MarkupLine("[yellow]Skipping dataset (file) deletion.[/]");
            }

            if (deleteIndexes)
            {
                DeleteVectorStores(definitions);
                _taskTracker?.CompleteSubTask("Deleting", "Indexes");
            }
            else
            {
                if (_taskTracker != null)
                    _taskTracker.AddLog("[yellow]Skipping index (vector store) deletion.[/]");
                else
                    AnsiConsole.MarkupLine("[yellow]Skipping index (vector store) deletion.[/]");
            }

            if (_taskTracker == null)
                AnsiConsole.WriteLine();
        }
        catch (Exception ex)
        {
            if (_taskTracker != null)
            {
                _taskTracker.AddLog($"[yellow]⚠[/] Unexpected error during deletion: {ex.Message}");
                _taskTracker.AddLog("[grey]Continuing with agent creation...[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]⚠[/] Unexpected error during deletion: {ex.Message}");
                AnsiConsole.MarkupLine("[grey]Continuing with agent creation...[/]\n");
            }
        }
    }

    private void DeleteAgents(IEnumerable<AgentDefinition> definitions)
    {
        if (_taskTracker != null)
        {
            var namesToDelete = new HashSet<string>(definitions.Select(d => d.Name), StringComparer.OrdinalIgnoreCase);
            int deletedAgents = 0;

            _taskTracker.AddLog("[grey]Deleting existing agents...[/]");

            var agents = _client.Agents.GetAgents();
            foreach (var existing in agents)
            {
                if (namesToDelete.Contains(existing.Name))
                {
                    try
                    {
                        _client.Agents.DeleteAgent(existing.Name);
                        _taskTracker.AddLog($"[red]✓[/] Deleted agent: [grey]{existing.Name}[/] ({existing.Id})");
                        deletedAgents++;
                    }
                    catch (Exception exDel)
                    {
                        _taskTracker.AddLog($"[red]✗[/] Failed to delete agent [grey]'{existing.Name}'[/] ({existing.Id}): {exDel.Message}");
                    }
                }
            }
            _taskTracker.AddLog($"[green]✓[/] Deleted {deletedAgents} agent(s).");
        }
        else
        {
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .Start("Deleting existing agents...", ctx =>
                {
                    var namesToDelete = new HashSet<string>(definitions.Select(d => d.Name), StringComparer.OrdinalIgnoreCase);
                    int deletedAgents = 0;

                    // Get all agents and delete matching ones
                    var agents = _client.Agents.GetAgents();
                    foreach (var existing in agents)
                    {
                        if (namesToDelete.Contains(existing.Name))
                        {
                            try
                            {
                                _client.Agents.DeleteAgent(existing.Name);
                                AnsiConsole.MarkupLine($"[red]✓[/] Deleted agent: [grey]{existing.Name}[/] ({existing.Id})");
                                deletedAgents++;
                            }
                            catch (Exception exDel)
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] Failed to delete agent [grey]'{existing.Name}'[/] ({existing.Id}): {exDel.Message}");
                            }
                        }
                    }
                    AnsiConsole.MarkupLine($"[green]✓[/] Deleted {deletedAgents} agent(s).\n");
                });
        }
    }

    private void DeleteReferencedFiles(IEnumerable<AgentDefinition> definitions)
    {
        try
        {
            var fileNames = new HashSet<string>(
                definitions.SelectMany(d => d.Files ?? new List<string>())
                    .Select(f => Path.GetFileName(f))
                    .Where(n => !string.IsNullOrWhiteSpace(n)),
                StringComparer.OrdinalIgnoreCase);
            if (fileNames.Count == 0)
            {
                if (_taskTracker != null)
                    _taskTracker.AddLog("[grey]No file names referenced in definitions to delete.[/]");
                else
                    AnsiConsole.MarkupLine("[grey]No file names referenced in definitions to delete.[/]");
                return;
            }

            if (_taskTracker != null)
                _taskTracker.AddLog($"[grey]Deleting {fileNames.Count} referenced file(s)...[/]");
            else
                AnsiConsole.MarkupLine($"[grey]Deleting {fileNames.Count} referenced file(s)...[/]");

            int deletedFiles = 0;

            OpenAIClient openAIClient = _client.GetProjectOpenAIClient();
            OpenAIFileClient fileClient = openAIClient.GetOpenAIFileClient();
            var filesResponse = fileClient.GetFiles();

            foreach (var existingFile in filesResponse.Value)
            {
                try
                {
                    if (fileNames.Contains(existingFile.Filename))
                    {
                        fileClient.DeleteFile(existingFile.Id);

                        if (_taskTracker != null)
                            _taskTracker.AddLog($"[red]✓[/] Deleted file: [grey]{existingFile.Filename}[/] ({existingFile.Id})");
                        else
                            AnsiConsole.MarkupLine($"[red]✓[/] Deleted file: [grey]{existingFile.Filename}[/] ({existingFile.Id})");
                        deletedFiles++;
                    }
                }
                catch (Exception exFile)
                {
                    if (_taskTracker != null)
                        _taskTracker.AddLog($"[red]✗[/] Failed to delete file [grey]'{existingFile.Filename}'[/]: {exFile.Message}");
                    else
                        AnsiConsole.MarkupLine($"[red]✗[/] Failed to delete file [grey]'{existingFile.Filename}'[/]: {exFile.Message}");
                }
            }

            if (_taskTracker != null)
                _taskTracker.AddLog($"[green]✓[/] Deleted {deletedFiles} file(s).");
            else
                AnsiConsole.MarkupLine($"[green]✓[/] Deleted {deletedFiles} file(s).\n");
        }
        catch (Exception ex)
        {
            if (_taskTracker != null)
                _taskTracker.AddLog($"[yellow]⚠[/] File deletion error: {ex.Message}");
            else
                AnsiConsole.MarkupLine($"[yellow]⚠[/] File deletion error: {ex.Message}");
        }
    }

    private void DeleteVectorStores(IEnumerable<AgentDefinition> definitions)
    {
        try
        {
            // Build a set of agent names to match against vector store names
            var agentNames = new HashSet<string>(
                definitions.Select(d => d.Name),
                StringComparer.OrdinalIgnoreCase);

            if (agentNames.Count == 0)
            {
                if (_taskTracker != null)
                    _taskTracker.AddLog("[grey]No agent names to derive vector store patterns.[/]");
                else
                    AnsiConsole.MarkupLine("[grey]No agent names to derive vector store patterns.[/]");
                return;
            }

            if (_taskTracker != null)
                _taskTracker.AddLog($"[grey]Searching for vector stores matching {agentNames.Count} agent(s)...[/]");
            else
                AnsiConsole.MarkupLine($"[grey]Searching for vector stores matching {agentNames.Count} agent(s)...[/]");

            int deletedVs = 0;

            OpenAIClient openAIClient = _client.GetProjectOpenAIClient();
            VectorStoreClient vectorStoreClient = openAIClient.GetVectorStoreClient();
            var vsPageable = vectorStoreClient.GetVectorStores();

            foreach (var vs in vsPageable)
            {
                try
                {
                    // Check if the vector store name matches the pattern: {AgentName}_vs
                    bool matches = agentNames.Any(agentName =>
                        vs.Name != null &&
                        vs.Name.StartsWith($"{agentName}_vs", StringComparison.OrdinalIgnoreCase));

                    if (matches)
                    {
                        vectorStoreClient.DeleteVectorStore(vs.Id);

                        if (_taskTracker != null)
                            _taskTracker.AddLog($"[red]✓[/] Deleted vector store: [grey]{vs.Name}[/] ({vs.Id})");
                        else
                            AnsiConsole.MarkupLine($"[red]✓[/] Deleted vector store: [grey]{vs.Name}[/] ({vs.Id})");
                        deletedVs++;
                    }
                }
                catch (Exception exVs)
                {
                    if (_taskTracker != null)
                        _taskTracker.AddLog($"[red]✗[/] Failed to delete vector store [grey]'{vs.Name}'[/]: {exVs.Message}");
                    else
                        AnsiConsole.MarkupLine($"[red]✗[/] Failed to delete vector store [grey]'{vs.Name}'[/]: {exVs.Message}");
                }
            }

            if (_taskTracker != null)
                _taskTracker.AddLog($"[green]✓[/] Deleted {deletedVs} vector store(s).");
            else
                AnsiConsole.MarkupLine($"[green]✓[/] Deleted {deletedVs} vector store(s).\n");
        }
        catch (Exception ex)
        {
            if (_taskTracker != null)
                _taskTracker.AddLog($"[yellow]⚠[/] Vector store deletion error: {ex.Message}");
            else
                AnsiConsole.MarkupLine($"[yellow]⚠[/] Vector store deletion error: {ex.Message}");
        }
    }
}
