using Microsoft.Agents.AI;
using SingleAgentDemo.Services;
using ZavaFoundryAgentsProvider;
using ZavaMAFAgentsProvider;

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

// Register service layer implementations for external services
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

/********************************************************/
// Register agents in the DI using the Microsoft Agent Framework
// This allows the SingleAgentControllerMAF to use agents directly from Microsoft Foundry
AddAgentsInAgentFx(builder);
/********************************************************/

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
