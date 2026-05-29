# ☁️ Task Manager – Azure Deployment Guide

Complete CI/CD + Infrastructure automation for deploying the Task Manager API (.NET 8, PostgreSQL) to Azure.

## 📋 Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| Azure CLI | Latest | `brew install azure-cli` |
| Terraform | ≥ 1.5 | `brew install terraform` |
| .NET SDK | 8.0.x | `brew install dotnet@8` |
| Docker | Latest | `brew install --cask docker` |
| psql / pg_dump | 16+ | `brew install postgresql@16` |

## 🔐 Authentication (No Service Principal Required)

Since Azure Student subscriptions block service principal login, we use these alternatives:

| Usage | Method |
|-------|--------|
| **Local Terraform** | `az login --use-device-code` (authenticates as your user) |
| **GitHub Actions → ACR** | ACR admin credentials (username + password from Terraform output) |
| **GitHub Actions → Web App** | Publish Profile (from Azure Portal → App Service → Get Publish Profile) |
| **GitHub Actions → PostgreSQL** | Connection string stored as GitHub Secret |
| **Web App → PostgreSQL** | Username/password from Key Vault (referenced via connection string) |

---

## 🏗️ Step 1: Deploy Infrastructure (Terraform)

### 1.1 Authenticate with Azure

```bash
az login --use-device-code
az account set --subscription "Your Student Subscription Name"
```

### 1.2 Initialize & Apply Terraform

```bash
cd terraform

# Copy example vars and customize if needed
cp terraform.tfvars.example terraform.tfvars

# Initialize Terraform (local state initially)
terraform init

# Preview what will be created
terraform plan

# Apply – this creates everything:
#   - Resource Group
#   - ACR (Basic)
#   - PostgreSQL Flexible Server (B1ms)
#   - App Service Plan (B1) + Web App (Linux, Docker)
#   - Key Vault + Secrets
#   - Storage Account (for state backup)
terraform apply -auto-approve
```

### 1.3 Capture Outputs

After `terraform apply`, capture these values:

```bash
# Get all outputs
terraform output

# Specific ones you'll need for GitHub Secrets:
terraform output acr_login_server
terraform output acr_admin_username
terraform output acr_admin_password        # sensitive
terraform output postgresql_connection_string  # sensitive
terraform output web_app_url
```

### 1.4 Migrate to Remote State (Optional but Recommended)

```bash
# After first apply, edit providers.tf:
# Uncomment the "backend" block, filling in:
#   storage_account_name = <from terraform output storage_account_name>

# Then migrate:
terraform init -migrate-state
# Type "yes" when prompted
```

---

## 🔄 Step 2: Set Up GitHub Actions Secrets

Go to your GitHub repo → **Settings → Secrets and variables → Actions** and add these secrets:

| Secret Name | Value |
|-------------|-------|
| `ACR_LOGIN_SERVER` | `<your-acr-name>.azurecr.io` (from `terraform output acr_login_server`) |
| `ACR_USERNAME` | ACR admin username (from `terraform output acr_admin_username`) |
| `ACR_PASSWORD` | ACR admin password (from `terraform output acr_admin_password`) |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Get from Azure Portal → App Service → Get Publish Profile (XML) |
| `AZURE_POSTGRES_CONNECTION_STRING` | Full connection string (from `terraform output postgresql_connection_string`) |
| `AZURE_POSTGRES_HOST` | PostgreSQL FQDN (from `terraform output postgresql_host`) |
| `AZURE_POSTGRES_APP_USER` | `taskuser` (or your configured username) |
| `AZURE_POSTGRES_APP_PASSWORD` | The app user password (get from Key Vault via Azure Portal) |
| `AZURE_POSTGRES_DATABASE` | `taskmanagerdb` |

### Getting the Publish Profile

```bash
# Option 1: Azure Portal
# App Service → Deployment → Deployment Center → Get Publish Profile

# Option 2: Azure CLI
az webapp deployment list-publishing-credentials \
  --resource-group taskmgr-rg-prod \
  --name taskmgr-app-prod \
  --query publishingPassword \
  --output tsv
```

### Getting Secrets from Key Vault

```bash
# List secrets
az keyvault secret list --vault-name taskmgr-kv-prod --query "[].name" -o tsv

# Get a specific secret
az keyvault secret show --vault-name taskmgr-kv-prod --name postgres-app-password --query value -o tsv

# Get connection string
az keyvault secret show --vault-name taskmgr-kv-prod --name connection-string --query value -o tsv
```

---

## 🚀 Step 3: Trigger CI/CD Pipeline

