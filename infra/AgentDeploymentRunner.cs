using Azure.AI.Projects;
using Spectre.Console;

namespace Infra.AgentDeployment;

/// <summary>
/// Orchestrates deletion and creation of Persistent Agents from a JSON file.
/// High-level workflow only; detailed concerns are delegated to helper service classes.
/// </summary>
public class AgentDeploymentRunner
{
    private readonly AIProjectClient _client;
    private readonly string _modelDeploymentName;
    private readonly string _configPath;
    private readonly IAgentDefinitionLoader _definitionLoader;
    private readonly IAgentDeletionService _deletionService;
    private readonly IAgentFileUploader _fileUploader;
    private readonly IAgentCreationService _creationService;
    private readonly IAgentPersistenceService _persistenceService;

    public AgentDeploymentRunner(
        AIProjectClient client,
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
            new AgentPersistenceService())
    { }

    // Internal primary constructor for DI / testing; keeps helper abstractions internal.
    internal AgentDeploymentRunner(
        AIProjectClient client,
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
            AnsiConsole.MarkupLine("[yellow]No agent definitions to process.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[cyan]Found {definitions.Length} agent definition(s) to process.[/]");
        AnsiConsole.WriteLine();

        // Ask for each deletion type separately
        bool deleteAgents = ShouldDeleteAgents(deleteFlag);
        bool deleteIndexes = ShouldDeleteIndexes(deleteFlag);
        bool deleteDatasets = ShouldDeleteDatasets(deleteFlag);

        if (deleteAgents || deleteIndexes || deleteDatasets)
        {
            _deletionService.DeleteExisting(definitions, deleteAgents, deleteIndexes, deleteDatasets);
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]Skipping all deletion operations.[/]\n");
        }

        if (!ConfirmCreation())
        {
            AnsiConsole.MarkupLine("[red]Agent creation canceled by user.[/]");
            return;
        }

        // Upload unique files once
        var uploadedFiles = _fileUploader.UploadAllFiles(definitions);

        var createdAgents = _creationService.CreateAgents(definitions, uploadedFiles);
        _persistenceService.PersistCreated(createdAgents);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]✓ Agent deployment completed successfully![/]");
    }

    private bool ShouldDeleteAgents(bool? flag)
    {
        if (flag.HasValue) return flag.Value; // command line override

        return AnsiConsole.Confirm("[yellow]Delete existing agents?[/]", true);
    }

    private bool ShouldDeleteIndexes(bool? flag)
    {
        if (flag.HasValue) return flag.Value; // command line override

        return AnsiConsole.Confirm("[yellow]Delete existing indexes (vector stores)?[/]", true);
    }

    private bool ShouldDeleteDatasets(bool? flag)
    {
        if (flag.HasValue) return flag.Value; // command line override

        return AnsiConsole.Confirm("[yellow]Delete existing datasets (files)?[/]", true);
    }

    private bool ConfirmCreation()
    {
        return AnsiConsole.Confirm("[cyan]Proceed to create agents now?[/]", true);
    }
}
