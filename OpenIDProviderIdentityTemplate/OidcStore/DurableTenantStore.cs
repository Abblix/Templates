using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Abblix.Oidc.Server.Common.Constants;
using Abblix.Oidc.Server.Features.ClientInformation;
using Abblix.Oidc.Server.Features.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AspNetIdentitySample.OidcStore;

/// <summary>
/// Serves the tenants, and the clients each one declares, from oidc.db instead of from code, so the database is the
/// one place a tenant and its clients are defined.
/// </summary>
/// <remarks>
/// The server reads the whole list at startup and again every <c>OidcOptions.RefreshTenantsEvery</c> (a minute by
/// default), so a row added, changed or deleted is served as such within that period. Each reading is judged by the
/// server's checks of a tenant list: a tenant they refuse, a client secret that could never match say, is left out
/// and logged as an error naming the reason, and the other tenants are served. A tenant whose rows cannot even be
/// read, a cut-off JSON document or an issuer that is no absolute URI say, is left out here the same way: the server
/// would otherwise take the failed reading for the whole list, refuse to start, and once started serve no change to
/// any tenant until the row is fixed. Registered as a singleton, since the server's catalog that calls it is one; it
/// opens a <c>DbContext</c> per call through <see cref="IDbContextFactory{TContext}"/>.
/// </remarks>
public sealed class DurableTenantStore(
    IDbContextFactory<OidcStoreDbContext> contextFactory,
    ILogger<DurableTenantStore> logger) : ITenantStore
{
    // A member the document names that ClientInfo does not have, a misspelt or camelCase one say, fails the read
    // instead of being skipped: skipped, "allowedResources" would leave the client with no limit on its resources.
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    // Why each tenant was last left out, so a row that stays broken is logged once rather than at every reading.
    // The server reads the store one reading at a time, so no lock is needed.
    private readonly Dictionary<string, string> _leftOut = new(StringComparer.Ordinal);

    public async Task<IReadOnlyCollection<StoredTenant>> ListAsync(CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var tenants = await db.Tenants.AsNoTracking().ToListAsync(cancellationToken);
        var clients = (await db.TenantClients.AsNoTracking().OrderBy(client => client.Id).ToListAsync(cancellationToken))
            .ToLookup(client => client.TenantId, StringComparer.Ordinal);

        var stored = new List<StoredTenant>();
        foreach (var tenant in tenants)
        {
            try
            {
                stored.Add(Stored(tenant, [..clients[tenant.Id]]));
                _leftOut.Remove(tenant.Id);
            }
            catch (InvalidDataException exception)
            {
                if (_leftOut.GetValueOrDefault(tenant.Id) != exception.Message)
                {
                    logger.LogError(
                        "The tenant {TenantId} is left out of the tenants served, since its rows in oidc.db cannot be read: {Reason}",
                        tenant.Id, exception.Message);
                }

                _leftOut[tenant.Id] = exception.Message;
            }
        }

        return stored;
    }

    private static StoredTenant Stored(PersistedTenant row, PersistedTenantClient[] clients)
    {
        var tenant = new TenantDefinition
        {
            Id = row.Id,
            Generation = row.Generation,
            Issuer = ReadUri(row.Issuer, UriKind.Absolute, nameof(row.Issuer)),
            LoginUri = row.LoginUri is null ? null : ReadUri(row.LoginUri, UriKind.RelativeOrAbsolute, nameof(row.LoginUri)),
            Resources = row.Resources is null ? null : Read<ResourceDefinition[]>(row.Resources, nameof(row.Resources)),
            Clients = [..clients.Select(ReadClient)],
        };

        return new StoredTenant(tenant, VersionOf(row, clients));
    }

    private static Uri ReadUri(string value, UriKind kind, string column)
        => Uri.TryCreate(value, kind, out var uri)
            ? uri
            : throw new InvalidDataException($"{column} '{value}' cannot be read as a URI of kind {kind}.");

    private static ClientInfo ReadClient(PersistedTenantClient row)
    {
        var client = Read<ClientInfo>(row.Payload, $"Client row {row.Id}");
        if (string.IsNullOrEmpty(client.ClientId))
            throw new InvalidDataException($"Client row {row.Id} has no {nameof(ClientInfo.ClientId)}.");

        return client;
    }

    // A ClientSecret throws FormatException for a hash written as text it cannot decode, so that is caught too
    private static T Read<T>(string json, string what)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, ReadOptions)
                   ?? throw new InvalidDataException($"{what} holds null instead of a {typeof(T).Name}.");
        }
        catch (Exception exception) when (exception is JsonException or FormatException)
        {
            throw new InvalidDataException($"{what} does not hold a valid {typeof(T).Name}: {exception.Message}", exception);
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
