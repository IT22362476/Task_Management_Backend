# =============================================================================
# Storage Account (Terraform state + logs)
# =============================================================================
resource "azurerm_storage_account" "main" {
  name                     = local.storage_account_name
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"

  blob_properties {
    versioning_enabled = true
  }

  tags = local.common_tags
}

resource "azurerm_storage_container" "tfstate" {
  name                  = local.tfstate_container_name
  storage_account_id    = azurerm_storage_account.main.id
  container_access_type = "private"
}

resource "azurerm_storage_container" "logs" {
  name                  = local.deployment_logs_container
  storage_account_id    = azurerm_storage_account.main.id
  container_access_type = "private"
}

# Output the storage account name so it can be used to configure the backend
output "storage_account_name" {
  value       = azurerm_storage_account.main.name
  description = "Use this name when configuring the Terraform Azurerm Backend"
}

output "storage_account_rg" {
  value       = azurerm_resource_group.main.name
  description = "Resource group of the storage account (for backend config)"
}
