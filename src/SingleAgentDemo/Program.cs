using Microsoft.Agents.AI;
using SingleAgentDemo.Services;
using ZavaFoundryAgentsProvider;
using ZavaMAFAgentsProvider;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register MAFAgentProvider for Microsoft Foundry integration
var microsoftFoundryProjectConnection = builder.Configuration.GetConnectionString("microsoftfoundryproject");
builder.Services.AddSingleton(_ => new MAFAgentProvider(microsoftFoundryProjectConnection!));

// Register HTTP clients for external services (used by LLM direct call mode)
RegisterHttpClients(builder);

// Register AI agents from Microsoft Foundry (used by MAF mode)
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
/// Registers HTTP clients for external service communication (LLM direct call mode).
/// </summary>
static void RegisterHttpClients(WebApplicationBuilder builder)
{
    builder.Services.AddHttpClient<AnalyzePhotoService>(
        client => client.BaseAddress = new Uri("https+http://analyzephotoservice"));

    builder.Services.AddHttpClient<CustomerInformationService>(
        client => client.BaseAddress = new Uri("https+http://customerinformationservice"));

    builder.Services.AddHttpClient<ToolReasoningService>(
        client => client.BaseAddress = new Uri("https+http://toolreasoningservice"));

    builder.Services.AddHttpClient<InventoryService>(
        client => client.BaseAddress = new Uri("https+http://inventoryservice"));

    builder.Services.AddHttpClient<ProductSearchService>(
        client => client.BaseAddress = new Uri("https+http://productsearchservice"));
}

/// <summary>
/// Registers AI agents from Microsoft Foundry for MAF mode.
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
