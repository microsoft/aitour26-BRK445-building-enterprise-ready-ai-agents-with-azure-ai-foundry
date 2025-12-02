using Azure.AI.Agents.Persistent;
using Azure.AI.Projects;
using System.IO;
using Spectre.Console;

namespace Infra.AgentDeployment;

internal interface IAgentDeletionService
{
    void DeleteExisting(IEnumerable<AgentDefinition> definitions);
}

internal sealed class AgentDeletionService : IAgentDeletionService
{
    private readonly AIProjectClient _client;
    public AgentDeletionService(AIProjectClient client) => _client = client;
    public void DeleteExisting(IEnumerable<AgentDefinition> definitions)
    {
        try
        {
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .Start("Deleting existing agents...", ctx =>
                {
                    var namesToDelete = new HashSet<string>(definitions.Select(d => d.Name), StringComparer.OrdinalIgnoreCase);
                    int deletedAgents = 0;
                    foreach (var existing in _client.Administration.GetAgents())
                    {
                        if (namesToDelete.Contains(existing.Name))
                        {
                            try
                            {
                                _client.Administration.DeleteAgent(existing.Id);
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

            DeleteReferencedFiles(definitions);
            DeleteVectorStores(definitions);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠[/] Unexpected error during deletion: {ex.Message}");
            AnsiConsole.MarkupLine("[grey]Continuing with agent creation...[/]\n");
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
                AnsiConsole.MarkupLine("[grey]No file names referenced in definitions to delete.[/]");
                return;
            }
            AnsiConsole.MarkupLine($"[grey]Deleting {fileNames.Count} referenced file(s)...[/]");
            int deletedFiles = 0;
            var filesResponse = _client.Files.GetFiles();
            foreach (var existingFile in filesResponse.Value)
            {
                try
                {
                    if (fileNames.Contains(existingFile.Filename))
                    {
                        _client.Files.DeleteFile(existingFile.Id);
                        AnsiConsole.MarkupLine($"[red]✓[/] Deleted file: [grey]{existingFile.Filename}[/] ({existingFile.Id})");
                        deletedFiles++;
                    }
                }
                catch (Exception exFile)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to delete file [grey]'{existingFile.Filename}'[/]: {exFile.Message}");
                }
            }
            AnsiConsole.MarkupLine($"[green]✓[/] Deleted {deletedFiles} file(s).\n");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠[/] File deletion error: {ex.Message}");
        }
    }

    private void DeleteVectorStores(IEnumerable<AgentDefinition> definitions)
    {
        try
        {
            var vectorStoreNames = new HashSet<string>(
                definitions.Select(d => $"{d.Name}_vs"),
                StringComparer.OrdinalIgnoreCase);
            if (vectorStoreNames.Count == 0)
            {
                AnsiConsole.MarkupLine("[grey]No vector store names derived from definitions.[/]");
                return;
            }
            AnsiConsole.MarkupLine($"[grey]Deleting {vectorStoreNames.Count} vector store(s)...[/]");
            int deletedVs = 0;
            var vsPageable = _client.VectorStores.GetVectorStores();
            foreach (var vs in vsPageable)
            {
                try
                {
                    if (vectorStoreNames.Contains(vs.Name))
                    {
                        _client.VectorStores.DeleteVectorStore(vs.Id);
                        AnsiConsole.MarkupLine($"[red]✓[/] Deleted vector store: [grey]{vs.Name}[/] ({vs.Id})");
                        deletedVs++;
                    }
                }
                catch (Exception exVs)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to delete vector store [grey]'{vs.Name}'[/]: {exVs.Message}");
                }
            }
            AnsiConsole.MarkupLine($"[green]✓[/] Deleted {deletedVs} vector store(s).\n");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠[/] Vector store deletion error: {ex.Message}");
        }
    }
}
