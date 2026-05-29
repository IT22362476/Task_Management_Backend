# =============================================================================
# Azure Database for PostgreSQL Flexible Server (B1ms – Free-friendly)
# =============================================================================

# --------------------
# Server
# --------------------
resource "azurerm_postgresql_flexible_server" "main" {
  name                          = local.postgres_server_name
  resource_group_name           = azurerm_resource_group.main.name
  location                      = azurerm_resource_group.main.location
  version                       = "16"
  delegated_subnet_id           = null # Kept null unless VNet integration is enabled
  private_dns_zone_id           = null
  public_network_access_enabled = true

  administrator_login    = var.postgres_admin_username
  administrator_password = random_password.postgres_admin.result

  sku_name   = "B_Standard_B1ms"
  storage_mb = 32768

  backup_retention_days        = 7
  geo_redundant_backup_enabled = false

  # Auto-grow storage to prevent full-disk issues
  auto_grow_enabled = true

  # SSL/TLS is enforced by default on Azure PostgreSQL Flexible Server

  tags = local.common_tags

  lifecycle {
    ignore_changes = [
      zone, # Azure assigns this automatically; can't be changed arbitrarily
    ]
  }

  depends_on = [
    azurerm_key_vault_access_policy.terraform_user
  ]
}

# --------------------
# Firewall: Allow Azure services (0.0.0.0 – 0.0.0.0 is the magic CIDR)
# --------------------
resource "azurerm_postgresql_flexible_server_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_postgresql_flexible_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# --------------------
# Firewall: Allow your current public IP (for local pgadmin/psql access)
# --------------------
data "http" "my_ip" {
  url = "https://api.ipify.org/?format=text"
}

resource "azurerm_postgresql_flexible_server_firewall_rule" "allow_my_ip" {
  name             = "AllowMyIP_${replace(data.http.my_ip.response_body, ".", "_")}"
  server_id        = azurerm_postgresql_flexible_server.main.id
  start_ip_address = data.http.my_ip.response_body
  end_ip_address   = data.http.my_ip.response_body
}

# --------------------
# Connect to the server to create the application database and user
# --------------------
provider "postgresql" {
  host         = azurerm_postgresql_flexible_server.main.fqdn
  port         = 5432
  database     = "postgres" # Connect to the default admin database
  username     = azurerm_postgresql_flexible_server.main.administrator_login
  password     = random_password.postgres_admin.result
  sslmode      = "require"
  superuser    = false # Azure Flexible Server does not grant SUPERUSER
}

# Create the application database
resource "postgresql_database" "app" {
  name              = var.postgres_database_name
  owner             = azurerm_postgresql_flexible_server.main.administrator_login
  allow_connections = true
  depends_on = [
    azurerm_postgresql_flexible_server.main
  ]
}

# Create the application (non-admin) role
resource "postgresql_role" "app_user" {
  name     = var.postgres_app_username
  password = random_password.postgres_app.result
  login    = true

  depends_on = [
    azurerm_postgresql_flexible_server.main
  ]
}

# Grant all privileges on the application database to the app user
resource "postgresql_grant" "schema_public" {
  database    = postgresql_database.app.name
  role        = postgresql_role.app_user.name
  schema      = "public"
  object_type = "schema"
  privileges  = ["CREATE", "USAGE"]

  depends_on = [postgresql_role.app_user]
}

resource "postgresql_default_privileges" "app_tables" {
  database    = postgresql_database.app.name
  role        = postgresql_role.app_user.name
  schema      = "public"
  owner       = azurerm_postgresql_flexible_server.main.administrator_login
  object_type = "table"
  privileges  = ["ALL"]

  depends_on = [postgresql_role.app_user]
}

resource "postgresql_default_privileges" "app_sequences" {
  database    = postgresql_database.app.name
  role        = postgresql_role.app_user.name
  schema      = "public"
  owner       = azurerm_postgresql_flexible_server.main.administrator_login
  object_type = "sequence"
  privileges  = ["ALL"]

  depends_on = [postgresql_role.app_user]
}

# Grant ALL on ALL tables in the public schema
resource "postgresql_grant" "all_tables" {
  database    = postgresql_database.app.name
  role        = postgresql_role.app_user.name
  schema      = "public"
  object_type = "table"
  privileges  = ["ALL"]

  depends_on = [postgresql_role.app_user]
}

# --------------------
# Connection string (local value – also stored in Key Vault)
# --------------------
locals {
  postgres_connection_string = "Host=${local.postgres_fqdn};Port=5432;Database=${var.postgres_database_name};Username=${var.postgres_app_username};Password=${random_password.postgres_app.result};SSL Mode=Require;Trust Server Certificate=false;"
}

# Outputs
output "postgresql_host" {
  value       = azurerm_postgresql_flexible_server.main.fqdn
  description = "PostgreSQL server FQDN (for App Service and psql)"
}

output "postgresql_database_name" {
  value       = var.postgres_database_name
  description = "Name of the application database"
}

output "postgresql_app_username" {
  value       = var.postgres_app_username
  description = "Application (non-admin) PostgreSQL username"
}

output "postgresql_connection_string" {
  value       = local.postgres_connection_string
  sensitive   = true
  description = "Full connection string for the Web App (also stored in Key Vault)"
}
