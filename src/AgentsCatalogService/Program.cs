using AgentsCatalogService;
using ZavaAIFoundrySKAgentsProvider;
using ZavaMAFAgentsProvider;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Add AI Foundry Agent Provider for agent testing
builder.Services.AddSingleton<AIFoundryAgentProvider>(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var aiFoundryProjectConnection = config?.GetConnectionString("foundryproject") ?? "";
    return new AIFoundryAgentProvider(aiFoundryProjectConnection, "");
});

builder.Services.AddSingleton<MAFAgentProvider>(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var aiFoundryProjectConnection = config?.GetConnectionString("foundryproject") ?? "";
    return new MAFAgentProvider(aiFoundryProjectConnection);
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

// init the agent list
AgentCatalog.InitAgentList(app.Services.GetService<IConfiguration>());

app.Run();