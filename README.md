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

It builds the Docker image, pushes to Azure Container Registry (ACR), and deploys to Azure App Service.

### Required GitHub Secrets

| Secret | Description |
|--------|-------------|
| `AZURE_CREDENTIALS` | Azure service principal JSON |
| `ACR_LOGIN_SERVER` | e.g. `myregistry.azurecr.io` |
| `ACR_USERNAME` | ACR admin username |
| `ACR_PASSWORD` | ACR admin password |
| `JWT_KEY` | JWT signing key (32+ chars) |
| `CONNECTION_STRINGS__DEFAULT_CONNECTION` | PostgreSQL connection string |
| `GOOGLEAUTH__CLIENTID` | Google OAuth client ID |
