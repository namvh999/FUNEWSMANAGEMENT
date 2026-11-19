#!/usr/bin/env bash
echo "========================================================================="
echo "  KHOI CHAY HE THONG PHAN TAN FUNewsManagementSystem (PRN232 Assignment 2)"
echo "========================================================================="

DOTNET_CMD="dotnet"
if ! command -v dotnet &> /dev/null; then
    if [ -f "/private/tmp/dotnet/dotnet" ]; then
        DOTNET_CMD="/private/tmp/dotnet/dotnet"
    fi
fi

echo "Using dotnet: $DOTNET_CMD"

# Run all 4 services in background
echo "[1/4] Starting Core API (Port: 5001)..."
(cd FUNewsManagement_CoreAPI && $DOTNET_CMD run --launch-profile http) &
PID1=$!

sleep 3

echo "[2/4] Starting Analytics API (Port: 5002)..."
(cd FUNewsManagement_AnalyticsAPI && $DOTNET_CMD run --launch-profile http) &
PID2=$!

sleep 2

echo "[3/4] Starting AI API (Port: 5003)..."
(cd FUNewsManagement_AIAPI && $DOTNET_CMD run --launch-profile http) &
PID3=$!

sleep 2

echo "[4/4] Starting Frontend MVC (Port: 5000)..."
(cd FUNewsManagement_FE && $DOTNET_CMD run --launch-profile http) &
PID4=$!

echo "========================================================================="
echo "  All 4 services are running:"
echo "  - Frontend:      http://localhost:5000"
echo "  - Core API:      http://localhost:5001/swagger"
echo "  - Analytics API: http://localhost:5002/swagger"
echo "  - AI API:        http://localhost:5003/swagger"
echo "  Press Ctrl+C to stop all services."
echo "========================================================================="

trap "kill $PID1 $PID2 $PID3 $PID4; exit" SIGINT SIGTERM
wait
