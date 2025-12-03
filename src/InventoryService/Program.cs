#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0001

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using ZavaAIFoundrySKAgentsProvider;
using ZavaSemanticKernelProvider;
using ZavaMAFAgentsProvider;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

/********************************************************/
// The following code registers the agent providers for Semantic Kernel
var microsoftFoundryCnnString = builder.Configuration.GetValue<string>("ConnectionStrings:microsoftfoundrycnnstring");
var chatDeploymentName = builder.Configuration["AI_ChatDeploymentName"] ?? "gpt-5-mini";
builder.Services.AddSingleton(sp =>
    new SemanticKernelProvider(microsoftFoundryCnnString, chatDeploymentName));
/********************************************************/

builder.Services.AddSingleton<IChatClient>(sp =>
{
    var skProvider = sp.GetService<SemanticKernelProvider>();
    var kernel = skProvider.GetKernel();
    return kernel.GetRequiredService<IChatCompletionService>().AsChatClient();
});

/********************************************************/
// The following code registers the agent providers for the Microsoft Foundry project.  
var microsoftFoundryProjectConnection = builder.Configuration.GetConnectionString("microsoftfoundryproject");
builder.Services.AddSingleton(sp =>
{
    return new AIFoundryAgentProvider(microsoftFoundryProjectConnection, "");
});

builder.Services.AddSingleton(sp =>
{
    return new MAFAgentProvider(microsoftFoundryProjectConnection!);
});
/********************************************************/

/********************************************************/
// get the agentId and register the AzureAIAgent and AIAgent services for the InventoryAgent
var agentId = builder.Configuration.GetConnectionString("inventoryagentid");
builder.Services.AddSingleton<AzureAIAgent>(sp =>
{
    var _aIFoundryAgentProvider = sp.GetService<AIFoundryAgentProvider>();
    return _aIFoundryAgentProvider.CreateAzureAIAgent(agentId);
});

builder.Services.AddSingleton<AIAgent>(sp =>
{
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
