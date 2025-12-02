#pragma warning disable IDE0017, OPENAI001

using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Files;
using OpenAI.VectorStores;
using System.ClientModel;

var azureFoundryProjectEndpoint = "https://bruno-brk445-02-resource.services.ai.azure.com/api/projects/bruno-brk445-02";

    //"https://bruno-realtime-resource.services.ai.azure.com/api/projects/bruno-realtime";

var chatModel = "gpt-4.1-mini";
var embeddingModel = "text-embedding-3-small";
var agentName = "delete01";

AIProjectClient projectClient = new(
    endpoint: new Uri(azureFoundryProjectEndpoint),
    tokenProvider: new DefaultAzureCredential());
//tokenProvider: new AzureCliCredential());

// Create a dataset with the docs in the docs folder
var datasetName = "dataset01";
var datasetVersion = "1.0";


// set a string with the value for the folder docs under the current directory
var docsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "docs");
Console.WriteLine($"Doc Folder Path: {docsFolderPath}");

List<string> fileIds = [];
OpenAIClient openAIClient = projectClient.GetProjectOpenAIClient();

// iterate the files in the docs folder and print their names
foreach (string filePath in Directory.GetFiles(docsFolderPath))
{
    Console.WriteLine($"  >> Uploading file: {Path.GetFileName(filePath)}");
    OpenAIFileClient fileClient = openAIClient.GetOpenAIFileClient();
    ClientResult<OpenAIFile> uploadResult = fileClient.UploadFile(
        filePath: filePath,
        purpose: FileUploadPurpose.Assistants);

    fileIds.Add(uploadResult.Value.Id);
    Console.WriteLine($"    >> file Uploaded: {Path.GetFileName(filePath)} - FileId : {uploadResult.Value.Id}");
}

Console.WriteLine();
var vectorStoreName = "vectorstore01";
var vectorStoreVersion = "1";
var vectorStoreId = "";
var vectorStoreOptions = new VectorStoreCreationOptions()
{
    Name = vectorStoreName    
};

foreach (var fileId in fileIds)
{
    vectorStoreOptions.FileIds.Add(fileId);
}

// Create a vector store in the Foundry service using the uploaded file.
VectorStoreClient vectorStoreClient = openAIClient.GetVectorStoreClient();
ClientResult<VectorStore> vectorStoreCreate = await vectorStoreClient.CreateVectorStoreAsync(options: vectorStoreOptions);

// display information about the created vector store
Console.WriteLine($"Vector Store Name: {vectorStoreCreate.Value.Name}");
Console.WriteLine($"Vector Store Id: {vectorStoreCreate.Value.Id}");
vectorStoreId = vectorStoreCreate.Value.Id;
Console.WriteLine();


/******************************************************/
// agent tools
var fileSearchTool = new HostedFileSearchTool() { Inputs = [new HostedVectorStoreContent(vectorStoreId)] };

// create agent
AIAgent aiAgent = projectClient.CreateAIAgent(
    model: chatModel,
    name: agentName,
    instructions: "You are a useful agent that replies in short and direct sentences.",
    tools: [
        new HostedCodeInterpreterTool() { Inputs = [] },
        fileSearchTool]);
/******************************************************/

/******************************************************/
// questions
var question = "what can you tell me about customer 1?";
var response = await aiAgent.RunAsync(question);
Console.WriteLine($"Agent [{agentName}]: {response.Text}");
Console.WriteLine();

question = "what can you tell me about customer 4?";
response = await aiAgent.RunAsync(question);
Console.WriteLine($"Agent [{agentName}]: {response.Text}");
/******************************************************/

Console.WriteLine("\r\nPress any key to delete the agent, dataset, index and exit...");
Console.ReadKey();

projectClient.Agents.DeleteAgent(agentName);
await projectClient.Datasets.DeleteAsync(name: datasetName, version: datasetVersion);

Console.WriteLine("Delete the Index version created above:");
projectClient.Indexes.Delete(name: vectorStoreName, version: vectorStoreVersion);

// delete the files uploaded to the project
foreach (var fileId in fileIds)
{
    projectClient.Datasets.Delete(name: fileId, version: "1");
}