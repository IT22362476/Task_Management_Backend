# =============================================================================
# Azure Key Vault (secrets storage – no plaintext passwords in state)
# =============================================================================
resource "azurerm_key_vault" "main" {
  name                       = local.key_vault_name
  resource_group_name        = azurerm_resource_group.main.name
  location                   = azurerm_resource_group.main.location
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  soft_delete_retention_days = 7
  purge_protection_enabled   = false

  tags = local.common_tags
}

# Grant the current user (running Terraform) access to manage secrets
resource "azurerm_key_vault_access_policy" "terraform_user" {
  key_vault_id = azurerm_key_vault.main.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = data.azurerm_client_config.current.object_id

  secret_permissions = [
    "Get", "List", "Set", "Delete", "Purge", "Recover"
  ]
}

# Grant the Azure App Service (via its managed identity) read access to Key Vault
resource "azurerm_key_vault_access_policy" "app_service" {
  key_vault_id = azurerm_key_vault.main.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_user_assigned_identity.app_service.principal_id

  secret_permissions = [
    "Get", "List"
  ]
}

# --------------------
# Secrets
# --------------------
resource "azurerm_key_vault_secret" "postgres_admin_password" {
  name         = "postgres-admin-password"
  value        = random_password.postgres_admin.result
  key_vault_id = azurerm_key_vault.main.id

  depends_on = [azurerm_key_vault_access_policy.terraform_user]
}

resource "azurerm_key_vault_secret" "postgres_app_password" {
  name         = "postgres-app-password"
  value        = random_password.postgres_app.result
  key_vault_id = azurerm_key_vault.main.id

  depends_on = [azurerm_key_vault_access_policy.terraform_user]
}

resource "azurerm_key_vault_secret" "connection_string" {
  name         = "connection-string"
  value        = local.postgres_connection_string
  key_vault_id = azurerm_key_vault.main.id

  depends_on = [azurerm_key_vault_access_policy.terraform_user]
}

resource "azurerm_key_vault_secret" "jwt_key" {
  name         = "jwt-key"
  value        = random_password.jwt_key.result
  key_vault_id = azurerm_key_vault.main.id

  depends_on = [azurerm_key_vault_access_policy.terraform_user]
}

# Current Azure client (used for tenant_id)
data "azurerm_client_config" "current" {}
