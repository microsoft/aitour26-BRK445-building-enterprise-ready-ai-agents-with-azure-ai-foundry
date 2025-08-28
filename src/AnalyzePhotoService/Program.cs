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
    var openAiConnection = config.GetConnectionString("aifoundry");
    var chatDeploymentName = config["AI_ChatDeploymentName"] ?? "gpt-5-mini";
    return new SemanticKernelProvider(openAiConnection, chatDeploymentName);
});

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var aiFoundryProjectConnection = config.GetConnectionString("aifoundryproject");
    var agentId = config.GetConnectionString("photoanalyzeragentid");
    return new AIFoundryAgentProvider(aiFoundryProjectConnection, agentId);
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
