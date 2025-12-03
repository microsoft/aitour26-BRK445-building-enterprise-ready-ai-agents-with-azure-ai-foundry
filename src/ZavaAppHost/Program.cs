#pragma warning disable CS8604

using Azure.Provisioning.CognitiveServices;

var builder = DistributedApplication.CreateBuilder(args);

// ============================================================================
// SECTION 1: INFRASTRUCTURE RESOURCES
// ============================================================================

// SQL Server and database configuration
var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);

var productsDb = sql
    .WithDataVolume()
    .AddDatabase("productsDb");

// Microsoft Foundry connection string (OpenAI) - used for chat and embeddings
IResourceBuilder<IResourceWithConnectionString>? microsoftfoundrycnnstring;
var chatDeploymentName = "gpt-5-mini";
var embeddingsDeploymentName = "text-embedding-3-small";

// Microsoft Foundry project connection - used for agent services
IResourceBuilder<IResourceWithConnectionString>? microsoftfoundryproject;

// Application Insights for telemetry
IResourceBuilder<IResourceWithConnectionString>? appInsights;

// ============================================================================
// SECTION 2: CORE SERVICES
// ============================================================================

// Products service with database dependency
var products = builder.AddProject<Projects.Products>("products")
    .WithReference(productsDb)
    .WaitFor(productsDb);

// ============================================================================
// SECTION 3: AGENT MICROSERVICES
// ============================================================================

// Individual agent services - each handles a specific agent functionality
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

// ============================================================================
// SECTION 4: DEMO SERVICES
// ============================================================================

// Single Agent Demo - demonstrates single agent scenarios
var singleAgentDemo = builder.AddProject<Projects.SingleAgentDemo>("singleagentdemo")
    .WithReference(analyzePhotoService)
    .WithReference(customerInformationService)
    .WithReference(toolReasoningService)
    .WithReference(inventoryService)
    .WithReference(productSearchService)
    .WithExternalHttpEndpoints();

// Multi Agent Demo - demonstrates multi-agent orchestration
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

// ============================================================================
// SECTION 5: CATALOG AND STORE SERVICES
// ============================================================================

// Agents Catalog Service - provides agent listing and management
var agentscatalogservice = builder.AddProject<Projects.AgentsCatalogService>("agentscatalogservice")
    .WaitFor(analyzePhotoService).WithReference(analyzePhotoService)
    .WaitFor(customerInformationService).WithReference(customerInformationService)
    .WaitFor(toolReasoningService).WithReference(toolReasoningService)
    .WaitFor(inventoryService).WithReference(inventoryService)
    .WaitFor(matchmakingService).WithReference(matchmakingService)
    .WaitFor(locationService).WithReference(locationService)
    .WaitFor(navigationService).WithReference(navigationService)
    .WaitFor(productSearchService).WithReference(productSearchService)
    .WithExternalHttpEndpoints();

// Store - main frontend application
var store = builder.AddProject<Projects.Store>("store")
    .WaitFor(analyzePhotoService).WithReference(analyzePhotoService)
    .WaitFor(customerInformationService).WithReference(customerInformationService)
    .WaitFor(toolReasoningService).WithReference(toolReasoningService)
    .WaitFor(inventoryService).WithReference(inventoryService)
    .WaitFor(matchmakingService).WithReference(matchmakingService)
    .WaitFor(locationService).WithReference(locationService)
    .WaitFor(navigationService).WithReference(navigationService)
    .WaitFor(productSearchService).WithReference(productSearchService)
    .WaitFor(products).WithReference(products)
    .WaitFor(singleAgentDemo).WithReference(singleAgentDemo)
    .WaitFor(multiAgentDemo).WithReference(multiAgentDemo)
    .WaitFor(agentscatalogservice).WithReference(agentscatalogservice)
    .WithExternalHttpEndpoints();

// ============================================================================
// SECTION 6: ENVIRONMENT-SPECIFIC CONFIGURATION
// ============================================================================

if (builder.ExecutionContext.IsPublishMode)
{
    // PRODUCTION: Use Azure-provisioned services
    appInsights = builder.AddAzureApplicationInsights("appInsights");
    var aoai = builder.AddAzureOpenAI("microsoftfoundry");

    // Configure chat model deployment
    var gpt5mini = aoai.AddDeployment(name: chatDeploymentName,
            modelName: "gpt-5-mini",
            modelVersion: "2025-08-07");
    gpt5mini.Resource.SkuName = "GlobalStandard";

    // Configure embeddings model deployment
    var embeddingsDeployment = aoai.AddDeployment(name: embeddingsDeploymentName,
        modelName: "text-embedding-3-small",
        modelVersion: "1");
    embeddingsDeployment.Resource.SkuName = "GlobalStandard";


    microsoftfoundrycnnstring = aoai;
}
else
{
    // DEVELOPMENT: Use connection strings from configuration
    microsoftfoundrycnnstring = builder.AddConnectionString("microsoftfoundrycnnstring");
    appInsights = builder.AddConnectionString("appinsights", "APPLICATIONINSIGHTS_CONNECTION_STRING");
}

// ============================================================================
// SECTION 7: APPLICATION INSIGHTS CONFIGURATION
// ============================================================================

// Add Application Insights to all services
products.WithReference(appInsights).WithExternalHttpEndpoints();
store.WithReference(appInsights).WithExternalHttpEndpoints();
analyzePhotoService.WithReference(appInsights).WithExternalHttpEndpoints();
customerInformationService.WithReference(appInsights).WithExternalHttpEndpoints();
toolReasoningService.WithReference(appInsights).WithExternalHttpEndpoints();
inventoryService.WithReference(appInsights).WithExternalHttpEndpoints();
matchmakingService.WithReference(appInsights).WithExternalHttpEndpoints();
locationService.WithReference(appInsights).WithExternalHttpEndpoints();
navigationService.WithReference(appInsights).WithExternalHttpEndpoints();
productSearchService.WithReference(appInsights).WithExternalHttpEndpoints();
singleAgentDemo.WithReference(appInsights).WithExternalHttpEndpoints();
multiAgentDemo.WithReference(appInsights).WithExternalHttpEndpoints();
agentscatalogservice.WithReference(appInsights).WithExternalHttpEndpoints();


// ============================================================================
// SECTION 8: MICROSOFT FOUNDRY CONFIGURATION
// ============================================================================

// Configure Microsoft Foundry project connection for all agent services
microsoftfoundryproject = builder.AddConnectionString("microsoftfoundryproject");

// Add AI configuration to Products service
products
    .WithReference(microsoftfoundrycnnstring)
    .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName)
    .WithEnvironment("AI_embeddingsDeploymentName", embeddingsDeploymentName);

// Add Microsoft Foundry configuration to all agent services
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

// ============================================================================
// RUN THE APPLICATION
// ============================================================================

builder.Build().Run();