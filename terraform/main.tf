# ============================================================
# Terraform Configuration — Task Manager Development Environment
# ============================================================
# 
# This configuration provisions a completely SEPARATE development
# environment that does NOT touch your existing production setup.
#
# Resources created:
#   1. Resource Group        — containers all dev resources
#   2. Azure Container Registry — stores Docker images
#   3. App Service Plan      — Linux, B1 (cheap for dev)
#   4. Linux Web App         — runs the containerized backend
#   5. User-Assigned Managed Identity — secure ACR access
#   6. Role Assignment       — grants AcrPull to the Web App
#
# ============================================================

terraform {
  # AzureRM provider with version constraint for stability
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }

  # Recommended: store state in Azure Storage for team collaboration.
  # Uncomment and configure the backend block below AFTER creating
  # the storage account manually (one-time setup):
  #
  # backend "azurerm" {
  #   resource_group_name  = "task-management-tfstate"
  #   storage_account_name = "taskmanagertfstate"
  #   container_name       = "tfstate"
  #   key                  = "task-manager-dev.tfstate"
  # }
}

# Configure the AzureRM provider
provider "azurerm" {
  features {
    # Ensures no accidental deletion of resources without recreation notice
    resource_group {
      prevent_deletion_if_contains_resources = true
    }
  }
}

# ============================================================
# 1. Resource Group
# ============================================================
# Groups all dev resources together. Named with "-dev" suffix
# to clearly separate from production resources.
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
# Stores Docker images for the dev environment.
# Uses "Basic" SKU to minimise cost during development.
# Admin is DISABLED — we use Managed Identity instead (more secure).
resource "azurerm_container_registry" "dev" {
  name                = var.acr_name
  resource_group_name = azurerm_resource_group.dev.name
  location            = azurerm_resource_group.dev.location
  sku                 = "Basic"
  admin_enabled       = false # Managed Identity is used instead of admin creds

  tags = {
    Environment = "Development"
    Project     = "Task Manager"
  }
}

# ============================================================
# 3. Linux App Service Plan
# ============================================================
# The compute layer that runs the web app.
# B1 = Basic tier, 1 core, 1.75GB RAM — sufficient for dev.
# reserved = true means Linux plan (required for Linux containers).
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
# Runs the Docker container from ACR.
# Uses Managed Identity to pull images (no admin credentials needed).
resource "azurerm_linux_web_app" "dev" {
  name                = var.web_app_name
  resource_group_name = azurerm_resource_group.dev.name
  location            = azurerm_resource_group.dev.location
  service_plan_id     = azurerm_service_plan.dev.id

  # Enables the "System Assigned" Managed Identity
  identity {
    type = "SystemAssigned"
  }

  # Application settings: these override appsettings.json values
  # and are injected as environment variables in the container
  app_settings = {
    # Tell Azure App Service which port the container listens on
    "WEBSITES_PORT" = "8080"
    # ASP.NET Core settings
    "ASPNETCORE_URLS"        = "http://+:8080"
    "ASPNETCORE_ENVIRONMENT" = "Development"
    # JWT — set this via GitHub Actions or manually in the portal
    # "Jwt__Key"                      = "@Microsoft.KeyVault(SecretUri=https://yourvault.vault.azure.net/secrets/jwt-key/)"
  }

  # Sticky session / connection strings are NOT used here.
  # All configuration goes through app_settings for container apps.

  site_config {
    # Use the current .NET 8 minimum version
    minimum_tls_version = "1.2"

    container_registry_use_managed_identity = true

    # Health check path used by Azure's load balancer
    health_check_path                 = "/health"
    health_check_eviction_time_in_min = 2

    # Enable app logging for troubleshooting
    application_stack {
      docker_image_name   = "${var.docker_image_name}:${var.docker_image_tag}"
      docker_registry_url = "https://${azurerm_container_registry.dev.login_server}"
    }
  }

  # Logs configuration
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
# This allows the Web App to pull container images from ACR
# WITHOUT using admin credentials. The Web App's system-assigned
# managed identity is granted the "AcrPull" role on the ACR.
resource "azurerm_role_assignment" "webapp_to_acr" {
  # The principal (who gets access) — the Web App's managed identity
  principal_id = azurerm_linux_web_app.dev.identity[0].principal_id
  # The role — "AcrPull" allows pulling images only (not pushing)
  role_definition_name = "AcrPull"
  # The scope (what they get access to) — the ACR resource
  scope = azurerm_container_registry.dev.id

  depends_on = [
    azurerm_linux_web_app.dev,
    azurerm_container_registry.dev
  ]
}
