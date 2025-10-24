#pragma warning disable SKEXP0110
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
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
        var openAiConnection = config.GetConnectionString("aifoundry");
        var chatDeploymentName = config["AI_ChatDeploymentName"] ?? "gpt-5-mini";
        return new SemanticKernelProvider(openAiConnection, chatDeploymentName);
    });

builder.Services.AddSingleton<IChatClient>(sp =>
{
    var skProvider = sp.GetService<SemanticKernelProvider>();
    var kernel = skProvider.GetKernel();
    return kernel.GetRequiredService<IChatCompletionService>().AsChatClient();
});

// Register Semantic Kernel agent provider
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var aiFoundryProjectConnection = config.GetConnectionString("aifoundryproject");
    var agentId = config.GetConnectionString("toolreasoningagentid");
    return new AIFoundryAgentProvider(aiFoundryProjectConnection, agentId);
});

// Register AgentFx agent provider
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var aiFoundryProjectConnection = config.GetConnectionString("aifoundryproject");
    return new AgentFxAgentProvider(aiFoundryProjectConnection);
});

builder.Services.AddSingleton<AzureAIAgent>(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var agentId = config.GetConnectionString("toolreasoningagentid");
    var aiFoundryAgentProvider = sp.GetService<AIFoundryAgentProvider>();
    return aiFoundryAgentProvider.CreateAzureAIAgent(agentId);
});

builder.Services.AddSingleton<AIAgent>(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var agentId = config.GetConnectionString("toolreasoningagentid");
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
