using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Infra.AgentDeployment; // added namespace for deployment types

// Console deployer for persistent agents
// Refactored for readability and single-responsibility separation.

var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var projectEndpoint = config["ProjectEndpoint"];
var modelDeploymentName = config["ModelDeploymentName"];
var tenantId = config["TenantId"];

if (string.IsNullOrWhiteSpace(projectEndpoint) || string.IsNullOrWhiteSpace(modelDeploymentName) || string.IsNullOrWhiteSpace(tenantId))
{
    Console.WriteLine("Missing required configuration settings: ProjectEndpoint, ModelDeploymentName, TenantId");
    return;
}

PersistentAgentsClient client = null;

// if tenantId is specified, use DefaultAzureCredential with tenant
if (!string.IsNullOrWhiteSpace(tenantId))
{
    var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = tenantId });
    client = new PersistentAgentsClient(projectEndpoint, credential);
}
else
{
    client = new PersistentAgentsClient(projectEndpoint, new AzureCliCredential());
}

    // Path to JSON configuration file containing agent metadata and optional knowledge files
    string agentConfigPath = Path.Combine(AppContext.BaseDirectory, "agents.json");

var runner = new AgentDeploymentRunner(client, modelDeploymentName, agentConfigPath);

// Support optional command line switch --delete to skip interactive prompt
bool? deleteFlag = args.Contains("--delete", StringComparer.OrdinalIgnoreCase) ? true :
                    args.Contains("--no-delete", StringComparer.OrdinalIgnoreCase) ? false : null;

runner.Run(deleteFlag);