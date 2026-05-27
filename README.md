# Task Manager Backend

.NET 8 Web API with PostgreSQL, deployed as a Docker container to Azure App Service.

---

## Architecture Overview

Two **separate** GitHub Actions workflows split Infrastructure as Code from Application CI/CD:

```
┌──────────────────────────────────────┐
│  terraform.yml                       │
│  (manual — workflow_dispatch only)   │
│                                      │
│  Manages Azure infrastructure        │
│  • Resource Group                    │
│  • Azure Container Registry (ACR)    │
│  • App Service Plan                  │
│  • Web App + Managed Identity        │
│  • Role Assignment (AcrPull)         │
└──────────────────────────────────────┘

┌──────────────────────────────────────┐
│  deploy-backend.yml                  │
│  (auto — push to main/develop)       │
│                                      │
│  Deploys the application             │
│  • dotnet restore + build            │
│  • Trivy vulnerability scan          │
│  • Docker build + push to ACR        │
│  • Restart Web App                   │
│  • Verify /health endpoint           │
└──────────────────────────────────────┘
```

**Key principle:** Infrastructure changes are intentional and planned.
Application deployments are frequent and automated.

---

## Workflow 1: Terraform — Infrastructure (`terraform.yml`)

**Trigger:** Manual only (`workflow_dispatch` — click a button in GitHub UI)

**Purpose:** Create and manage all Azure resources.

**How to run:**
1. Go to your GitHub repo → **Actions** tab
2. Select **"Terraform — Infrastructure"** in the left sidebar
3. Click **"Run workflow"** → pick branch → **"Run"**

**What it does:**
| Step | Description |
|------|-------------|
| Checkout | Gets your Terraform config files |
| Cache | Restores the Terraform state from a previous run |
| Setup Terraform | Installs Terraform 1.9.8 |
| Azure Login | Authenticates using the service principal |
| `terraform init` | Initializes providers and modules |
| `terraform validate` | Checks config syntax |
| `terraform plan` | Shows what will change |
| `terraform apply` | Applies changes (creates/updates/destroys resources) |

**Required secrets:**
| Secret | Description |
|--------|-------------|
| `AZURE_CREDENTIALS` | Azure service principal JSON with Contributor role |

---

## Workflow 2: Deploy Backend Application (`deploy-backend.yml`)

**Trigger:** Automatic on push to `main`, `develop`, or `Development`

**Purpose:** Build, scan, and deploy the .NET application to the existing infrastructure.

**What it does:**
| Step | Description |
|------|-------------|
| dotnet restore | Downloads NuGet packages |
| dotnet build | Compiles the application |
| Azure Login | Authenticates to Azure |
| ACR Login | Logs in to ACR via `az acr login` |
| Docker build | Builds the container image |
| Trivy scan | Scans for vulnerabilities |
| Docker push | Pushes image to ACR |
| App Settings | Updates JWT, DB connection, etc. |
| Restart Web App | Triggers a restart to pull the new `:latest` image |
| Verify | Checks `GET /health` returns 200 |

**Required secrets:**
| Secret | Description |
|--------|-------------|
| `AZURE_CREDENTIALS` | Azure service principal JSON |
| `JWT_KEY` | JWT signing key (32+ characters) |
| `DB_CONNECTION` | PostgreSQL connection string |
| `GOOGLE_CLIENT_ID` | Google OAuth client ID |

---

## Getting Started — First-Time Setup

### 1. Create an Azure Service Principal

Run this once in your terminal:

```bash
az ad sp create-for-rbac --name "github-actions" \
  --role contributor \
  --scopes /subscriptions/YOUR_SUBSCRIPTION_ID \
  --sdk-auth
```

Copy the entire JSON output — you'll need it for the `AZURE_CREDENTIALS` secret.

### 2. Add GitHub Secrets

Go to your repo → **Settings** → **Secrets and variables** → **Actions** → **New repository secret**

Add these secrets:

```
AZURE_CREDENTIALS   ← the JSON from step 1
JWT_KEY             ← a random string 32+ chars long
DB_CONNECTION       ← Host=...;Port=5432;Database=...;Username=...;Password=...
GOOGLE_CLIENT_ID    ← your Google OAuth client ID
```

### 3. Import Existing Azure Resources into Terraform (One-Time)

The Azure resources already exist (created manually or by a previous Terraform run).
The Terraform config already has an `import` block for the **Web App** in `main.tf` — it will auto-import on first apply.

