## Source code

Place the source code you're sharing for the session in this folder.

## Agent Framework Selection

This solution supports two agent frameworks:

1. **Semantic Kernel (SK)** - Default framework using Microsoft.SemanticKernel
2. **Microsoft Agent Framework (AgentFx)** - New framework using Microsoft.Agents.AI

### Switching Between Frameworks

To switch between frameworks, update the `appsettings.json` file in both `MultiAgentDemo` and `SingleAgentDemo` projects:

```json
{
  "AgentFramework": {
    "Type": "SK"  // Use "SK" for Semantic Kernel or "AgentFx" for Microsoft Agent Framework
  }
}
```

### Controllers

Each demo project now has two controller implementations:

**MultiAgentDemo:**
- `MultiAgentControllerSK.cs` - Uses Semantic Kernel with Azure AI Foundry agents
- `MultiAgentControllerAgentFx.cs` - Uses Microsoft Agent Framework

**SingleAgentDemo:**
- `SingleAgentControllerSK.cs` - Uses Semantic Kernel
- `SingleAgentControllerAgentFx.cs` - Uses Microsoft Agent Framework

Both controllers use the same route (`/api/multiagent` and `/api/singleagent`) to maintain compatibility with the Store UI.

## How to use this code for the session

For step-by-step instructions on how to start the services, run demos, and deliver the session content, see the session delivery guide:

- `session-delivery-resources\readme.md` — contains run instructions, presenter notes, demo scripts, and required configuration.
