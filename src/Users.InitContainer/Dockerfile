FROM mcr.microsoft.com/dotnet/runtime:8.0.16 AS base
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0.420 AS build
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release

WORKDIR /app

COPY ["./src/Users.InitContainer/Users.InitContainer.csproj", "src/Users.InitContainer/"]
COPY ["./src/Users.InitContainer.Data/Users.InitContainer.Data.csproj", "src/Users.InitContainer.Data/"]
COPY ["./src/Users.Infrastructure.CosmosDb/Users.Infrastructure.CosmosDb.csproj", "src/Users.Infrastructure.CosmosDb/"]
COPY ["./src/Users.Infrastructure.Contracts/Users.Infrastructure.Contracts.csproj", "src/Users.Infrastructure.Contracts/"]
COPY ["./src/Users.Infrastructure.Entities/Users.Infrastructure.Entities.csproj", "src/Users.Infrastructure.Entities/"]
COPY ["./src/Users.Authorization.Constants/Users.Authorization.Constants.csproj", "src/Users.Authorization.Constants/"]
COPY ["./src/Users.Shared/Users.Shared.csproj", "src/Users.Shared/"]
COPY ["./src/Libraries.Shared/Libraries.Shared.csproj", "src/Libraries.Shared/"]

RUN dotnet restore "src/Users.InitContainer/Users.InitContainer.csproj"
COPY . .
WORKDIR "/app/src/Users.InitContainer"
RUN dotnet build "Users.InitContainer.csproj" -c $BUILD_CONFIGURATION -a $TARGETARCH -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Users.InitContainer.csproj" -c $BUILD_CONFIGURATION -a $TARGETARCH -o /app/publish /p:UseAppHost=false

FROM base AS final
USER $APP_UID
WORKDIR /app
COPY --from=publish /app/publish .
COPY ["./src/Users.InitContainer/entrypoint.sh", "/entrypoint.sh"]
RUN chmod +x /entrypoint.sh
ENTRYPOINT ["/entrypoint.sh"]
