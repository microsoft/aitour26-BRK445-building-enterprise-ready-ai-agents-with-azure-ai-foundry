# Quick Start Guide: Switching Agent Frameworks

This guide shows how to switch between Semantic Kernel and Microsoft Agent Framework using the Store frontend.

## Prerequisites
- The solution is already built and running
- Azure AI Foundry agents are deployed and configured
- Store application is accessible in your browser

## Switching Steps

### Using the Settings Page (Recommended)

1. **Open the Store application**
   - Navigate to the Store frontend in your browser
   - The URL is typically displayed in the .NET Aspire dashboard

2. **Navigate to Settings**
   - Click on **Settings** in the left navigation menu
   - Or navigate directly to `/settings`

3. **Select Your Framework**
   - Use the toggle switch on the Settings page:
     - **Toggle OFF** (default) = Semantic Kernel (SK)
     - **Toggle ON** = Microsoft Agent Framework (AgentFx)
   - Your selection is automatically saved

4. **Verify the Change**
   - A success message will appear confirming your selection
   - The framework indicator shows which framework is currently active
   - Changes take effect immediately - no restart required

5. **Test the Framework**
   - Navigate to any agent demo page:
     - **Single Agent** demo
     - **Multi-Agent** demo
   - Submit a request and check the logs to see which framework is being used

## Verification

The Settings page displays:
- Current framework selection (SK or AgentFx)
- Framework comparison table showing active/inactive status
- Success confirmation when settings are saved

**Framework logs will show:**
- Semantic Kernel: `"Calling multi-agent service... using SK framework"`
- Microsoft Agent Framework: `"Calling multi-agent service... using AgentFx framework"`

## Configuration Persistence

- **Browser localStorage**: Framework preference is stored in browser localStorage
- **Per-browser**: Each browser has its own setting
- **Persistent**: Setting survives page refreshes and browser restarts
- **No server config needed**: No need to edit appsettings.json files

## Advanced: Programmatic Access

If you need to access the framework setting programmatically:

```javascript
// Get current framework
localStorage.getItem('agentFramework'); // Returns "SK" or "AgentFx"

// Set framework
localStorage.setItem('agentFramework', 'AgentFx');
```

## Troubleshooting

**Issue**: Settings page doesn't save my preference
- **Solution**: Check browser console for JavaScript errors
- **Solution**: Ensure localStorage is enabled in your browser

**Issue**: Framework doesn't change after toggling
- **Solution**: Hard refresh the page (Ctrl+F5 or Cmd+Shift+R)
- **Solution**: Clear browser cache and reload

**Issue**: "Framework not available" error
- **Solution**: Ensure both MultiAgentDemo and SingleAgentDemo services are running
- **Solution**: Check the .NET Aspire dashboard for service health

## Benefits of Frontend Switching

1. **No Server Restart**: Changes take effect immediately
2. **User-Friendly**: Simple toggle switch interface
3. **Per-User**: Different users/browsers can use different frameworks
4. **No Configuration Files**: No need to edit appsettings.json
5. **Visual Feedback**: Clear indication of active framework

## Performance Comparison

You can use the Settings page to quickly switch frameworks and compare:
- Response times
- Agent behavior
- Resource usage
- Implementation differences

Simply toggle between frameworks and run the same requests to compare results!