Push to `main` or `master`:

```bash
git add .
git commit -m "Deploy to Azure"
git push origin main
```

This triggers the GitHub Actions workflow (`.github/workflows/deploy.yml`):

1. **Build & Push** – Builds Docker image, pushes to ACR
2. **Deploy** – Deploys container to Azure Web App
3. **Migrate** – Runs `dotnet ef database update` against Azure PostgreSQL
4. **Seed** – (Optional, disabled by default) Loads seed data

### Monitor the Pipeline

```bash
# Watch deployment logs
az webapp log tail --resource-group taskmgr-rg-prod --name taskmgr-app-prod

# Check app health
curl https://taskmgr-app-prod.azurewebsites.net/health
```

---

## 🗄️ PostgreSQL Migration Strategy

### Fresh Start (Your Current Use Case)

For a brand-new deployment with no existing data to migrate:

```bash
# The CI/CD pipeline handles this automatically.
# EF Core migrations create all tables on first deploy.
```

If you want to manually run migrations locally:

```bash
# Install EF tools
dotnet tool install --global dotnet-ef

# Run migrations against Azure
dotnet ef database update \
  --connection "Host=taskmgr-psql-prod.postgres.database.azure.com;Port=5432;Database=taskmanagerdb;Username=taskuser;Password=****;SSL Mode=Require;Trust Server Certificate=false;"
```

### Full Data Migration (If You Had Existing Data)

Use the provided migration script:

```bash
# 1. Export your local credentials
export AZURE_DB_HOST="taskmgr-psql-prod.postgres.database.azure.com"
export AZURE_DB_PASSWORD="$(az keyvault secret show --vault-name taskmgr-kv-prod --name postgres-app-password --query value -o tsv)"

# 2. Run the migration script
chmod +x scripts/migrate-local-to-azure.sh
./scripts/migrate-local-to-azure.sh
```

The script performs:
1. `pg_dump --schema-only` from your local PostgreSQL
2. Apply schema to Azure PostgreSQL
3. Run EF Core migrations (idempotent)
4. Validate row counts and migration history

### Future Migrations (Development Best Practice)

**Recommended: EF Core Migrations**

```bash
# 1. Create a new migration (after changing models)
dotnet ef migrations add AddUserAvatar

# 2. Commit the migration files
git add Migrations/
git commit -m "Add user avatar migration"

# 3. CI/CD will auto-apply on next push
```

The CI/CD pipeline's `migrate` job runs `dotnet ef database update` which is **idempotent** – it only applies pending migrations and won't fail if already up-to-date.

If you need to manually roll back:

```bash
dotnet ef database update PreviousMigrationName \
  --connection "Host=...;Database=...;Username=...;Password=...;SSL Mode=Require;"
```

---

## 🔌 Connection Strings

### Local (Development)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=taskmanagerdb;Username=taskuser;Password=taskpass"
  }
}
```

### Azure (Production)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=taskmgr-psql-prod.postgres.database.azure.com;Port=5432;Database=taskmanagerdb;Username=taskuser;Password=***;SSL Mode=Require;Trust Server Certificate=false;"
  }
}
```

**Key Differences:**

| Property | Local | Azure |
|----------|-------|-------|
| `Host` | `localhost` | `*.postgres.database.azure.com` |
| `Port` | `5432` | `5432` |
| `SSL Mode` | Not required (often `Prefer`) | **`Require`** (enforced by Azure) |
| `Trust Server Certificate` | N/A | `false` (must use trusted CA) |

---

## 🔒 SSL / Certificate Troubleshooting

Azure PostgreSQL **enforces TLS 1.2+** connections. If you see SSL errors:

### Error: `SSL connection required`

**Fix:** Ensure your connection string includes `SSL Mode=Require`.

### Error: `Certificate chain could not be verified`

**Fix:** Download and trust the Azure DigiCert Global Root CA:

```bash
# Option 1: Use the connection string as-is (Trust Server Certificate=false)
# .NET Npgsql uses the system trust store – works on most systems

# Option 2: If still failing, download the Azure CA cert
curl -o /usr/local/share/ca-certificates/DigiCertGlobalRootCA.crt \
  https://www.digicert.com/CACerts/DigiCertGlobalRootCA.crt
sudo update-ca-certificates

# Option 3: For psql commands, set environment variable
export PGSSLMODE=require
export PGSSLROOTCERT=/path/to/DigiCertGlobalRootCA.crt
```

### Test the connection

