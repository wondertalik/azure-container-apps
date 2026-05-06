namespace Users.Authorization.Constants;

public static class UsersCosmosDbConstants
{
    public static ContainerOptions Users => new("users", "/id");

    public static ContainerOptions Tenants => new("tenants", "/id");

    public static ContainerOptions Roles => new("roles", "/tenantId");

    public static ContainerOptions Actions => new("actions", "/id");

    public static ContainerOptions Permissions => new("permissions", "/tenantId");

    public static ContainerOptions Migrations => new("migrations", "/id");

    public sealed record ContainerOptions(string Name, string PartitionKey);
}
