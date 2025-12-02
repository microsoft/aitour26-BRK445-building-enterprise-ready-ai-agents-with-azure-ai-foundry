#pragma warning disable SKEXP0110
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using ZavaAIFoundrySKAgentsProvider;
using ZavaMAFAgentsProvider;
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

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var aiFoundryProjectConnection = config.GetConnectionString("foundryproject");
    var agentId = config.GetConnectionString("locationserviceagentid");
    return new AIFoundryAgentProvider(aiFoundryProjectConnection, agentId);
});

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var aiFoundryProjectConnection = config.GetConnectionString("foundryproject");
    return new MAFAgentProvider(aiFoundryProjectConnection);
});

builder.Services.AddSingleton<AzureAIAgent>(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var agentId = config.GetConnectionString("locationserviceagentid");
    var aiFoundryAgentProvider = sp.GetService<AIFoundryAgentProvider>();
    return aiFoundryAgentProvider.CreateAzureAIAgent(agentId);
});

builder.Services.AddSingleton<AIAgent>(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var agentId = config.GetConnectionString("locationserviceagentid");
    var agentFxProvider = sp.GetService<MAFAgentProvider>();
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
