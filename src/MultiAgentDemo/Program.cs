using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using MultiAgentDemo.Services;
using ZavaFoundryAgentsProvider;
using ZavaMAFAgentsProvider;

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
// The following code registers the agent providers for the Microsoft Foundry project.  
var microsoftFoundryProjectConnection = builder.Configuration.GetConnectionString("microsoftfoundryproject");
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

// Register agents in the DI using the Microsoft Agent Framework
AddAgentsInAgentFx(builder);

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

// Local function to register agents using the Microsoft Agent Framework
static void AddAgentsInAgentFx(WebApplicationBuilder builder)
{
    // iterate through the enum values of AgentNamesProvider.AgentName
    foreach (AgentNamesProvider.AgentName agentName in Enum.GetValues(typeof(AgentNamesProvider.AgentName)))
    {
        var agentId = AgentNamesProvider.GetAgentName(agentName);

        builder.Services.AddKeyedSingleton<AIAgent>(agentId, (sp, key) =>
        {
            var agentFxProvider = sp.GetRequiredService<MAFAgentProvider>();
            return agentFxProvider.GetAIAgent(agentId);
        });
    }
}
