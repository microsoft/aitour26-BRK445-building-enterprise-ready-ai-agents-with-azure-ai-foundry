using Microsoft.Agents.AI;
using ZavaMAFAgentsProvider;
using ZavaFoundryAgentsProvider;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers();
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

/********************************************************/
// get the agentId and register the AIAgent services for the ToolReasoningAgent
builder.Services.AddSingleton<AIAgent>(sp =>
{
    var agentId = AgentNamesProvider.GetAgentName(AgentNamesProvider.AgentName.ToolReasoningAgent);
    var agentFxProvider = sp.GetService<MAFAgentProvider>();
    return agentFxProvider.GetAIAgent(agentId);
});
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
