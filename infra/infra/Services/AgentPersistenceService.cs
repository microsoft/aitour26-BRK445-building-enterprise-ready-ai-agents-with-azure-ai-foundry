using System.Text;
using System.Text.Json;
using Spectre.Console;

namespace Infra.AgentDeployment;

internal interface IAgentPersistenceService
{
    void PersistCreated(List<(string Name, string Id)> created);
}

internal sealed class AgentPersistenceService : IAgentPersistenceService
{
    private readonly TaskTracker? _taskTracker;

    public AgentPersistenceService(TaskTracker? taskTracker = null)
    {
        _taskTracker = taskTracker;
    }

    public void PersistCreated(List<(string Name, string Id)> created)
    {
        if (created.Count == 0)
        {
            if (_taskTracker != null)
                _taskTracker.AddLog("[yellow]No agents were created.[/]");
            else
                AnsiConsole.MarkupLine("[yellow]No agents were created.[/]");
            return;
        }

        if (_taskTracker != null)
            _taskTracker.AddLog("[cyan]Saving agent information...[/]");
        else
            AnsiConsole.MarkupLine("[cyan]Saving agent information...[/]");

        string logPath = Path.Combine(AppContext.BaseDirectory, "CreatedAgents.txt");
        using var txt = new StreamWriter(logPath, append: false, encoding: Encoding.UTF8);
        txt.WriteLine($"Creation Timestamp (UTC): {DateTime.UtcNow:O}");
        txt.WriteLine($"Agent Count: {created.Count}");
        txt.WriteLine("Agents:");
        foreach (var a in created) txt.WriteLine($"- Name: {a.Name} | Id: {a.Id}");

        if (_taskTracker != null)
        {
            _taskTracker.AddLog($"[green]✓[/] Plain text log:");
            _taskTracker.SetOutputPaths(logPath, null);
        }
        else
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Plain text log:");
            var tp = new Spectre.Console.TextPath(logPath)
                .RootColor(Color.Green)
                .SeparatorColor(Color.Grey)
                .StemColor(Color.White)
                .LeafColor(Color.Yellow);
            AnsiConsole.Write(tp);
        }

        var jsonPath = Path.Combine(AppContext.BaseDirectory, "CreatedAgents.json");
        var map = new Dictionary<string, string>();
        foreach (var a in created) map[BuildConnectionStringKey(a.Name)] = a.Id;
        var json = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(jsonPath, json, Encoding.UTF8);

        // Save activity log to file
        var logFilePath = Path.Combine(AppContext.BaseDirectory, "CreatedAgents.log");
        if (_taskTracker != null)
        {
            var allLogs = _taskTracker.GetAllLogs();
            using var logWriter = new StreamWriter(logFilePath, append: false, encoding: Encoding.UTF8);
            logWriter.WriteLine($"Activity Log - Creation Timestamp (UTC): {DateTime.UtcNow:O}");
            logWriter.WriteLine(new string('=', 80));
            foreach (var log in allLogs)
            {
                // Strip Spectre.Console markup for plain text log
                var plainLog = System.Text.RegularExpressions.Regex.Replace(log, @"\[.*?\]", "");
                logWriter.WriteLine(plainLog);
            }
            _taskTracker.AddLog($"[green]✓[/] Activity log file:");
        }

        if (_taskTracker != null)
        {
            _taskTracker.AddLog($"[green]✓[/] JSON connection string map:");
            _taskTracker.SetOutputPaths(logPath, jsonPath, logFilePath);
        }
        else
        {
            AnsiConsole.MarkupLine($"[green]✓[/] JSON connection string map:");
            var tp = new Spectre.Console.TextPath(jsonPath)
                .RootColor(Color.Green)
                .SeparatorColor(Color.Grey)
                .StemColor(Color.White)
                .LeafColor(Color.Yellow);
            AnsiConsole.Write(tp);
        }
    }
    private static string BuildConnectionStringKey(string name)
    {
        var cleaned = new string(name.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray()).Replace("analysis", "analyzer");
        return $"Parameters:{cleaned}id";
    }
}
