## Source code

Place the source code you're sharing for the session in this folder.

## Agent Framework Selection

This solution supports two agent frameworks:

1. **Semantic Kernel (SK)** - Default framework using Microsoft.SemanticKernel
2. **Microsoft Agent Framework (AgentFx)** - New framework using Microsoft.Agents.AI

### Switching Between Frameworks

The framework selection is now managed through the **Settings page** in the Store frontend application. 

**To switch frameworks:**
1. Navigate to the **Settings** page in the Store app (accessible from the navigation menu)
2. Use the toggle switch to select your preferred framework:
   - **OFF** = Semantic Kernel (SK) - Default
   - **ON** = Microsoft Agent Framework (AgentFx)
3. Your preference is automatically saved in browser localStorage
4. All agent demos will immediately use the selected framework

**Note:** The framework preference is stored in your browser and persists across sessions. No server restart is required.

### Controllers

Each demo project now has two controller implementations with different routes:

**MultiAgentDemo:**
- `MultiAgentControllerSK.cs` - Route: `/api/multiagent/sk/*`
- `MultiAgentControllerAgentFx.cs` - Route: `/api/multiagent/agentfx/*`

**SingleAgentDemo:**
- `SingleAgentControllerSK.cs` - Route: `/api/singleagent/sk/*`
- `SingleAgentControllerAgentFx.cs` - Route: `/api/singleagent/agentfx/*`

The Store frontend automatically routes requests to the appropriate controller based on your selection in the Settings page.

## How to use this code for the session

For step-by-step instructions on how to start the services, run demos, and deliver the session content, see the session delivery guide:

- `session-delivery-resources\readme.md` — contains run instructions, presenter notes, demo scripts, and required configuration.
