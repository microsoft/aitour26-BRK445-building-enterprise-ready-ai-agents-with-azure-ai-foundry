using Azure.AI.Agents.Persistent;

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
 int deletedCount =0;
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
}
