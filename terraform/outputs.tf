# =============================================================================
# Consolidated Outputs
# =============================================================================

output "all_connection_info" {
  description = "Summary of all connection information"
  value = {
    web_app_url          = "https://${azurerm_linux_web_app.main.default_hostname}"
    acr_login_server     = azurerm_container_registry.main.login_server
    postgresql_host      = azurerm_postgresql_flexible_server.main.fqdn
    postgresql_database  = var.postgres_database_name
    postgresql_app_user  = var.postgres_app_username
    key_vault_name       = azurerm_key_vault.main.name
    storage_account_name = azurerm_storage_account.main.name
    resource_group       = azurerm_resource_group.main.name
  }
}

# Publish profile for GitHub Actions deployment
output "publish_profile_cmd" {
  value       = "az webapp deployment list-publishing-credentials --resource-group ${azurerm_resource_group.main.name} --name ${azurerm_linux_web_app.main.name} --query publishingPassword --output tsv"
  description = "Run this az CLI command to get the publish profile password for GitHub Actions"
}
