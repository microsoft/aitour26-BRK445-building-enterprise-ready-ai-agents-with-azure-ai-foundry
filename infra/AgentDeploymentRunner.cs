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
    private readonly TaskTracker? _taskTracker;
    private readonly IAgentDefinitionLoader _definitionLoader;
    private readonly IAgentDeletionService _deletionService;
    private readonly IAgentFileUploader _fileUploader;
    private readonly IAgentCreationService _creationService;
    private readonly IAgentPersistenceService _persistenceService;

    public AgentDeploymentRunner(
        AIProjectClient client,
        string modelDeploymentName,
        string configPath,
        TaskTracker? taskTracker = null)
        : this(
            client,
            modelDeploymentName,
            configPath,
            taskTracker,
            new JsonAgentDefinitionLoader(configPath),
            new AgentDeletionService(client, taskTracker),
            new AgentFileUploader(client, taskTracker),
            new AgentCreationService(client, modelDeploymentName, taskTracker),
            new AgentPersistenceService(taskTracker))
    { }

    // Internal primary constructor for DI / testing; keeps helper abstractions internal.
    internal AgentDeploymentRunner(
        AIProjectClient client,
        string modelDeploymentName,
        string configPath,
        TaskTracker? taskTracker,
        IAgentDefinitionLoader definitionLoader,
        IAgentDeletionService deletionService,
        IAgentFileUploader fileUploader,
        IAgentCreationService creationService,
        IAgentPersistenceService persistenceService)
    {
        _client = client;
        _modelDeploymentName = modelDeploymentName;
        _configPath = configPath;
        _taskTracker = taskTracker;
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
            if (_taskTracker != null)
                _taskTracker.AddLog("[yellow]No agent definitions to process.[/]");
            else
                AnsiConsole.MarkupLine("[yellow]No agent definitions to process.[/]");
            return;
        }

        if (_taskTracker != null)
            _taskTracker.AddLog($"[cyan]Found {definitions.Length} agent definition(s) to process.[/]");
        else
            AnsiConsole.MarkupLine($"[cyan]Found {definitions.Length} agent definition(s) to process.[/]");

        // Calculate operation counts for progress tracking
        int agentsCount = definitions.Length;
        int indexesCount = definitions.Count(d => d.Files?.Any() == true);
        int datasetsCount = definitions.SelectMany(d => d.Files ?? Enumerable.Empty<string>()).Distinct().Count();

        // Ask for each deletion type separately
        bool deleteAgents = ShouldDeleteAgents(deleteFlag);
        bool deleteIndexes = ShouldDeleteIndexes(deleteFlag);
        bool deleteDatasets = ShouldDeleteDatasets(deleteFlag);

        // Set operation counts for accurate progress tracking
        if (_taskTracker != null)
        {
            _taskTracker.SetOperationCounts(
                deleteAgents ? agentsCount : 0,
                deleteIndexes ? indexesCount : 0,
                deleteDatasets ? datasetsCount : 0,
                datasetsCount,
                indexesCount,
                agentsCount);
        }

        if (deleteAgents || deleteIndexes || deleteDatasets)
        {
            _deletionService.DeleteExisting(definitions, deleteAgents, deleteIndexes, deleteDatasets);
        }
        else
        {
            if (_taskTracker != null)
                _taskTracker.AddLog("[yellow]Skipping all deletion operations.[/]");
            else
                AnsiConsole.MarkupLine("[yellow]Skipping all deletion operations.[/]\n");
        }

        if (!ConfirmCreation())
        {
            if (_taskTracker != null)
                _taskTracker.AddLog("[red]Agent creation canceled by user.[/]");
            else
                AnsiConsole.MarkupLine("[red]Agent creation canceled by user.[/]");
            return;
        }

        // Upload unique files once
        var uploadedFiles = _fileUploader.UploadAllFiles(definitions);

        var createdAgents = _creationService.CreateAgents(definitions, uploadedFiles);
        _persistenceService.PersistCreated(createdAgents);
    }

    private bool ShouldDeleteAgents(bool? flag)
    {
        if (flag.HasValue) return flag.Value; // command line override
        if (_taskTracker != null)
        {
            return _taskTracker.PromptYesNo("Delete existing agents?", true);
        }
        return AnsiConsole.Confirm("[yellow]Delete existing agents?[/]", true);
    }

    private bool ShouldDeleteIndexes(bool? flag)
    {
        if (flag.HasValue) return flag.Value; // command line override
        if (_taskTracker != null)
        {
            return _taskTracker.PromptYesNo("Delete existing indexes (vector stores)?", true);
        }
        return AnsiConsole.Confirm("[yellow]Delete existing indexes (vector stores)?[/]", true);
    }

    private bool ShouldDeleteDatasets(bool? flag)
    {
        if (flag.HasValue) return flag.Value; // command line override
        if (_taskTracker != null)
        {
            return _taskTracker.PromptYesNo("Delete existing datasets (files)?", true);
        }
        return AnsiConsole.Confirm("[yellow]Delete existing datasets (files)?[/]", true);
    }

    private bool ConfirmCreation()
    {
        if (_taskTracker != null)
        {
            return _taskTracker.PromptYesNo("Proceed to create agents now?", true);
        }
        return AnsiConsole.Confirm("[cyan]Proceed to create agents now?[/]", true);
    }
}
