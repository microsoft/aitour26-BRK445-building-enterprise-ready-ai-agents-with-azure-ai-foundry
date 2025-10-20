# Agent Framework Implementation Summary

## Overview
This implementation adds support for Microsoft Agent Framework (AgentFx) alongside the existing Semantic Kernel implementation, allowing developers to choose between two agent frameworks via configuration.

## Changes Made

### 1. Controller Restructuring

#### MultiAgentDemo Project
- **Renamed**: `MultiAgentController.cs` → `MultiAgentControllerSK.cs`
- **Created**: `MultiAgentControllerAgentFx.cs`
- SK controller route: `/api/multiagent/sk/*`
- AgentFx controller route: `/api/multiagent/agentfx/*`
- Both controllers implement endpoints:
  - POST `/assist`
  - POST `/assist/sequential`
  - POST `/assist/concurrent`
  - POST `/assist/handoff`
  - POST `/assist/groupchat`
  - POST `/assist/magentic`

#### SingleAgentDemo Project
- **Renamed**: `SingleAgentController.cs` → `SingleAgentControllerSK.cs`
- **Created**: `SingleAgentControllerAgentFx.cs`
- SK controller route: `/api/singleagent/sk/*`
- AgentFx controller route: `/api/singleagent/agentfx/*`
- Both controllers implement endpoint:
  - POST `/analyze`

### 2. Frontend-Based Switching

**Store Frontend:**
- Created `Settings.razor` page with framework toggle switch
- Created `AgentFrameworkService` to manage framework preference
- Framework selection saved to browser localStorage
- No server restart required - changes take effect immediately

**Store Services:**
- Updated `MultiAgentService` to route based on selected framework
- Updated `SingleAgentService` to route based on selected framework
- Services inject `AgentFrameworkService` to read user preference
- Dynamic endpoint construction: `/api/{service}/{framework}/*`

**Configuration files:**
- Removed `AgentFramework:Type` from appsettings.json files
- All framework selection is now UI-based

### 3. Program.cs Updates

Both `MultiAgentDemo/Program.cs` and `SingleAgentDemo/Program.cs` were updated to:
- Register **both** agent providers simultaneously
- Remove conditional registration logic
- Support both Semantic Kernel and AgentFx at runtime

### 4. Project References

Updated `.csproj` files to include:
- `ZavaAgentFxAgentsProvider` project reference (in addition to existing `ZavaAIFoundrySKAgentsProvider`)

### 5. Documentation Updates

Updated the following files:
- `README.MD` - Added agent framework section
- `src/readme.md` - Detailed switching instructions
- `session-delivery-resources/docs/HowToRunDemoLocally.md` - Configuration guide

## How It Works

### Frontend-Based Switching
- User opens the **Settings page** in the Store frontend
- Toggle switch allows selection between SK and AgentFx
- Selection is saved to browser **localStorage**
- `AgentFrameworkService` reads the preference
- Services dynamically route to appropriate controller endpoints

### Route Architecture
- SK controllers use routes: `/api/multiagent/sk/*` and `/api/singleagent/sk/*`
- AgentFx controllers use routes: `/api/multiagent/agentfx/*` and `/api/singleagent/agentfx/*`
- Frontend services determine which route to call based on user preference
- Both controllers are always registered and available

### Default Behavior
- **Semantic Kernel (SK)** is the default if no preference is saved
- No server restart required - changes take effect immediately
- Preference persists across browser sessions via localStorage

### Store Service Integration
The Store services (`MultiAgentService` and `SingleAgentService`) now:
1. Inject `AgentFrameworkService`
2. Read framework preference asynchronously
3. Construct appropriate endpoint URL
4. Route request to correct controller

## Architecture Benefits

1. **Flexibility**: Switch between frameworks without code changes
2. **Compatibility**: Both frameworks connect to same Azure AI Foundry agents
3. **Comparison**: Easy to compare framework capabilities
4. **Future-proof**: Can add more frameworks using same pattern

## Testing

All projects build successfully:
- `MultiAgentDemo` - ✓ Builds with warnings (pre-existing)
- `SingleAgentDemo` - ✓ Builds with warnings (pre-existing)
- Both SK and AgentFx controllers compile successfully

## Notes

### AgentFx Implementation
The AgentFx controllers provide simplified implementations that demonstrate the integration pattern. They include:
- Proper logging with framework identification
- Simplified agent invocation patterns
- Same response structures as SK controllers

### Future Enhancements
To make the AgentFx implementation fully functional:
1. Implement actual AIAgent invocation using Microsoft.Agents.AI APIs
2. Add proper error handling for AgentFx-specific exceptions
3. Implement full orchestration patterns in AgentFx style

## Security Considerations
- No sensitive data exposed in configuration
- Both frameworks use same Azure credentials
- Configuration-based switching reduces attack surface (no runtime compilation)

## Deployment Notes
- Default configuration (`"SK"`) maintains backward compatibility
- Existing deployments continue to work without changes
- AgentFx can be enabled per environment via configuration
