# =============================================================================
# Azure Container Registry
# =============================================================================
resource "azurerm_container_registry" "main" {
  name                = local.acr_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = "Basic"
  admin_enabled       = true # Required for App Service to pull images without managed identity

  tags = local.common_tags
}

# Output ACR credentials (sensitive – reference via terraform output -json)
output "acr_login_server" {
  value       = azurerm_container_registry.main.login_server
  description = "ACR login server URL (for Docker push and App Service)"
}

output "acr_admin_username" {
  value       = azurerm_container_registry.main.admin_username
  description = "ACR admin username (for CI/CD Docker login)"
}

output "acr_admin_password" {
  value       = azurerm_container_registry.main.admin_password
  sensitive   = true
  description = "ACR admin password (store as GitHub Secret)"
}
