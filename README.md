# Introduction

This is a simple project to demonstrate how to use Azure Container Apps with .NET 8.

## DEVELOPMENT SETUP

### Install tools and add .env.dev

- install [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/)
- [Azure Functions Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- in the root directory, create the.env.dev file with

```.env.dev
VOLUMES_PATH=~/Works/var/azure-container-apps
HTTPS_CERT_PATH=~/Works/Training/AzureContainerApps/certs
HTTPS_CERT_NAME_CRT=dev4.crt
HTTPS_CERT_NAME_KEY=dev4.key
HTTPS_CERT_NAME_PEM=dev4.pem
DOTNET_ENVIRONMENT=Development

FUNCTION_APP_1_IMAGE=my-func-app-1:1.0.0
FUNCTION_APP_1_HTTP_PORT=7263
FUNCTION_APP_1_OLTP_NAME=FunctionApp1
FUNC_APP_1_SENTRY_DSN=
FUNC_APP_1_SENTRY_TRACES_SAMPLE_RATE=1.0

HTTPAPI_IMAGE=my-httpapi:1.0.0
HTTPAPI_HTTP_PORT=5238
HTTPAPI_HTTPS_PORT=7125
HTTPAPI_OLTP_NAME=HttpApi
HTTPAPI_SENTRY_DSN=
HTTPAPI_SENTRY_TRACES_SAMPLE_RATE=1.0

AZURITE_CONNECTION_STRING=AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;DefaultEndpointsProtocol=http;BlobEndpoint=http://azurite:10000/devstoreaccount1;QueueEndpoint=http://azurite:10001/devstoreaccount1;TableEndpoint=http://azurite:10002/devstoreaccount1;
APPLICATIONINSIGHTS_CONNECTION_STRING=

# observability
OTELCOL_URL=https://otel-collector:4317
OTEL_EXPORTER_OTLP_HEADERS=x-otlp-api-key=myprimaryapikey

#aspire dashboard
ASPIRE_DASHBOARD_OTLP_PRIMARYAPIKEY=myprimaryapikey
```

### Run during development

Authentication is required to pull images from Azure Container Registry (Optional).

```bash
az login
```

```bash
az acr login -n myexampleacrtst
```

We use [docker compose](https://docs.docker.com/compose/) to run dependencies.

From a root directory of the project run commands:

- run all services (azurite, aspire-dashboard, jaeger, prometheus, cAdvisor)

```bash
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml --env-file .env.dev -p my-container-apps up --build --remove-orphans
```

Will be available services:

- [Aspire Dashboard](http://localhost:18888)
- [Jaeger UI](http://localhost:16686)
- [Prometheus](http://localhost:9090)
- [cAdvisor](http://localhost:8083)

- stop and remove all services

```bash
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml --env-file .env.dev -p my-container-apps down
```

### Self-signed certificate

Before you start, you need to generate certificates

More detail in an
official [docs](https://learn.microsoft.com/en-us/dotnet/core/additional-tools/self-signed-certificates-guide#with-openssl)

```text
#!/bin/bash

PARENT="dev4"

# Array of DNS entries
DNS_ENTRIES=(
    "localhost"
    "seq"
    "jaeger"
    "cadvisor"
    "prometheus"
    "aspire-dashboard"
    "otel-collector"
    "httpapi"
    "func-app-1"
)

# Generate the DNS entries with proper numbering
DNS_SECTION=""
ORDER=1
for DNS in "${DNS_ENTRIES[@]}"; do
    DNS_SECTION+="DNS.${ORDER} = ${DNS}\n"
    ((ORDER++))
    DNS_SECTION+="DNS.${ORDER} = www.${DNS}\n"
    ((ORDER++))
done

openssl req \
-x509 \
-newkey rsa:4096 \
-sha256 \
-days 365 \
-nodes \
-keyout $PARENT.key \
-out $PARENT.crt \
-subj "/CN=${PARENT}" \
-extensions v3_ca \
-extensions v3_req \
-config <( \
  echo '[req]'; \
  echo 'default_bits= 4096'; \
  echo 'distinguished_name=req'; \
  echo 'x509_extension = v3_ca'; \
  echo 'req_extensions = v3_req'; \
  echo '[v3_req]'; \
  echo 'basicConstraints = CA:FALSE'; \
  echo 'keyUsage = nonRepudiation, digitalSignature, keyEncipherment'; \
  echo 'subjectAltName = @alt_names'; \
  echo '[ alt_names ]'; \
  echo -e "${DNS_SECTION}"; \
  echo '[v3_ca]'; \
  echo 'subjectKeyIdentifier=hash'; \
  echo 'authorityKeyIdentifier=keyid:always,issuer'; \
  echo 'basicConstraints = critical, CA:TRUE, pathlen:0'; \
  echo 'keyUsage = critical, cRLSign, keyCertSign'; \
  echo 'extendedKeyUsage = serverAuth, clientAuth')

openssl x509 -noout -text -in $PARENT.crt
```

Generate certificates

```bash
mkdir certs && cd certs
chmod +x certs.sh
./certs.sh
```

Install certs in your system

- on macOS from the root directory of the project run:

```bash
rm -rf ~/.aspnet
dotnet dev-certs https --trust --verbose

sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain certs/dev4.crt
sudo security import certs/dev4.key -k /Library/Keychains/System.keychain
```

## DevOps

In this section, we will build Docker images for the FunctionApp1 and HttpApi services and run them in containers.`

### Build and push Docker image

#### Local build

- build FunctionApp1 image

```bash
docker buildx build --platform linux/amd64 --progress plain --build-arg BUILD_CONFIGURATION=Release --secret id=dev-crt,src=./certs/dev4.crt --secret id=dev-key,src=./certs/dev4.key -t my-func-app-1:1.0.0 -f src/FunctionApp1/Dockerfile .
```

- build HttpApi image

```bash
docker buildx build --platform linux/amd64,linux/arm64 --progress plain --build-arg BUILD_CONFIGURATION=Release --secret id=dev-crt,src=./certs/dev4.crt --secret id=dev-key,src=./certs/dev4.key -t my-httpapi:1.0.0 -f src/HttpApi/Dockerfile .
```

- build ManualContainerAppJob image

```bash
docker buildx build --progress plain --platform linux/amd64,linux/arm64 --build-arg BUILD_CONFIGURATION=Release --secret id=dev-crt,src=./certs/dev4.crt --secret id=dev-key,src=./certs/dev4.key -t my-manual-container-app-job:1.0.0 -f src/ManualContainerAppJob/Dockerfile .
```

#### Build and push Docker images to Azure Container Registry (Optional)

Optionally, we can push the images to Azure Container Registry (ACR).
To do this, you need to have an ACR instance created and authenticated.

For this example, we will use the `myexampleacrtst` ACR instance. To build and push images to ACR, you need to
authenticate with ACR using the Azure CLI:

```bash
az acr login -n myexampleacrtst
```

Then, you can build and push the images using docker buildx

```bash
docker buildx build --platform linux/amd64 --progress plain --build-arg BUILD_CONFIGURATION=Release --secret id=dev-crt,src=./certs/dev4.crt --secret id=dev-key,src=./certs/dev4.key --push -t myexampleacrtst.azurecr.io/my-func:1.0.0 -f src/FunctionApp1/Dockerfile .
```

### Run to check is everything works with containers

- run all services in containers

```bash
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml -f docker-compose.func-app-1.yaml -f docker-compose.httpapi.yaml -f docker-compose.manual-container-app-job.yaml --env-file .env.dev -p my-container-apps up --build --remove-orphans 
```

Additional services will be available:

- [HttpApi](https://localhost:7125/swagger/index.html)

- stop containers and remove containers

```bash
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml -f docker-compose.func-app-1.yaml -f docker-compose.httpapi.yaml --env-file .env.dev -p my-container-apps down
```