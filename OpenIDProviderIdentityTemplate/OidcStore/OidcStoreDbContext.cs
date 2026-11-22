using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace AspNetIdentitySample.OidcStore;

/// <summary>
/// Durable storage for the OpenID Connect configuration and protocol state that must outlive a process:
/// the tenants with the clients they declare, the signing keys and the dynamic client registrations. Kept
/// in its own database, separate from the Identity user store, so the two concerns (who the users are
/// versus how the protocol is configured) do not share a schema.
/// </summary>
/// <remarks>
/// A signing key and a registration name the tenant they belong to by the tenant's id and generation
/// together: one tenant never publishes or signs with another's keys, a client registered at one tenant is
/// unknown at another, and a tenant created again under the id of a removed one finds none of the removed
/// one's rows. A new database starts with the rows of <see cref="InitialData"/>.
/// </remarks>
public class OidcStoreDbContext(DbContextOptions<OidcStoreDbContext> options) : DbContext(options)
{
    public DbSet<PersistedTenant> Tenants => Set<PersistedTenant>();
    public DbSet<PersistedTenantClient> TenantClients => Set<PersistedTenantClient>();
    public DbSet<PersistedSigningKey> SigningKeys => Set<PersistedSigningKey>();
    public DbSet<PersistedClient> Clients => Set<PersistedClient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PersistedTenant>().HasData(InitialData.Tenant);

        // Deleting a tenant deletes the clients it declares, where foreign keys are on: EF Core turns them on for
        // its connections, the sqlite3 shell only after PRAGMA foreign_keys = ON
        modelBuilder.Entity<PersistedTenantClient>()
            .HasOne<PersistedTenant>()
            .WithMany()
            .HasForeignKey(client => client.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PersistedTenantClient>().HasData(InitialData.DemoClients);
    }
}

/// <summary>
/// A signing key at rest. The full JWK, private material included, is serialized as JSON and
/// keyed by its <c>kid</c>, so a restart reloads the same key and tokens issued before the
/// restart still validate.
/// </summary>
[Index(nameof(TenantId), nameof(Generation), nameof(Usage))]
public class PersistedSigningKey
{
    [Key]
    public required string KeyId { get; set; }

    // The Id and Generation of the tenant in Tenants
    public required string TenantId { get; set; }
    public required string Generation { get; set; }

    // "sig" or "enc": this sample only ever stores signing keys.
    public required string Usage { get; set; }

    // The signing key JWK, encrypted at rest with ASP.NET Data Protection (ciphertext, not raw JSON).
#warning JwkJson stores the signing key ENCRYPTED with ASP.NET Data Protection, which RELOCATES rather than removes the custody problem: the Data Protection key ring must now itself be persisted, shared across instances, and protected (PersistKeysTo + ProtectKeysWith a cert or KMS), or it can lose your signing keys on its own rotation. The real home for signing keys is an HSM or KMS, not a database. See https://docs.abblix.com/docs/signing-key-persistence#encrypting-the-stored-key-at-rest
    public required string JwkJson { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// A client registration at rest. <c>ClientInfo</c> carries sixty-plus members, so the registration is
/// stored as one JSON document rather than a column per property: the server only ever looks a client
/// up by id, and a column-per-property schema would demand a migration on every new policy knob.
/// </summary>
[PrimaryKey(nameof(TenantId), nameof(Generation), nameof(ClientKey))]
public class PersistedClient
{
    // The Id and Generation of the tenant in Tenants
    public required string TenantId { get; set; }
    public required string Generation { get; set; }

    // The client id upper-cased: the server compares client ids without regard to case, so the key does too.
    public required string ClientKey { get; set; }

    // The identifier of the registration access token that manages this registration. A change or a removal
    // takes effect only while the row still carries the one it was decided on, so it has a column of its own.
    public required string TokenId { get; set; }

#warning Payload embeds client secret hashes, and for client_secret_jwt clients the raw secret. Treat this store like a password database (encrypted disk, TLS, a least-privilege role, guarded backups) before production. See https://docs.abblix.com/docs/durable-client-store
    public required string Payload { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
