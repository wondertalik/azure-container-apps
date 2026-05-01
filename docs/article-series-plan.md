# Article Series Plan: Building Production-Ready .NET Apps on Azure Container Apps

> A practical, code-first series that walks through every major Azure Container Apps workload type —
> REST APIs, GraphQL APIs, container jobs, and Azure Functions — with real infrastructure (Bicep),
> real tests, real DevOps pipelines, and AI-assisted development workflows.

---

## Series Overview

| # | Title | Status |
|---|-------|--------|
| 1 | [Repo Setup & Local Development Environment](#1-repo-setup--local-development-environment) | 🔲 Planned |
| 2 | [RESTful API on Azure Container Apps with .NET](#2-restful-api-on-azure-container-apps-with-net) | 🔲 Planned |
| 3 | [GraphQL API with HotChocolate on Azure Container Apps](#3-graphql-api-with-hotchocolate-on-azure-container-apps) | 🔲 Planned |
| 4 | [Enterprise Observability: OpenTelemetry, Sentry & Azure Monitor](#4-enterprise-observability-opentelemetry-sentry--azure-monitor) | 🔲 Planned |
| 5 | [Azure Container App Jobs: Manual, Scheduled & Event-Driven](#5-azure-container-app-jobs-manual-scheduled--event-driven) | 🔲 Planned |
| 6 | [Azure Functions on Container Apps: Timer, HTTP & Service Bus](#6-azure-functions-on-container-apps-timer-http--service-bus) | 🔲 Planned |
| 7 | [Testing .NET Apps: Unit Tests, Integration Tests & Coverage Reports](#7-testing-net-apps-unit-tests-integration-tests--coverage-reports) | 🔲 Planned |
| 8 | [Code Quality & Formatting in .NET](#8-code-quality--formatting-in-net) | 🔲 Planned |
| 9 | [CI/CD with GitHub Actions: Build, Test & Deploy to Azure](#9-cicd-with-github-actions-build-test--deploy-to-azure) | 🔲 Planned |
| 10 | [AI-Assisted Development: Claude & GitHub Copilot CLI](#10-ai-assisted-development-claude--github-copilot-cli) | 🔲 Planned |
| 11 | [AI Agents & MCP Servers on Azure Container Apps](#11-ai-agents--mcp-servers-on-azure-container-apps) | 🔲 Planned |

---

## Articles

### 1. Repo Setup & Local Development Environment

**Subtitle:** _How to structure a multi-service .NET monorepo for Azure Container Apps and run the full stack locally_

**Summary:**
The series starts by walking through the repository structure, the tools needed (Docker Desktop, Azure CLI, Azure Functions Core Tools), and how to get a full local development environment running from a single `docker compose` command. We cover self-signed certificate generation for HTTPS, the `.env.dev` convention for local secrets, and how the observability stack (OTEL Collector → Jaeger, Prometheus, Seq, Aspire Dashboard) fits together before a single line of application code is written. We also introduce how `CLAUDE.md` and GitHub Copilot CLI instructions live in the repo to give AI assistants the right context from day one.

**Key topics:**
- Repository layout: `src/`, `tests/`, `infrastructure/`, `docs/`, `.github/`
- Docker Compose multi-file composition pattern
- Self-signed certificates with OpenSSL for local HTTPS
- `.env.dev` for local environment variables
- Observability stack: OTEL Collector, Jaeger, Prometheus, Seq, Aspire Dashboard
- `CLAUDE.md` and Copilot CLI instructions setup

**Repo references:**
- `docker-compose.yaml`, `docker-compose.observability.yaml`
- `otel-collector-config.yaml`, `prometheus.yml`
- `certs/certs.sh`
- `.env.dev`
- `CLAUDE.md`

---

### 2. RESTful API on Azure Container Apps with .NET

**Subtitle:** _From ASP.NET Core Web API to a production-grade container app with Bicep infrastructure_

**Summary:**
We build and deploy the `HttpApi` service — an ASP.NET Core Web API with Swagger, health checks, and OpenTelemetry instrumentation. The article covers writing a multi-stage Dockerfile with build secrets for certificates, deploying to Azure using Bicep (resource group, Container Apps environment, Azure Container Registry, user-assigned managed identity, Key Vault), and configuring health probes and autoscaling rules. We walk through what each Bicep module does and why, so readers understand the infrastructure as well as they understand the code.

**Key topics:**
- ASP.NET Core Web API with controllers, Swagger/OpenAPI, and `/healthz`
- Multi-stage Dockerfile with `--secret` for certificates
- Bicep modules: resource group, ACA environment, ACR, Key Vault, managed identity
- `azure-container-app-helper.bicep` pattern: reusable app module
- Health probes (liveness, readiness) on ACA
- Scaling rules: min/max replicas
- Deploying with `az deployment sub create`

**Repo references:**
- `src/HttpApi/`
- `src/HttpApi/Dockerfile`
- `infrastructure/main.bicep`
- `infrastructure/modules/`
- `infrastructure/modules/helpers/azure-container-app-helper.bicep`

---

### 3. GraphQL API with HotChocolate on Azure Container Apps

**Subtitle:** _Building a code-first GraphQL API with HotChocolate and deploying it as a container app_

**Summary:**
We add a second service — a GraphQL API built with the HotChocolate library — to demonstrate running multiple container apps in the same environment. The article covers HotChocolate's code-first approach: defining types, queries, mutations, and subscriptions in C#. We configure Banana Cake Pop (the HotChocolate IDE) for development, wire up the same OpenTelemetry observability, and extend the Bicep templates to deploy a second container app. We also cover how to version a GraphQL schema and think about breaking changes.

**Key topics:**
- HotChocolate code-first schema: object types, query/mutation/subscription
- Banana Cake Pop for local development
- DataLoader pattern for N+1 prevention
- OpenTelemetry integration with HotChocolate
- Extending `main.bicep` to add a second container app
- GraphQL schema versioning considerations

**Repo references:**
- `src/GraphQL/` _(to be built)_
- `infrastructure/modules/helpers/azure-container-app-helper.bicep`

---

### 4. Enterprise Observability: OpenTelemetry, Sentry & Azure Monitor

**Subtitle:** _A shared observability library that wires up traces, metrics, and logs across all your .NET services_

**Summary:**
Good observability shouldn't be copy-pasted across services. This article deep-dives into `Shared.Observability` — a project-local NuGet-style library that every service references. We explain how OpenTelemetry SDK is configured once and reused, how the OTEL Collector fans out to multiple backends (Jaeger for traces, Prometheus for metrics, Seq for structured logs), and how Azure Monitor and Sentry integrate into the same pipeline. We show real traces flowing from the `HttpApi` through to Jaeger and Application Insights.

**Key topics:**
- `Shared.Observability` library design: extension methods for `IServiceCollection`
- OpenTelemetry SDK: TracerProvider, MeterProvider, LoggerProvider
- OTLP exporter → OTEL Collector → Jaeger / Prometheus / Seq
- Azure Monitor exporter (`AddAzureMonitor`)
- Sentry integration via `traceBuilder.AddSentry()`
- Custom activity sources and instrumentation classes
- Local observability dashboards walkthrough

**Repo references:**
- `src/Shared.Observability/`
- `src/HttpApi/Diagnostics/HttpApiInstrumentation.cs`
- `src/FunctionApp1/Diagnostics/FunctionApp1Instrumentation.cs`
- `otel-collector-config.yaml`
- `infrastructure/modules/telemetry.bicep`

---

### 5. Azure Container App Jobs: Manual, Scheduled & Event-Driven

**Subtitle:** _When your workload isn't a long-running service — Container App Jobs explained with .NET Worker examples_

**Summary:**
Container App Jobs are a first-class concept in ACA for workloads that run to completion: data migrations, report generation, queue drains, and so on. This article covers all three job trigger types — manual (triggered via API or CLI), scheduled (cron), and event-driven (KEDA scalers) — and builds a .NET Worker Service for each. We extend the Bicep templates to define job resources and show how to pass parameters to manual jobs at invocation time.

**Key topics:**
- ACA Jobs vs Container Apps: when to use each
- .NET Worker Service as a job: `IHostedService` + exit codes
- Manual job: triggering via `az containerapp job start`, passing arguments
- Scheduled job: cron expression in Bicep
- Event-driven job: KEDA Azure Queue scaler, message processing
- `ManualContainerAppJob` project walkthrough
- Bicep for `Microsoft.App/jobs` resource type
- Job execution history and log streaming

**Repo references:**
- `src/ManualContainerAppJob/` _(to be built)_
- `infrastructure/modules/` _(jobs module to be added)_

---

### 6. Azure Functions on Container Apps: Timer, HTTP & Service Bus

**Subtitle:** _Running Azure Functions v4 Isolated in containers on ACA — three trigger types, one Dockerfile_

**Summary:**
Azure Functions running on Container Apps give you the full Functions programming model (triggers, bindings, durable functions) with the flexibility of custom containers and the ACA infrastructure. This article covers Azure Functions v4 Isolated process model, containerizing it with a Dockerfile, and implementing all three trigger types used in the series: timer (cron), HTTP (direct invocation), and Azure Service Bus (event-driven). We cover `local.settings.json` conventions, Azurite for local Azure Storage emulation, and the `azure-function-container-app-helper.bicep` module for deploying to ACA.

**Key topics:**
- Azure Functions v4 Isolated process model vs in-process
- `TimerTriggerOne`: cron schedule, `TimerInfo.ScheduleStatus`
- HTTP trigger: function-level auth, request/response binding
- Service Bus trigger: message processing, dead-letter handling
- `local.settings.json` vs `appsettings.json` — what goes where
- Azurite for local Azure Storage/Queue emulation
- Multi-stage Dockerfile with `CERT_HASH` build arg
- `azure-function-container-app-helper.bicep` explained
- Elastic scaling on ACA for Functions

**Repo references:**
- `src/FunctionApp1/`
- `src/FunctionApp1/Functions/TimerTriggerOne.cs`
- `src/FunctionApp1/Dockerfile`
- `infrastructure/modules/helpers/azure-function-container-app-helper.bicep`
- `docker-compose.func-app-1.yaml`

---

### 7. Testing .NET Apps: Unit Tests, Integration Tests & Coverage Reports

**Subtitle:** _xUnit, WebApplicationFactory, Testcontainers, and beautiful HTML coverage reports with ReportGenerator_

**Summary:**
This article establishes a full testing strategy for the services in this series. We build unit tests for the domain logic, integration tests for the `HttpApi` using `WebApplicationFactory` (spinning up the real ASP.NET Core pipeline in-process), and heavier integration tests using Testcontainers to run real dependencies (Azure Service Bus emulator, Azurite) in Docker. We configure `dotnet test` with coverage collection (Coverlet) and generate rich HTML reports with ReportGenerator. Finally, we set up a `tests/` project structure that's easy to navigate and extend.

**Key topics:**
- `xUnit` project setup: `[Fact]`, `[Theory]`, test fixtures
- `WebApplicationFactory<Program>` for integration tests
- `Microsoft.AspNetCore.Mvc.Testing` — overriding configuration for tests
- Testcontainers for .NET: real dependencies in Docker during tests
- Coverlet for code coverage collection (`--collect:"XPlat Code Coverage"`)
- ReportGenerator: HTML, Cobertura, and badge generation
- Test project naming conventions and solution structure
- `HttpApi.IntegrationTests` walkthrough

**Repo references:**
- `tests/HttpApi.IntegrationTests/` _(to be built)_
- `src/HttpApi/Program.cs` (partial class for test entry point)

---

### 8. Code Quality & Formatting in .NET

**Subtitle:** _EditorConfig, CSharpier, Roslyn analyzers, and enforcing standards automatically_

**Summary:**
Code quality is a process, not a one-time task. This article sets up the full quality toolchain for a .NET solution: `.editorconfig` for IDE consistency, CSharpier for opinionated formatting (no arguments, just format), Roslyn analyzers (including `Microsoft.CodeAnalysis.NetAnalyzers`) for static analysis, and a `Directory.Build.props` that applies settings to every project automatically. We add a GitHub Actions check that fails the build if any file is incorrectly formatted or any analyzer rule is violated — making quality enforcement automatic rather than manual.

**Key topics:**
- `.editorconfig` — indent style, line endings, C# conventions
- CSharpier: `dotnet csharpier .` and `--check` mode
- `Directory.Build.props` for solution-wide analyzer configuration
- `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`
- Roslyn analyzers: `Microsoft.CodeAnalysis.NetAnalyzers`
- `StyleCop.Analyzers` (optional but common)
- Suppression files and `GlobalSuppressions.cs`
- GitHub Actions step: format check + analyzer check

**Repo references:**
- `.editorconfig` _(to be added)_
- `Directory.Build.props` _(to be added)_
- `AzureContainerApps.sln`

---

### 9. CI/CD with GitHub Actions: Build, Test & Deploy to Azure

**Subtitle:** _A complete GitHub Actions pipeline: build, test, coverage, Docker push to ACR, and Bicep deploy_

**Summary:**
We build the full CI/CD pipeline that puts every previous article's work together. The CI workflow: checkout → restore → build → test with coverage → publish ReportGenerator report as a workflow artifact → fail on analyzer violations. The CD workflow: build multi-platform Docker images → push to Azure Container Registry → deploy Bicep templates → verify health endpoints post-deploy. We also integrate `release-please` for automated semantic versioning and CHANGELOG generation, and cover secrets management (OIDC federated credentials vs service principal).

**Key topics:**
- GitHub Actions workflow structure: triggers, jobs, steps, matrix
- OIDC federated credentials for Azure authentication (no secrets rotation)
- `az acr build` vs `docker buildx` + `docker push`
- Reusable workflows and composite actions
- `release-please` configuration: conventional commits, version bumping
- Environment protection rules and deployment approvals
- Health check verification post-deployment
- Coverage report as workflow artifact + PR comment

**Repo references:**
- `.github/workflows/` _(to be extended)_
- `.github/workflows/release-please.yml`
- `.release-please-config.json`
- `infrastructure/runs/test-dev.sh`, `test-dev-jobs.sh`

---

### 10. AI-Assisted Development: Claude & GitHub Copilot CLI

**Subtitle:** _Making your repo AI-native: CLAUDE.md, custom skills, slash commands, and Copilot CLI workflows_

**Summary:**
Modern development workflows increasingly involve AI assistants. This article shows how to make the repository itself AI-native — not just by adding a README, but by creating structured instructions, skills, and commands that Claude and GitHub Copilot CLI can use to assist with repo-specific tasks. We cover `CLAUDE.md` conventions (architecture, commands, conventions), creating custom Claude skills for common tasks (generating new ACA modules, writing Bicep, scaffolding tests), and Copilot CLI custom commands. We show practical examples: asking Claude to scaffold a new service, asking Copilot to generate an integration test, and running custom slash commands.

**Key topics:**
- `CLAUDE.md`: architecture overview, common commands, conventions
- Custom Claude skills: scaffolding new container apps, generating Bicep modules
- GitHub Copilot CLI: `gh copilot suggest`, `gh copilot explain`
- Copilot CLI custom commands (`.github/copilot-instructions.md`)
- Practical AI workflows: "add a new container app job", "write an integration test for X"
- `.claude/` directory: skills, commands, settings
- When to use AI vs when to write it yourself

**Repo references:**
- `CLAUDE.md`
- `.claude/` _(to be added)_
- `.github/copilot-instructions.md` _(to be added)_

---

### 11. AI Agents & MCP Servers on Azure Container Apps

**Subtitle:** _Deploying a Semantic Kernel AI agent and a Model Context Protocol server as container apps_

**Summary:**
The final article takes everything built in the series and applies it to the fastest-growing workload type on ACA: AI agents and MCP servers. We build a Semantic Kernel-based agent that orchestrates calls to the `HttpApi` and the GraphQL API, exposes a chat endpoint, and runs as a container app. We then build a Model Context Protocol (MCP) server — a standardized interface that lets AI assistants (Claude, Copilot) call your internal APIs as tools. Both are deployed to ACA using the same Bicep patterns established earlier. We close with practical considerations: rate limiting, auth, token budgets, and scaling AI workloads on ACA.

**Key topics:**
- Microsoft Semantic Kernel: kernel setup, plugins, function calling
- Agent loop pattern: planner → tool call → response
- Building an MCP server with ASP.NET Core (tools, resources, prompts)
- Hosting MCP server as a container app with Bicep
- Connecting Claude / Copilot CLI to the MCP server
- Azure OpenAI integration via Semantic Kernel connector
- Auth for AI endpoints: API key, managed identity
- Scaling AI workloads: concurrency limits, KEDA HTTP scaler
- Rate limiting and token budget management

**Repo references:**
- `src/` _(AI agent project to be added)_
- `src/` _(MCP server project to be added)_
- `infrastructure/modules/helpers/azure-container-app-helper.bicep`

---

## What Needs to Be Built

The following projects are scaffolded in the repo but not yet implemented, or need to be created:

| Project | Article | Status |
|---------|---------|--------|
| `src/GraphQL/` | Article 3 | Scaffolded, empty |
| `src/ManualContainerAppJob/` | Article 5 | Scaffolded, empty |
| Service Bus + HTTP trigger in `src/FunctionApp1/` | Article 6 | Partial (only timer trigger exists) |
| `tests/HttpApi.IntegrationTests/` | Article 7 | Scaffolded, empty |
| `.editorconfig`, `Directory.Build.props` | Article 8 | Not added |
| CI/CD workflows in `.github/workflows/` | Article 9 | Only release-please exists |
| `.claude/` skills & Copilot instructions | Article 10 | Not added |
| AI agent + MCP server projects | Article 11 | Not added |

---

## Publishing Notes

- All articles publish to **Medium** under the series tag `Azure Container Apps .NET`
- Each article links to the GitHub repo and references the specific folder/tag for that article
- Code samples in articles use real code from the repo — no artificial toy examples
- Each article is self-contained enough to read standalone, but rewards reading in order
- Target length: 8–15 min read per article
- Article markdown source lives in `docs/articles/` (one file per article)
