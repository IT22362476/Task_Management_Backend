# ============================================================
# Terraform Configuration — Task Manager Dev Environment
# ============================================================
#
# IMPORTANT: This uses LOCAL state (stored in terraform.tfstate).
# See the README for instructions on migrating to remote state.
#
# Resources managed:
#   1. Resource Group
#   2. Azure Container Registry (ACR)
#   3. App Service Plan (Linux, B1)
#   4. Linux Web App for Containers
#   5. Role Assignment (AcrPull for Web App Managed Identity)
#
# ============================================================

terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
  # State: LOCAL (terraform.tfstate)
  # The state is cached between GitHub Actions runs via actions/cache.
  # For teams, migrate to remote state (see README).
}

# Get the current Azure subscription ID (used for imports)
data "azurerm_subscription" "current" {}

# Configure the AzureRM provider
provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = true
    }
  }
}

# ============================================================
# Import existing resources into Terraform state
# ============================================================
# These import blocks tell Terraform: "these resources already
# exist in Azure, just add them to my state file."
#
# They are IDEMPOTENT — if the resource is already in state,
# Terraform skips the import. So it's safe to keep them here.
#
# Resources already in state (RG, ACR, App Plan) don't need
# import blocks — they were created by Terraform previously.

import {
  to = azurerm_linux_web_app.dev
  id = "${data.azurerm_subscription.current.id}/resourceGroups/${var.resource_group_name}/providers/Microsoft.Web/sites/${var.web_app_name}"
}

# The Role Assignment may or may not exist yet.
# If it doesn't exist, Terraform will create it on first apply.
# If it does exist and causes a conflict, run the import command
# documented in the README.

# ============================================================
# 1. Resource Group
# ============================================================
resource "azurerm_resource_group" "dev" {
  name     = var.resource_group_name
  location = var.location

  tags = {
    Environment = "Development"
    Project     = "Task Manager"
    ManagedBy   = "Terraform"
  }
}

# ============================================================
# 2. Azure Container Registry (ACR)
# ============================================================
resource "azurerm_container_registry" "dev" {
  name                = var.acr_name
  resource_group_name = azurerm_resource_group.dev.name
  location            = azurerm_resource_group.dev.location
  sku                 = "Basic"
  admin_enabled       = false # Managed Identity is used instead

  tags = {
    Environment = "Development"
    Project     = "Task Manager"
  }
}

# ============================================================
# 3. Linux App Service Plan
# ============================================================
resource "azurerm_service_plan" "dev" {
  name                = var.app_service_plan_name
  resource_group_name = azurerm_resource_group.dev.name
  location            = azurerm_resource_group.dev.location
  os_type             = "Linux"
  sku_name            = "B1"

  tags = {
    Environment = "Development"
    Project     = "Task Manager"
  }
}

# ============================================================
# 4. Linux Web App for Containers
# ============================================================
resource "azurerm_linux_web_app" "dev" {
  name                = var.web_app_name
  resource_group_name = azurerm_resource_group.dev.name
  location            = azurerm_resource_group.dev.location
  service_plan_id     = azurerm_service_plan.dev.id

  identity {
    type = "SystemAssigned"
  }

  app_settings = {
    "WEBSITES_PORT"          = "8080"
    "ASPNETCORE_URLS"        = "http://+:8080"
    "ASPNETCORE_ENVIRONMENT" = "Development"
  }

  site_config {
    minimum_tls_version = "1.2"

    container_registry_use_managed_identity = true

    health_check_path                 = "/health"
    health_check_eviction_time_in_min = 2

    application_stack {
      # The deploy-backend workflow pushes to :latest tag,
      # so the web app always picks up the newest image on restart.
      docker_image_name   = "${var.docker_image_name}:${var.docker_image_tag}"
      docker_registry_url = "https://${azurerm_container_registry.dev.login_server}"
    }
  }

  logs {
    application_logs {
      file_system_level = "Verbose"
    }
    http_logs {
      file_system {
        retention_in_days = 7
        retention_in_mb   = 35
      }
    }
  }

  depends_on = [
    azurerm_container_registry.dev
  ]

  tags = {
    Environment = "Development"
    Project     = "Task Manager"
  }
}

# ============================================================
# 5. Role Assignment: AcrPull for Web App Managed Identity
# ============================================================
resource "azurerm_role_assignment" "webapp_to_acr" {
  principal_id         = azurerm_linux_web_app.dev.identity[0].principal_id
  role_definition_name = "AcrPull"
  scope                = azurerm_container_registry.dev.id

  depends_on = [
    azurerm_linux_web_app.dev,
    azurerm_container_registry.dev
  ]
}
