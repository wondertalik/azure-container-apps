# Introduction

This is a simple project to demonstrate how to use Azure Functions with .NET 8.

## DEVELOPMENT SETUP

### Install tools and add .env.dev

- install [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/)
- [Azure Functions Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- in root directory create .env.dev file with

```.env.dev
VOLUMES_PATH=~/Works/var/my-func-tst

ASPNETCORE_ENVIRONMENT=Development
EXAMPLE_FUNCION_APP1_IMAGE=myexampleacrtst.azurecr.io/my-func-tst:0.0.2-release
EXAMPLE_FUNCION_APP1_HTTP_PORT=7099
EXAMPLE_FUNCION_APP1_OLTP_APP_NAME=example-funcion-app1

# well-known connection strings from https://learn.microsoft.com/en-us/azure/storage/common/storage-configure-connection-string#configure-a-connection-string-for-azurite
AZURITE_CONNECTION_STRING=DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;QueueEndpoint=http://azurite:10001/devstoreaccount1;TableEndpoint=http://azurite:10002/devstoreaccount1;

# observability
ASPIRE_DASHBOARD_OTLP_PRIMARYAPIKEY=myprimaryapikey
```

### Run during development

Authentication is required to pull images from Azure Container Registry.

```bash
az login
```

```bash
az acr login -n myexampleacrtst
```

We use [docker compose](https://docs.docker.com/compose/) to run dependencies.

From a root directory of project run commands:

- run all services (azurite, aspire-dashboard)

```bash
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml --env-file .env.dev -p my-func-tst up --build --remove-orphans
```

- stop and remove all services

```bash
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml --env-file .env.dev -p my-func-tst down
```

## DevOps

This is a temporary solution to build and deploy application to Azure Container Registry and Azure Container Apps. 
The final solution will be implemented in CI/CD.

### Build and push Docker image

```bash
az login
```

```bash
az acr login -n myexampleacrtst
```

- build Example.FunctionApp1 image

```bash
docker buildx build --platform linux/amd64 --progress plain --build-arg BUILD_CONFIGURATION=Release --push -t myexampleacrtst.azurecr.io/my-func-tst:0.0.2-release -f src/Example.FunctionApp1/Dockerfile .
```

### Run to check is everything works with containers

- run all services in containers

```bash
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml -f docker-compose.func.yaml --env-file .env.dev -p my-func-tst up --build --remove-orphans 
```

Will be available services:

- [Aspire Dashboard](http://localhost:18888)

- stop containers

```bash
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml -f docker-compose.func.yaml --env-file .env.dev -p my-func-tst stop
```

- stop containers and remove containers

```bash
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml -f docker-compose.func.yaml --env-file .env.dev -p my-func-tst down
```