using Microsoft.SemanticKernel;
using MultiAgentDemo.Services;
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

var openAiConnection = builder.Configuration.GetValue<string>("ConnectionStrings:aifoundry");
var chatDeploymentName = builder.Configuration["AI_ChatDeploymentName"] ?? "gpt-5-mini";
builder.Services.AddSingleton(sp =>
    new SemanticKernelProvider(openAiConnection, chatDeploymentName));

builder.Services.AddSingleton(sp => builder.Configuration);

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var aiFoundryProjectConnection = config.GetConnectionString("aifoundryproject");
    return new AIFoundryAgentProvider(aiFoundryProjectConnection, "");
});

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
