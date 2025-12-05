# Implementation Plan: Agent Registration, DevUI Integration, and Navigation Fixes

**Date:** December 5, 2025  
**Status:** Ready for Implementation  
**Pull Request:** #17 - Apply fixes after Ignite

---

## Overview

This plan outlines the implementation of three major improvements to the AI Tour demo application:

1. **Refactor agent registration** using extension methods for cleaner dependency injection
2. **Integrate DevUI** for enhanced debugging and visualization capabilities
3. **Fix duplicate navigation icons** by adding proper CSS definitions

---

## Task 1: Register MAF Agents Using Extension Methods

### Objective

Refactor agent registration in SingleAgentDemo and MultiAgentDemo projects to use extension methods, enabling proper dependency injection and cleaner code organization.

### Current State

- Both projects manually instantiate `ZavaMAFLocalAgentsProvider` and `ZavaMAFAgentsProvider` in Program.cs
- Agents are registered as keyed singletons using lambda expressions
- Controllers retrieve agents via `IKeyedServiceProvider`

### Implementation Steps

#### 1.1 Create AgentServicesExtensions.cs for SingleAgentDemo

**Location:** `src/SingleAgentDemo/AgentServices/AgentServicesExtensions.cs`

**Content:**

```csharp
using Microsoft.Extensions.DependencyInjection;
using ZavaMAFLocalAgentsProvider;
using ZavaMAFAgentsProvider;

namespace SingleAgentDemo.AgentServices;

public static class AgentServicesExtensions
{
    /// <summary>
    /// Registers local Microsoft Agent Framework agents
    /// </summary>
    public static IServiceCollection RegisterMAFAgentsLocal(this IServiceCollection services)
    {
        // Register the local MAF agent provider as singleton
        services.AddSingleton<ZavaMAFLocalAgentsProvider.ZavaMAFLocalAgentsProvider>();
        
        return services;
    }

    /// <summary>
    /// Registers Azure AI Foundry-based Microsoft Agent Framework agents
    /// </summary>
    public static IServiceCollection RegisterMAFAgentsFoundry(this IServiceCollection services)
    {
        // Register the Foundry MAF agent provider as singleton
        services.AddSingleton<ZavaMAFAgentsProvider.ZavaMAFAgentsProvider>();
        
        return services;
    }
}
```

#### 1.2 Create AgentServicesExtensions.cs for MultiAgentDemo

**Location:** `src/MultiAgentDemo/AgentServices/AgentServicesExtensions.cs`

**Content:** Same as SingleAgentDemo (identical implementation)

#### 1.3 Update SingleAgentDemo/Program.cs

**Changes:**

- Add using statement: `using SingleAgentDemo.AgentServices;`
- Replace lines 18-25 (manual agent instantiation) with:

  ```csharp
  // Register MAF agents based on working mode
  if (workingMode == ZavaWorkingModes.WorkingMode.MAFLocal)
  {
      builder.Services.RegisterMAFAgentsLocal();
  }
  else if (workingMode == ZavaWorkingModes.WorkingMode.MAFFoundry)
  {
      builder.Services.RegisterMAFAgentsFoundry();
  }
  ```

#### 1.4 Update MultiAgentDemo/Program.cs

**Changes:** Identical to SingleAgentDemo/Program.cs

### Benefits

- **Cleaner code:** Reduces Program.cs complexity
- **Better maintainability:** Agent registration logic centralized in one place
- **Consistent pattern:** Follows established .NET extension method conventions
- **Easier testing:** Services can be easily mocked in unit tests

### Files Modified

- ✨ New: `src/SingleAgentDemo/AgentServices/AgentServicesExtensions.cs`
- ✨ New: `src/MultiAgentDemo/AgentServices/AgentServicesExtensions.cs`
- 📝 Modified: `src/SingleAgentDemo/Program.cs`
- 📝 Modified: `src/MultiAgentDemo/Program.cs`

---

## Task 2: Add DevUI Support to Store Project

### Objective

Integrate Microsoft Extensions AI DevUI to provide debugging and visualization capabilities for agent interactions.

### Current State

- Store project has no DevUI integration
- Missing OpenAI response tracking services
- No DevUI navigation menu entry

