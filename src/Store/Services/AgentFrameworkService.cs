using Microsoft.JSInterop;
using ZavaWorkingModes;

namespace Store.Services;

public class AgentFrameworkService
{
    private readonly IJSRuntime _jsRuntime;
    private WorkingMode? _cachedMode;

    public AgentFrameworkService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string> GetSelectedFrameworkAsync()
    {
        var mode = await GetSelectedModeAsync();
        return WorkingModeProvider.GetShortName(mode);
    }

    public async Task<WorkingMode> GetSelectedModeAsync()
    {
        if (_cachedMode.HasValue)
        {
            return _cachedMode.Value;
        }

        try
        {
            var framework = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "agentFramework");
            _cachedMode = WorkingModeProvider.Parse(framework);
            return _cachedMode.Value;
        }
        catch
        {
            // Default to MafFoundry if localStorage is not available
            _cachedMode = WorkingModeProvider.DefaultMode;
            return _cachedMode.Value;
        }
    }

    public async Task SetSelectedFrameworkAsync(string framework)
    {
        _cachedMode = WorkingModeProvider.Parse(framework);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "agentFramework", framework);
    }

    public async Task SetSelectedModeAsync(WorkingMode mode)
    {
        _cachedMode = mode;
        var shortName = WorkingModeProvider.GetShortName(mode);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "agentFramework", shortName);
    }

    public void ClearCache()
    {
        _cachedMode = null;
    }
}
