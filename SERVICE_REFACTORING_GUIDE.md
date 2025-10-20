# Service Controller Refactoring Guide

## Overview

This guide documents the pattern for adding dual endpoints (Semantic Kernel and Microsoft Agent Framework) to all service controllers.

## Refactoring Pattern

### 1. Controller Updates

#### Update Usings
```csharp
// Add these imports
using Microsoft.Agents.AI;
using ZavaAgentFxAgentsProvider;
```

#### Update Constructor
```csharp
public class MyController : ControllerBase
{
    private readonly ILogger<MyController> _logger;
    private readonly AIFoundryAgentProvider _aIFoundryAgentProvider;
    private readonly AgentFxAgentProvider _agentFxAgentProvider; // ADD THIS
    
    public MyController(
        ILogger<MyController> logger,
        AIFoundryAgentProvider aIFoundryAgentProvider,
        AgentFxAgentProvider agentFxAgentProvider) // ADD THIS
    {
        _logger = logger;
        _aIFoundryAgentProvider = aIFoundryAgentProvider;
        _agentFxAgentProvider = agentFxAgentProvider; // ADD THIS
    }
}
```

#### Endpoint Pattern
```csharp
// RENAME: Original endpoint becomes "sk" version
[HttpPost("searchsk")] // Was: [HttpPost("search")]
public async Task<ActionResult<Response>> SearchSKAsync([FromBody] Request request)
{
    _logger.LogInformation("Using Semantic Kernel...");
    // Existing SK implementation
}

// ADD: New AgentFx endpoint
[HttpPost("searchagentfx")]
public async Task<ActionResult<Response>> SearchAgentFxAsync([FromBody] Request request)
{
    _logger.LogInformation("Using Microsoft Agent Framework...");
    try
    {
        var agent = await _agentFxAgentProvider.GetAzureAIAgent();
        // TODO: Implement Agent Framework logic
        // For now, use fallback or simplified implementation
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Agent Framework invocation failed");
        // Return fallback response
    }
}
```

#### Extract Shared Logic
```csharp
// Extract common prompt building, parsing, or business logic
private string BuildPrompt(string query)
{
    return $"Common prompt logic for: {query}";
}

private Response ParseResponse(string agentResponse)
{
    // Common parsing logic
}
```

### 2. Project File Updates

```xml
<ItemGroup>
  <ProjectReference Include="..\ZavaAIFoundrySKAgentsProvider\ZavaAIFoundrySKAgentsProvider.csproj" />
  <ProjectReference Include="..\ZavaAgentFxAgentsProvider\ZavaAgentFxAgentsProvider.csproj" /> <!-- ADD THIS -->
  <ProjectReference Include="..\ZavaServiceDefaults\ZavaServiceDefaults.csproj" />
</ItemGroup>
```

### 3. Program.cs Updates

```csharp
using ZavaAIFoundrySKAgentsProvider;
using ZavaAgentFxAgentsProvider; // ADD THIS

// Register SK provider
builder.Services.AddSingleton<AIFoundryAgentProvider>(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var connection = config?.GetConnectionString("aifoundryproject");
    var agentId = config?.GetConnectionString("agentid");
    return new AIFoundryAgentProvider(connection, agentId);
});

// ADD: Register AgentFx provider
builder.Services.AddSingleton<AgentFxAgentProvider>(sp =>
{
    var config = sp.GetService<IConfiguration>();
    var connection = config?.GetConnectionString("aifoundryproject");
    return new AgentFxAgentProvider(connection);
});
```

## Services Refactoring Status

### ✅ Completed
- [x] AnalyzePhotoService
  - Endpoints: `/analyzesk`, `/analyzeagentfx`
  - Helper: `BuildAnalysisPrompt()`
  
- [x] AgentsCatalogService
  - Endpoints: `/testsk`, `/testagentfx`
  - Helper: `BuildTestPrompt()`

### 🔄 In Progress
- [ ] InventoryService
  - Endpoints: `/searchsk`, `/searchagentfx`
  - Helpers: Extract SKU parsing logic

### 📋 Remaining
- [ ] CustomerInformationService
- [ ] LocationService
- [ ] MatchmakingService
- [ ] NavigationService
- [ ] ToolReasoningService

## Next Steps: Consumer Updates (Phase 2)

After all services are updated, consumers need to route to correct endpoints:

### MultiAgentDemo Services
- InventoryAgentService → route to `/searchsk` or `/searchagentfx`
- MatchmakingAgentService → route appropriately
- LocationAgentService → route appropriately
- NavigationAgentService → route appropriately

### SingleAgentDemo Services
- AnalyzePhotoService → route to `/analyzesk` or `/analyzeagentfx`
- CustomerInformationService → route appropriately
- ToolReasoningService → route appropriately

## Testing Strategy

1. Build each service after update
2. Verify both endpoints compile
3. Test SK endpoint maintains existing behavior
4. Test AgentFx endpoint (may use fallback initially)
5. Integration testing with consumers in Phase 2

## Notes

- AgentFx implementations may initially use fallbacks until full Agent Framework integration is completed
- Maintain backward compatibility - SK is the default/primary implementation
- Both endpoints should return identical response formats
- Logging should clearly distinguish which framework is being used
