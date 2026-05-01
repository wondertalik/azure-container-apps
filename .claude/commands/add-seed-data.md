Add seed data for the Users module interactively.

Usage: /add-seed-data <type>
Types: user, tenant, role, action

The seed data root is: `src/Users/Users.InitContainer.Data/SeedData/users-db/`

For `user`:
1. Ask for: email, firstname, lastname, userId (GUID, generate if not provided), defaultTenantId (optional).
2. Create directory `users/{email}/`.
3. Write `users/{email}/user.json` as a `DbUser` JSON document (all audit fields set to now / a placeholder createdBy GUID).
4. Write `users/{email}/permissions.json` as an empty array `[]` — remind the user this file is required and they must add at least one `DbPermission` entry with roleAssignments before seeding.

For `tenant`:
1. Ask for: name, tenantId (GUID, generate if not provided), parentId (optional, empty = root), tenantType (Node/Leaf).
2. Append to `tenants.json`. If parentId is provided, also add the new tenantId to the parent's `childIds` array in the same file.

For `role`:
1. Ask for: name, roleId (GUID, generate if not provided), tenantId (empty GUID = global), group (optional), actionIds (comma-separated, can be empty).
2. Append to `roles.json`.

For `action`:
1. Ask for: name, actionId (suggest `{domain}:{verb}` format, e.g. `users:read`).
2. Append to `actions.json`.

After writing, run `dotnet build src/Users/Users.InitContainer.Data/Users.InitContainer.Data.csproj --no-restore -v q` to verify JSON is valid (build copies files to output).
