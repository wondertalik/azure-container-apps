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
HTTPS_CERT_PATH=~/Works/study/AzureContainerApps/certs
HTTPS_CERT_NAME_CRT=dev4.crt
HTTPS_CERT_NAME_KEY=dev4.key
DOTNET_ENVIRONMENT=Development

FUNCTION_APP1_IMAGE=myexampleacrtst.azurecr.io/my-func-tst:2.0.8-release
FUNCTION_APP1_HTTP_PORT=7263
FUNCTION_APP1_OLTP_NAME=FunctionApp1

HTTPAPI_IMAGE=myexampleacrtst.azurecr.io/my-httpapi-tst:1.0.2-release
HTTPAPI_HTTP_PORT=5238
HTTPAPI_HTTPS_PORT=7125
HTTPAPI_OLTP_NAME=HttpApi

AZURITE_CONNECTION_STRING=
APPLICATIONINSIGHTS_CONNECTION_STRING=DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;QueueEndpoint=http://azurite:10001/devstoreaccount1;TableEndpoint=http://azurite:10002/devstoreaccount1;

# observability
ASPIRE_DASHBOARD_OTLP_PRIMARYAPIKEY=myprimaryapikey
#aspire dashboard
OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:18889
OTEL_EXPORTER_OTLP_HEADERS=x-otlp-api-key=myprimaryapikey
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
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml --env-file .env.dev -p my-container-apps up --build --remove-orphans
```

- stop and remove all services

```bash
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml --env-file .env.dev -p my-container-apps down
```

### Self-signed certificate

Before start necessary generate self-signed certificated.
More detail in an
official [docs](https://learn.microsoft.com/en-us/dotnet/core/additional-tools/self-signed-certificates-guide#with-openssl)

```text
#!/bin/bash

PARENT="dev4"

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
  echo "DNS.1 = www.localhost"; \
  echo "DNS.2 = localhost"; \
  echo "DNS.3 = www.httpapi"; \
  echo "DNS.4 = httpapi"; \
  echo '[ v3_ca ]'; \
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

- on Mac OS

```bash
rm -rf ~/.aspnet
dotnet dev-certs https --trust --verbose

sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain certs/dev4.crt
sudo security import certs/dev4.key -k /Library/Keychains/System.keychain
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

- build FunctionApp1 image

```bash
docker buildx build --platform linux/amd64 --progress plain --build-arg BUILD_CONFIGURATION=Release --push -t myexampleacrtst.azurecr.io/my-func-tst:2.0.10-release -f src/FunctionApp1/Dockerfile .
```

- build HttpApi image

```bash
docker buildx build --platform linux/amd64,linux/arm64 --progress plain --build-arg SSL_CRT_DIRECTORY=certs --build-arg SSL_CRT_NAME=dev4.crt --build-arg SSL_KEY_NAME=dev4.key --build-arg BUILD_CONFIGURATION=Release --push -t myexampleacrtst.azurecr.io/my-httpapi-tst:1.0.4-release -f src/HttpApi/Dockerfile .
```

### Run to check is everything works with containers

- run all services in containers

```bash
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml -f docker-compose.func.yaml -f docker-compose.httpapi.yaml --env-file .env.dev -p my-container-apps up --build --remove-orphans 
```

Will be available services:

- [Aspire Dashboard](http://localhost:18888)
- [HttpApi](https://localhost:7125/swagger/index.html)

- stop containers

```bash
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml -f docker-compose.func.yaml -f docker-compose.httpapi.yaml --env-file .env.dev -p my-container-apps stop
```

- stop containers and remove containers

```bash
docker compose -f docker-compose.yaml -f docker-compose.observability.yaml -f docker-compose.func.yaml -f docker-compose.httpapi.yaml --env-file .env.dev -p my-container-apps down
```