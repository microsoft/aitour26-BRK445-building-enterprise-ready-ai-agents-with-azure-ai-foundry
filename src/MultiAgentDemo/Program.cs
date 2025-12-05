using Microsoft.Agents.AI;
using MultiAgentDemo.Services;
using ZavaFoundryAgentsProvider;
using ZavaMAFAgentsProvider;
using ZavaMAFLocalAgentsProvider;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register MAFAgentProvider for Microsoft Foundry integration
var microsoftFoundryProjectConnection = builder.Configuration.GetConnectionString("microsoftfoundryproject");
builder.Services.AddSingleton(_ => new MAFAgentProvider(microsoftFoundryProjectConnection!));

// Register MAFLocalAgentProvider for local agent creation
var microsoftFoundryCnnString = builder.Configuration.GetConnectionString("microsoftfoundrycnnstring");
var chatDeploymentName = builder.Configuration["AI_ChatDeploymentName"] ?? "gpt-5-mini";
if (!string.IsNullOrEmpty(microsoftFoundryCnnString))
{
    builder.Services.AddSingleton(_ => new MAFLocalAgentProvider(microsoftFoundryCnnString, chatDeploymentName));
}

// Register HTTP clients for external services (used by LLM direct call and DirectCall modes)
RegisterHttpClients(builder);

// Register orchestration services for LLM mode
RegisterOrchestrationServices(builder);

// Register AI agents from Microsoft Foundry (used by MAF Foundry mode)
RegisterFoundryAgents(builder);

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>
/// Registers HTTP clients for external service communication (LLM direct call and DirectCall modes).
/// </summary>
static void RegisterHttpClients(WebApplicationBuilder builder)
{
    builder.Services.AddHttpClient<InventoryAgentService>(
        client => client.BaseAddress = new Uri("https+http://inventoryservice"));

    builder.Services.AddHttpClient<MatchmakingAgentService>(
        client => client.BaseAddress = new Uri("https+http://matchmakingservice"));

    builder.Services.AddHttpClient<LocationAgentService>(
        client => client.BaseAddress = new Uri("https+http://locationservice"));

    builder.Services.AddHttpClient<NavigationAgentService>(
        client => client.BaseAddress = new Uri("https+http://navigationservice"));
}

/// <summary>
/// Registers orchestration services for different multi-agent patterns (LLM mode).
/// </summary>
static void RegisterOrchestrationServices(WebApplicationBuilder builder)
{
    builder.Services.AddScoped<SequentialOrchestrationService>();
    builder.Services.AddScoped<ConcurrentOrchestrationService>();
    builder.Services.AddScoped<HandoffOrchestrationService>();
    builder.Services.AddScoped<GroupChatOrchestrationService>();
    builder.Services.AddScoped<MagenticOrchestrationService>();
}

/// <summary>
/// Registers AI agents from Microsoft Foundry for MAF Foundry mode.
/// Each agent is registered as a keyed singleton for dependency injection.
/// </summary>
static void RegisterFoundryAgents(WebApplicationBuilder builder)
{
    foreach (AgentNamesProvider.AgentName agentName in Enum.GetValues<AgentNamesProvider.AgentName>())
    {
        var agentId = AgentNamesProvider.GetAgentName(agentName);

        builder.Services.AddKeyedSingleton<AIAgent>(agentId, (sp, _) =>
        {
            var provider = sp.GetRequiredService<MAFAgentProvider>();
            return provider.GetAIAgent(agentId);
        });
    }
}
