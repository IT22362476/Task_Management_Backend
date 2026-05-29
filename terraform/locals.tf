# =============================================================================
# Local variables – derived naming
# =============================================================================
locals {
  # Sanitize location for naming (remove spaces, lowercase)
  location_short = replace(lower(var.location), "/\\s+/", "")

  # Resource names
  resource_group_name = "${var.prefix}-rg-${var.environment}"

  acr_name = "${replace(var.prefix, "-", "")}acr${random_id.acr_suffix.hex}"

  app_service_plan_name = "${var.prefix}-plan-${var.environment}"
  app_service_name      = "${var.prefix}-app-${var.environment}"

  postgres_server_name = "${var.prefix}-psql-${var.environment}"
  postgres_fqdn        = "${local.postgres_server_name}.postgres.database.azure.com"

  key_vault_name = "${var.prefix}-kv-${var.environment}"

  storage_account_name = replace("${var.prefix}stg${var.environment}", "-", "")

  # Container names
  tfstate_container_name    = "tfstate"
  deployment_logs_container = "deployment-logs"

  # Tags
  common_tags = {
    Environment = var.environment
    ManagedBy   = "Terraform"
    Project     = "TaskManager"
  }
}
