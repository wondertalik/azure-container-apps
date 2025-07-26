FROM mcr.microsoft.com/dotnet/aspnet:8.0.18 AS base
WORKDIR /app
# Install ca-certificates for certificate management
RUN apt-get update && apt-get install -y ca-certificates && rm -rf /var/lib/apt/lists/*

# Install SSL certificate and key using Docker secrets
RUN --mount=type=secret,id=dev-crt,target=/tmp/dev.crt \
    --mount=type=secret,id=dev-key,target=/tmp/dev.key \
    mkdir -p /usr/local/share/dev && \
    cp /tmp/dev.crt /usr/local/share/ca-certificates/dev.crt && \
    cp /tmp/dev.key /usr/local/share/dev/dev.key && \
    chmod 644 /usr/local/share/ca-certificates/dev.crt && \
    chmod 644 /usr/local/share/dev/dev.key && \
    cat /usr/local/share/ca-certificates/dev.crt >> /etc/ssl/certs/ca-certificates.crt && \
    update-ca-certificates

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0.412 AS build
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release

WORKDIR /app

COPY ["./src/ManualContainerAppJob/ManualContainerAppJob.csproj", "src/ManualContainerAppJob/"]
COPY ["./src/Shared.Observability/Shared.Observability.csproj", "src/Shared.Observability/"]

RUN dotnet restore "src/ManualContainerAppJob/ManualContainerAppJob.csproj"
COPY . .
WORKDIR "/app/src/ManualContainerAppJob"
RUN dotnet build "ManualContainerAppJob.csproj" -c $BUILD_CONFIGURATION -a $TARGETARCH -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ManualContainerAppJob.csproj" -c $BUILD_CONFIGURATION -a $TARGETARCH -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Copy and set up entrypoint script for certificate installation
COPY src/ManualContainerAppJob/entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh
ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
