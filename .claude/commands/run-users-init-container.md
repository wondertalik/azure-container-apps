Run the Users init container locally against the Docker Compose CosmosDB emulator.

Steps:
1. Check the emulator is running and healthy:
   `docker ps --filter "name=cosmosdb-emulator" --format "{{.Status}}"`
   If not healthy, print instructions to start it and stop.

2. Load the connection string from `.env.dev`:
   Parse `COSMOSDB_CONNECTION_STRING` and `USERS_COSMOSDB_DATABASE` from `.env.dev`.

3. Run the init container:
   ```
   ASPNETCORE_ENVIRONMENT=dev \
   Users__CosmosDb__ConnectionString="<from .env.dev>" \
   Users__CosmosDb__DatabaseName="<from .env.dev>" \
   Users__CosmosDb__IgnoreSslCertificateValidation=true \
   Seeder__TenantsSeed=true \
   Seeder__UsersSeed=true \
   Seeder__SeedDataFilePath=./src/Users/Users.InitContainer.Data/SeedData/users-db \
   dotnet run --project src/Users/Users.InitContainer/Users.InitContainer.csproj
   ```

4. Report the exit code and any errors. If exit code is non-zero, show the last 30 lines of output.