### Implementation Steps

#### 2.1 Add DevUI NuGet Package

**File:** `src/Store/Store.csproj`

**Add package reference:**

```xml
<PackageReference Include="Microsoft.Extensions.AI.DevUI" Version="1.0.0-preview.251125.1" />
```

**Note:** Check NuGet for the latest preview version if needed.

#### 2.2 Update Store/Program.cs

**Add using statements:**

```csharp
using Microsoft.Extensions.AI;
```

**Register services (after existing service registrations):**

```csharp
// Register services for OpenAI responses and conversations (required for DevUI)
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

// Add DevUI for agent debugging and visualization
builder.AddDevUI();
```

**Map endpoints (in the app building section, within development environment check):**

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenAIResponses();
    app.MapOpenAIConversations();
    app.MapDevUI(); // Creates /devui endpoint
}
```

#### 2.3 Add DevUI Navigation Menu Item

**File:** `src/Store/Components/Layout/NavMenu.razor`

**Add navigation item (before closing `</nav>` tag):**

```razor
<hr class="nav-divider" />
<div class="nav-item px-3">
    <a class="nav-link" href="devui" target="_blank" rel="noopener noreferrer">
        <span class="bi bi-code-slash" aria-hidden="true"></span> DevUI
    </a>
</div>
```

**Note:** Uses `<a>` tag instead of `<NavLink>` to open in new tab.

### Benefits

- **Enhanced debugging:** Visualize agent conversations and responses in real-time
- **Better development experience:** Track OpenAI API calls and agent interactions
- **Troubleshooting:** Easier identification of issues in agent communication

### Files Modified

- 📝 Modified: `src/Store/Store.csproj`
- 📝 Modified: `src/Store/Program.cs`
- 📝 Modified: `src/Store/Components/Layout/NavMenu.razor`

### Testing

After implementation:

1. Run the Store application in development mode
2. Navigate to `http://localhost:port/devui` (should open automatically from menu)
3. Verify DevUI interface loads successfully
4. Interact with agents and verify conversations appear in DevUI

---

## Task 3: Fix Duplicate Icons in Navigation Bar

### Objective

Resolve duplicate icon rendering issue by adding proper CSS definitions for Bootstrap Icons.

### Root Cause Analysis

The current implementation uses Bootstrap Icon class names (`bi-house-door-fill`, `bi-list-nested`, etc.) but doesn't include the actual icon definitions. The project embeds SVG icons as CSS background images using data URIs since it doesn't reference the full Bootstrap Icons library.

### Implementation Steps

#### 3.1 Add Missing Icon CSS Definitions

**File:** `src/Store/Components/Layout/NavMenu.razor.css`

**Add the following CSS classes (append to existing file):**

