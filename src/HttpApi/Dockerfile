FROM mcr.microsoft.com/dotnet/aspnet:8.0.18 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# install necessary packages for the application (optional)
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

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

COPY ["./src/HttpApi/HttpApi.csproj", "src/HttpApi/"]
COPY ["./src/Shared.Observability/Shared.Observability.csproj", "src/Shared.Observability/"]

RUN dotnet restore "src/HttpApi/HttpApi.csproj"
COPY . .
WORKDIR "/app/src/HttpApi"
RUN dotnet build "HttpApi.csproj" -c $BUILD_CONFIGURATION -a $TARGETARCH -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "HttpApi.csproj" -c $BUILD_CONFIGURATION -a $TARGETARCH -o /app/publish /p:UseAppHost=false

FROM base AS final
USER $APP_UID
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HttpApi.dll"]
