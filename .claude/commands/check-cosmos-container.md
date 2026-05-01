Query the local CosmosDB emulator and show the state of the users-db containers.

Steps:
1. Check emulator is running:
   `docker ps --filter "name=cosmosdb" --format "{{.Status}}"`
   Stop with an error message if not running.

2. Load connection string from `.env.dev` (parse `COSMOSDB_CONNECTION_STRING` and `USERS_COSMOSDB_DATABASE`).
   If running against the Parallels Desktop emulator instead of Docker, use the connection string from
   `src/Users.InitContainer/appsettings.dev.json`.

3. Write and run a temporary C# script using `dotnet-script` or a temp console project that:
   - Connects to the emulator. Set `CosmosClientOptions.HttpClientFactory` to skip TLS validation if `IgnoreSslCertificateValidation=true` is needed (Docker Linux emulator only; not needed for Parallels Desktop emulator).
   - For each container in `users-db` (users, tenants, roles, actions, permissions, migrations):
     - Count total documents: `SELECT VALUE COUNT(1) FROM c`
     - Count non-deleted documents: `SELECT VALUE COUNT(1) FROM c WHERE (NOT IS_DEFINED(c.deletedAt) OR IS_NULL(c.deletedAt))`

4. Print a summary table:
   ```
   Container     Total    Active   Soft-deleted
   users           12        11          1
   tenants          8         8          0
   roles            4         4          0
   actions         16        16          0
   permissions     11        11          0
   migrations       1         1          0
   ```

5. If any container is missing (NotFoundException), report it as "not provisioned — run /run-users-init-container".
