using Azure.AI.Projects;
using Azure.Identity;
using Infra.AgentDeployment;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

// Console deployer for persistent agents with task tracking UI

var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

// Read user secrets values (may be null or empty)
var secretProjectEndpoint = config["ProjectEndpoint"] ?? "";
var secretModelDeploymentName = config["ModelDeploymentName"] ?? "";
var secretTenantId = config["TenantId"] ?? "";

// Start live display immediately with defaults (may be empty) and collect inputs inside the box
var taskTracker = new TaskTracker(secretProjectEndpoint, secretModelDeploymentName);
var liveDisplayTask = System.Threading.Tasks.Task.Run(() => taskTracker.StartLiveDisplay());
// Allow live context to initialize
System.Threading.Thread.Sleep(200);
var (projectEndpoint, modelDeploymentName, tenantId) = taskTracker.CollectInitialInputs(secretProjectEndpoint, secretModelDeploymentName, secretTenantId);

// slight pause to show logs of input completion
System.Threading.Thread.Sleep(200);

AIProjectClient? client = null;

try
{
    taskTracker.StartTask("Set Environment Values");

    // if tenantId is specified, use DefaultAzureCredential with tenant
    if (!string.IsNullOrWhiteSpace(tenantId))
    {
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = tenantId });
        client = new AIProjectClient(new Uri(projectEndpoint), credential);
        taskTracker.AddLog("[green]✓[/] Connected with DefaultAzureCredential");
    }
    else
    {
        client = new AIProjectClient(new Uri(projectEndpoint), new AzureCliCredential());
        taskTracker.AddLog("[green]✓[/] Connected with AzureCliCredential");
    }

    taskTracker.CompleteTask("Set Environment Values");

    // Path to JSON configuration file containing agent metadata and optional knowledge files
    string agentConfigPath = Path.Combine(AppContext.BaseDirectory, "agents.json");

    if (!File.Exists(agentConfigPath))
    {
        taskTracker.AddLog($"[red]Error: Configuration file not found at {agentConfigPath}[/]");
        return;
    }

    taskTracker.AddLog($"[grey]Using configuration:[/] [cyan]{Path.GetFileName(agentConfigPath)}[/]");

    var runner = new AgentDeploymentRunner(client, modelDeploymentName, agentConfigPath, taskTracker);

    // Support optional command line switch --delete to skip interactive prompt
    bool? deleteFlag = args.Contains("--delete", StringComparer.OrdinalIgnoreCase) ? true :
                        args.Contains("--no-delete", StringComparer.OrdinalIgnoreCase) ? false : null;

    runner.Run(deleteFlag);

    taskTracker.CompleteTask("Creating");
    taskTracker.CompleteTask("Deleting");
    taskTracker.AddLog("");
    taskTracker.AddLog("[green]✓ Deployment completed successfully![/]");

    System.Threading.Thread.Sleep(1000);
}
catch (Exception ex)
{
    taskTracker.AddLog($"[red]Error: {ex.Message}[/]");
    System.Threading.Thread.Sleep(2000);
}

// Stop live display
taskTracker.StopLiveDisplay();
liveDisplayTask.Wait();

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[grey]Press any key to exit...[/]");
Console.ReadKey();