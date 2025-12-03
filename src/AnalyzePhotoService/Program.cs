using Microsoft.Agents.AI;
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

// Register the PhotoAnalyzerAgent from Microsoft Foundry
builder.Services.AddSingleton<AIAgent>(sp =>
{
    var agentId = AgentNamesProvider.GetAgentName(AgentNamesProvider.AgentName.PhotoAnalyzerAgent);
    var provider = sp.GetRequiredService<MAFAgentProvider>();
    return provider.GetAIAgent(agentId);
});

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
