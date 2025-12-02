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
    public void PersistCreated(List<(string Name, string Id)> created)
    {
        if (created.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No agents were created.[/]");
            return;
        }

        AnsiConsole.MarkupLine("[cyan]Saving agent information...[/]");

        string logPath = Path.Combine(AppContext.BaseDirectory, "CreatedAgents.txt");
        using var txt = new StreamWriter(logPath, append: false, encoding: Encoding.UTF8);
        txt.WriteLine($"Creation Timestamp (UTC): {DateTime.UtcNow:O}");
        txt.WriteLine($"Agent Count: {created.Count}");
        txt.WriteLine("Agents:");
        foreach (var a in created) txt.WriteLine($"- Name: {a.Name} | Id: {a.Id}");
        AnsiConsole.MarkupLine($"[green]✓[/] Plain text log: [grey]{logPath}[/]");

        var jsonPath = Path.Combine(AppContext.BaseDirectory, "CreatedAgents.json");
        var map = new Dictionary<string, string>();
        foreach (var a in created) map[BuildConnectionStringKey(a.Name)] = a.Id;
        var json = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(jsonPath, json, Encoding.UTF8);
        AnsiConsole.MarkupLine($"[green]✓[/] JSON connection string map: [grey]{jsonPath}[/]");
    }
    private static string BuildConnectionStringKey(string name)
    {
        var cleaned = new string(name.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray()).Replace("analysis", "analyzer");
        return $"ConnectionStrings:{cleaned}id";
    }
}
