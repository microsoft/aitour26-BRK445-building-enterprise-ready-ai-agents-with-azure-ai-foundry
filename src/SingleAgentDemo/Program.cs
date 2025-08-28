using SingleAgentDemo.Services;
using ZavaSemanticKernelProvider;

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
