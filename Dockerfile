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
# Install EF Core tools in BUILD stage (where SDK exists)
# ============================================
RUN dotnet tool install --global dotnet-ef

# ============================================
# Stage 2: Runtime image
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Expose the port the app runs on
EXPOSE 8080

# Copy published binaries from build stage
COPY --from=build /app/publish .

# ============================================
# Copy EF Core tools from build stage (so they exist in runtime)
# ============================================
COPY --from=build /root/.dotnet/tools /root/.dotnet/tools
ENV PATH="$PATH:/root/.dotnet/tools"

# Copy entrypoint script (supports migrations + app start)
COPY scripts/entrypoint.sh .
RUN chmod +x entrypoint.sh

# Set environment defaults (override via Azure App Settings)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Use the entrypoint script (runs migrations, then starts the app)
ENTRYPOINT ["./entrypoint.sh"]