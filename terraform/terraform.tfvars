# ============================================================
# Terraform Variable Values
# ============================================================
# These are the default values. They can be overridden via
# TF_VAR_* environment variables (as done in GitHub Actions).
#
# ============================================================

# Azure region
location = "Southeast Asia"

# Resource group for all resources
resource_group_name = "task-management-prod-rg"

# ACR name (must be globally unique — change if taken)
acr_name = "taskmanagementprodacr"

# Linux App Service Plan
app_service_plan_name = "taskmanagement-prod-plan"

# Linux Web App for Containers
web_app_name = "taskmanagement-prod-app"

# Docker image
docker_image_name = "task-manager-backend"
docker_image_tag  = "latest"
