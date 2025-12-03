#pragma warning disable SKEXP0110

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.SemanticKernel.Agents.AzureAI;
using MultiAgentDemo.Services;
using ZavaAIFoundrySKAgentsProvider;
using ZavaFoundryAgentsProvider;
using ZavaMAFAgentsProvider;
using ZavaSemanticKernelProvider;

// KernelAzureOpenAIConfigurator moved to its own file under Services to avoid mixing
// type declarations with top-level statements in Program.cs.

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers();

// Add Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

/********************************************************/
// The following code registers the agent providers for Semantic Kernel
var microsoftFoundryCnnString = builder.Configuration.GetValue<string>("ConnectionStrings:microsoftfoundrycnnstring");
var chatDeploymentName = builder.Configuration["AI_ChatDeploymentName"] ?? "gpt-5-mini";
builder.Services.AddSingleton(sp =>
    new SemanticKernelProvider(microsoftFoundryCnnString, chatDeploymentName));
/********************************************************/

/********************************************************/
// The following code registers the agent providers for the Microsoft Foundry project.  
var microsoftFoundryProjectConnection = builder.Configuration.GetConnectionString("microsoftfoundryproject");
builder.Services.AddSingleton(sp =>
{
    return new AIFoundryAgentProvider(microsoftFoundryProjectConnection, "");
});

builder.Services.AddSingleton(sp =>
{
    return new MAFAgentProvider(microsoftFoundryProjectConnection!);
});
/********************************************************/

builder.Services.AddSingleton(sp => builder.Configuration);

// Register service layer implementations for multi-agent external services
builder.Services.AddHttpClient<InventoryAgentService>(
    client => client.BaseAddress = new("https+http://inventoryservice"));

builder.Services.AddHttpClient<MatchmakingAgentService>(
    client => client.BaseAddress = new Uri("https+http://matchmakingservice"));

builder.Services.AddHttpClient<LocationAgentService>(
    client => client.BaseAddress = new Uri("https+http://locationservice"));

builder.Services.AddHttpClient<NavigationAgentService>(
    client => client.BaseAddress = new Uri("https+http://navigationservice"));

// Register orchestration services
builder.Services.AddScoped<SequentialOrchestrationService>();
builder.Services.AddScoped<ConcurrentOrchestrationService>();
builder.Services.AddScoped<HandoffOrchestrationService>();
builder.Services.AddScoped<GroupChatOrchestrationService>();
builder.Services.AddScoped<MagenticOrchestrationService>();

// Register agents in the DI using Semantic Kernel and the Microsoft Agent Framework
AddAgentInSkAndAgentFx(builder);

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Local function to register agents using the SemanticKernel
static void AddAgentInSkAndAgentFx(WebApplicationBuilder builder)
{
    // iterate through the enum values of AgentNamesProvider.AgentName
    foreach (AgentNamesProvider.AgentName agentName in Enum.GetValues(typeof(AgentNamesProvider.AgentName)))
    {
        var agentId = AgentNamesProvider.GetAgentName(agentName);

        builder.Services.AddKeyedSingleton<AzureAIAgent>(agentId, (sp, key) =>
        {
            var agentSKProvider = sp.GetRequiredService<AIFoundryAgentProvider>();
            return agentSKProvider.CreateAzureAIAgent(agentId);
        });
        builder.Services.AddKeyedSingleton<AIAgent>(agentId, (sp, key) =>
        {
            var agentFxProvider = sp.GetRequiredService<MAFAgentProvider>();
            return agentFxProvider.GetAIAgent(agentId);
        });
    }
}
