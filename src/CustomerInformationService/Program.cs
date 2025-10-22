#pragma warning disable SKEXP0110
using Microsoft.Agents.AI;
using Microsoft.SemanticKernel.Agents.AzureAI;
using ZavaAIFoundrySKAgentsProvider;
using ZavaAgentFxAgentsProvider;
using ZavaSemanticKernelProvider;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var aiFoundryConnection = config.GetConnectionString("aifoundry");
    var chatDeploymentName = config["AI_ChatDeploymentName"] ?? "gpt-5-mini";
    return new SemanticKernelProvider(aiFoundryConnection, chatDeploymentName);
});

// Register both agent providers for dual framework support
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var aiFoundryProjectConnection = config.GetConnectionString("aifoundryproject");
    var agentId = config.GetConnectionString("customerinformationagentid");
    return new AIFoundryAgentProvider(aiFoundryProjectConnection, agentId);
});

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var aiFoundryProjectConnection = config.GetConnectionString("aifoundryproject");
    return new AgentFxAgentProvider(aiFoundryProjectConnection!);
});

builder.Services.AddSingleton<AzureAIAgent>(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var agentId = config.GetConnectionString("customerinformationagentid");
    var aiFoundryAgentProvider = sp.GetService<AIFoundryAgentProvider>();
    return aiFoundryAgentProvider.CreateAzureAIAgent(agentId);
});

builder.Services.AddSingleton<AIAgent>(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var agentId = config.GetConnectionString("customerinformationagentid");
    var agentFxProvider = sp.GetService<AgentFxAgentProvider>();
    return agentFxProvider.GetAIAgent(agentId);
});

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
