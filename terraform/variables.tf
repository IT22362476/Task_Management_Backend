# ============================================================
# Terraform Variables — Task Manager Development Environment
# ============================================================
# 
# All configurable values are defined here. Set them in
# terraform.tfvars or pass via environment variables (TF_VAR_*).
#
# ============================================================

# Azure region where all resources will be created
variable "location" {
  description = "Azure region for all resources (e.g., East US, West Europe)"
  type        = string
}

# Resource Group — containers all dev resources
variable "resource_group_name" {
  description = "Name of the development resource group"
  type        = string
}

# Azure Container Registry (ACR) — stores Docker images
# Note: ACR names must be globally unique and alphanumeric only (no hyphens)
variable "acr_name" {
  description = "Globally unique name for Azure Container Registry (letters+numbers only)"
  type        = string
}

# App Service Plan — defines the compute tier for the web app
variable "app_service_plan_name" {
  description = "Name of the Linux App Service Plan"
  type        = string
}

# Web App — runs the containerized .NET backend
variable "web_app_name" {
  description = "Name of the Azure Web App for Containers"
  type        = string
}

# Docker image name (as tagged in ACR)
variable "docker_image_name" {
  description = "Name of the Docker image in ACR (e.g., task-manager-backend)"
  type        = string
  default     = "task-manager-backend"
}

# Docker image tag (version)
variable "docker_image_tag" {
  description = "Tag of the Docker image to deploy (e.g., latest, v1.0.0, git-sha)"
  type        = string
  default     = "latest"
}
