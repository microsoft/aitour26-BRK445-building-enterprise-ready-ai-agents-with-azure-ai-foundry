using Azure.AI.Agents.Persistent;
using System.IO;

namespace Infra.AgentDeployment;

internal interface IAgentDeletionService
{
    void DeleteExisting(IEnumerable<AgentDefinition> definitions);
}

internal sealed class AgentDeletionService : IAgentDeletionService
{
    private readonly PersistentAgentsClient _client;
    public AgentDeletionService(PersistentAgentsClient client) => _client = client;
    public void DeleteExisting(IEnumerable<AgentDefinition> definitions)
    {
        try
        {
            Console.WriteLine("Starting deletion of matching agents...");
            var namesToDelete = new HashSet<string>(definitions.Select(d => d.Name), StringComparer.OrdinalIgnoreCase);
            int deletedAgents =0;
            foreach (var existing in _client.Administration.GetAgents())
            {
                if (namesToDelete.Contains(existing.Name))
                {
                    try
                    {
                        _client.Administration.DeleteAgent(existing.Id);
                        Console.WriteLine($"Deleted agent: {existing.Name} ({existing.Id})");
                        deletedAgents++;
                    }
                    catch (Exception exDel)
                    {
                        Console.WriteLine($"Failed to delete agent '{existing.Name}' ({existing.Id}): {exDel.Message}");
                    }
                }
            }
            Console.WriteLine($"Deletion phase (agents) complete. Deleted {deletedAgents} agent(s).\n");

            DeleteReferencedFiles(definitions);
            DeleteVectorStores(definitions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error during deletion phase: {ex.Message}");
            Console.WriteLine("Continuing with agent creation...\n");
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
            if (fileNames.Count ==0)
            {
                Console.WriteLine("No file names referenced in definitions to delete.");
                return;
            }
            Console.WriteLine($"Attempting to delete files referenced by definitions ({fileNames.Count} unique name(s))...");
            int deletedFiles =0;
            var filesResponse = _client.Files.GetFiles();
            foreach (var existingFile in filesResponse.Value)
            {
                try
                {
                    if (fileNames.Contains(existingFile.Filename))
                    {
                        _client.Files.DeleteFile(existingFile.Id);
                        Console.WriteLine($"Deleted file: {existingFile.Filename} ({existingFile.Id})");
                        deletedFiles++;
                    }
                }
                catch (Exception exFile)
                {
                    Console.WriteLine($"Failed to delete file '{existingFile.Filename}' ({existingFile.Id}): {exFile.Message}");
                }
            }
            Console.WriteLine($"File deletion phase complete. Deleted {deletedFiles} file(s).\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] File deletion phase encountered an error: {ex.Message}");
        }
    }

    private void DeleteVectorStores(IEnumerable<AgentDefinition> definitions)
    {
        try
        {
            var vectorStoreNames = new HashSet<string>(
                definitions.Select(d => $"{d.Name}_vs"),
                StringComparer.OrdinalIgnoreCase);
            if (vectorStoreNames.Count ==0)
            {
                Console.WriteLine("No vector store names derived from definitions.");
                return;
            }
            Console.WriteLine($"Attempting to delete vector stores matching agent naming convention ({vectorStoreNames.Count} name(s))...");
            int deletedVs =0;
            var vsPageable = _client.VectorStores.GetVectorStores();
            foreach (var vs in vsPageable)
            {
                try
                {
                    if (vectorStoreNames.Contains(vs.Name))
                    {
                        _client.VectorStores.DeleteVectorStore(vs.Id);
                        Console.WriteLine($"Deleted vector store: {vs.Name} ({vs.Id})");
                        deletedVs++;
                    }
                }
                catch (Exception exVs)
                {
                    Console.WriteLine($"Failed to delete vector store '{vs.Name}' ({vs.Id}): {exVs.Message}");
                }
            }
            Console.WriteLine($"Vector store deletion phase complete. Deleted {deletedVs} store(s).\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Vector store deletion phase encountered an error: {ex.Message}");
        }
    }
}
