# =============================================================================
# Provider Configuration
# =============================================================================
terraform {
  required_version = ">= 1.5"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
    postgresql = {
      source  = "cyrilgdn/postgresql"
      version = "~> 1.25"
    }
    http = {
      source  = "hashicorp/http"
      version = "~> 3.4"
    }
  }

  # === UNCOMMENT AFTER FIRST APPLY TO MIGRATE TO REMOTE STATE ===
  # backend "azurerm" {
  #   resource_group_name  = "taskmanager-rg"
  #   storage_account_name = "<storage-account-name-from-output>"
  #   container_name       = "tfstate"
  #   key                  = "terraform.tfstate"
  # }
}

provider "azurerm" {
  features {
    key_vault {
      purge_soft_delete_on_destroy    = true
      recover_soft_deleted_key_vaults = true
    }
  }
}
