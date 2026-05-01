Query the local CosmosDB emulator and show the state of the users-db containers.

Steps:
1. Check emulator is running:
   `docker ps --filter "name=cosmosdb" --format "{{.Status}}"`
   Stop with an error message if not running.

2. Load connection string from `.env.dev` (parse `COSMOSDB_CONNECTION_STRING` and `USERS_COSMOSDB_DATABASE`).

3. Write and run a temporary C# script using `dotnet-script` or a temp console project that:
   - Connects to the emulator with `IgnoreSslCertificateValidation = true`
   - For each container in `users-db` (users, tenants, roles, actions, permissions):
     - Count total documents: `SELECT VALUE COUNT(1) FROM c`
     - Count non-deleted documents: `SELECT VALUE COUNT(1) FROM c WHERE (NOT IS_DEFINED(c.deletedAt) OR IS_NULL(c.deletedAt))`

4. Print a summary table:
   ```
   Container     Total    Active   Soft-deleted
   users           12        11          1
   tenants          8         8          0
   roles            4         4          0
   actions         10        10          0
   permissions     11        11          0
   ```

5. If any container is missing (NotFoundException), report it as "not provisioned — run /run-users-init-container".

Note: The emulator's well-known key is:
`C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD34b9n=`
