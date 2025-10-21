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

        if (!ConfirmCreation())
        {
            Console.WriteLine("Agent creation canceled by user.");
            return;
        }

        CreateAgentsAsync(definitions);
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

        Console.Write("Delete existing agents matching definitions? (Y/n): ");
        var input = Console.ReadLine();
        // Default YES when empty
        return string.IsNullOrWhiteSpace(input) || input.Trim().Equals("y", StringComparison.OrdinalIgnoreCase);
    }

    private bool ConfirmCreation()
    {
        Console.Write("Proceed to create agents now? (Y/n): ");
        var input = Console.ReadLine();
        // Default YES when empty
        return string.IsNullOrWhiteSpace(input) || input.Trim().Equals("y", StringComparison.OrdinalIgnoreCase);
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

    private async Task CreateAgentsAsync(IEnumerable<AgentDefinition> definitions)
    {
        Console.WriteLine("Creating agents...\n");
        var created = new List<(string Name, string Id)>();

        foreach (var def in definitions)
        {
            try
            {
                Console.WriteLine($"--- Starting creation of agent: {def.Name}");
                Console.WriteLine($" Instructions length: {def.Instructions?.Length ?? 0} chars");
                Console.WriteLine($" Files referenced: {(def.Files?.Count ?? 0)}");

                PersistentAgent agent = null;

                // first create the vector store if there are files to be added to the agent
                if (def.Files is { Count: > 0 })
                {
                    FileSearchToolResource fileSearchToolResource = new FileSearchToolResource();
                    Dictionary<string, string> uploadedFileIds = new();

                    foreach (var file in def.Files)
                    {
                        var filePath = ResolveSourceFilePath(file);
                        Console.WriteLine($" -> Resolving file: {file} => {filePath}");
                        if (!File.Exists(filePath))
                        {
                            Console.WriteLine($" [WARN] Missing file skipped");
                            continue;
                        }

                        try
                        {
                            var fileInfo = new FileInfo(filePath);
                            using var stream = File.OpenRead(filePath);
                            Console.WriteLine($" Uploading file: {fileInfo.Name} (Size: {fileInfo.Length} bytes)");
                            PersistentAgentFileInfo uploadedAgentFile = _client.Files.UploadFile(
                                data: stream,
                                purpose: PersistentAgentFilePurpose.Agents,
                                filename: fileInfo.Name);
                            uploadedFileIds.Add(uploadedAgentFile.Id, uploadedAgentFile.Filename);
                            Console.WriteLine($" Uploaded: {uploadedAgentFile.Filename} (Id: {uploadedAgentFile.Id})");
                        }
                        catch (Exception exUp)
                        {
                            Console.WriteLine($" [ERROR] Upload failed for {file}: {exUp.Message}");
                        }
                    }

                    if (uploadedFileIds.Count > 0)
                    {
                        var vectorStoreName = $"{def.Name}_vs";
                        Console.WriteLine($" Creating vector store: {vectorStoreName} with {uploadedFileIds.Count} file(s)");
                        PersistentAgentsVectorStore vectorStore = _client.VectorStores.CreateVectorStore(
                            fileIds: uploadedFileIds.Keys.ToList(),
                            name: vectorStoreName);
                        Console.WriteLine($" Vector store created: {vectorStore.Id}");
                        fileSearchToolResource.VectorStoreIds.Add(vectorStore.Id);

                        Console.WriteLine(" Creating agent with FileSearch + CodeInterpreter tools...");
                        agent = _client.Administration.CreateAgent(
                            model: _modelDeploymentName,
                            name: def.Name,
                            instructions: def.Instructions,
                            tools: new List<ToolDefinition> {
                                new CodeInterpreterToolDefinition(),
                                new FileSearchToolDefinition() },
                            toolResources: new ToolResources() { FileSearch = fileSearchToolResource });
                    }
                }

                // if no files (or none successfully uploaded), just create the agent with the provided instructions
                if (agent == null)
                {
                    Console.WriteLine(" Creating agent with CodeInterpreter tool only...");
                    agent = _client.Administration.CreateAgent(
                        model: _modelDeploymentName,
                        name: def.Name,
                        instructions: def.Instructions,
                        tools: new List<ToolDefinition> { new CodeInterpreterToolDefinition() });
                }

                created.Add((def.Name, agent.Id));
                Console.WriteLine($" [SUCCESS] Agent created: {def.Name} => {agent.Id}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to create agent '{def.Name}': {ex.Message}\n");
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

        // Plain text log with timestamp
        string logPath = Path.Combine(AppContext.BaseDirectory, "CreatedAgents.txt");
        using var txt = new StreamWriter(logPath, false, Encoding.UTF8);
        txt.WriteLine($"Creation Timestamp (UTC): {DateTime.UtcNow:O}");
        txt.WriteLine($"Agent Count: {created.Count}");
        txt.WriteLine("Agents:");
        foreach (var a in created)
        {
            txt.WriteLine($"- Name: {a.Name} | Id: {a.Id}");
        }
        Console.WriteLine($"Plain text log written to: {logPath}");

        // JSON mapping file (ConnectionStrings style)
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "CreatedAgents.json");
        var map = new Dictionary<string, string>();
        foreach (var a in created)
        {
            map[BuildConnectionStringKey(a.Name)] = a.Id;
        }
        var json = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(jsonPath, json, Encoding.UTF8);
        Console.WriteLine($"JSON connection string map written to: {jsonPath}");
    }

    private static string BuildConnectionStringKey(string name)
    {
        // Normalize: lowercase, remove non-alphanumerics, replace 'analysis' with 'analyzer', append 'id'
        var cleaned = new string(name.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        cleaned = cleaned.Replace("analysis", "analyzer");
        return $"ConnectionStrings:{cleaned}id";
    }

    // Resolves a source file path provided in agents.json (may contain forward slashes and repo-relative prefixes)
    private static string ResolveSourceFilePath(string file)
    {
        if (string.IsNullOrWhiteSpace(file)) return file ?? string.Empty;

        // Normalize separators
        var normalized = file.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);

        // If already rooted, just return
        if (Path.IsPathRooted(normalized)) return normalized;

        // 1. Try relative to current working directory
        var cwdCandidate = Path.GetFullPath(normalized, Directory.GetCurrentDirectory());
        if (File.Exists(cwdCandidate)) return cwdCandidate;

        // 2. Try relative to AppContext.BaseDirectory
        var baseDirCandidate = Path.GetFullPath(normalized, AppContext.BaseDirectory);
        if (File.Exists(baseDirCandidate)) return baseDirCandidate;

        // 3. Walk up from base directory to find file (handles bin/Debug/netX output nesting)
        var probe = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 6 && probe != null; i++)
        {
            var candidate = Path.Combine(probe.FullName, normalized);
            if (File.Exists(candidate)) return candidate;
            probe = probe.Parent;
        }

        // 4. If path starts with known folder like "infra" and we are under bin, attempt to drop leading folder if duplicated
        var parts = normalized.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1)
        {
            var withoutFirst = string.Join(Path.DirectorySeparatorChar, parts.Skip(1));
            probe = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 6 && probe != null; i++)
            {
                var candidate = Path.Combine(probe.FullName, withoutFirst);
                if (File.Exists(candidate)) return candidate;
                probe = probe.Parent;
            }
        }

        // Return the best-effort baseDirCandidate even if it does not exist; caller will handle missing.
        return baseDirCandidate;
    }
}
