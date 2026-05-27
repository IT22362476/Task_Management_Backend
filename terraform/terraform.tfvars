# ============================================================
# Terraform Variable Values — Development Environment
# ============================================================
# 
# Fill in your preferred region. The naming convention uses
# "task-management-dev-*" to avoid conflict with production.
#
# ============================================================

# Azure region — change to your closest region
location = "Southeast Asia"

# Resource group for all development resources
resource_group_name = "task-management-dev-rg"

# ACR name (must be globally unique — change if "taskmanagementdevacr" is taken)
acr_name = "taskmanagementdevacr"

# Linux App Service Plan
app_service_plan_name = "taskmanagement-dev-plan"

# Linux Web App for Containers
web_app_name = "taskmanagement-dev-app"

# Docker image as built by GitHub Actions
docker_image_name = "task-manager-backend"
docker_image_tag  = "latest"
