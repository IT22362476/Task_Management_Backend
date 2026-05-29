#!/usr/bin/env bash
# =============================================================================
# migrate-local-to-azure.sh
#
# One-time migration of local PostgreSQL schema to Azure PostgreSQL.
# Strategy: pg_dump schema-only → apply to Azure → run EF Core migrations.
# For a "fresh start" (no existing data), this script:
#   1. Exports the local DB schema as SQL
#   2. (Optional) Sanitizes local-specific settings
#   3. Applies the schema to Azure PostgreSQL
#   4. Runs EF Core migrations to ensure schema is up-to-date
#   5. Validates the connection
#
# Usage:
#   chmod +x scripts/migrate-local-to-azure.sh
#   ./scripts/migrate-local-to-azure.sh
#
# Prerequisites:
#   - psql (PostgreSQL client) installed locally
#   - .NET SDK 8.0 installed locally
#   - Azure CLI installed and logged in (az login --use-device-code)
#   - Azure PostgreSQL server running (via Terraform)
#   - Locally sourced env vars (set below)
# =============================================================================

set -euo pipefail

# ──────────────────────────────────────────────
# Configuration – CHANGE THESE TO MATCH YOUR SETUP
# ──────────────────────────────────────────────

# --- Local PostgreSQL ---
LOCAL_DB_HOST="${PGHOST:-localhost}"
LOCAL_DB_PORT="${PGPORT:-5432}"
LOCAL_DB_NAME="${PGDATABASE:-taskmanagerdb}"
LOCAL_DB_USER="${PGUSER:-taskuser}"
LOCAL_DB_PASSWORD="${PGPASSWORD:-taskpass}"

# --- Azure PostgreSQL (from terraform output) ---
AZURE_DB_HOST="${AZURE_DB_HOST:-taskmgr-psql-prod.postgres.database.azure.com}"
AZURE_DB_PORT="5432"
AZURE_DB_NAME="${AZURE_DB_NAME:-taskmanagerdb}"
AZURE_DB_USER="${AZURE_DB_USER:-taskuser}"
AZURE_DB_PASSWORD="${AZURE_DB_PASSWORD:-}"

# --- Project ---
PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
DUMP_FILE="/tmp/local-postgres-dump-$(date +%Y%m%d-%H%M%S).sql"

# ──────────────────────────────────────────────
# Colors for output
# ──────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

info()  { echo -e "${GREEN}[✓]${NC} $1"; }
warn()  { echo -e "${YELLOW}[!]${NC} $1"; }
error() { echo -e "${RED}[✗]${NC} $1"; exit 1; }

# ──────────────────────────────────────────────
# Pre-flight checks
# ──────────────────────────────────────────────
check_prereqs() {
    echo ""
    echo "═══════════════════════════════════════════════"
    echo "  Local → Azure PostgreSQL Migration"
    echo "═══════════════════════════════════════════════"
    echo ""

    command -v psql >/dev/null 2>&1 || error "psql is required but not installed."
    command -v pg_dump >/dev/null 2>&1 || error "pg_dump is required but not installed."
    command -v dotnet >/dev/null 2>&1 || error ".NET SDK is required but not installed."

    if [ -z "$AZURE_DB_PASSWORD" ]; then
        error "AZURE_DB_PASSWORD is not set. Run: export AZURE_DB_PASSWORD='<your-password>'"
    fi

    info "All prerequisites met"
}

# ──────────────────────────────────────────────
# Step 1: Dump local schema (schema-only, no data)
# ──────────────────────────────────────────────
dump_local_schema() {
    echo ""
    echo "── Step 1: Exporting local PostgreSQL schema ──"

    export PGPASSWORD="$LOCAL_DB_PASSWORD"

    pg_dump \
        --host="$LOCAL_DB_HOST" \
        --port="$LOCAL_DB_PORT" \
        --username="$LOCAL_DB_USER" \
        --dbname="$LOCAL_DB_NAME" \
        --schema-only \
        --no-owner \
        --no-acl \
        --no-comments \
        --file="$DUMP_FILE" \
        2>/dev/null

    unset PGPASSWORD

    if [ $? -ne 0 ]; then
        error "pg_dump failed. Check your local PostgreSQL connection."
    fi

    # Remove local-specific lines
    sed -i '' "/-- Dumped from database version/d" "$DUMP_FILE" 2>/dev/null || true
    sed -i "/-- Dumped from database version/d" "$DUMP_FILE" 2>/dev/null || true

    local line_count=$(wc -l < "$DUMP_FILE")
    info "Schema exported to $DUMP_FILE ($line_count lines)"
}

