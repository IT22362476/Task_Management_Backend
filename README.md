# Task Manager Backend

.NET 8 Web API with PostgreSQL.

## Local Development

```bash
# Run with dotnet (requires PostgreSQL on localhost:5432)
export JWT_KEY="your-32-char-min-key"
dotnet run
```

## Docker

```bash
# Build image
docker build -t task-manager-backend:latest .

# Run container
docker run -p 5135:8080 \
  -e JWT_KEY="your-key" \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=taskmanagerdb;Username=taskuser;Password=taskpass" \
  -e ASPNETCORE_ENVIRONMENT=Production \
  task-manager-backend:latest
```

## CI/CD Pipeline

The GitHub Actions workflow is located at:
```
.github/workflows/deploy-backend.yml
```

It fully automates everything — no manual Azure setup needed:

1. **Terraform** — Automatically provisions/updates the Azure infrastructure:
   - Resource Group
   - Azure Container Registry (ACR)
   - Linux App Service Plan
   - Linux Web App for Containers (with System-Assigned Managed Identity)
   - Role Assignment (AcrPull for secure image pulls — no admin credentials)
2. **Build & Push** — Builds the Docker image and pushes to ACR using Azure CLI auth
3. **Deploy** — Sets app settings (JWT, DB connection, etc.) and restarts the Web App
4. **Verify** — Checks the `/health` endpoint to confirm the deployment succeeded

Triggers on pushes to `main` and `develop` branches.

## Infrastructure (Terraform)

Terraform configs are in [`terraform/`](terraform/):

| File | Purpose |
|------|---------|
| `main.tf` | Core resources — RG, ACR, App Service Plan, Web App, Role Assignment |
| `variables.tf` | Input variables with descriptions |
| `outputs.tf` | Outputs — ACR login server, web app URL, etc. |
| `terraform.tfvars` | Default variable values |

### Required GitHub Secrets

| Secret | Description |
|--------|-------------|
| `AZURE_CREDENTIALS` | Azure service principal JSON (with Contributor + AcrPush roles) |
| `JWT_KEY` | JWT signing key for production (32+ chars) |
| `DB_CONNECTION` | PostgreSQL connection string for production DB |
| `GOOGLE_CLIENT_ID` | Google OAuth client ID |

### One-Time: Terraform Remote State Setup

The Terraform state is stored in Azure Storage so that GitHub Actions runs share it.
Run these commands **once** before the first pipeline run:

```bash
# Login to Azure
az login

# Create the state storage resource group
az group create --name task-management-tfstate --location "East US"

# Create the storage account
az storage account create --name taskmanagertfstate \
  --resource-group task-management-tfstate --sku Standard_LRS

# Create the container
az storage container create --name tfstate \
  --account-name taskmanagertfstate

# Migrate local state to remote (done locally, one time)
cd terraform
terraform init -migrate-state
```

> **Note:** The ACR uses **Managed Identity** for image pulls (not admin credentials).
> The Build step uses `az acr login` (Azure CLI with service principal) to push images,
> and the Web App uses its System-Assigned Managed Identity to pull them — all without
> storing any ACR passwords in GitHub Secrets.