```css
/* Bootstrap Icon definitions using SVG data URIs */

.bi-house-door-fill {
    background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' fill='currentColor' class='bi bi-house-door-fill' viewBox='0 0 16 16'%3E%3Cpath d='M6.5 14.5v-3.505c0-.245.25-.495.5-.495h2c.25 0 .5.25.5.5v3.5a.5.5 0 0 0 .5.5h4a.5.5 0 0 0 .5-.5v-7a.5.5 0 0 0-.146-.354L13 5.793V2.5a.5.5 0 0 0-.5-.5h-1a.5.5 0 0 0-.5.5v1.293L8.354 1.146a.5.5 0 0 0-.708 0l-6 6A.5.5 0 0 0 1.5 7.5v7a.5.5 0 0 0 .5.5h4a.5.5 0 0 0 .5-.5Z'/%3E%3C/svg%3E");
}

.bi-list-nested {
    background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' fill='currentColor' class='bi bi-list-nested' viewBox='0 0 16 16'%3E%3Cpath fill-rule='evenodd' d='M4.5 11.5A.5.5 0 0 1 5 11h10a.5.5 0 0 1 0 1H5a.5.5 0 0 1-.5-.5zm-2-4A.5.5 0 0 1 3 7h10a.5.5 0 0 1 0 1H3a.5.5 0 0 1-.5-.5zm-2-4A.5.5 0 0 1 1 3h10a.5.5 0 0 1 0 1H1a.5.5 0 0 1-.5-.5z'/%3E%3C/svg%3E");
}

.bi-cart {
    background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' fill='currentColor' class='bi bi-cart' viewBox='0 0 16 16'%3E%3Cpath d='M0 1.5A.5.5 0 0 1 .5 1H2a.5.5 0 0 1 .485.379L2.89 3H14.5a.5.5 0 0 1 .491.592l-1.5 8A.5.5 0 0 1 13 12H4a.5.5 0 0 1-.491-.408L2.01 3.607 1.61 2H.5a.5.5 0 0 1-.5-.5zM3.102 4l1.313 7h8.17l1.313-7H3.102zM5 12a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm7 0a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm-7 1a1 1 0 1 1 0 2 1 1 0 0 1 0-2zm7 0a1 1 0 1 1 0 2 1 1 0 0 1 0-2z'/%3E%3C/svg%3E");
}

.bi-people {
    background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' fill='currentColor' class='bi bi-people' viewBox='0 0 16 16'%3E%3Cpath d='M15 14s1 0 1-1-1-4-5-4-5 3-5 4 1 1 1 1h8Zm-7.978-1A.261.261 0 0 1 7 12.996c.001-.264.167-1.03.76-1.72C8.312 10.629 9.282 10 11 10c1.717 0 2.687.63 3.24 1.276.593.69.758 1.457.76 1.72l-.008.002a.274.274 0 0 1-.014.002H7.022ZM11 7a2 2 0 1 0 0-4 2 2 0 0 0 0 4Zm3-2a3 3 0 1 1-6 0 3 3 0 0 1 6 0ZM6.936 9.28a5.88 5.88 0 0 0-1.23-.247A7.35 7.35 0 0 0 5 9c-4 0-5 3-5 4 0 .667.333 1 1 1h4.216A2.238 2.238 0 0 1 5 13c0-1.01.377-2.042 1.09-2.904.243-.294.526-.569.846-.816ZM4.92 10A5.493 5.493 0 0 0 4 13H1c0-.26.164-1.03.76-1.724.545-.636 1.492-1.256 3.16-1.275ZM1.5 5.5a3 3 0 1 1 6 0 3 3 0 0 1-6 0Zm3-2a2 2 0 1 0 0 4 2 2 0 0 0 0-4Z'/%3E%3C/svg%3E");
}

.bi-gear {
    background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' fill='currentColor' class='bi bi-gear' viewBox='0 0 16 16'%3E%3Cpath d='M8 4.754a3.246 3.246 0 1 0 0 6.492 3.246 3.246 0 0 0 0-6.492zM5.754 8a2.246 2.246 0 1 1 4.492 0 2.246 2.246 0 0 1-4.492 0z'/%3E%3Cpath d='M9.796 1.343c-.527-1.79-3.065-1.79-3.592 0l-.094.319a.873.873 0 0 1-1.255.52l-.292-.16c-1.64-.892-3.433.902-2.54 2.541l.159.292a.873.873 0 0 1-.52 1.255l-.319.094c-1.79.527-1.79 3.065 0 3.592l.319.094a.873.873 0 0 1 .52 1.255l-.16.292c-.892 1.64.901 3.434 2.541 2.54l.292-.159a.873.873 0 0 1 1.255.52l.094.319c.527 1.79 3.065 1.79 3.592 0l.094-.319a.873.873 0 0 1 1.255-.52l.292.16c1.64.893 3.434-.902 2.54-2.541l-.159-.292a.873.873 0 0 1 .52-1.255l.319-.094c1.79-.527 1.79-3.065 0-3.592l-.319-.094a.873.873 0 0 1-.52-1.255l.16-.292c.893-1.64-.902-3.433-2.541-2.54l-.292.159a.873.873 0 0 1-1.255-.52l-.094-.319zm-2.633.283c.246-.835 1.428-.835 1.674 0l.094.319a1.873 1.873 0 0 0 2.693 1.115l.291-.16c.764-.415 1.6.42 1.184 1.185l-.159.292a1.873 1.873 0 0 0 1.116 2.692l.318.094c.835.246.835 1.428 0 1.674l-.319.094a1.873 1.873 0 0 0-1.115 2.693l.16.291c.415.764-.42 1.6-1.185 1.184l-.291-.159a1.873 1.873 0 0 0-2.693 1.116l-.094.318c-.246.835-1.428.835-1.674 0l-.094-.319a1.873 1.873 0 0 0-2.692-1.115l-.292.16c-.764.415-1.6-.42-1.184-1.185l.159-.291A1.873 1.873 0 0 0 1.945 8.93l-.319-.094c-.835-.246-.835-1.428 0-1.674l.319-.094A1.873 1.873 0 0 0 3.06 4.377l-.16-.292c-.415-.764.42-1.6 1.185-1.184l.292.159a1.873 1.873 0 0 0 2.692-1.115l.094-.319z'/%3E%3C/svg%3E");
}

.bi-robot {
    background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' fill='currentColor' class='bi bi-robot' viewBox='0 0 16 16'%3E%3Cpath d='M6 12.5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 0 1h-3a.5.5 0 0 1-.5-.5ZM3 8.062C3 6.76 4.235 5.765 5.53 5.886a26.58 26.58 0 0 0 4.94 0C11.765 5.765 13 6.76 13 8.062v1.157a.933.933 0 0 1-.765.935c-.845.147-2.34.346-4.235.346-1.895 0-3.39-.2-4.235-.346A.933.933 0 0 1 3 9.219V8.062Zm4.542-.827a.25.25 0 0 0-.217.068l-.92.9a24.767 24.767 0 0 1-1.871-.183.25.25 0 0 0-.068.495c.55.076 1.232.149 2.02.193a.25.25 0 0 0 .189-.071l.754-.736.847 1.71a.25.25 0 0 0 .404.062l.932-.97a25.286 25.286 0 0 0 1.922-.188.25.25 0 0 0-.068-.495c-.538.074-1.207.145-1.98.189a.25.25 0 0 0-.166.076l-.754.785-.842-1.7a.25.25 0 0 0-.182-.135Z'/%3E%3Cpath d='M8.5 1.866a1 1 0 1 0-1 0V3h-2A4.5 4.5 0 0 0 1 7.5V8a1 1 0 0 0-1 1v2a1 1 0 0 0 1 1v1a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-1a1 1 0 0 0 1-1V9a1 1 0 0 0-1-1v-.5A4.5 4.5 0 0 0 10.5 3h-2V1.866ZM14 7.5V13a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1V7.5A3.5 3.5 0 0 1 5.5 4h5A3.5 3.5 0 0 1 14 7.5Z'/%3E%3C/svg%3E");
}

.bi-code-slash {
    background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' fill='currentColor' class='bi bi-code-slash' viewBox='0 0 16 16'%3E%3Cpath d='M10.478 1.647a.5.5 0 1 0-.956-.294l-4 13a.5.5 0 0 0 .956.294l4-13zM4.854 4.146a.5.5 0 0 1 0 .708L1.707 8l3.147 3.146a.5.5 0 0 1-.708.708l-3.5-3.5a.5.5 0 0 1 0-.708l3.5-3.5a.5.5 0 0 1 .708 0zm6.292 0a.5.5 0 0 0 0 .708L14.293 8l-3.147 3.146a.5.5 0 0 0 .708.708l3.5-3.5a.5.5 0 0 0 0-.708l-3.5-3.5a.5.5 0 0 0-.708 0z'/%3E%3C/svg%3E");
}

/* Navigation divider styling */
.nav-divider {
    border: none;
    border-top: 1px solid rgba(255, 255, 255, 0.2);
    margin: 0.5rem 1rem;
}
```

