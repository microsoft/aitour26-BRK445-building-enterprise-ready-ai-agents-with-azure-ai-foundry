using Spectre.Console;
using Spectre.Console.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace Infra.AgentDeployment;

public class TaskTracker
{
    private string _projectEndpoint;
    private string _modelName;
    private readonly Dictionary<string, bool> _tasks = new();
    private readonly Dictionary<string, Dictionary<string, bool>> _subTasks = new();
    private readonly List<string> _logs = new();
    private readonly object _lock = new();
    private int _totalSteps;
    private int _completedSteps;
    private string _currentInteraction = "";
    private LiveDisplayContext? _liveContext;
    private Table? _mainTable;
    private bool _liveActive = false;

    // Dynamic operation counters
    private int _agentsToDelete = 0;
    private int _indexesToDelete = 0;
    private int _datasetsToDelete = 0;
    private int _datasetsToCreate = 0;
    private int _indexesToCreate = 0;
    private int _agentsToCreate = 0;

    public TaskTracker(string projectEndpoint, string modelName)
    {
        _projectEndpoint = projectEndpoint;
        _modelName = modelName;

        // Initialize tasks
        _tasks["Set Environment Values"] = false;

        _subTasks["Deleting"] = new Dictionary<string, bool>
        {
            ["Agents"] = false,
            ["Indexes"] = false,
            ["DataSets"] = false
        };

        _subTasks["Creating"] = new Dictionary<string, bool>
        {
            ["DataSets"] = false,
            ["Indexes"] = false,
            ["Agents"] = false
        };

        // Calculate total steps (will be updated with actual counts)
        _totalSteps = 1; // Start with environment setup
    }

    public void UpdateConfiguration(string projectEndpoint, string modelName)
    {
        lock (_lock)
        {
            _projectEndpoint = projectEndpoint;
            _modelName = modelName;
            UpdateDisplay();
        }
    }

    public void StartTask(string taskName)
    {
        lock (_lock)
        {
            if (_tasks.ContainsKey(taskName))
            {
                _tasks[taskName] = false;
            }
            UpdateDisplay();
        }
    }

    public void CompleteTask(string taskName)
    {
        lock (_lock)
        {
            if (_tasks.ContainsKey(taskName) && !_tasks[taskName])
            {
                _tasks[taskName] = true;
                _completedSteps++;
            }
            UpdateDisplay();
        }
    }

    public void CompleteSubTask(string parentTask, string subTask)
    {
        lock (_lock)
        {
            if (_subTasks.ContainsKey(parentTask) &&
                _subTasks[parentTask].ContainsKey(subTask) &&
                !_subTasks[parentTask][subTask])
            {
                _subTasks[parentTask][subTask] = true;
            }
            UpdateDisplay();
        }
    }

    /// <summary>
    /// Set the expected operation counts to calculate accurate progress.
    /// </summary>
    public void SetOperationCounts(int agentsToDelete, int indexesToDelete, int datasetsToDelete,
                                   int datasetsToCreate, int indexesToCreate, int agentsToCreate)
    {
        lock (_lock)
        {
            _agentsToDelete = agentsToDelete;
            _indexesToDelete = indexesToDelete;
            _datasetsToDelete = datasetsToDelete;
            _datasetsToCreate = datasetsToCreate;
            _indexesToCreate = indexesToCreate;
            _agentsToCreate = agentsToCreate;

            _totalSteps = 1 + // Environment setup
                         agentsToDelete + indexesToDelete + datasetsToDelete +
                         datasetsToCreate + indexesToCreate + agentsToCreate;

            UpdateDisplay();
        }
    }

    /// <summary>
    /// Increment progress by one step (for individual file/index/agent operations).
    /// </summary>
    public void IncrementProgress()
    {
        lock (_lock)
        {
            _completedSteps++;
            UpdateDisplay();
        }
    }

    public void AddLog(string message)
    {
        lock (_lock)
        {
            _logs.Add(message);
            UpdateDisplay();
        }
    }

