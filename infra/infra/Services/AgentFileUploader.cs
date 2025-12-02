using Azure.AI.Agents.Persistent;
using Azure.AI.Projects;
using Spectre.Console;

namespace Infra.AgentDeployment;

internal sealed record UploadedFile(string UploadedId, string Filename, string FilePath);

internal interface IAgentFileUploader
{
    Dictionary<string, UploadedFile> UploadAllFiles(IEnumerable<AgentDefinition> definitions);
}

internal sealed class AgentFileUploader : IAgentFileUploader
{
    private readonly AIProjectClient _client;
    public AgentFileUploader(AIProjectClient client) => _client = client;
    public Dictionary<string, UploadedFile> UploadAllFiles(IEnumerable<AgentDefinition> definitions)
    {
        AnsiConsole.MarkupLine("\n[cyan]Analyzing agent definitions for file uploads...[/]");
        var uniquePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var def in definitions)
        {
            if (def.Files is { Count: > 0 })
            {
                foreach (var f in def.Files)
                {
                    var resolved = PathResolver.ResolveSourceFilePath(f);
                    if (!string.IsNullOrWhiteSpace(resolved)) uniquePaths.Add(resolved);
                }
            }
        }
        if (uniquePaths.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No files referenced by any agent. Skipping upload phase.[/]\n");
            return new Dictionary<string, UploadedFile>(StringComparer.OrdinalIgnoreCase);
        }

        var uploaded = new Dictionary<string, UploadedFile>(StringComparer.OrdinalIgnoreCase);

        AnsiConsole.Progress()
            .Start(ctx =>
            {
                var task = ctx.AddTask($"[cyan]Uploading {uniquePaths.Count} file(s)[/]", maxValue: uniquePaths.Count);

                int attempted = 0;
                foreach (var path in uniquePaths)
                {
                    attempted++;
                    if (!File.Exists(path))
                    {
                        AnsiConsole.MarkupLine($"[yellow]⚠[/] File missing, skipped: [grey]{path}[/]");
                        task.Increment(1);
                        continue;
                    }
                    if (uploaded.ContainsKey(path))
                    {
                        task.Increment(1);
                        continue;
                    }

                    try
                    {
                        var info = new FileInfo(path);
                        task.Description = $"[cyan]Uploading[/] {info.Name}";
                        using var stream = File.OpenRead(path);
                        PersistentAgentFileInfo uploadedInfo = _client.Files.UploadFile(data: stream, purpose: PersistentAgentFilePurpose.Agents, filename: info.Name);
                        uploaded[path] = new UploadedFile(uploadedInfo.Id, uploadedInfo.Filename, path);
                        AnsiConsole.MarkupLine($"[green]✓[/] Uploaded: [grey]{uploadedInfo.Filename}[/] (Id: {uploadedInfo.Id})");
                    }
                    catch (Exception exUp)
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Upload failed for [grey]{path}[/]: {exUp.Message}");
                    }
                    task.Increment(1);
                }
            });

        AnsiConsole.MarkupLine($"[green]✓[/] Upload complete. Successfully uploaded {uploaded.Count}/{uniquePaths.Count} file(s).\n");
        return uploaded;
    }
}
