#!/bin/bash
# Script to cleanup Aspire processes and free up ports

echo "🧹 Cleaning up Aspire processes..."

# Kill processes on Aspire dashboard ports
PORTS="19009 20136 15295 17104 21237 22003"
for port in $PORTS; do
    PID=$(lsof -ti:$port 2>/dev/null)
    if [ ! -z "$PID" ]; then
        echo "  Killing process on port $port (PID: $PID)"
        kill -9 $PID 2>/dev/null
    fi
done

# Kill any remaining Aspire/AppHost processes
ASPIRE_PIDS=$(ps aux | grep -E "aspire|ZavaAppHost|dotnet.*ZavaAppHost" | grep -v grep | awk '{print $2}')
if [ ! -z "$ASPIRE_PIDS" ]; then
    echo "  Killing Aspire processes: $ASPIRE_PIDS"
    echo "$ASPIRE_PIDS" | xargs kill -9 2>/dev/null
fi

echo "✅ Cleanup complete!"
