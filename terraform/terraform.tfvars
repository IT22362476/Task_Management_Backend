# ============================================================
# Terraform Variable Values — Development Environment
# ============================================================
# These match the Azure resources that already exist.
# Do NOT change these unless you intentionally want to
# recreate infrastructure.
#
# ============================================================

# Azure region (where resources are currently deployed)
location = "Southeast Asia"

# Existing resource group
resource_group_name = "task-management-dev-rg"

# Existing ACR (must be globally unique)
acr_name = "taskmanagementdevacr"

# Existing App Service Plan
app_service_plan_name = "taskmanagement-dev-plan"

# Existing Web App
web_app_name = "taskmanagement-dev-app"

# Docker image configuration
docker_image_name = "task-manager-backend"
docker_image_tag  = "latest"