```bash
# Test with psql
PGPASSWORD="your-password" psql \
  -h taskmgr-psql-prod.postgres.database.azure.com \
  -U taskuser \
  -d taskmanagerdb \
  -c "SELECT 1 AS test;" \
  --set=sslmode=require

# Test with .NET (from your local machine)
dotnet ef database update \
  --connection "Host=taskmgr-psql-prod.postgres.database.azure.com;Port=5432;Database=taskmanagerdb;Username=taskuser;Password=***;SSL Mode=Require;Trust Server Certificate=false;"
```

---

## 📁 Project Structure

```
.
├── .github/workflows/
│   └── deploy.yml              # CI/CD pipeline (build → push → deploy → migrate)
├── terraform/
│   ├── providers.tf             # Provider config (uncomment backend for remote state)
│   ├── variables.tf             # Input variables
│   ├── locals.tf                # Computed locals
│   ├── resource-group.tf        # Azure Resource Group
│   ├── storage.tf               # Storage Account (state + logs)
│   ├── acr.tf                   # Azure Container Registry
│   ├── keyvault.tf              # Key Vault + access policies + secrets
│   ├── random.tf                # Random passwords
│   ├── postgresql.tf            # PostgreSQL Flexible Server + DB + user + grants
│   ├── appservice.tf            # App Service Plan + Linux Web App (Docker)
│   ├── outputs.tf               # Consolidated outputs
│   └── terraform.tfvars.example # Example variable values
├── scripts/
│   ├── entrypoint.sh            # Container startup script (migrations + app start)
│   ├── migrate-local-to-azure.sh# One-time local → Azure migration
│   └── seed-data.sql            # Optional seed/reference data
├── Dockerfile                   # Multi-stage .NET 8 build + runtime
├── README-AZURE-DEPLOYMENT.md   # This file
├── Program.cs                   # .NET application entry point
└── ...
```

---

## 💰 Cost Optimization

| Resource | Tier | Monthly Cost (est.) |
|----------|------|-------------------|
| PostgreSQL Flexible Server | B_Standard_B1ms (Burstable) | ~$15 (750 hrs free/month included with student sub) |
| App Service Plan | B1 (Basic) | ~$13 |
| Container Registry | Basic | ~$5 |
| Key Vault | Standard | ~$1 |
| Storage Account | Standard LRS | ~$1 |
| **Total** | | **~$35/month** (less if using free credits) |

> 💡 **Tip:** After deployment, you can stop the App Service and PostgreSQL when not in use to save costs. Just re-start when you need it.

---

## 🛠️ Useful Commands

```bash
# View App Service logs
az webapp log tail --resource-group taskmgr-rg-prod --name taskmgr-app-prod

# SSH into the container (for debugging)
az webapp ssh --resource-group taskmgr-rg-prod --name taskmgr-app-prod

# Restart the web app
az webapp restart --resource-group taskmgr-rg-prod --name taskmgr-app-prod

# Show PostgreSQL connection info
az postgres flexible-server show \
  --resource-group taskmgr-rg-prod \
  --name taskmgr-psql-prod \
  --query "{host:fullyQualifiedDomainName, version:version, sku:skuName}"

# List all resources in the resource group
az resource list --resource-group taskmgr-rg-prod --output table

# Destroy everything (careful!)
cd terraform && terraform destroy
```

---

## ⚠️ Troubleshooting

### "Docker image pull failed" in Web App

**Cause:** ACR credentials not set correctly.

**Fix:** 
```bash
# Verify ACR credentials
az acr credential show --name taskmgracre --query "passwords[0].value"

# Restart the web app
az webapp restart --resource-group taskmgr-rg-prod --name taskmgr-app-prod
```

### "Cannot connect to PostgreSQL server"

**Cause 1:** Firewall blocking.
**Fix:** Verify firewall rules:
```bash
az postgres flexible-server firewall-rule list \
  --resource-group taskmgr-rg-prod \
  --name taskmgr-psql-prod \
  --output table
```

**Cause 2:** SSL not enabled.
**Fix:** Ensure connection string has `SSL Mode=Require`.

### "Migrations failed" in CI/CD

**Cause:** Connection string secret is incorrect or missing.

**Fix:** Update `AZURE_POSTGRES_CONNECTION_STRING` secret in GitHub.

```bash
# Test the connection string manually
dotnet ef database update \
  --connection "Host=...;Database=...;Username=...;Password=...;SSL Mode=Require;"
```

### Terraform "Error checking existence"

**Cause:** You're not logged in or the subscription is wrong.

**Fix:**
```bash
az login --use-device-code
az account list --output table
az account set --subscription "Your Subscription"
```
