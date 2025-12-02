#pragma warning disable IDE0017, OPENAI001

using Azure.AI.Projects;
using OpenAI;
using OpenAI.Files;
using Spectre.Console;
using System.ClientModel;

namespace Infra.AgentDeployment;

internal sealed record UploadedFile(string UploadedId, string Filename, string FilePath);

internal interface IAgentFileUploader
{
    Dictionary<string, UploadedFile> UploadAllFiles(IEnumerable<AgentDefinition> definitions);
}

internal sealed class AgentFileUploader : IAgentFileUploader
{
    private readonly AIProjectClient _client;
    private readonly TaskTracker? _taskTracker;

    public AgentFileUploader(AIProjectClient client, TaskTracker? taskTracker = null)
    {
        _client = client;
        _taskTracker = taskTracker;
    }

    public Dictionary<string, UploadedFile> UploadAllFiles(IEnumerable<AgentDefinition> definitions)
    {
        if (_taskTracker != null)
            _taskTracker.AddLog("[cyan]Analyzing agent definitions for file uploads...[/]");
        else
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
            if (_taskTracker != null)
                _taskTracker.AddLog("[grey]No files referenced by any agent. Skipping upload phase.[/]");
            else
                AnsiConsole.MarkupLine("[grey]No files referenced by any agent. Skipping upload phase.[/]\n");
            return new Dictionary<string, UploadedFile>(StringComparer.OrdinalIgnoreCase);
        }

        var uploaded = new Dictionary<string, UploadedFile>(StringComparer.OrdinalIgnoreCase);
        OpenAIClient openAIClient = _client.GetProjectOpenAIClient();
        OpenAIFileClient fileClient = openAIClient.GetOpenAIFileClient();

        if (_taskTracker != null)
        {
            _taskTracker.AddLog($"[cyan]Uploading {uniquePaths.Count} file(s)...[/]");
            int attempted = 0;
            foreach (var path in uniquePaths)
            {
                attempted++;
                if (!File.Exists(path))
                {
                    _taskTracker.AddLog($"[yellow]⚠[/] File missing, skipped: [grey]{path}[/]");
                    continue;
                }
                if (uploaded.ContainsKey(path))
                {
                    continue;
                }

                try
                {
                    var info = new FileInfo(path);
                    _taskTracker.AddLog($"[cyan]Uploading[/] {info.Name}");
                    ClientResult<OpenAIFile> uploadResult = fileClient.UploadFile(
                        filePath: path,
                        purpose: FileUploadPurpose.Assistants);
                    uploaded[path] = new UploadedFile(uploadResult.Value.Id, uploadResult.Value.Filename, path);
                    _taskTracker.AddLog($"[green]✓[/] Uploaded: [grey]{uploadResult.Value.Filename}[/] (Id: {uploadResult.Value.Id})");
                }
                catch (Exception exUp)
                {
                    _taskTracker.AddLog($"[red]✗[/] Upload failed for [grey]{path}[/]: {exUp.Message}");
                }
            }
            _taskTracker.AddLog($"[green]✓[/] Upload complete. Successfully uploaded {uploaded.Count}/{uniquePaths.Count} file(s).");
            _taskTracker.CompleteSubTask("Creating", "DataSets");
        }
        else
        {
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
                            ClientResult<OpenAIFile> uploadResult = fileClient.UploadFile(
                                filePath: path,
                                purpose: FileUploadPurpose.Assistants);
                            uploaded[path] = new UploadedFile(uploadResult.Value.Id, uploadResult.Value.Filename, path);
                            AnsiConsole.MarkupLine($"[green]✓[/] Uploaded: [grey]{uploadResult.Value.Filename}[/] (Id: {uploadResult.Value.Id})");
                        }
                        catch (Exception exUp)
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Upload failed for [grey]{path}[/]: {exUp.Message}");
                        }
                        task.Increment(1);
                    }
                });

            AnsiConsole.MarkupLine($"[green]✓[/] Upload complete. Successfully uploaded {uploaded.Count}/{uniquePaths.Count} file(s).\n");
        }

        return uploaded;
    }
}
