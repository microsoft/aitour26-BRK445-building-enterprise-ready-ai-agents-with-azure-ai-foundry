#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0001

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using ZavaAgentFxAgentsProvider;
using ZavaAIFoundrySKAgentsProvider;
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

builder.Services.AddSingleton<IChatClient>(sp =>
{
    var skProvider = sp.GetService<SemanticKernelProvider>();
    var kernel = skProvider.GetKernel();
    return kernel.GetRequiredService<IChatCompletionService>().AsChatClient();
});

builder.Services.AddSingleton<AIFoundryAgentProvider>(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var aiFoundryProjectConnection = config.GetConnectionString("aifoundryproject");
    var agentId = config.GetConnectionString("photoanalyzeragentid");
    return new AIFoundryAgentProvider(aiFoundryProjectConnection, agentId);
});

builder.Services.AddSingleton<AgentFxAgentProvider>(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var aiFoundryProjectConnection = config.GetConnectionString("aifoundryproject");
    return new AgentFxAgentProvider(aiFoundryProjectConnection);
});


builder.Services.AddSingleton<AzureAIAgent>(sp =>
{
    // return the photo analyzer agent using Semantic Kernel
    var config = sp.GetService<IConfiguration>();
    var agentId = config.GetConnectionString("photoanalyzeragentid");
    var _aIFoundryAgentProvider = sp.GetService<AIFoundryAgentProvider>();
    return _aIFoundryAgentProvider.CreateAzureAIAgent(agentId);
});

builder.Services.AddSingleton<AIAgent>(sp =>
{
    // return the photo analyzer agent using Microsoft Agent Framework
    var config = sp.GetService<IConfiguration>();
    var agentId = config.GetConnectionString("photoanalyzeragentid");
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
