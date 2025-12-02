using Azure.AI.Agents.Persistent;
using Azure.AI.Projects;
using Azure.Identity;
using Infra.AgentDeployment;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

// Console deployer for persistent agents
// Refactored for readability and single-responsibility separation.

AnsiConsole.Write(new FigletText("AI Agents Deployer").Centered().Color(Color.Blue));
AnsiConsole.WriteLine();

var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

// Default values
const string DefaultProjectEndpoint = "https://your-project.services.ai.azure.com/api/projects/your-project";
const string DefaultModelDeploymentName = "gpt-4.1-mini";
const string DefaultTenantId = "";

// Read configuration with defaults
var projectEndpoint = config["ProjectEndpoint"];
var modelDeploymentName = config["ModelDeploymentName"];
var tenantId = config["TenantId"];

// Prompt for missing values with defaults
if (string.IsNullOrWhiteSpace(projectEndpoint))
{
    projectEndpoint = AnsiConsole.Ask<string>("Enter [green]Project Endpoint[/]:", DefaultProjectEndpoint);
}
else
{
    AnsiConsole.MarkupLine($"[grey]Using Project Endpoint:[/] [cyan]{projectEndpoint}[/]");
}

if (string.IsNullOrWhiteSpace(modelDeploymentName))
{
    modelDeploymentName = AnsiConsole.Ask<string>("Enter [green]Model Deployment Name[/]:", DefaultModelDeploymentName);
}
else
{
    AnsiConsole.MarkupLine($"[grey]Using Model Deployment:[/] [cyan]{modelDeploymentName}[/]");
}

if (string.IsNullOrWhiteSpace(tenantId))
{
    tenantId = AnsiConsole.Prompt(
        new TextPrompt<string>("Enter [green]Tenant ID[/] (optional):")
            .AllowEmpty()
            .DefaultValue(DefaultTenantId));
}
else
{
    AnsiConsole.MarkupLine($"[grey]Using Tenant ID:[/] [cyan]{tenantId}[/]");
}

if (string.IsNullOrWhiteSpace(projectEndpoint) || string.IsNullOrWhiteSpace(modelDeploymentName))
{
    AnsiConsole.MarkupLine("[red]Error: Missing required configuration settings: ProjectEndpoint, ModelDeploymentName[/]");
    return;
}

AnsiConsole.WriteLine();
AnsiConsole.WriteLine();

//PersistentAgentsClient client = null;

AIProjectClient client = null;

AnsiConsole.Status()
    .Start("Initializing Azure AI Project Client...", ctx =>
    {
        // if tenantId is specified, use DefaultAzureCredential with tenant
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = tenantId });
            client = new AIProjectClient(new Uri(projectEndpoint), credential);
            AnsiConsole.MarkupLine("[green]✓[/] Connected with DefaultAzureCredential (Tenant-specific)");
        }
        else
        {
            client = new AIProjectClient(new Uri(projectEndpoint), new AzureCliCredential());
            AnsiConsole.MarkupLine("[green]✓[/] Connected with AzureCliCredential");
        }
    });

AnsiConsole.WriteLine();

AnsiConsole.WriteLine();

// Path to JSON configuration file containing agent metadata and optional knowledge files
string agentConfigPath = Path.Combine(AppContext.BaseDirectory, "agents.json");

if (!File.Exists(agentConfigPath))
{
    AnsiConsole.MarkupLine($"[red]Error: Configuration file not found at {agentConfigPath}[/]");
    return;
}

AnsiConsole.MarkupLine($"[grey]Using configuration:[/] [cyan]{Path.GetFileName(agentConfigPath)}[/]");
AnsiConsole.WriteLine();

var runner = new AgentDeploymentRunner(client, modelDeploymentName, agentConfigPath);

// Support optional command line switch --delete to skip interactive prompt
bool? deleteFlag = args.Contains("--delete", StringComparer.OrdinalIgnoreCase) ? true :
                    args.Contains("--no-delete", StringComparer.OrdinalIgnoreCase) ? false : null;

runner.Run(deleteFlag);