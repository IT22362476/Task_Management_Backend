# =============================================================================
# App Service Plan + Web App (Linux, Docker)
# =============================================================================

# User-assigned managed identity for the App Service
resource "azurerm_user_assigned_identity" "app_service" {
  name                = "${var.prefix}-identity-${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location

  tags = local.common_tags
}

# App Service Plan (Linux, B1)
resource "azurerm_service_plan" "main" {
  name                = local.app_service_plan_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  os_type             = "Linux"
  sku_name            = "B1"

  tags = local.common_tags
}

# App Service (Web App for Containers)
resource "azurerm_linux_web_app" "main" {
  name                = local.app_service_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  service_plan_id     = azurerm_service_plan.main.id

  # HTTPS only
  https_only = true

  # Managed identity
  identity {
    type = "UserAssigned"
    identity_ids = [
      azurerm_user_assigned_identity.app_service.id
    ]
  }

  # Docker container configuration
  site_config {
    always_on                                     = true
    http2_enabled                                 = true
    container_registry_use_managed_identity        = true
    container_registry_managed_identity_client_id  = azurerm_user_assigned_identity.app_service.client_id

    application_stack {
      docker_image_name   = "taskmanager:latest"
      docker_registry_url = "https://${azurerm_container_registry.main.login_server}"
    }

    health_check_path                 = "/health"
    health_check_eviction_time_in_min = 10
  }

  # App settings
  app_settings = {
    # ── PostgreSQL connection (via Key Vault reference) ──
    "ConnectionStrings__DefaultConnection" = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.connection_string.versionless_id}/)"

    # ── ASP.NET Core settings ──
    "ASPNETCORE_ENVIRONMENT" = "Production"
    "ASPNETCORE_URLS"        = "http://+:8080"

    # ── JWT Settings ──
    "Jwt__Issuer"                   = "TaskManager"
    "Jwt__Audience"                 = "TaskManagerUsers"
    "Jwt__AccessTokenExpiryMinutes" = "15"
    "Jwt__RefreshTokenExpiryDays"   = "7"
    "Jwt__Key"                      = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.jwt_key.versionless_id}/)"

    # ── Google Auth (overridable) ──
    "GoogleAuth__ClientId" = ""
  }

  # Logs enabled
  logs {
    application_logs {
      file_system_level = "Information"
    }
    http_logs {
      file_system {
        retention_in_days = 7
        retention_in_mb   = 35
      }
    }
  }

  tags = local.common_tags
}

# Slot for staging (optional – uncomment for blue-green deployments)
# resource "azurerm_linux_web_app_slot" "staging" {
#   name           = "staging"
#   app_service_id = azurerm_linux_web_app.main.id
#   ...
# }

# Outputs
output "app_service_hostname" {
  value       = azurerm_linux_web_app.main.default_hostname
  description = "Web App URL (https://<hostname>)"
}

output "app_service_name" {
  value       = azurerm_linux_web_app.main.name
  description = "App Service name (for az webapp commands)"
}

output "web_app_url" {
  value       = "https://${azurerm_linux_web_app.main.default_hostname}"
  description = "Full URL of the deployed web application"
}
