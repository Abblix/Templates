using Abblix.DependencyInjection;
using Abblix.Jwt;
using Abblix.Oidc;
using Abblix.Oidc.Server.Common.Constants;
using Abblix.Oidc.Server.Features;
using Abblix.Oidc.Server.Features.ClientInformation;
using Abblix.Oidc.Server.Features.Consents;
using Abblix.Oidc.Server.Features.MultiTenancy;
using Abblix.Oidc.Server.Features.UserInfo;
using Abblix.Oidc.Server.Mvc;
using OpenIDProviderApp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

#warning Dev-only: TestUserStorage is an in-memory demo store; back IUserInfoProvider with a real user database for production. https://docs.abblix.com/docs/aspnet-identity-integration
// Add the TestUserStorage as a singleton service in the DI container.
var userInfoStorage = new TestUserStorage(
#if (HasSampleUser)
    // Dev-only sample account seeded from --user / --password so you can sign in
    // immediately. Scaffold without --user to start with an empty store.
    new UserInfo(
        Subject: "SAMPLE_USER_SUBJECT",
        Name: "Demo User",
        Email: "SAMPLE_USER_EMAIL",
        Password: "SAMPLE_USER_PASSWORD")
#endif
);
builder.Services.AddSingleton(userInfoStorage);

// Use AddAlias to register TestUserStorage also as an implementation of IUserInfoProvider.
builder.Services.AddAlias<IUserInfoProvider, TestUserStorage>();

// Grants every client everything it asks for, which suits only clients of your own;
// to ask the end user for consent, register your own IUserConsentsProvider instead
builder.Services.AddConsentGrantedForEveryClient();

// Register and configure Abblix OIDC Server, serving one issuer
builder.Services.AddOidcServices(options => options.Tenants.Add(new TenantDefinition
{
    Id = "main",

    // The address the app is served on, written into every token it issues
    Issuer = new Uri("https://localhost:5001"),

    // Configure the issuer's scopes and resources here:

    // Scopes = [new ScopeDefinition(/*...*/)],

    //Resources =
    //[
    //    new(new Uri(/*...*/, UriKind.Absolute), new ScopeDefinition(/*...*/)),
    //    /*...*/
    //],

    // The clients the Oidc:Clients configuration section declares. Only appsettings.Development.json declares
    // one, console_client, whose secret is published with this template, so a host started in any other
    // environment has none. Add further clients there, in the same shape; a client's keys go in by JwksUri, since
    // an inline Jwks cannot be bound from configuration. For production, register clients in a durable store
    // instead. See https://docs.abblix.com/docs/durable-client-store
    Clients = ReadClients(builder.Configuration),

    // The following URL leads to Login action of AuthController
    LoginUri = new Uri("/Auth/Login", UriKind.Relative),

    #warning Dev-only: signing key is regenerated on every startup; provide a persistent, shared key for production via a custom IAuthServiceKeysProvider. https://docs.abblix.com/docs/signing-key-persistence
    SigningKeys = [JsonWebKeyFactory.CreateRsa(PublicKeyUsages.Signature)],
}));

// The clients the Oidc:Clients section declares, none when it is absent. Left to itself the binder skips a value
// it cannot convert and keeps an entry with no ClientId, so both are refused here, naming the section.
static ClientInfo[] ReadClients(IConfiguration configuration)
{
    const string section = "Oidc:Clients";
    ClientInfo[]? clients;
    try
    {
        clients = configuration.GetSection(section).Get<ClientInfo[]>(options => options.ErrorOnUnknownConfiguration = true);
    }
    catch (InvalidOperationException exception)
    {
        // The binder's own message says only that binding failed; the key and the value it could not read are in
        // the exceptions beneath it.
        var reasons = new List<string>();
        for (var reason = exception.InnerException ?? exception; reason is not null; reason = reason.InnerException)
            reasons.Add(reason.Message);

        throw new InvalidOperationException($"The {section} configuration section does not describe clients: {string.Join(" ", reasons)}", exception);
    }

    var unnamed = Array.FindIndex(clients ?? [], client => string.IsNullOrEmpty(client.ClientId));
    if (unnamed >= 0)
        throw new InvalidOperationException($"The {section} configuration section has a client with no ClientId at {section}:{unnamed}");

    return clients ?? [];
}

// Token introspection (RFC 7662) is opt-in: this call registers the endpoint and advertises it in discovery.
// A client authenticates to it with its own credentials, as requests.http does with console_client.
builder.Services.AddIntrospection();

// Add authentication services
builder.Services
    .AddAuthentication()
    .AddCookie();

#warning Dev-only: in-memory cache is per-process and lost on restart; use a distributed cache (Redis/SQL) in production. https://docs.abblix.com/docs/choosing-a-cache-backend
builder.Services
    .AddDistributedMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors();
app.UseAuthorization();

// The library's controllers carry no routes of their own, so the default route below never reaches them
app.MapOidcControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
