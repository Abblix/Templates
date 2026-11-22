using System.ComponentModel.DataAnnotations;

namespace AspNetIdentitySample.OidcStore;

/// <summary>
/// A client the tenant declares, at rest: one the tenant's administrator set up, as opposed to one that registered
/// itself (those are in <see cref="PersistedClient"/>). The server checks these with the tenant they belong to and
/// serves them ahead of any registration under the same id.
/// </summary>
/// <remarks>
/// <c>ClientInfo</c> carries sixty-plus members, so the client is stored as one JSON document rather than a column
/// per property. Its id lives in that document alone: the server finds a declared client in the tenant it reads,
/// never by a lookup here, so a second copy of the id in a column could only disagree with the document.
/// </remarks>
public class PersistedTenantClient
{
    [Key]
    public int Id { get; set; }

    // The Id of the tenant in Tenants that declares this client
    public required string TenantId { get; set; }

    // The ClientInfo as JSON, secret hashes included, as in PersistedClient and with the #warning given there
    public required string Payload { get; set; }
}
