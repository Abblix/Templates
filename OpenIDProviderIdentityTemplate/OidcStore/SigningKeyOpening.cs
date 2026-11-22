using System.Text.Json;
using Abblix.Jwt;
using Abblix.Oidc.Server.Features.MultiTenancy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace AspNetIdentitySample.OidcStore;

/// <summary>
/// Gives each tenant a signing key before the server first serves it, when it has none yet, and stores it in oidc.db
/// encrypted with ASP.NET Core Data Protection. The server readies every tenant it reads this way, at startup and
/// whenever a new one appears in Tenants, so a tenant added while the provider runs signs from its first request.
/// </summary>
/// <remarks>
/// On a later start the key is already stored, so the same key keeps validating tokens issued before the restart. A
/// real deployment keeps signing keys in an HSM or KMS rather than the database (see the #warning on
/// PersistedSigningKey).
/// </remarks>
public sealed class SigningKeyOpening(
    IDbContextFactory<OidcStoreDbContext> contextFactory,
    IDataProtectionProvider dataProtection,
    TimeProvider clock) : ITenantOpening
{
    private readonly IDataProtector _protector =
        dataProtection.CreateProtector(DatabaseKeysProvider.SigningKeyProtectorPurpose);

    public async Task<IReadOnlyDictionary<string, Exception>> OpenAsync(
        IReadOnlyCollection<TenantDefinition> tenants,
        CancellationToken cancellationToken)
    {
        var failures = new Dictionary<string, Exception>(StringComparer.Ordinal);
        foreach (var tenant in tenants)
        {
            try
            {
                await EnsureSigningKeyAsync(tenant, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // The server leaves out only the tenant named here, logs why, and tries it again at its next reading
                failures[tenant.Id] = exception;
            }
        }

        return failures;
    }

    private async Task EnsureSigningKeyAsync(TenantDefinition tenant, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        if (await db.SigningKeys.AnyAsync(key => key.TenantId == tenant.Id
                                                 && key.Generation == tenant.Generation
                                                 && key.IsActive
                                                 && key.Usage == PublicKeyUsages.Signature,
                cancellationToken))
        {
            return;
        }

        var signingKey = JsonWebKeyFactory.CreateRsa(PublicKeyUsages.Signature);
        db.SigningKeys.Add(new PersistedSigningKey
        {
            KeyId = signingKey.KeyId!,
            TenantId = tenant.Id,
            Generation = tenant.Generation,
            Usage = PublicKeyUsages.Signature,
            JwkJson = _protector.Protect(JsonSerializer.Serialize<JsonWebKey>(signingKey)),
            CreatedAt = clock.GetUtcNow(),
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
