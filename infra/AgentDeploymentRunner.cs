using Azure.AI.Agents.Persistent;
using Infra.AgentDeployment;

namespace Infra.AgentDeployment;

/// <summary>
/// Orchestrates deletion and creation of Persistent Agents from a JSON file.
/// High-level workflow only; detailed concerns are delegated to helper service classes.
/// </summary>
public class AgentDeploymentRunner
{
    private readonly PersistentAgentsClient _client;
    private readonly string _modelDeploymentName;
    private readonly string _configPath;
    private readonly IAgentDefinitionLoader _definitionLoader;
    private readonly IAgentDeletionService _deletionService;
    private readonly IAgentFileUploader _fileUploader;
    private readonly IAgentCreationService _creationService;
    private readonly IAgentPersistenceService _persistenceService;

    public AgentDeploymentRunner(
        PersistentAgentsClient client,
        string modelDeploymentName,
        string configPath)
        : this(
            client,
            modelDeploymentName,
            configPath,
            new JsonAgentDefinitionLoader(configPath),
            new AgentDeletionService(client),
            new AgentFileUploader(client),
            new AgentCreationService(client, modelDeploymentName),
            new AgentPersistenceService()) { }

    // Internal primary constructor for DI / testing; keeps helper abstractions internal.
    internal AgentDeploymentRunner(
        PersistentAgentsClient client,
        string modelDeploymentName,
        string configPath,
        IAgentDefinitionLoader definitionLoader,
        IAgentDeletionService deletionService,
        IAgentFileUploader fileUploader,
        IAgentCreationService creationService,
        IAgentPersistenceService persistenceService)
    {
        _client = client;
        _modelDeploymentName = modelDeploymentName;
        _configPath = configPath;
        _definitionLoader = definitionLoader;
        _deletionService = deletionService;
        _fileUploader = fileUploader;
        _creationService = creationService;
        _persistenceService = persistenceService;
    }

    public void Run(bool? deleteFlag)
    {
        var definitions = _definitionLoader.LoadDefinitions();
        if (definitions.Length == 0)
        {
            Console.WriteLine("No agent definitions to process.");
            return;
        }

        bool deleteRequested = ShouldDelete(deleteFlag);
        if (deleteRequested)
        {
            _deletionService.DeleteExisting(definitions);
        }
        else
        {
            Console.WriteLine("Skipping deletion of existing agents.\n");
        }

        if (!ConfirmCreation())
        {
            Console.WriteLine("Agent creation canceled by user.");
            return;
        }

        // Upload unique files once
        var uploadedFiles = _fileUploader.UploadAllFiles(definitions);

        var createdAgents = _creationService.CreateAgents(definitions, uploadedFiles);
        _persistenceService.PersistCreated(createdAgents);
    }

    private bool ShouldDelete(bool? flag)
    {
        if (flag.HasValue) return flag.Value; // command line override

        Console.Write("Delete existing agents matching definitions? (Y/n): ");
        var input = Console.ReadLine();
        // Default YES when empty
        return string.IsNullOrWhiteSpace(input) || input.Trim().Equals("y", StringComparison.OrdinalIgnoreCase);
    }

    private bool ConfirmCreation()
    {
        Console.Write("Proceed to create agents now? (Y/n): ");
        var input = Console.ReadLine();
        // Default YES when empty
        return string.IsNullOrWhiteSpace(input) || input.Trim().Equals("y", StringComparison.OrdinalIgnoreCase);
    }
}
