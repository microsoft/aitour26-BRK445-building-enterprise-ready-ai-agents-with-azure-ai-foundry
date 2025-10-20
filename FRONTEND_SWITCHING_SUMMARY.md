# Frontend-Based Agent Framework Switching - Implementation Summary

## Overview
This implementation replaces the appsettings.json-based framework switching with a user-friendly **Settings page** in the Store frontend application.

## User Experience

### How to Switch Frameworks:
1. Navigate to the **Settings** page in the Store app (accessible from left navigation menu)
2. Toggle the switch to select your preferred framework:
   - **OFF** = Semantic Kernel (SK) - Default
   - **ON** = Microsoft Agent Framework (AgentFx)
3. Selection is automatically saved to browser localStorage
4. Changes take effect immediately (no restart required)

### UI Screenshots:

**Settings Page - Semantic Kernel:**
![SK Settings](https://github.com/user-attachments/assets/689eb2ff-bb15-478b-97e8-8de6eb9423c4)

**Settings Page - Microsoft Agent Framework:**
![AgentFx Settings](https://github.com/user-attachments/assets/4516bf2f-e01d-4cda-8f6b-7c7411d75d6b)

## Technical Implementation

### Frontend Changes:

1. **Settings.razor** - New settings page with:
   - Toggle switch for framework selection
   - Visual feedback showing active framework
   - Framework comparison table
   - Success confirmation message

2. **AgentFrameworkService.cs** - Service to manage framework preference:
   - Reads/writes to browser localStorage
   - Caches preference for performance
   - Provides async API for framework selection

3. **MultiAgentService.cs & SingleAgentService.cs** - Updated to:
   - Inject `AgentFrameworkService`
   - Read user's framework preference
   - Dynamically construct endpoint URLs based on selection

4. **NavMenu.razor** - Added Settings link to navigation

5. **Store/Program.cs** - Registered `AgentFrameworkService`

### Backend Changes:

1. **Controller Routes Updated**:
   - `MultiAgentControllerSK`: `/api/multiagent/sk/*`
   - `MultiAgentControllerAgentFx`: `/api/multiagent/agentfx/*`
   - `SingleAgentControllerSK`: `/api/singleagent/sk/*`
   - `SingleAgentControllerAgentFx`: `/api/singleagent/agentfx/*`

2. **Program.cs Files Simplified**:
   - Removed conditional framework registration
   - Both SK and AgentFx providers always registered
   - Removed appsettings-based configuration logic

3. **appsettings.json Cleaned**:
   - Removed `AgentFramework:Type` configuration
   - All framework selection now UI-based

### Documentation Updates:

All documentation updated to reflect frontend-based switching:
- README.MD
- src/readme.md
- SWITCHING_GUIDE.md
- AGENT_FRAMEWORK_IMPLEMENTATION.md
- session-delivery-resources/docs/HowToRunDemoLocally.md

## Benefits

✅ **User-Friendly**: Simple toggle switch instead of editing config files
✅ **No Restart**: Changes take effect immediately
✅ **Persistent**: Setting survives browser restarts via localStorage
✅ **Per-User**: Different users can use different frameworks simultaneously
✅ **Visual Feedback**: Clear indication of which framework is active
✅ **No Server Config**: Eliminates need to edit appsettings.json files

## Compatibility

- ✅ All existing functionality preserved
- ✅ Both frameworks connect to same Azure AI Foundry agents
- ✅ Default behavior: Semantic Kernel (SK) if no preference saved
- ✅ Works across all agent demos (Single Agent and Multi-Agent)

## Testing

All projects build successfully:
- ✅ Store project (frontend)
- ✅ MultiAgentDemo project (backend)
- ✅ SingleAgentDemo project (backend)

Framework switching tested and verified through Settings page UI.
