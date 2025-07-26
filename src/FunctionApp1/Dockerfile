# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4.1040.300-3-dotnet-isolated8.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 8080

# install necessary packages for the application (optional)
RUN apt-get update \
    && apt-get install -y --no-install-recommends wget \
    && rm -rf /var/lib/apt/lists/*

# Install SSL certificate and key using Docker secrets
RUN --mount=type=secret,id=dev-crt,target=/tmp/dev.crt \
    --mount=type=secret,id=dev-key,target=/tmp/dev.key \
    mkdir -p /usr/local/share/dev && \
    cp /tmp/dev.crt /usr/local/share/ca-certificates/dev.crt && \
    cp /tmp/dev.key /usr/local/share/dev/dev.key && \
    chmod 644 /usr/local/share/ca-certificates/dev.crt && \
    cat /usr/local/share/ca-certificates/dev.crt >> /etc/ssl/certs/ca-certificates.crt && \
    update-ca-certificates

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0.412 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /app

COPY ["./src/FunctionApp1/FunctionApp1.csproj", "src/FunctionApp1/"]
COPY ["./src/Shared.Observability/Shared.Observability.csproj", "src/Shared.Observability/"]

RUN dotnet restore "src/FunctionApp1/FunctionApp1.csproj"
COPY ./src src
WORKDIR "/app/src/FunctionApp1"
RUN dotnet build "./FunctionApp1.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FunctionApp1.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true