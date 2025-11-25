using Azure.AI.Agents.Persistent;

namespace Infra.AgentDeployment;

internal interface IAgentCreationService
{
 List<(string Name, string Id)> CreateAgents(IEnumerable<AgentDefinition> definitions, Dictionary<string, UploadedFile> uploadedFiles);
}

internal sealed class AgentCreationService : IAgentCreationService
{
 private readonly PersistentAgentsClient _client;
 private readonly string _modelDeploymentName;
 public AgentCreationService(PersistentAgentsClient client, string modelDeploymentName)
 {
 _client = client;
 _modelDeploymentName = modelDeploymentName;
 }
 public List<(string Name, string Id)> CreateAgents(IEnumerable<AgentDefinition> definitions, Dictionary<string, UploadedFile> uploadedFiles)
 {
 Console.WriteLine("Creating agents...\n");
 var created = new List<(string Name, string Id)>();
 foreach (var def in definitions)
 {
 try
 {
 Console.WriteLine($"--- Starting creation of agent: {def.Name}");
 Console.WriteLine($" Instructions length: {def.Instructions?.Length ??0} chars");
 Console.WriteLine($" Files referenced: {(def.Files?.Count ??0)}");
 PersistentAgent agent = null;
 List<string> agentFileIds = new();
 if (def.Files is { Count: >0 })
 {
 foreach (var fileRef in def.Files)
 {
 var resolved = PathResolver.ResolveSourceFilePath(fileRef);
 if (uploadedFiles.TryGetValue(resolved, out var meta)) agentFileIds.Add(meta.UploadedId);
 else Console.WriteLine($" [WARN] File not uploaded (missing or failed earlier): {fileRef}");
 }
 }
 if (agentFileIds.Count >0)
 {
 Console.WriteLine($" Creating vector store for agent with {agentFileIds.Count} file(s)...");
 var fileSearchToolResource = new FileSearchToolResource();
 var vectorStoreName = $"{def.Name}_vs";
 PersistentAgentsVectorStore vectorStore = _client.VectorStores.CreateVectorStore(fileIds: agentFileIds, name: vectorStoreName);
 Console.WriteLine($" Vector store created: {vectorStore.Id}");
 fileSearchToolResource.VectorStoreIds.Add(vectorStore.Id);
 Console.WriteLine(" Creating agent with FileSearch + CodeInterpreter tools...");
 agent = _client.Administration.CreateAgent(model: _modelDeploymentName, name: def.Name, instructions: def.Instructions, tools: new List<ToolDefinition> { new CodeInterpreterToolDefinition(), new FileSearchToolDefinition() }, toolResources: new ToolResources { FileSearch = fileSearchToolResource });
 }
 if (agent == null)
 {
 Console.WriteLine(" Creating agent with CodeInterpreter tool only...");
 agent = _client.Administration.CreateAgent(model: _modelDeploymentName, name: def.Name, instructions: def.Instructions, tools: new List<ToolDefinition> { new CodeInterpreterToolDefinition() });
 }
 created.Add((def.Name, agent.Id));
 Console.WriteLine($" [SUCCESS] Agent created: {def.Name} => {agent.Id}\n");
 }
 catch (Exception ex)
 {
 Console.WriteLine($"[ERROR] Failed to create agent '{def.Name}': {ex.Message}\n");
 }
 }
 return created;
 }
}