For the **Role Assignment** (if it already exists and causes a conflict on first apply):

```bash
# Get the role assignment ID
az role assignment list \
  --scope /subscriptions/YOUR_SUB_ID/resourceGroups/task-management-dev-rg/providers/Microsoft.ContainerRegistry/registries/taskmanagementdevacr \
  --query "[?principalType=='ServicePrincipal'].{id:id, principalName:principalName}" \
  --output table

# Then import it:
terraform import azurerm_role_assignment.webapp_to_acr /subscriptions/.../providers/Microsoft.Authorization/roleAssignments/ROLE_ASSIGNMENT_ID
```

### 4. Run the Terraform Workflow

1. Go to GitHub → **Actions** → **Terraform — Infrastructure**
2. Click **"Run workflow"**
3. This will:
   - Import the existing Web App into state
   - Detect any drift and show a plan
   - Apply any changes needed to match the config

### 5. Deploy the Application

Push a commit to the `develop` or `main` branch — the **Deploy Backend Application** workflow will automatically:
- Build the .NET code
- Push the Docker image to ACR
- Restart the Web App
- Verify the `/health` endpoint

---

## Understanding Terraform State

### What is Terraform State?

Terraform state is a **JSON file** (`terraform.tfstate`) that maps the resources in your `.tf` config files to the actual resources in Azure. It's Terraform's source of truth.

Think of it like a shopping list that Terraform keeps checking:
- "Did I already buy milk?" → checks the state file
- "Is the ACR already created?" → checks the state file

Without state, Terraform has no idea what it already created and will try to create everything from scratch — which fails because the resources already exist.

### Why Remote Backend Matters

Currently, this project uses **local state** (the `.tfstate` file stays on your machine or in a GitHub Actions cache).

| Approach | Pros | Cons |
|----------|------|------|
| **Local state** (current) | Simple, no extra setup | Only one person can run Terraform at a time; state can be lost |
| **Remote state** (Azure Storage) | Team-safe, state is never lost, locking prevents conflicts | Requires a storage account (one-time setup) |

### Why Terraform Should NOT Run Automatically on Every Push

1. **Terraform changes infrastructure** — accidentally deleting a resource could take down your app.
2. **It requires a human to review the plan** — `terraform plan` shows what will change, and someone should verify it.
3. **The state must be in sync** — if the state is out of sync, Terraform might try to recreate resources.
4. **Application changes are frequent** — you don't want to run `terraform apply` every time you change a line of code.

**Best practice:** Infrastructure changes go through a manual review process. Application deployments are automated.

---

## Future Improvements

### 1. Migrate to Remote State (Azure Storage)

When you're ready for remote state:

```bash
# One-time setup
az group create --name task-management-tfstate --location "East US"
az storage account create --name taskmanagertfstate \
  --resource-group task-management-tfstate --sku Standard_LRS
az storage container create --name tfstate \
  --account-name taskmanagertfstate

# Then in main.tf, change to:
# backend "azurerm" {
#   resource_group_name  = "task-management-tfstate"
#   storage_account_name = "taskmanagertfstate"
#   container_name       = "tfstate"
#   key                  = "task-manager-backend.tfstate"
# }

# Migrate existing state
terraform init -migrate-state
```

After migration, remove the `actions/cache` step from `terraform.yml` and uncomment the `backend "azurerm"` block in `main.tf`.

### 2. Automate Infrastructure Deployment (Future)

Once the remote backend is stable, you can change `terraform.yml` from `workflow_dispatch` to trigger on:
- Push to a dedicated `infra` branch
- Pull request merge with infrastructure labels

This creates a **safe automation** path where infrastructure changes are reviewed via PRs before being applied.

### 3. Add Staging / Production Environments

Create separate `.tfvars` files:
```
terraform/
  env/
    dev.tfvars
    prod.tfvars
```

And separate workflows or workflow environments in GitHub Actions.

---

## File Reference

```
Task_Manager-Backend/
├── .github/
│   └── workflows/
│       ├── terraform.yml          # Manual infrastructure provisioning
│       └── deploy-backend.yml     # Automatic application CI/CD
├── terraform/
│   ├── main.tf                    # All Azure resource definitions + imports
│   ├── variables.tf               # Input variables
│   ├── outputs.tf                 # Output values
│   └── terraform.tfvars           # Variable values for dev environment
├── Dockerfile                     # Container build instructions
├── Program.cs                     # .NET application entry point
└── README.md                      # This file
```
