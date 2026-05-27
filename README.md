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
.github/workflows/deploy-dev.yml
```

It:
1. **Terraform** — Provisions/updates Azure infrastructure (RG, ACR, App Service Plan, Web App)
2. **Build & Push** — Builds the Docker image and pushes to ACR
3. **Deploy** — Sets app settings (JWT, DB connection, etc.) and restarts the Web App

Triggers on pushes to the `develop` branch.

### Required GitHub Secrets

| Secret | Description |
|--------|-------------|
| `AZURE_CREDENTIALS` | Azure service principal JSON (with Contributor + AcrPush roles) |
| `DEV_JWT_KEY` | JWT signing key for dev environment (32+ chars) |
| `DEV_DB_CONNECTION` | PostgreSQL connection string for dev DB |
| `DEV_GOOGLE_CLIENT_ID` | Google OAuth client ID for dev |

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

### Why the Pipeline Was Fixed

The original setup had several issues that caused the pipeline to fail:

1. **Workflow file was outside the git repo** — `deploy-dev.yml` was at the outer `Task_Manager/.github/workflows/` level, but the git repo root is `Task_Manager-Backend/`. GitHub only sees workflows inside the repo's `.github/workflows/` directory.

2. **Wrong paths in the workflow** — Since the git root is `Task_Manager-Backend/`, paths like `DOTNET_PROJECT_PATH: Task_Manager-Backend` resolved to a non-existent nested directory. Fixed to use `.` as the project root.

3. **Terraform state not persisted** — State was stored locally and excluded from git. Without remote state (Azure Storage backend), each GitHub Actions run would try to create resources from scratch, causing "already exists" errors.

4. **Terraform directory untracked** — The `terraform/` folder wasn't committed to git. Now all `.tf` config files are tracked (state files are gitignored for security).
