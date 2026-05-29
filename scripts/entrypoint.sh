#!/bin/bash
# =============================================================================
# entrypoint.sh
# 
# Container startup script that:
#   1. Runs EF Core database migrations (idempotent – safe to run every time)
#   2. Starts the .NET application
#
# This script is used when you want migrations to run at container startup
# (Option 1 from the requirements). 
#
# Environment variables expected (set via Azure App Settings):
#   ConnectionStrings__DefaultConnection  – PostgreSQL connection string
#   ASPNETCORE_ENVIRONMENT                – "Production"
#   ASPNETCORE_URLS                       – "http://+:8080"
# =============================================================================

set -e

echo "═══════════════════════════════════════════════"
echo "  Task Manager API – Container Startup"
echo "═══════════════════════════════════════════════"

# ── Run EF Core database migrations ──
echo ""
echo "[1/2] Running database migrations..."
if [ -n "${ConnectionStrings__DefaultConnection}" ]; then
    dotnet ef database update \
        --connection "${ConnectionStrings__DefaultConnection}" \
        --verbose 2>&1
    
    if [ $? -eq 0 ]; then
        echo "✓ Migrations applied successfully"
    else
        echo "⚠ Migration had issues (may already be up-to-date)"
    fi
else
    echo "⚠ ConnectionStrings__DefaultConnection not set – skipping migrations"
fi

# ── Start the application ──
echo ""
echo "[2/2] Starting application..."
echo "═══════════════════════════════════════════════"
echo ""

exec dotnet Task_Manager-Backend.dll
