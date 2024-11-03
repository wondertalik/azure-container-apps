FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
ARG SSL_CRT_DIRECTORY=certs
ARG SSL_CRT_NAME=dev.crt
ARG SSL_KEY_NAME=dev.key
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

RUN apt-get update \
    && rm -rf /var/lib/apt/lists/*

COPY ${SSL_CRT_DIRECTORY}/${SSL_CRT_NAME} /usr/local/share/ca-certificates/$SSL_CRT_NAME
COPY ${SSL_CRT_DIRECTORY}/${SSL_KEY_NAME} /usr/local/share/dev/$SSL_KEY_NAME

RUN chmod 644 /usr/local/share/ca-certificates/$SSL_CRT_NAME

RUN cat /usr/local/share/ca-certificates/$SSL_CRT_NAME >> /etc/ssl/certs/ca-certificates.crt \
       && update-ca-certificates 

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release

WORKDIR /app

COPY ["src/HttpApi/HttpApi.csproj", "src/HttpApi/"]
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
