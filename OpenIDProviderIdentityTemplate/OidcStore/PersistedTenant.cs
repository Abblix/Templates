using System.ComponentModel.DataAnnotations;

namespace AspNetIdentitySample.OidcStore;

/// <summary>
/// A tenant this provider serves, at rest: one issuer with its own clients, keys and settings. The server reads
/// every row of this table again on a timer, so a tenant added, changed or deleted here is served as such once that
/// period has passed, with no restart.
/// </summary>
/// <remarks>
/// Only the members of the tenant definition this template uses have a column. Another one, such as Scopes or
/// ConsentUri, takes a column here and a line in <see cref="DurableTenantStore"/> that reads it.
/// </remarks>
public class PersistedTenant
{
    [Key]
    public required string Id { get; set; }

    // Tells this creation of the tenant from one made again under the same id: the signing keys and the dynamic
    // client registrations are kept under the id and the generation together, so a tenant created again finds none
    // of the removed one's. A new row takes a value of its own.
    public required string Generation { get; set; }

    // The issuer identifier, written into every token the tenant issues and the address it is served at
    public required string Issuer { get; set; }

    // The page the server sends a user to sign in, relative to the issuer or absolute
    public string? LoginUri { get; set; }

    // The APIs the tenant issues access tokens for, as a JSON array of ResourceDefinition
    public string? Resources { get; set; }
}
