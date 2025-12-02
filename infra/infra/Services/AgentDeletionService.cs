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
    public AgentDeletionService(AIProjectClient client) => _client = client;
    public void DeleteExisting(IEnumerable<AgentDefinition> definitions, bool deleteAgents, bool deleteIndexes, bool deleteDatasets)
    {
        try
        {
            if (deleteAgents)
            {
                DeleteAgents(definitions);
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Skipping agent deletion.[/]");
            }

            if (deleteDatasets)
            {
                DeleteReferencedFiles(definitions);
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Skipping dataset (file) deletion.[/]");
            }

            if (deleteIndexes)
            {
                DeleteVectorStores(definitions);
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Skipping index (vector store) deletion.[/]");
            }

            AnsiConsole.WriteLine();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠[/] Unexpected error during deletion: {ex.Message}");
            AnsiConsole.MarkupLine("[grey]Continuing with agent creation...[/]\n");
        }
    }

    private void DeleteAgents(IEnumerable<AgentDefinition> definitions)
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
            // Build a set of agent names to match against vector store names
            var agentNames = new HashSet<string>(
                definitions.Select(d => d.Name),
                StringComparer.OrdinalIgnoreCase);

            if (agentNames.Count == 0)
            {
                AnsiConsole.MarkupLine("[grey]No agent names to derive vector store patterns.[/]");
                return;
            }

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
