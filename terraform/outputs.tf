# ============================================================
# Terraform Outputs — Useful values after provisioning
# ============================================================
# 
# After `terraform apply`, these values are printed to the
# terminal. Use them to configure GitHub Actions or verify
# the deployment.
#
# ============================================================

output "resource_group_name" {
  description = "Name of the development resource group"
  value       = azurerm_resource_group.dev.name
}

output "acr_login_server" {
  description = "ACR login server URL (use this in Docker commands and GitHub Actions)"
  value       = azurerm_container_registry.dev.login_server
}

output "acr_name" {
  description = "Name of the Azure Container Registry"
  value       = azurerm_container_registry.dev.name
}

output "web_app_url" {
  description = "Default hostname of the deployed web app (access the API here)"
  value       = "https://${azurerm_linux_web_app.dev.default_hostname}"
}

output "web_app_name" {
  description = "Name of the Azure Web App (use in GitHub Actions deploy step)"
  value       = azurerm_linux_web_app.dev.name
}

output "web_app_principal_id" {
  description = "Managed Identity principal ID of the Web App (for troubleshooting RBAC)"
  value       = azurerm_linux_web_app.dev.identity[0].principal_id
}

output "app_service_plan_name" {
  description = "Name of the Linux App Service Plan"
  value       = azurerm_service_plan.dev.name
}

output "docker_image" {
  description = "Full Docker image reference deployed to the Web App"
  value       = "${azurerm_container_registry.dev.login_server}/${var.docker_image_name}:${var.docker_image_tag}"
}
