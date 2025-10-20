# Quick Start Guide: Switching Agent Frameworks

This guide shows how to switch between Semantic Kernel and Microsoft Agent Framework.

## Prerequisites
- The solution is already built and configured
- Azure AI Foundry agents are deployed and configured

## Switching Steps

### Option 1: Use Semantic Kernel (Default)
No changes needed - this is the default configuration.

### Option 2: Use Microsoft Agent Framework

1. **Update MultiAgentDemo configuration**
   
   Edit `src/MultiAgentDemo/appsettings.json`:
   ```json
   {
     "AgentFramework": {
       "Type": "AgentFx"
     }
   }
   ```

2. **Update SingleAgentDemo configuration**
   
   Edit `src/SingleAgentDemo/appsettings.json`:
   ```json
   {
     "AgentFramework": {
       "Type": "AgentFx"
     }
   }
   ```

3. **Restart the application**
   ```bash
   # Stop the running application (Ctrl+C)
   
   # Rebuild and run
   cd src
   dotnet build Zava-Aspire.slnx
   dotnet run --project ./ZavaAppHost/ZavaAppHost.csproj
   ```

## Verification

Check the application logs to verify which framework is being used:

**Semantic Kernel:**
```
Starting sequential orchestration for query: {ProductQuery}
```

**Microsoft Agent Framework:**
```
Starting sequential orchestration for query: {ProductQuery} using Microsoft Agent Framework
```

## Switching Back to Semantic Kernel

Simply change `"Type": "AgentFx"` back to `"Type": "SK"` in both appsettings.json files and restart.

## Configuration Values

| Value | Framework | Description |
|-------|-----------|-------------|
| `"SK"` | Semantic Kernel | Default - uses Microsoft.SemanticKernel |
| `"AgentFx"` | Microsoft Agent Framework | Uses Microsoft.Agents.AI |

## Important Notes

1. **No Code Changes Required**: Switching is purely configuration-based
2. **Same Azure Resources**: Both frameworks connect to the same Azure AI Foundry agents
3. **API Compatibility**: Store UI works with both frameworks (same route paths)
4. **Environment-Specific**: You can use different frameworks in different environments

## Troubleshooting

**Issue**: Application doesn't start after changing configuration
- **Solution**: Check that the value is exactly `"SK"` or `"AgentFx"` (case-sensitive)

**Issue**: "Agent provider not registered" error
- **Solution**: Ensure both projects (MultiAgentDemo and SingleAgentDemo) are updated

**Issue**: Controllers not found
- **Solution**: Rebuild the solution after making changes

## Examples

### Development Environment (Semantic Kernel)
```json
{
  "AgentFramework": {
    "Type": "SK"
  }
}
```

### Testing Environment (Microsoft Agent Framework)
```json
{
  "AgentFramework": {
    "Type": "AgentFx"
  }
}
```

### Environment Variables Override
You can also set via environment variable:
```bash
export AgentFramework__Type=AgentFx
```

Or in Windows:
```powershell
$env:AgentFramework__Type = "AgentFx"
```

## Performance Comparison

You can use this switching mechanism to compare:
- Response times
- Agent behavior
- Resource usage
- Implementation differences

Simply run the same requests with both frameworks and compare results!
