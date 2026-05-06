using Libraries.Shared.Interfaces;
using Newtonsoft.Json;

namespace Users.Infrastructure.Entities.Models.DbUsers;

public sealed record DbUser : ICreatable, IUpdatable, ISoftDeletable, ILockable
{
    /// <summary>
    /// Id of User
    /// </summary>
    [JsonProperty("id")]
    public required string UserId { get; set; }

    /// <summary>
    /// User profile firstname
    /// </summary>
    [JsonProperty("firstname")]
    public required string Firstname { get; set; }

    /// <summary>
    /// User profile lastname
    /// </summary>
    [JsonProperty("lastname")]
    public required string Lastname { get; set; }

    /// <summary>
    /// User e-mail
    /// </summary>
    [JsonProperty("email")]
    public required string Email { get; set; }

    /// <summary>
    /// User Profile image URI
    /// </summary>
    [JsonProperty("userProfileImageUri")]
    public string? UserProfileImageUri { get; init; }

    /// <summary>
    /// Tenants profile assignments
    /// </summary>
    [JsonProperty("tenantAssignments")]
    public IList<DbTenantAssignment> TenantAssignments { get; set; } = new List<DbTenantAssignment>();

    public sealed record DbTenantAssignment : ICreatable, ISoftDeletable
    {
        /// <summary>
        /// Id of tenant
        /// </summary>
        [JsonProperty("tenantId")]
        public required string TenantId { get; set; }

        /// <summary>
        /// When the tenant assignment was created
        /// </summary>
        [JsonProperty("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Who created the tenant assignment
        /// </summary>
        [JsonProperty("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// When the tenant assignment was deleted (soft-delete)
        /// </summary>
        [JsonProperty("deletedAt")]
        public DateTimeOffset? DeletedAt { get; set; }

        /// <summary>
        /// Who deleted the tenant assignment
        /// </summary>
        [JsonProperty("deletedBy")]
        public string? DeletedBy { get; set; }
    }

    /// <summary>
    /// Default tenant id
    /// </summary>
    [JsonProperty("defaultTenantId")]
    public string? DefaultTenantId { get; set; }

    /// <summary>
    /// Indicates whether the user account is enabled/active. Null means unknown state.
    /// </summary>
    [JsonProperty("accountEnabled")]
    public bool AccountEnabled { get; set; }

    /// <summary>
    /// Timestamp when the account was disabled. Null if account is not disabled or never been disabled.
    /// </summary>
    [JsonProperty("accountDisabledAt")]
    public DateTimeOffset? AccountDisabledAt { get; set; }

    /// <summary>
    /// Who disabled the user account. Null if account is not disabled or never been disabled.
    /// </summary>
    [JsonProperty("accountDisabledBy")]
    public string? AccountDisabledBy { get; set; }

    /// <summary>
    /// Timestamp when the user last connected. Null if never connected.
    /// </summary>
    [JsonProperty("connectedAt")]
    public DateTimeOffset? ConnectedAt { get; set; }

    /// <summary>
    /// When the user profile was created
    /// </summary>
    [JsonProperty("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Who created the user profile
    /// </summary>
    [JsonProperty("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// When the user profile was last updated
    /// </summary>
    [JsonProperty("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Who last updated the user profile
    /// </summary>
    [JsonProperty("updatedBy")]
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>
    /// When the entity was deleted
    /// </summary>
    [JsonProperty("deletedAt")]
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Who deleted the entity
    /// </summary>
    [JsonProperty("deletedBy")]
    public string? DeletedBy { get; set; }

    /// <summary>
    /// When the entity was locked for provisioning. Null means unlocked.
    /// </summary>
    [JsonProperty("lockedAt")]
    public DateTimeOffset? LockedAt { get; set; }
}
