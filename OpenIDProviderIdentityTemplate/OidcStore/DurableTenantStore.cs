using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Abblix.Oidc.Server.Common.Constants;
using Abblix.Oidc.Server.Features.ClientInformation;
using Abblix.Oidc.Server.Features.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace AspNetIdentitySample.OidcStore;

/// <summary>
/// Serves the tenants, and the clients each one declares, from oidc.db instead of from code, so the database is the
/// one place a tenant and its clients are defined.
/// </summary>
/// <remarks>
/// The server reads the whole list at startup and again every <c>OidcOptions.RefreshTenantsEvery</c> (a minute by
/// default), so a row added, changed or deleted is served as such within that period. Each reading is judged by the
/// server's checks of a tenant list: a tenant they refuse, a client secret that could never match say, is left out
/// and logged as an error naming the reason, and the other tenants are served. Registered as a singleton, since the
/// server's catalog that calls it is one; it opens a <c>DbContext</c> per call through
/// <see cref="IDbContextFactory{TContext}"/>.
/// </remarks>
public sealed class DurableTenantStore(IDbContextFactory<OidcStoreDbContext> contextFactory) : ITenantStore
{
    public async Task<IReadOnlyCollection<StoredTenant>> ListAsync(CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var tenants = await db.Tenants.AsNoTracking().ToListAsync(cancellationToken);
        var clients = (await db.TenantClients.AsNoTracking().OrderBy(client => client.Id).ToListAsync(cancellationToken))
            .ToLookup(client => client.TenantId, StringComparer.Ordinal);

        return [..tenants.Select(tenant => Stored(tenant, [..clients[tenant.Id]]))];
    }

    private static StoredTenant Stored(PersistedTenant row, PersistedTenantClient[] clients)
    {
        var tenant = new TenantDefinition
        {
            Id = row.Id,
            Generation = row.Generation,
            Issuer = new Uri(row.Issuer, UriKind.Absolute),
            LoginUri = row.LoginUri is null ? null : new Uri(row.LoginUri, UriKind.RelativeOrAbsolute),
            Resources = row.Resources is null ? null : Read<ResourceDefinition[]>(row.Resources, $"tenant '{row.Id}'"),
            Clients = [..clients.Select(client => Read<ClientInfo>(client.Payload, $"client row {client.Id} of tenant '{row.Id}'"))],
        };

        return new StoredTenant(tenant, VersionOf(row, clients));
    }

    private static T Read<T>(string json, string owner)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json)
                   ?? throw new InvalidOperationException($"The {owner} in oidc.db holds null instead of a {typeof(T).Name}.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"The {owner} in oidc.db does not hold a valid {typeof(T).Name}: {exception.Message}", exception);
        }
    }

    // The server keeps serving what it built for a tenant while the version stays, so the version is taken from
    // everything the definition is built from: any edit to the tenant's row or its clients' rows changes it.
    private static string VersionOf(PersistedTenant row, PersistedTenantClient[] clients)
    {
        var content = JsonSerializer.Serialize(new
        {
            row.Generation,
            row.Issuer,
            row.LoginUri,
            row.Resources,
            Clients = clients.Select(client => new { client.Id, client.Payload }),
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }
}
