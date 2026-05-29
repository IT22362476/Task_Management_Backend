# ============================================
# Stage 1: Build the .NET application
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies (layer caching)
COPY Task_Manager-Backend.csproj ./
RUN dotnet restore

# Copy all source files and build
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# ============================================
# Stage 2: Runtime image (no SDK — smaller & faster)
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Expose the port the app runs on
EXPOSE 8080

# Copy published binaries from build stage
COPY --from=build /app/publish .

# Set environment defaults (override via Azure App Settings)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Start the application directly
# (Migrations are handled by GitHub Actions CI/CD pipeline — not at container startup)
ENTRYPOINT ["dotnet", "Task_Manager-Backend.dll"]