    public void SetInteraction(string message)
    {
        lock (_lock)
        {
            _currentInteraction = message;
            UpdateDisplay();
        }
    }

    public void ClearInteraction()
    {
        lock (_lock)
        {
            _currentInteraction = "";
            UpdateDisplay();
        }
    }

    public void StartLiveDisplay()
    {
        _mainTable = BuildMainTable();
        _liveActive = true;

        AnsiConsole.Live(_mainTable)
            .AutoClear(false)
            .Overflow(VerticalOverflow.Ellipsis)
            .Cropping(VerticalOverflowCropping.Top)
            .Start(ctx =>
            {
                _liveContext = ctx;
                while (_liveActive)
                {
                    System.Threading.Thread.Sleep(50);
                }
            });
    }

    private void UpdateDisplay()
    {
        if (_liveContext != null)
        {
            _mainTable = BuildMainTable();
            _liveContext.UpdateTarget(_mainTable);
            _liveContext.Refresh();
        }
    }

    public void StopLiveDisplay()
    {
        _liveActive = false;
        _liveContext = null;
    }

    public void Render()
    {
        // Fallback for non-live mode
        lock (_lock)
        {
            AnsiConsole.Clear();

            var panel = new Panel(BuildMainTable())
            {
                Header = new PanelHeader("[bold blue]BRK445 - INFRA[/]", Justify.Center),
                Border = BoxBorder.Double,
                BorderStyle = new Style(Color.Blue),
                Padding = new Padding(2, 1)
            };

            AnsiConsole.Write(panel);
        }
    }

    private Table BuildMainTable()
    {
        var table = new Table()
            .Border(TableBorder.Double)
            .BorderColor(Color.Blue)
            .Title("[bold blue]BRK445 - INFRA[/]")
            .Expand()
            .HideHeaders();

        table.AddColumn("");

        // Progress Bar
        var progressBar = BuildProgressBar();
        table.AddRow($"[bold yellow]Progress[/]\n{progressBar}");

        // Configuration
        table.AddRow(
            $"[bold yellow]Configuration[/]\n" +
            $"[grey]Project:[/] {TruncateUrl(_projectEndpoint)}\n" +
            $"[grey]Model:[/] {_modelName}");

        // Tasks
        var tasksText = BuildTasksText();
        table.AddRow($"[bold yellow]Tasks[/]\n{tasksText}");

        // Activity Log (last 5 lines only)
        var logText = BuildLogText();
        table.AddRow($"[bold yellow]Activity Log[/]\n{logText}");

        // Always show input cell at bottom separated by a line
        var inputContent = "[grey]" + new string('─', 60) + "[/]\n[bold yellow]> Input[/]\n[yellow on black]" + (_currentInteraction ?? "") + "[/]";
        table.AddRow(inputContent);

        return table;
    }

    private string BuildProgressBar()
    {
        var percentage = _totalSteps > 0 ? (double)_completedSteps / _totalSteps * 100 : 0;
        var barWidth = 40;
        var filledWidth = (int)(barWidth * percentage / 100);
        var emptyWidth = barWidth - filledWidth;

        var bar = "[green]" + new string('█', filledWidth) + "[/]" +
                  "[grey]" + new string('░', emptyWidth) + "[/]";

        return $"{bar} [cyan]{_completedSteps}/{_totalSteps}[/] ([yellow]{percentage:F0}%[/])";
    }

    private string BuildTasksText()
    {
        var lines = new List<string>();

        foreach (var task in _tasks)
        {
            var checkMark = task.Value ? "[green]✅[/]" : "[grey]⬜[/]";
            lines.Add($"{checkMark} [cyan]{task.Key}[/]");
        }

        foreach (var parentTask in _subTasks)
        {
            lines.Add($"[cyan]{parentTask.Key}[/]");

            foreach (var subTask in parentTask.Value)
            {
                var checkMark = subTask.Value ? "[green]✅[/]" : "[grey]⬜[/]";
                lines.Add($"  {checkMark} {subTask.Key}");
            }
        }

        return string.Join("\n", lines);
    }

