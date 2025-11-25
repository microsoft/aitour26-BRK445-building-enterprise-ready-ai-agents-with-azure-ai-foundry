#!/bin/bash
# Script to run Aspire with automatic cleanup

set -e

echo "🚀 Starting Aspire with automatic cleanup..."
echo ""

# Cleanup first
./cleanup-aspire.sh
echo ""

# Wait a moment for ports to be freed
sleep 2

# Run Aspire
echo "📦 Starting Aspire..."
export ASPIRE_ALLOW_UNSECURED_TRANSPORT=true
export ASPNETCORE_ENVIRONMENT=Development
export DOTNET_ENVIRONMENT=Development

aspire run --launch-profile http
