# Running Aspire in Dev Container

## Problem
When running `aspire run` multiple times, you may encounter port binding errors because processes from the previous run are still holding onto the ports.

## Permanent Solution

### Option 1: Use the automated run script (Recommended)
```bash
cd src
./run-aspire.sh
```

This script automatically:
1. Cleans up any hanging Aspire processes
2. Frees up the required ports
3. Starts Aspire with the correct HTTP configuration

### Option 2: Manual cleanup before each run
```bash
cd src
./cleanup-aspire.sh
aspire run --launch-profile http
```

### Option 3: Direct aspire command with cleanup
```bash
cd src
# First, cleanup any existing processes
./cleanup-aspire.sh

# Then run aspire
aspire run --launch-profile http
```

## Why This Happens

The issue occurs because:
1. When you stop Aspire with CTRL+C, sometimes background processes don't terminate cleanly
2. The dashboard and resource service ports (19009, 20136, 15295) remain occupied
3. On the next run, Aspire cannot bind to these ports and fails

## Configuration Changes Made

The following files have been configured for HTTP-only operation in dev containers:

1. **launchSettings.json**: 
   - Set `ASPIRE_ALLOW_UNSECURED_TRANSPORT=true`
   - Configured HTTP endpoints on `0.0.0.0` for container compatibility

2. **appsettings.Development.json**: 
   - Dashboard OTLP and Resource Service URLs set to HTTP

## Quick Reference

**Ports used by Aspire:**
- 15295: Main dashboard
- 19009: OTLP endpoint  
- 20136: Resource service endpoint

**To check what's using these ports:**
```bash
lsof -i:15295,19009,20136
```

**To kill all Aspire processes:**
```bash
./cleanup-aspire.sh
```
