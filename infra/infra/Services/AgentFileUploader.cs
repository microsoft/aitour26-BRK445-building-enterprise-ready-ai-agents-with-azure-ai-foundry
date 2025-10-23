using Azure.AI.Agents.Persistent;

namespace Infra.AgentDeployment;

internal sealed record UploadedFile(string UploadedId, string Filename, string FilePath);

internal interface IAgentFileUploader
{
 Dictionary<string, UploadedFile> UploadAllFiles(IEnumerable<AgentDefinition> definitions);
}

internal sealed class AgentFileUploader : IAgentFileUploader
{
 private readonly PersistentAgentsClient _client;
 public AgentFileUploader(PersistentAgentsClient client) => _client = client;
 public Dictionary<string, UploadedFile> UploadAllFiles(IEnumerable<AgentDefinition> definitions)
 {
 Console.WriteLine("\nAnalyzing agent definitions for file uploads...");
 var uniquePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 foreach (var def in definitions)
 {
 if (def.Files is { Count: >0 })
 {
 foreach (var f in def.Files)
 {
 var resolved = PathResolver.ResolveSourceFilePath(f);
 if (!string.IsNullOrWhiteSpace(resolved)) uniquePaths.Add(resolved);
 }
 }
 }
 if (uniquePaths.Count ==0)
 {
 Console.WriteLine("No files referenced by any agent. Skipping upload phase.\n");
 return new Dictionary<string, UploadedFile>(StringComparer.OrdinalIgnoreCase);
 }
 Console.WriteLine($"Found {uniquePaths.Count} unique file path(s) referenced across agents.");
 var uploaded = new Dictionary<string, UploadedFile>(StringComparer.OrdinalIgnoreCase);
 int attempted =0;
 foreach (var path in uniquePaths)
 {
 attempted++;
 if (!File.Exists(path))
 {
 Console.WriteLine($" [WARN] File missing, skipped: {path}");
 continue;
 }
 if (uploaded.ContainsKey(path)) continue;
 try
 {
 var info = new FileInfo(path);
 using var stream = File.OpenRead(path);
 Console.WriteLine($" Uploading ({attempted}/{uniquePaths.Count}): {info.Name} (Size: {info.Length} bytes)");
 PersistentAgentFileInfo uploadedInfo = _client.Files.UploadFile(data: stream, purpose: PersistentAgentFilePurpose.Agents, filename: info.Name);
 uploaded[path] = new UploadedFile(uploadedInfo.Id, uploadedInfo.Filename, path);
 Console.WriteLine($" -> Uploaded: {uploadedInfo.Filename} (Id: {uploadedInfo.Id})");
 }
 catch (Exception exUp)
 {
 Console.WriteLine($" [ERROR] Upload failed for {path}: {exUp.Message}");
 }
 }
 Console.WriteLine($"Upload phase complete. Successfully uploaded {uploaded.Count} file(s).\n");
 return uploaded;
 }
}