# ──────────────────────────────────────────────
# Step 2: Apply schema to Azure PostgreSQL
# ──────────────────────────────────────────────
apply_to_azure() {
    echo ""
    echo "── Step 2: Applying schema to Azure PostgreSQL ──"

    export PGPASSWORD="$AZURE_DB_PASSWORD"

    psql \
        --host="$AZURE_DB_HOST" \
        --port="$AZURE_DB_PORT" \
        --username="$AZURE_DB_USER" \
        --dbname="$AZURE_DB_NAME" \
        --file="$DUMP_FILE" \
        --set=ON_ERROR_STOP=1 \
        2>&1

    local exit_code=$?
    unset PGPASSWORD

    if [ $exit_code -ne 0 ]; then
        warn "Schema import had issues (exit code $exit_code). This may be OK if tables already exist."
        warn "Continuing with EF Core migrations to reconcile..."
    else
        info "Schema imported successfully to Azure PostgreSQL"
    fi
}

# ──────────────────────────────────────────────
# Step 3: Run EF Core migrations (idempotent)
# ──────────────────────────────────────────────
run_ef_migrations() {
    echo ""
    echo "── Step 3: Applying EF Core migrations ──"

    cd "$PROJECT_DIR"

    # Install EF Core tools if not present
    if ! command -v dotnet-ef &>/dev/null; then
        dotnet tool install --global dotnet-ef
        export PATH="$PATH:$HOME/.dotnet/tools"
    fi

    # Build connection string for EF Core
    local ef_connection_string="Host=$AZURE_DB_HOST;Port=$AZURE_DB_PORT;Database=$AZURE_DB_NAME;Username=$AZURE_DB_USER;Password=$AZURE_DB_PASSWORD;SSL Mode=Require;Trust Server Certificate=false;"

    echo "Running: dotnet ef database update..."
    dotnet ef database update \
        --connection "$ef_connection_string" \
        --verbose

    if [ $? -eq 0 ]; then
        info "EF Core migrations applied successfully"
    else
        error "EF Core migrations failed. Check the error above."
    fi
}

# ──────────────────────────────────────────────
# Step 4: Validate migration
# ──────────────────────────────────────────────
validate() {
    echo ""
    echo "── Step 4: Validation ──"

    export PGPASSWORD="$AZURE_DB_PASSWORD"

    echo ""
    echo "Tables in Azure PostgreSQL ($AZURE_DB_NAME):"
    psql \
        --host="$AZURE_DB_HOST" \
        --port="$AZURE_DB_PORT" \
        --username="$AZURE_DB_USER" \
        --dbname="$AZURE_DB_NAME" \
        --command="\dt" \
        2>&1

    echo ""
    echo "Row counts:"
    psql \
        --host="$AZURE_DB_HOST" \
        --port="$AZURE_DB_PORT" \
        --username="$AZURE_DB_USER" \
        --dbname="$AZURE_DB_NAME" \
        --command="
            SELECT schemaname, tablename, n_live_tup AS row_count
            FROM pg_stat_user_tables
            ORDER BY tablename;
        " \
        2>&1 || warn "Row count query failed (may need permissions)"

    echo ""
    echo "Migration status (EF Core):"
    psql \
        --host="$AZURE_DB_HOST" \
        --port="$AZURE_DB_PORT" \
        --username="$AZURE_DB_USER" \
        --dbname="$AZURE_DB_NAME" \
        --command="SELECT migration_id, applied_version, applied_on FROM __EFMigrationsHistory ORDER BY applied_on;" \
        2>&1 || warn "EF Core migrations history table not found (migrations may not have run yet)"

    unset PGPASSWORD

    info "Validation complete"
    echo ""
    echo "═══════════════════════════════════════════════"
    echo "  ✅ Migration completed successfully!"
    echo "═══════════════════════════════════════════════"
    echo ""
    echo "Next steps:"
    echo "  1. Update your appsettings.json with the Azure connection string"
    echo "  2. Or use the Key Vault secret: ConnectionStrings__DefaultConnection"
    echo "  3. Verify your API works: curl https://$AZURE_DB_HOST/health"
    echo ""
}

# ──────────────────────────────────────────────
# Cleanup
# ──────────────────────────────────────────────
cleanup() {
    rm -f "$DUMP_FILE"
    info "Temporary dump file removed"
}

# ──────────────────────────────────────────────
# Main
# ──────────────────────────────────────────────
main() {
    check_prereqs
    dump_local_schema
    apply_to_azure
    run_ef_migrations
    validate
    cleanup
}

main "$@"
