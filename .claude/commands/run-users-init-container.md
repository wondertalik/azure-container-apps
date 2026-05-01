Run the Users init container locally against the CosmosDB emulator.

Steps:
1. Check the emulator is running and healthy:
   `docker ps --filter "name=cosmosdb-emulator" --format "{{.Status}}"`
   If not healthy, print instructions to start it and stop.

2. Load the connection string and database name from `.env.dev`:
   Parse `COSMOSDB_CONNECTION_STRING` and `USERS_COSMOSDB_DATABASE` from `.env.dev`.

3. Run the init container:
   ```
   ASPNETCORE_ENVIRONMENT=dev \
   UsersDropDatabaseIfExists=false \
   Users__UsersInfrastructureCosmosDbOptions__ConnectionString="<from .env.dev COSMOSDB_CONNECTION_STRING>" \
   Users__UsersInfrastructureCosmosDbOptions__DatabaseId="<from .env.dev USERS_COSMOSDB_DATABASE>" \
   Users__UsersInfrastructureCosmosDbOptions__Throughput=400 \
   Users__UsersInfrastructureCosmosDbOptions__UseIntegratedCache=false \
   Users__UsersInfrastructureCosmosDbOptions__IgnoreSslCertificateValidation=true \
   SeederOptions__TenantsSeed=true \
   SeederOptions__UsersSeed=true \
   SeederOptions__SeedDataFilePath=./src/Users.InitContainer.Data/SeedData \
   dotnet run --project src/Users.InitContainer/Users.InitContainer.csproj
   ```

4. Report the exit code and any errors. If exit code is non-zero, show the last 30 lines of output.