### Expected Results

- Icons render correctly as single, properly styled elements
- No duplicate or malformed icon display
- Consistent icon appearance across all navigation items
- DevUI menu separator displays correctly

### Files Modified

- 📝 Modified: `src/Store/Components/Layout/NavMenu.razor.css`

### Testing

1. Run the Store application
2. Verify navigation menu displays icons correctly
3. Check that no duplicate icons appear
4. Verify all icons (Home, Products, Cart, Customers, Settings, Agents, DevUI) render properly

---

## Implementation Order

**Recommended sequence:**

1. **Task 1 (Agent Registration)** - Foundational change, should be implemented first
2. **Task 3 (Fix Icons)** - Quick fix, prepares navigation for DevUI addition
3. **Task 2 (Add DevUI)** - Final integration, depends on clean navigation UI

**Alternative:** Tasks 2 and 3 can be implemented in parallel since they affect different aspects of the Store project.

---

## Dependencies

### NuGet Packages

- `Microsoft.Extensions.AI.DevUI` (version 1.0.0-preview.251125.1 or later)

### Existing Dependencies (already in projects)

- `ZavaMAFLocalAgentsProvider`
- `ZavaMAFAgentsProvider`
- `ZavaWorkingModes`
- Blazor.Bootstrap (3.5.0)

