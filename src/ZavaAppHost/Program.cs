#pragma warning disable CS8604

using Azure.Provisioning.CognitiveServices;

var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);

var productsDb = sql
    .WithDataVolume()
    .AddDatabase("productsDb");

// openai connection string will be used for both products and agent services
IResourceBuilder<IResourceWithConnectionString>? openai;
var chatDeploymentName = "gpt-5-mini";
var embeddingsDeploymentName = "text-embedding-ada-002";

// aifoundryproject is used for both products and agent services
IResourceBuilder<IResourceWithConnectionString>? aifoundryproject;
IResourceBuilder<IResourceWithConnectionString>? customerInformationAgentId;
IResourceBuilder<IResourceWithConnectionString>? inventoryAgentId;
IResourceBuilder<IResourceWithConnectionString>? locationServiceAgentId;
IResourceBuilder<IResourceWithConnectionString>? navigationAgentId;
IResourceBuilder<IResourceWithConnectionString>? photoAnalyzerAgentId;
IResourceBuilder<IResourceWithConnectionString>? productMatchMakingAgentId;
IResourceBuilder<IResourceWithConnectionString>? productSearchAgentId;
IResourceBuilder<IResourceWithConnectionString>? toolReasoningAgentId;

// application insights connection string
IResourceBuilder<IResourceWithConnectionString>? appInsights;


var products = builder.AddProject<Projects.Products>("products")
    .WithReference(productsDb)
    .WaitFor(productsDb);

// Add new microservices for agent functionality
var analyzePhotoService = builder.AddProject<Projects.AnalyzePhotoService>("analyzephotoservice")
    .WithExternalHttpEndpoints();

var customerInformationService = builder.AddProject<Projects.CustomerInformationService>("customerinformationservice")
    .WithExternalHttpEndpoints();

var toolReasoningService = builder.AddProject<Projects.ToolReasoningService>("toolreasoningservice")
    .WithExternalHttpEndpoints();

var inventoryService = builder.AddProject<Projects.InventoryService>("inventoryservice")
    .WithExternalHttpEndpoints();

// Add new multi-agent specific services
var matchmakingService = builder.AddProject<Projects.MatchmakingService>("matchmakingservice")
    .WithExternalHttpEndpoints();

var locationService = builder.AddProject<Projects.LocationService>("locationservice")
    .WithExternalHttpEndpoints();

var navigationService = builder.AddProject<Projects.NavigationService>("navigationservice")
    .WithExternalHttpEndpoints();

// Add new agent demo services
var singleAgentDemo = builder.AddProject<Projects.SingleAgentDemo>("singleagentdemo")
    .WithReference(analyzePhotoService)
    .WithReference(customerInformationService)
    .WithReference(toolReasoningService)
    .WithReference(inventoryService)
    .WithExternalHttpEndpoints();

var multiAgentDemo = builder.AddProject<Projects.MultiAgentDemo>("multiagentdemo")
    .WaitFor(analyzePhotoService)
    .WithReference(analyzePhotoService)
    .WaitFor(customerInformationService)
    .WithReference(customerInformationService)
    .WaitFor(toolReasoningService)
    .WithReference(toolReasoningService)
    .WaitFor(inventoryService)
    .WithReference(inventoryService)
    .WaitFor(matchmakingService)
    .WithReference(matchmakingService)
    .WaitFor(locationService)
    .WithReference(locationService)
    .WaitFor(navigationService)
    .WithReference(navigationService)
    .WithExternalHttpEndpoints();

var agentscatalogservice = builder.AddProject<Projects.AgentsCatalogService>("agentscatalogservice")
    .WaitFor(analyzePhotoService)
    .WithReference(analyzePhotoService)
    .WaitFor(customerInformationService)
    .WithReference(customerInformationService)
    .WaitFor(toolReasoningService)
    .WithReference(toolReasoningService)
    .WaitFor(inventoryService)
    .WithReference(inventoryService)
    .WaitFor(matchmakingService)
    .WithReference(matchmakingService)
    .WaitFor(locationService)
    .WithReference(locationService)
    .WaitFor(navigationService)
    .WithReference(navigationService)
    .WithExternalHttpEndpoints();

var store = builder.AddProject<Projects.Store>("store")
    .WaitFor(analyzePhotoService)
    .WithReference(analyzePhotoService)
    .WaitFor(customerInformationService)
    .WithReference(customerInformationService)
    .WaitFor(toolReasoningService)
    .WithReference(toolReasoningService)
    .WaitFor(inventoryService)
    .WithReference(inventoryService)
    .WaitFor(matchmakingService)
    .WithReference(matchmakingService)
    .WaitFor(locationService)
    .WithReference(locationService)
    .WaitFor(navigationService)
    .WithReference(navigationService)
    .WithReference(products)
    .WaitFor(products)
    .WithReference(singleAgentDemo)
    .WaitFor(singleAgentDemo)
    .WithReference(multiAgentDemo)
    .WaitFor(multiAgentDemo)
    .WithReference(agentscatalogservice)
    .WaitFor(agentscatalogservice)    
    .WithExternalHttpEndpoints();


