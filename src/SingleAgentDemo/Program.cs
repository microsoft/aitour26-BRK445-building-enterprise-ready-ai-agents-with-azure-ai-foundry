using SingleAgentDemo.Services;
using ZavaAIFoundrySKAgentsProvider;
using ZavaMAFAgentsProvider;
using ZavaSemanticKernelProvider;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers();

// Add Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register both agent providers - they will be available for their respective controllers
var microsoftFoundryCnnString = builder.Configuration.GetValue<string>("ConnectionStrings:microsoftfoundrycnnstring");
var chatDeploymentName = builder.Configuration["AI_ChatDeploymentName"] ?? "gpt-5-mini";
builder.Services.AddSingleton(sp =>
    new SemanticKernelProvider(microsoftFoundryCnnString, chatDeploymentName));

var microsoftFoundryProjectConnection = builder.Configuration.GetConnectionString("microsoftfoundryproject");
builder.Services.AddSingleton(sp =>
{
    return new AIFoundryAgentProvider(microsoftFoundryProjectConnection, "");
});

builder.Services.AddSingleton(sp =>
{
    return new MAFAgentProvider(microsoftFoundryProjectConnection!);
});

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

builder.Services.AddHttpClient<ProductSearchService>(
    client => client.BaseAddress = new Uri("https+http://productsearchservice"));

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
