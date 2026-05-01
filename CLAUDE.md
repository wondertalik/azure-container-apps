# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A demonstration project for Azure Container Apps using .NET 8, featuring two containerized microservices with enterprise-grade
observability (OpenTelemetry, Sentry, Azure Monitor). Current version: 1.1.2.

## Common Commands

### Build & Test

```bash
dotnet build
dotnet test
dotnet test tests/HttpApi.IntegrationTests
```

### Local Development (Docker Compose)

```bash
# Start observability stack + core services (Azurite, Jaeger, Prometheus, Seq, Aspire Dashboard, OTEL Collector)
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml --env-file .env.dev -p my-container-apps up --build --remove-orphans

# Add containerized services
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml -f docker-compose.func-app-1.yaml -f docker-compose.httpapi.yaml --env-file .env.dev -p my-container-apps up --build --remove-orphans

# Teardown
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml --env-file .env.dev -p my-container-apps down
```

### Docker Image Builds

```bash
# FunctionApp1 (requires certs secrets and CERT_HASH build arg)
docker buildx build --platform linux/amd64 --progress plain \
  --build-arg BUILD_CONFIGURATION=Release \
  --build-arg CERT_HASH=$(cat ./certs/dev.crt ./certs/dev.key | sha256sum | cut -d' ' -f1) \
  --secret id=dev-crt,src=./certs/dev.crt \
  --secret id=dev-key,src=./certs/dev.key \
  -t my-func-app-1:1.0.0 -f src/FunctionApp1/Dockerfile .

# HttpApi (multi-platform)
docker buildx build --platform linux/amd64,linux/arm64 --progress plain \
  --build-arg BUILD_CONFIGURATION=Release \
  --secret id=dev-crt,src=./certs/dev.crt \
  --secret id=dev-key,src=./certs/dev.key \
  -t my-httpapi:1.0.0 -f src/HttpApi/Dockerfile .
```

### First-Time Setup

1. Copy `src/FunctionApp1/local.settings.template.json` → `local.settings.json`
2. Generate self-signed certificates into `/certs` directory and trust them
3. Create `.env.dev` from the template documented in README.md
4. Set `Users.InitContainer` secrets via .NET user secrets (section `Users:UsersInfrastructureCosmosDbOptions`):
   - `ConnectionString` — CosmosDB connection string
   - `Throughput` — autoscale RU/s (e.g. `400`)
   - `UseIntegratedCache` — `false` for local dev (Direct mode); `true` enables Gateway/Integrated Cache

## Architecture

### Services

- **FunctionApp1** (`src/FunctionApp1/`) — Azure Functions v4 Isolated, with HTTP and timer triggers. Exposes `/api/health`. Port
  7263 locally.
- **HttpApi** (`src/HttpApi/`) — ASP.NET Core Web API with Swagger. Health check at `/healthz`. HTTP: 5238 / HTTPS: 7125 locally.
- **Shared.Observability** (`src/Shared.Observability/`) — Shared NuGet-style library that wires up OpenTelemetry, Sentry, and
  Azure Monitor for both services. Both services reference this project.
- **Libraries.Shared.CosmosDb** (`src/Libraries.Shared.CosmosDb/`) — Reusable multi-database CosmosDB infrastructure
  (`CosmosDbConfigurator`, `CosmosDbClientProvider`, container/keys providers). Any module that needs CosmosDB references this
  project and calls `services.AddCosmosDb()`.

### Observability Pipeline

All telemetry flows through the OTEL Collector (`otel-collector-config.yaml`), which receives OTLP and fans out to Jaeger (
traces), Prometheus (metrics), and Seq (logs). The Aspire Dashboard (port 18888) is also available as a lightweight OTEL UI for
local dev.

### Infrastructure

Bicep templates in `/infrastructure/` deploy to Azure Container Apps environment with Azure Container Registry. Modules are under
`infrastructure/modules/`.

### Key Ports (local)

| Service          | Port  |
|------------------|-------|
| Aspire Dashboard | 18888 |
| Jaeger UI        | 16686 |
| Seq              | 5384  |
| Prometheus       | 9090  |
| cAdvisor         | 8083  |
| OTLP gRPC        | 4317  |
| OTLP HTTP        | 4318  |

### Versioning & Releases

Automated via `release-please` (`.github/workflows/release-please.yml`). Version is tracked in `version.txt` and
`.release-please-manifest.json`. Changelog sections are defined in `.release-please-config.json`.
