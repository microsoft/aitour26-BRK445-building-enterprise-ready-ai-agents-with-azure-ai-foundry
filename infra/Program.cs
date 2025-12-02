using Azure.AI.Projects;
using Azure.Identity;
using Infra.AgentDeployment;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

// Console deployer for persistent agents
// Refactored for readability and single-responsibility separation.

AnsiConsole.Write(new FigletText("BRK445 - INFRA").Centered().Color(Color.Blue));
AnsiConsole.WriteLine();

var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

// Read user secrets values (may be null or empty)
var secretProjectEndpoint = config["ProjectEndpoint"] ?? "";
var secretModelDeploymentName = config["ModelDeploymentName"] ?? "";
var secretTenantId = config["TenantId"] ?? "";

// Always prompt user with secrets as defaults
var projectEndpoint = AnsiConsole.Prompt(
    new TextPrompt<string>("Enter [green]Project Endpoint[/]:")
        .DefaultValue(secretProjectEndpoint)
        .AllowEmpty()
        .Validate(value =>
        {
            if (string.IsNullOrWhiteSpace(value))
                return ValidationResult.Error("[red]Project Endpoint cannot be empty[/]");
            return ValidationResult.Success();
        }));

var modelDeploymentName = AnsiConsole.Prompt(
    new TextPrompt<string>("Enter [green]Model Deployment Name[/]:")
        .DefaultValue(secretModelDeploymentName)
        .AllowEmpty()
        .Validate(value =>
        {
            if (string.IsNullOrWhiteSpace(value))
                return ValidationResult.Error("[red]Model Deployment Name cannot be empty[/]");
            return ValidationResult.Success();
        }));

var tenantId = AnsiConsole.Prompt(
    new TextPrompt<string>("Enter [green]Tenant ID[/] (optional):")
        .DefaultValue(secretTenantId)
        .AllowEmpty());

AnsiConsole.WriteLine();
AnsiConsole.WriteLine();

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