if (builder.ExecutionContext.IsPublishMode)
{
    // production code uses Azure services, so we need to add them here
    appInsights = builder.AddAzureApplicationInsights("appInsights");
    var aoai = builder.AddAzureOpenAI("aifoundry");

    var gpt5mini = aoai.AddDeployment(name: chatDeploymentName,
            modelName: "gpt-5-mini",
            modelVersion: "2025-08-07");
    gpt5mini.Resource.SkuName = "GlobalStandard";

    var embeddingsDeployment = aoai.AddDeployment(name: embeddingsDeploymentName,
        modelName: "text-embedding-ada-002",
        modelVersion: "2");

    products.WithReference(appInsights);

    store.WithReference(appInsights)
        .WithExternalHttpEndpoints();

    // Add Application Insights to microservices
    analyzePhotoService
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();
    customerInformationService
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();
    toolReasoningService
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();
    inventoryService
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();
    matchmakingService
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();
    locationService
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();
    navigationService
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();

    singleAgentDemo.WithReference(appInsights)
        .WithExternalHttpEndpoints();

    multiAgentDemo.WithReference(appInsights)
        .WithExternalHttpEndpoints();

    agentscatalogservice
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();

    openai = aoai;
}
else
{
    openai = builder.AddConnectionString("aifoundry");

    appInsights = builder.AddConnectionString(
    "appinsights",
    "APPLICATIONINSIGHTS_CONNECTION_STRING");

    products.WithReference(appInsights);

    store.WithReference(appInsights)
        .WithExternalHttpEndpoints();

    // Add Application Insights to microservices
    analyzePhotoService
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();
    customerInformationService
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();
    toolReasoningService
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();
    inventoryService
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();
    matchmakingService
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();
    locationService
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();
    navigationService
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();

    singleAgentDemo.WithReference(appInsights)
        .WithExternalHttpEndpoints();

    multiAgentDemo.WithReference(appInsights)
        .WithExternalHttpEndpoints();

    agentscatalogservice
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();


}

// aifoundry settings here
aifoundryproject = builder.AddConnectionString("aifoundryproject");
customerInformationAgentId = builder.AddConnectionString("customerinformationagentid");
inventoryAgentId = builder.AddConnectionString("inventoryagentid");
locationServiceAgentId = builder.AddConnectionString("locationserviceagentid");
navigationAgentId = builder.AddConnectionString("navigationagentid");
photoAnalyzerAgentId = builder.AddConnectionString("photoanalyzeragentid");
productMatchMakingAgentId = builder.AddConnectionString("productmatchmakingagentid");
productSearchAgentId = builder.AddConnectionString("productsearchagentid");
toolReasoningAgentId = builder.AddConnectionString("toolreasoningagentid");

products.WithReference(openai)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName)
    .WithEnvironment("AI_embeddingsDeploymentName", embeddingsDeploymentName);

analyzePhotoService
    .WithReference(aifoundryproject)
    .WithReference(photoAnalyzerAgentId)
    .WithReference(openai)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);

customerInformationService
    .WithReference(aifoundryproject)
    .WithReference(customerInformationAgentId)
    .WithReference(openai)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);
toolReasoningService
    .WithReference(aifoundryproject)
    .WithReference(toolReasoningAgentId)
    .WithReference(openai)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);
inventoryService
    .WithReference(aifoundryproject)
    .WithReference(inventoryAgentId)
    .WithReference(openai)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);
matchmakingService
    .WithReference(aifoundryproject)
    .WithReference(openai)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);
locationService
    .WithReference(aifoundryproject)
    .WithReference(openai)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);
navigationService
    .WithReference(aifoundryproject)
    .WithReference(openai)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);
singleAgentDemo
    .WithReference(aifoundryproject)
    .WithReference(openai)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);
multiAgentDemo
    .WithReference(aifoundryproject)
    .WithReference(inventoryAgentId)
    .WithReference(toolReasoningAgentId)
    .WithReference(navigationAgentId)
    .WithReference(locationServiceAgentId)
    .WithReference(photoAnalyzerAgentId)
    .WithReference(productMatchMakingAgentId)
    .WithReference(productSearchAgentId)
    .WithReference(openai)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);

agentscatalogservice
    .WithReference(aifoundryproject)
    .WithReference(customerInformationAgentId)
    .WithReference(inventoryAgentId)
    .WithReference(locationServiceAgentId)
    .WithReference(navigationAgentId)
    .WithReference(photoAnalyzerAgentId)
    .WithReference(productMatchMakingAgentId)
    .WithReference(productSearchAgentId)
    .WithReference(toolReasoningAgentId)
    .WithReference(openai)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);

builder.Build().Run();
