using SingleAgentDemo.Services;
using ZavaAgentFxAgentsProvider;
using ZavaSemanticKernelProvider;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Determine which agent framework to use (default: Semantic Kernel)
var agentFramework = builder.Configuration.GetValue<string>("AgentFramework:Type") ?? "SK";
var useSemanticKernel = agentFramework.Equals("SK", StringComparison.OrdinalIgnoreCase);
var useAgentFx = agentFramework.Equals("AgentFx", StringComparison.OrdinalIgnoreCase);

// Add services to the container.
builder.Services.AddControllers()
    .ConfigureApplicationPartManager(manager =>
    {
        // Only register the controller for the selected framework
        if (useSemanticKernel)
        {
            // SK controller is already included by default
        }
        else if (useAgentFx)
        {
            // AgentFx controller is already included by default
        }
    });

// Add Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register agent providers based on selected framework
if (useSemanticKernel)
{
    var openAiConnection = builder.Configuration.GetValue<string>("ConnectionStrings:aifoundry");
    var chatDeploymentName = builder.Configuration["AI_ChatDeploymentName"] ?? "gpt-5-mini";
    builder.Services.AddSingleton(sp =>
        new SemanticKernelProvider(openAiConnection, chatDeploymentName));
}

if (useAgentFx)
{
    builder.Services.AddSingleton(sp =>
    {
        var config = sp.GetService<IConfiguration>();
        var aiFoundryProjectConnection = config!.GetConnectionString("aifoundryproject");
        return new AgentFxAgentProvider(aiFoundryProjectConnection!);
    });
}

builder.Services.AddSingleton(sp => builder.Configuration);

// Register service layer implementations for external services
builder.Services.AddHttpClient<AnalyzePhotoService>(
    client => client.BaseAddress = new Uri("https+http://analyzephotoservice"));

builder.Services.AddHttpClient<CustomerInformationService>(
    client => client.BaseAddress = new Uri("https+http://customerinformationservice"));

builder.Services.AddHttpClient<ToolReasoningService>(
    client => client.BaseAddress = new Uri("https+http://toolreasoningservice"));

builder.Services.AddHttpClient<InventoryService>(
    client => client.BaseAddress = new Uri("https+http://inventoryservice"));

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
