# =============================================================================
# Variables
# =============================================================================

variable "prefix" {
  description = "Prefix used for naming all Azure resources"
  type        = string
  default     = "taskmgr"
}

variable "location" {
  description = "Azure region for all resources"
  type        = string
  default     = "Southeast Asia"
}

variable "environment" {
  description = "Deployment environment (e.g., dev, staging, prod)"
  type        = string
  default     = "prod"
}

variable "postgres_admin_username" {
  description = "PostgreSQL admin username (avoid 'postgres' or 'admin')"
  type        = string
  default     = "pgadmin"
}

variable "postgres_database_name" {
  description = "Name of the application database"
  type        = string
  default     = "taskmanagerdb"
}

variable "postgres_app_username" {
  description = "PostgreSQL application (non-admin) username"
  type        = string
  default     = "taskuser"
}

# Derived naming convention:
# ACR:          <prefix>acr (max 50 chars, alphanumeric)
# App Service:  <prefix>-app-<environment> (max 60 chars)
# PostgreSQL:   <prefix>-psql-<environment> (max 63 chars)
# Key Vault:    <prefix>-kv-<environment> (max 24 chars, globally unique)
# Storage:      <prefix>stg<environment> (max 24 chars, globally unique, lowercase)
# App Plan:     <prefix>-plan-<environment>
# RG:           <prefix>-rg-<environment>
