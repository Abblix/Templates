using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Abblix.Oidc;
using Abblix.Oidc.Server.Common.Constants;
using Abblix.Oidc.Server.Features.ClientInformation;

namespace AspNetIdentitySample.OidcStore;

/// <summary>
/// The rows a new oidc.db starts with: the tenant this provider serves and the demo clients test_client and
/// console_client. They are the model's seed data, so EnsureCreated writes them only as it creates the database: an
/// edit here reaches a database created after it, never one that exists. Change an existing one in its tables.
/// </summary>
public static class InitialData
{
    public const string TenantId = "main";

    // A constant, so the seed data is the same each time the model is built
    private const string TenantGeneration = "initial";

    // The address the app is served on, written into every token it issues
    private const string Issuer = "https://localhost:5001";

#warning DEV convenience only: the demo clients test_client and console_client below redirect to localhost and share a secret held in this source file. They exist for Development: before a real deployment remove them from DemoClients, so a database created from then on holds none, and delete their rows from TenantClients in any oidc.db already created.
    // The demo clients' secret, generated for this project when it was created
    public const string DemoClientSecret = "DEMO_CLIENT_SECRET";

    public static PersistedTenant Tenant => new()
    {
        Id = TenantId,
        Generation = TenantGeneration,

        Issuer = Issuer,
        LoginUri = "/Auth/Login",
        Resources = JsonSerializer.Serialize<ResourceDefinition[]>(
        [
            new ResourceDefinition(new Uri("https://localhost:5004", UriKind.Absolute), new ScopeDefinition("weather")),
        ]),
    };

    public static PersistedTenantClient[] DemoClients =>
    [
        new()
        {
            Id = 1,
            TenantId = TenantId,

            // An interactive client: a user signs in on the login page and the client exchanges the code for tokens.
            // This project serves its pages itself in Development (DemoClient.cs): the home page starts the sign-in.
            Payload = Payload(new ClientInfo(DemoClient.ClientId)
            {
                ClientSecrets = [DemoSecret()],
                TokenEndpointAuthMethod = ClientAuthenticationMethods.ClientSecretPost,
                AllowedGrantTypes = [GrantTypes.AuthorizationCode, GrantTypes.RefreshToken],

                // A refresh token for the offline_access scope, which the demo client asks for
                OfflineAccess = OfflineAccess.Allowed,
                PkceRequired = true,

                // First the demo client's pages, at this project's own address; then those of a client application
                // of your own run on https://localhost:5002
                RedirectUris =
                [
                    new Uri(Issuer + DemoClient.CallbackPath),
                    new Uri("https://localhost:5002/signin-oidc"),
                ],
                PostLogoutRedirectUris =
                [
                    new Uri(Issuer + DemoClient.SignedOutPath),
                    new Uri("https://localhost:5002/signout-callback-oidc"),
                ],
            }),
        },
        new()
        {
            Id = 2,
            TenantId = TenantId,

            // A headless client that obtains a token through client_credentials and introspects it, which is how
            // requests.http shows the server working before any interactive client exists.
            Payload = Payload(new ClientInfo("console_client")
            {
                ClientSecrets = [DemoSecret()],
                TokenEndpointAuthMethod = ClientAuthenticationMethods.ClientSecretPost,
                AllowedGrantTypes = [GrantTypes.ClientCredentials],

                // No resource: its token is addressed only to this provider (the issuer), and no API registered in
                // Resources accepts it.
                AllowedResources = [],
            }),
        },
    ];

    // ClientSecret also exposes each hash as Base64 and as hex text, for configuration binding. Written out with
    // them, a secret carries its hash three times, and as the document is read each copy overwrites the one before,
    // so an edit to any but the last would be lost. The payload is written with the hash alone; any of the three
    // forms is read.
    private static readonly JsonSerializerOptions PayloadOptions = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { OmitHashText } },
    };

    private static readonly string[] HashText =
    [
        nameof(ClientSecret.Sha256HashBase64),
        nameof(ClientSecret.Sha512HashBase64),
        nameof(ClientSecret.Sha256HashHex),
        nameof(ClientSecret.Sha512HashHex),
    ];

    private static void OmitHashText(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type != typeof(ClientSecret))
            return;

        foreach (var property in typeInfo.Properties.Where(property => HashText.Contains(property.Name)).ToList())
            typeInfo.Properties.Remove(property);
    }

    private static string Payload(ClientInfo client) => JsonSerializer.Serialize(client, PayloadOptions);

    // The provider stores a secret's hash, never the secret
    private static ClientSecret DemoSecret() => new()
    {
        Sha512Hash = SHA512.HashData(Encoding.UTF8.GetBytes(DemoClientSecret)),
    };
}
