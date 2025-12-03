#pragma warning disable CS8604

using Azure.Provisioning.CognitiveServices;

var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);

var productsDb = sql
    .WithDataVolume()
    .AddDatabase("productsDb");

// openai connection string will be used for both products and agent services
IResourceBuilder<IResourceWithConnectionString>? microsoftfoundrycnnstring;
var chatDeploymentName = "gpt-5-mini";
var embeddingsDeploymentName = "text-embedding-3-small";

// microsoftfoundryproject is used for both products and agent services
IResourceBuilder<IResourceWithConnectionString>? microsoftfoundryproject;

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

var matchmakingService = builder.AddProject<Projects.MatchmakingService>("matchmakingservice")
    .WithExternalHttpEndpoints();

var locationService = builder.AddProject<Projects.LocationService>("locationservice")
    .WithExternalHttpEndpoints();

var navigationService = builder.AddProject<Projects.NavigationService>("navigationservice")
    .WithExternalHttpEndpoints();

var productSearchService = builder.AddProject<Projects.ProductSearchService>("productsearchservice")
    .WithExternalHttpEndpoints();

// Add new agent demo services
var singleAgentDemo = builder.AddProject<Projects.SingleAgentDemo>("singleagentdemo")
    .WithReference(analyzePhotoService)
    .WithReference(customerInformationService)
    .WithReference(toolReasoningService)
    .WithReference(inventoryService)
    .WithReference(productSearchService)
    .WithExternalHttpEndpoints();

var multiAgentDemo = builder.AddProject<Projects.MultiAgentDemo>("multiagentdemo")
    .WithReference(analyzePhotoService)
    .WithReference(customerInformationService)
    .WithReference(toolReasoningService)
    .WithReference(inventoryService)
    .WithReference(productSearchService)
    .WithReference(matchmakingService)
    .WithReference(locationService)
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
    .WaitFor(productSearchService)
    .WithReference(productSearchService)
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
    .WaitFor(productSearchService)
    .WithReference(productSearchService)
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
    var aoai = builder.AddAzureOpenAI("microsoftfoundry");

    var gpt5mini = aoai.AddDeployment(name: chatDeploymentName,
            modelName: "gpt-5-mini",
            modelVersion: "2025-08-07");
    gpt5mini.Resource.SkuName = "GlobalStandard";

    var embeddingsDeployment = aoai.AddDeployment(name: embeddingsDeploymentName,
        modelName: "text-embedding-3-small",
        modelVersion: "1");
    embeddingsDeployment.Resource.SkuName = "GlobalStandard";

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
    productSearchService
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();

    singleAgentDemo
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();

    multiAgentDemo
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();

    agentscatalogservice
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();

    microsoftfoundrycnnstring = aoai;
}
else
{
    microsoftfoundrycnnstring = builder.AddConnectionString("microsoftfoundrycnnstring");

    appInsights = builder.AddConnectionString("appinsights", "APPLICATIONINSIGHTS_CONNECTION_STRING");

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
    productSearchService
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();

    singleAgentDemo.WithReference(appInsights)
        .WithExternalHttpEndpoints();

    multiAgentDemo
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();

    agentscatalogservice
        .WithReference(appInsights)
        .WithExternalHttpEndpoints();
}

// aifoundry settings here
microsoftfoundryproject = builder.AddConnectionString("foundryproject");

products
    .WithReference(microsoftfoundrycnnstring)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName)
    .WithEnvironment("AI_embeddingsDeploymentName", embeddingsDeploymentName);
analyzePhotoService
    .WithReference(microsoftfoundryproject)
    .WithReference(microsoftfoundrycnnstring)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);
customerInformationService
    .WithReference(microsoftfoundryproject)
    .WithReference(microsoftfoundrycnnstring)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);
toolReasoningService
    .WithReference(microsoftfoundryproject)
    .WithReference(microsoftfoundrycnnstring)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);
inventoryService
    .WithReference(microsoftfoundryproject)
    .WithReference(microsoftfoundrycnnstring)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);
matchmakingService
    .WithReference(microsoftfoundryproject)
    .WithReference(microsoftfoundrycnnstring)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);
locationService
    .WithReference(microsoftfoundryproject)
    .WithReference(microsoftfoundrycnnstring)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);
navigationService
    .WithReference(microsoftfoundryproject)
    .WithReference(microsoftfoundrycnnstring)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);
productSearchService
    .WithReference(microsoftfoundryproject)
    .WithReference(microsoftfoundrycnnstring)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);
singleAgentDemo
    .WithReference(microsoftfoundryproject)
    .WithReference(microsoftfoundrycnnstring)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);
multiAgentDemo
    .WithReference(microsoftfoundryproject)
    .WithReference(microsoftfoundrycnnstring)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);

agentscatalogservice
    .WithReference(microsoftfoundryproject)
    .WithReference(microsoftfoundrycnnstring)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName);

builder.Build().Run();