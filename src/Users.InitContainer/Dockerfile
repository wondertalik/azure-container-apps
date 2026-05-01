FROM mcr.microsoft.com/dotnet/runtime:8.0.16 AS base
WORKDIR /app

RUN apt-get update && apt-get install -y ca-certificates && rm -rf /var/lib/apt/lists/*

# Install SSL certificate and key using Docker secrets
ARG CERT_HASH
RUN --mount=type=secret,id=dev-crt,target=/tmp/dev.crt \
    --mount=type=secret,id=dev-key,target=/tmp/dev.key \
    echo "cert-hash:${CERT_HASH}" && \
    mkdir -p /usr/local/share/dev && \
    cp /tmp/dev.crt /usr/local/share/ca-certificates/dev.crt && \
    cp /tmp/dev.key /usr/local/share/dev/dev.key && \
    chmod 644 /usr/local/share/ca-certificates/dev.crt && \
    chmod 644 /usr/local/share/dev/dev.key && \
    cat /usr/local/share/ca-certificates/dev.crt >> /etc/ssl/certs/ca-certificates.crt && \
    update-ca-certificates

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0.420 AS build
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release

WORKDIR /app

COPY ["src/Users.InitContainer/Users.InitContainer.csproj", "src/Users.InitContainer/"]
COPY ["src/Users.InitContainer.Data/Users.InitContainer.Data.csproj", "src/Users.InitContainer.Data/"]
COPY ["src/Users.Infrastructure.CosmosDb/Users.Infrastructure.CosmosDb.csproj", "src/Users.Infrastructure.CosmosDb/"]
COPY ["src/Users.Infrastructure.CosmosDb.Migrations/Users.Infrastructure.CosmosDb.Migrations.csproj", "src/Users.Infrastructure.CosmosDb.Migrations/"]
COPY ["src/Users.Infrastructure.Contracts/Users.Infrastructure.Contracts.csproj", "src/Users.Infrastructure.Contracts/"]
COPY ["src/Users.Infrastructure.Entities/Users.Infrastructure.Entities.csproj", "src/Users.Infrastructure.Entities/"]
COPY ["src/Users.Authorization.Constants/Users.Authorization.Constants.csproj", "src/Users.Authorization.Constants/"]
COPY ["src/Users.Shared/Users.Shared.csproj", "src/Users.Shared/"]
COPY ["src/Libraries.Shared/Libraries.Shared.csproj", "src/Libraries.Shared/"]
COPY ["src/Libraries.Shared.CosmosDb/Libraries.Shared.CosmosDb.csproj", "src/Libraries.Shared.CosmosDb/"]

RUN dotnet restore "src/Users.InitContainer/Users.InitContainer.csproj" -a $TARGETARCH
COPY ./src/ ./src/

RUN dotnet build "src/Users.InitContainer/Users.InitContainer.csproj" -a $TARGETARCH -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "src/Users.InitContainer/Users.InitContainer.csproj" -a $TARGETARCH -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
USER $APP_UID
WORKDIR /app
COPY --from=publish /app/publish .
COPY --chmod=755 src/Users.InitContainer/entrypoint.sh /usr/local/bin/entrypoint.sh
ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
