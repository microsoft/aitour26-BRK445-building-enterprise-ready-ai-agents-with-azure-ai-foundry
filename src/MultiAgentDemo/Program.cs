#pragma warning disable SKEXP0110

using Microsoft.Agents.AI;
using Microsoft.SemanticKernel.Agents.AzureAI;
using MultiAgentDemo.Services;
using ZavaAgentFxAgentsProvider;
using ZavaAIFoundrySKAgentsProvider;
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

// Register both agent providers - they will be available for their respective controllers
var openAiConnection = builder.Configuration.GetValue<string>("ConnectionStrings:aifoundry");
var chatDeploymentName = builder.Configuration["AI_ChatDeploymentName"] ?? "gpt-5-mini";
builder.Services.AddSingleton(sp =>
    new SemanticKernelProvider(openAiConnection, chatDeploymentName));

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var aiFoundryProjectConnection = config.GetConnectionString("aifoundryproject");
    return new AIFoundryAgentProvider(aiFoundryProjectConnection, "");
});

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var aiFoundryProjectConnection = config.GetConnectionString("aifoundryproject");
    return new AgentFxAgentProvider(aiFoundryProjectConnection!);
});

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

// =====================================================================
// Register agents in the DI using Semantic Kernel and the Microsoft Agent Framework
AddAgentInSkAndAgentFx(builder, "customerinformationagentid");
AddAgentInSkAndAgentFx(builder, "inventoryagentid");
AddAgentInSkAndAgentFx(builder, "locationserviceagentid");
AddAgentInSkAndAgentFx(builder, "navigationagentid");
AddAgentInSkAndAgentFx(builder, "photoanalyzeragentid");
AddAgentInSkAndAgentFx(builder, "productmatchmakingagentid");
AddAgentInSkAndAgentFx(builder, "productsearchagentid");
AddAgentInSkAndAgentFx(builder, "toolreasoningagentid");
// =====================================================================

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
static void AddAgentInSkAndAgentFx(WebApplicationBuilder builder, string key)
{
    builder.Services.AddKeyedSingleton<AzureAIAgent>(key, (sp, key) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var agentId = config.GetConnectionString(key.ToString());
        var agentSKProvider = sp.GetRequiredService<AIFoundryAgentProvider>();
        return agentSKProvider.CreateAzureAIAgent(agentId);
    });
    builder.Services.AddKeyedSingleton<AIAgent>(key, (sp, key) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var agentId = config.GetConnectionString(key.ToString());
        var agentFxProvider = sp.GetRequiredService<AgentFxAgentProvider>();
        return agentFxProvider.GetAIAgent(agentId);
    });
}
