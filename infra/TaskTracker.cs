using Spectre.Console;
using Spectre.Console.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace Infra.AgentDeployment;

public class TaskTracker
{
    private string _projectEndpoint;
    private string _modelName;
    private string _tenantId = string.Empty;
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
    private string? _lastPlainTextLogPath;
    private string? _lastJsonLogPath;

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

        // Initialize activity log with 8 empty rows
        for (int i = 0; i < 8; i++)
        {
            _logs.Add("[grey]...[/]");
        }
    }

    public void UpdateConfiguration(string projectEndpoint, string modelName, string tenantId)
    {
        lock (_lock)
        {
            _projectEndpoint = projectEndpoint;
            _modelName = modelName;
            _tenantId = tenantId;
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
                // Don't increment here, we use IncrementProgress() for actual operations
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

    public void SetOutputPaths(string? plainTextPath, string? jsonPath)
    {
        lock (_lock)
        {
            _lastPlainTextLogPath = plainTextPath;
            _lastJsonLogPath = jsonPath;
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
            .Title("[bold blue]BRK445 :rocket: - INFRA[/]")
            .Expand()
            .HideHeaders();

        table.AddColumn("");

        // Progress Bar
        var progressBar = BuildProgressBar();
        table.AddRow(new Rows(new Markup("[bold yellow]Progress :bar_chart:[/]"), new Markup(progressBar)));

        // Configuration
        var tenantDisplay = string.IsNullOrWhiteSpace(_tenantId) ? "[italic grey]none[/]" : _tenantId;
        var configGrid = new Grid().AddColumn().AddColumn();
        // Use TextPath only for local file/system paths; URLs should be rendered as links
        IRenderable projectRenderable;
        if (Uri.IsWellFormedUriString(_projectEndpoint, UriKind.Absolute))
        {
            var safeText = Markup.Escape(_projectEndpoint);
            projectRenderable = new Markup($"[link={safeText}]{safeText}[/]");
        }
        else
        {
            var projectPath = new TextPath(_projectEndpoint)
                .RootColor(Color.CadetBlue)
                .SeparatorColor(Color.Grey)
                .StemColor(Color.White)
                .LeafColor(Color.Yellow);
            projectRenderable = projectPath;
        }
        configGrid.AddRow(new Markup("[bold yellow]Configuration :gear:[/]"));
        configGrid.AddRow(new Markup("[grey]Project:[/]"), projectRenderable);
        configGrid.AddRow(new Markup("[grey]Model:[/]"), new Markup($"{_modelName}"));
        configGrid.AddRow(new Markup("[grey]Tenant:[/]"), new Markup($"{tenantDisplay}"));
        table.AddRow(configGrid);

        // Tasks
        var tasksText = BuildTasksText();
        table.AddRow(new Rows(new Markup("[bold yellow]Tasks :check_mark_button:[/]"), new Markup(tasksText)));

        // Activity Log (last 8 lines)
        var logText = BuildLogText();
        table.AddRow(new Rows(new Markup("[bold yellow]Activity Log :memo:[/]"), new Markup(logText)));

        // Outputs section with TextPath for generated files on new lines
        if (!string.IsNullOrWhiteSpace(_lastPlainTextLogPath) || !string.IsNullOrWhiteSpace(_lastJsonLogPath))
        {
            var renderables = new List<IRenderable>();
            renderables.Add(new Markup("[bold yellow]Outputs :floppy_disk:[/]"));
            if (!string.IsNullOrWhiteSpace(_lastPlainTextLogPath))
            {
                var tp = new TextPath(_lastPlainTextLogPath!)
                    .RootColor(Color.Green)
                    .SeparatorColor(Color.Grey)
                    .StemColor(Color.White)
                    .LeafColor(Color.Yellow);
                renderables.Add(new Rows(new Markup("[grey]Plain text log:[/]"), tp));
            }
            if (!string.IsNullOrWhiteSpace(_lastJsonLogPath))
            {
                var tp = new TextPath(_lastJsonLogPath!)
                    .RootColor(Color.Green)
                    .SeparatorColor(Color.Grey)
                    .StemColor(Color.White)
                    .LeafColor(Color.Yellow);
                renderables.Add(new Rows(new Markup("[grey]JSON map:[/]"), tp));
            }
            table.AddRow(new Rows(renderables.ToArray()));
        }

        // Always show input cell at bottom separated by a line
        var inputContent = "[grey]" + new string('─', 60) + "[/]\n[bold yellow]> Input :keyboard:[/]\n[yellow on blue]" + (_currentInteraction ?? "") + "[/]";
        table.AddRow(new Markup(inputContent));

        return table;
    }

    private string BuildProgressBar()
    {
        // Clamp completed steps to total to ensure we reach 100%
        var effectiveCompleted = Math.Min(_completedSteps, _totalSteps);
        var percentage = _totalSteps > 0 ? (double)effectiveCompleted / _totalSteps * 100 : 0;
        var barWidth = 40;
        var filledWidth = (int)(barWidth * percentage / 100);
        var emptyWidth = barWidth - filledWidth;

        var bar = "[green]" + new string('█', filledWidth) + "[/]" +
                  "[grey]" + new string('░', emptyWidth) + "[/]";

        return $"{bar} [cyan]{effectiveCompleted}/{_totalSteps}[/] ([yellow]{percentage:F0}%[/])";
    }

    private string BuildTasksText()
    {
        var lines = new List<string>();

        foreach (var task in _tasks)
        {
            var checkMark = task.Value ? ":check_mark_button:" : ":white_large_square:";
            lines.Add($"{checkMark} [cyan]{task.Key}[/]");
        }

        foreach (var parentTask in _subTasks)
        {
            lines.Add($"[cyan]{parentTask.Key}[/]");

            foreach (var subTask in parentTask.Value)
            {
                var checkMark = subTask.Value ? ":check_mark_button:" : ":white_large_square:";
                lines.Add($"  {checkMark} {subTask.Key}");
            }
        }

        return string.Join("\n", lines);
    }

    private string BuildLogText()
    {
        // Show last 8 rows, truncate long messages to prevent console breaking
        var recentLogs = _logs.TakeLast(8).Select(log => TruncateLogMessage(log)).ToList();
        return string.Join("\n", recentLogs);
    }

    private string TruncateLogMessage(string message)
    {
        // Strip markup to measure real length
        var plainText = System.Text.RegularExpressions.Regex.Replace(message, @"\[.*?\]", "");
        if (plainText.Length <= 100)
            return message;

        // Find first markup tag to preserve formatting
        var firstTagMatch = System.Text.RegularExpressions.Regex.Match(message, @"^(\[.*?\])");
        var prefix = firstTagMatch.Success ? firstTagMatch.Groups[1].Value : "";

        // Truncate and add ellipsis
        var truncated = message.Substring(0, Math.Min(message.Length, 100));
        return truncated + "...[/]";
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
            ("tenant", "Enter Tenant ID (optional, press ESC to skip)", false, tenant)
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
                            // For tenant (optional), Enter should confirm only if a value was provided.
                            if (p.key == "tenant" && string.IsNullOrWhiteSpace(buffer))
                            {
                                // Do not skip on Enter; require ESC to skip.
                                showError = false; // no error, just continue waiting for input or ESC
                            }
                            else
                            {
                                done = true;
                            }
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
                        // ESC will skip the Tenant ID by clearing and completing.
                        if (p.key == "tenant")
                        {
                            buffer = string.Empty;
                            done = true;
                        }
                        else
                        {
                            buffer = string.Empty;
                        }
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
        UpdateConfiguration(project, model, tenant);
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
