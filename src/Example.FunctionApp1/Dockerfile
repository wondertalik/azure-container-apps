FROM mcr.microsoft.com/dotnet/sdk:8.0 AS installer-env

COPY ./src/Example.FunctionApp1 /src/Example.FunctionApp1
RUN cd /src/Example.FunctionApp1 && \
mkdir -p /home/site/wwwroot && \
dotnet publish Example.FunctionApp1.csproj --output /home/site/wwwroot

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0-appservice
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0
USER $APP_UID
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]