    private string BuildLogText()
    {
        if (_logs.Count == 0)
        {
            return "[grey]Waiting for activity...[/]";
        }

        var recentLogs = _logs.TakeLast(5).ToList();
        return string.Join("\n", recentLogs);
    }

    private string TruncateUrl(string url)
    {
        if (url.Length <= 60)
            return url;

        var uri = new Uri(url);
        return $"{uri.Host}/.../{uri.Segments.Last()}";
    }

    /// <summary>
    /// Collect initial user inputs (Project Endpoint, Model Deployment Name, Tenant Id optional)
    /// directly inside the live display bottom input cell.
    /// </summary>
    public (string projectEndpoint, string modelDeploymentName, string tenantId) CollectInitialInputs(string defaultProjectEndpoint, string defaultModelDeploymentName, string defaultTenantId)
    {
        // Ensure live display started
        if (_liveContext == null)
        {
            StartLiveDisplay();
        }

        string project = defaultProjectEndpoint;
        string model = defaultModelDeploymentName;
        string tenant = defaultTenantId;

        // Sequential prompt definitions
        var prompts = new List<(string key, string label, bool required, string current)>
        {
            ("project", "Enter Project Endpoint", true, project),
            ("model", "Enter Model Deployment Name", true, model),
            ("tenant", "Enter Tenant ID (optional, press Enter to skip)", false, tenant)
        };

        foreach (var p in prompts.ToList())
        {
            string buffer = p.current ?? string.Empty;
            bool done = false;
            bool showError = false;
            SetInteraction($"{p.label}: {buffer}");
            UpdateDisplay();

            while (!done)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter)
                    {
                        if (p.required && string.IsNullOrWhiteSpace(buffer))
                        {
                            showError = true;
                            AddLog($"[red]{p.label} cannot be empty[/]");
                            SetInteraction($"{p.label}: {buffer}");
                        }
                        else
                        {
                            done = true;
                        }
                    }
                    else if (key.Key == ConsoleKey.Backspace)
                    {
                        if (buffer.Length > 0)
                        {
                            buffer = buffer.Substring(0, buffer.Length - 1);
                        }
                    }
                    else if (key.Key == ConsoleKey.Escape)
                    {
                        buffer = string.Empty;
                    }
                    else if (!char.IsControl(key.KeyChar))
                    {
                        buffer += key.KeyChar;
                    }

                    // Update interaction cell
                    if (showError && !string.IsNullOrWhiteSpace(buffer))
                    {
                        showError = false; // clear error state when user starts typing again
                    }
                    SetInteraction($"{p.label}: {buffer}");
                }

                System.Threading.Thread.Sleep(40); // small idle delay
            }

            // Persist value
            switch (p.key)
            {
                case "project": project = buffer; break;
                case "model": model = buffer; break;
                case "tenant": tenant = buffer; break;
            }

            AddLog($"[green]✓[/] {p.label} set");
        }

        ClearInteraction();
        UpdateConfiguration(project, model);
        return (project, model, tenant);
    }

    /// <summary>
    /// Simple yes/no prompt rendered in bottom input cell. Enter accepts default.
    /// </summary>
    public bool PromptYesNo(string question, bool defaultValue = true)
    {
        if (_liveContext == null)
        {
            StartLiveDisplay();
        }

        string hint = defaultValue ? "(Y/n)" : "(y/N)";
        string buffer = string.Empty;
        SetInteraction($"{question} {hint}");
        bool decided = false;
        bool result = defaultValue;

        while (!decided)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    // Accept default
                    decided = true;
                    result = defaultValue;
                }
                else if (key.Key == ConsoleKey.Y)
                {
                    decided = true;
                    result = true;
                }
                else if (key.Key == ConsoleKey.N)
                {
                    decided = true;
                    result = false;
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    decided = true;
                    result = defaultValue;
                }

                SetInteraction($"{question} {hint} {buffer}");
            }
            System.Threading.Thread.Sleep(40);
        }

        ClearInteraction();
        AddLog($"[green]✓[/] {question} => {(result ? "Yes" : "No")}");
        return result;
    }
}