---

## Verification Checklist

### Task 1: Agent Registration

- [ ] AgentServicesExtensions.cs files created in both demo projects
- [ ] Extension methods `RegisterMAFAgentsLocal()` and `RegisterMAFAgentsFoundry()` implemented
- [ ] Program.cs files updated to use extension methods
- [ ] Application builds successfully
- [ ] Agents are properly registered and accessible via DI
- [ ] Controllers can still retrieve agents via `IKeyedServiceProvider`

### Task 2: DevUI Integration

- [ ] DevUI NuGet package added to Store.csproj
- [ ] OpenAI services registered in Program.cs
- [ ] DevUI endpoints mapped in development mode
- [ ] Navigation menu item added with proper icon
- [ ] DevUI accessible at `/devui` endpoint
- [ ] DevUI interface loads without errors
- [ ] Agent interactions visible in DevUI

### Task 3: Navigation Icons

- [ ] CSS definitions added for all navigation icons
- [ ] Icons render correctly without duplicates
- [ ] No console errors related to missing resources
- [ ] Navigation divider displays properly
- [ ] Icon styling consistent across all menu items

---

## Rollback Plan

If issues occur during implementation:

1. **Task 1 Rollback:**
   - Revert Program.cs changes in both demo projects
   - Delete AgentServicesExtensions.cs files
   - Original manual registration will still function

2. **Task 2 Rollback:**
   - Remove DevUI navigation menu item from NavMenu.razor
   - Remove DevUI service registrations and endpoint mappings from Program.cs
   - Remove DevUI NuGet package reference
   - Application will function without DevUI

3. **Task 3 Rollback:**
   - Revert NavMenu.razor.css to original state
   - Icons will return to current (problematic) state but won't break functionality

---

## Known Considerations

### DevUI Package Version

The reference uses preview version `1.0.0-preview.251125.1`. Consider:

- Check NuGet for most recent preview or stable version
- Test compatibility with current .NET 10.0 target framework
- Document any version-specific issues

### Agent Provider Dependency

Current implementation uses `IKeyedServiceProvider` for agent retrieval. Future enhancement:

- Consider refactoring controllers to inject specific agents directly via constructor DI
- Would provide better type safety and IntelliSense support
- Would require more significant refactoring of controller classes

### Testing Strategy

- Manual testing required for all three tasks
- Focus on development environment behavior (DevUI only in dev mode)
- Verify agent functionality remains unchanged after registration refactor
- Test navigation across different browsers to ensure icon rendering consistency

---

## Reference Links

- Agent Registration Pattern: [a2aapiredemo/AgentServicesExtensions.cs](https://github.com/elbruno/a2aapiredemo/blob/main/src-05-mermaid-location/AgentServices/AgentServicesExtensions.cs)
- DevUI Implementation: [a2aapiredemo/Store](https://github.com/elbruno/a2aapiredemo/tree/main/src-05-mermaid-location/Store)
- Navigation CSS Reference: [a2aapiredemo/NavMenu.razor.css](https://github.com/elbruno/a2aapiredemo/tree/main/src-05-mermaid-location/Store/Components/Layout/NavMenu.razor.css)

---

## Summary

This plan systematically addresses three key improvements to the demo application:

1. **Cleaner architecture** through extension method-based agent registration
2. **Enhanced debugging** via DevUI integration for better development experience
3. **Polished UI** by fixing navigation icon rendering issues

All changes are non-breaking and maintain backward compatibility with existing functionality. The modular nature of the tasks allows for independent implementation and testing.
