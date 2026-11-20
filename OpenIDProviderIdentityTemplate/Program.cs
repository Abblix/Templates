using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Abblix.DependencyInjection;
using Abblix.Jwt;
using Abblix.Oidc;
using Abblix.Oidc.Server.AspNetCore.Formatters;
using Abblix.Oidc.Server.Common.Configuration;
using Abblix.Oidc.Server.Common.Constants;
using Abblix.Oidc.Server.Common.Interfaces;
using Abblix.Oidc.Server.Features;
using Abblix.Oidc.Server.Features.ClientInformation;
using Abblix.Oidc.Server.Features.Consents;
using Abblix.Oidc.Server.Features.MultiTenancy;
using Abblix.Oidc.Server.Features.RandomGenerators;
using Abblix.Oidc.Server.Features.UserInfo;
#if (UseMvc)
using Abblix.Oidc.Server.Mvc;
#else
using Abblix.Oidc.Server.MinimalApi;
#endif
using AspNetIdentitySample;
using AspNetIdentitySample.OidcStore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
#if (UseMvc)
using Microsoft.AspNetCore.Mvc;
#endif
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ASP.NET Core Identity owns users and credentials: the standard Identity tables
// live in this DbContext, and the core registration brings the user store,
// password hashing, and lockout without Identity's own cookie schemes.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Users")));

builder.Services
    .AddIdentityCore<IdentityUser>(options =>
    {
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager();

// Password-hashing work factor: Identity's default hasher here is PBKDF2-HMAC-SHA512. Bind
// IterationCount from appsettings.json so it can be raised to meet current OWASP guidance for that
// algorithm without a recompile.
builder.Services.Configure<PasswordHasherOptions>(builder.Configuration.GetSection("PasswordHasher"));

// Without this registration the server falls back to a stand-in that knows no user: no ID token is issued and
// UserInfo answers invalid_token.
builder.Services.AddScoped<IUserInfoProvider, IdentityUserInfoProvider>();

#if (UseMvc)
// Add services to the container.
builder.Services.AddControllersWithViews();
#else
// The Minimal API host has no MVC, so the services the pipeline relies on are registered directly
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
#endif

// Grants every client everything it asks for, which suits this provider: its clients are all its own.
// To ask the end user for consent, register your own IUserConsentsProvider instead.
builder.Services.AddConsentGrantedForEveryClient();

// The one issuer this provider serves. The seeding below writes the signing key and the configured clients
// under this tenant's id, so the same definition is handed to the server and kept for the seeding.
var tenant = DefineTenant([]);

// The tenant with the given clients declared on it. The server is handed it with none, since its clients live in
// the durable store below; the seeding judges the configured clients by declaring them on this same definition.
static TenantDefinition DefineTenant(ClientInfo[] clients) => new()
{
    Id = "main",

    // The address the app is served on, written into every token it issues
    Issuer = new Uri("https://localhost:5001"),

    Resources =
    [
        new ResourceDefinition(new Uri("https://localhost:5004", UriKind.Absolute), new ScopeDefinition("weather")),
    ],
    LoginUri = new Uri("/Auth/Login", UriKind.Relative),

    // SigningKeys are deliberately NOT set here, and Clients only for that judgement. Both come from the
    // SQLite-backed stores registered below, so they outlive a restart. A first-party client could still be
    // declared on the tenant handed to the server, as the argument of DefineTenant above: the server serves those
    // ahead of any stored registration under the same id.
    Clients = clients,
};

// Register and configure Abblix OIDC Server
builder.Services.AddOidcServices(options => options.Tenants.Add(tenant));

// The end-session endpoint leaves two pages to the host: the confirmation of a sign-out no client vouched for, and
// the page shown when no client asked to be sent back. Registered after AddOidcServices, it wraps the library's own.
#if (UseMvc)
builder.Services.Decorate<IEndSessionResponseFormatter<ActionResult>, EndSessionPages>();
#else
builder.Services.Decorate<IEndSessionResponseFormatter<IResult>, EndSessionPages>();
#endif

// Token introspection (RFC 7662) is opt-in: this call registers the endpoint and advertises it in discovery.
// A client authenticates to it with its own credentials, as requests.http does with console_client.
builder.Services.AddIntrospection();

// Durable OIDC state (signing keys and client registrations) lives in SQLite so a restart neither
// invalidates issued tokens nor forgets registered clients. The providers below are singletons, so
// they open a context per call through IDbContextFactory rather than holding a scoped DbContext.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDbContextFactory<OidcStoreDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("OidcStore")));

// Data Protection encrypts the signing key before it lands in SQLite, so a stolen database file
// yields ciphertext, not a private key. The key ring is persisted to disk EXPLICITLY rather than
// through the default, which lands in a per-user profile folder (why a local restart still decrypts)
// but degrades to ephemeral in-memory keys inside a container with no user profile. SetApplicationName
// fixes the key isolation so every instance derives the same keys.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        System.IO.Path.Combine(builder.Environment.ContentRootPath, "dataprotection-keys")))
    .SetApplicationName("AspNetIdentitySample");
#warning The Data Protection key ring above is a LOCAL, UNENCRYPTED folder: single-node and unprotected. For real deployments persist it to a store every instance shares and add ProtectKeysWith an X.509 certificate or a KMS, or a fresh pod cannot decrypt the stored signing key. See https://docs.abblix.com/docs/signing-key-persistence#encrypting-the-stored-key-at-rest

// Signing keys: the database-backed provider takes the place of the server's default, which reads each
// tenant's SigningKeys from its definition. Registered after AddOidcServices, it replaces that default,
// since the last registration of a service is the one resolved, and answers for each tenant's keys, so
// the tenant above declares none.
builder.Services.AddSingleton<IAuthServiceKeysProvider, DatabaseKeysProvider>();

// Client store: the server keeps its own, which serves the clients the tenant declares first, and keeps
// the registrations in the store the host registers here instead of in memory. A singleton, since the
// server's client store that calls it is one.
builder.Services.AddSingleton<ITenantClientRegistrationStore, DurableClientStore>();

// The host-registered cookie carries the library's OIDC session: Identity never signs in, it only verifies.
// The security stamp snapshotted at login is compared on every request, so a password change
// or UpdateSecurityStampAsync ends existing sessions instead of letting them ride out the cookie lifetime.
builder.Services
    .AddAuthentication()
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Events.OnValidatePrincipal = async context =>
        {
            var userManager = context.HttpContext.RequestServices
                .GetRequiredService<UserManager<IdentityUser>>();
            var subject = context.Principal?.FindFirstValue("sub");
            var user = subject is null ? null : await userManager.FindByIdAsync(subject);
            var snapshot = context.Principal?.FindFirstValue("security_stamp");

            if (user is null || await userManager.GetSecurityStampAsync(user) != snapshot)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(context.Scheme.Name);
            }
        };
    });

builder.Services
    .AddDistributedMemoryCache();

var app = builder.Build();

// Create the Identity schema and seed a first user, so the sample runs out of the box.
// A real deployment replaces this block with EF migrations and its own registration flow.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();

    var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
#if (HasSampleUser)
    // A development account with a login known from the template: a host started in any other environment
    // seeds no user, so a deployed provider never holds it
    if (app.Environment.IsDevelopment() && await users.FindByEmailAsync("SAMPLE_USER_EMAIL") is null)
    {
        var user = new IdentityUser
        {
            UserName = "SAMPLE_USER_EMAIL",
            Email = "SAMPLE_USER_EMAIL",
            EmailConfirmed = true,
        };
#if (HasSamplePassword)
        // Seed password supplied at scaffold time via --password. Identity stores only its salted
        // PBKDF2 hash, never this plaintext; the value below is the one-time seed, not a stored secret.
        var password = "SAMPLE_USER_PASSWORD";
#else
        // Generate the seed password instead of hardcoding one: nothing sensitive lands in source,
        // and Identity stores only its salted PBKDF2 hash, never the plaintext.
        var password = GeneratePassword();
#endif
        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
            throw new InvalidOperationException(string.Join("; ", created.Errors.Select(e => e.Description)));

        // profile claims Identity does not model live in its claim store:
        // the IdentityUserInfoProvider surfaces them when the profile scope asks
        await users.AddClaimAsync(user, new Claim("name", "John Doe"));
#if (!HasSamplePassword)

#warning DEV convenience only: even on the console this credential reaches container stdout and any log sink scraping it. A real deployment never surfaces a password: seed with no usable password and email a reset link, or force a change on first login.
        // Show the generated password once, on the console, so the sample is runnable out of the box.
        Console.WriteLine($"Seeded {user.Email} with a one-time generated password: {password}");

        // Pause so an operator at a terminal can copy the password before startup logs scroll it away.
        // Skipped when stdin is not interactive (container, CI, a background run) so it never hangs a headless start.
        if (!Console.IsInputRedirected)
        {
            Console.WriteLine("Copy it now, then press Enter to start the server...");
            Console.ReadLine();
        }

        // Builds a cryptographically random password that satisfies the default Identity policy: one
        // character guaranteed from each required class (upper, lower, digit, non-alphanumeric), the rest
        // drawn from the full set, then shuffled so the guaranteed characters are not always in front.
        static string GeneratePassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@#$%^&*-_";
            const string all = upper + lower + digits + special;

            var chars = new char[16];
            chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
            chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
            chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
            chars[3] = special[RandomNumberGenerator.GetInt32(special.Length)];
            for (var i = 4; i < chars.Length; i++)
                chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

            for (var i = chars.Length - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars);
        }
#endif
    }
#endif

    // Seed the OIDC store so the sample runs out of the box: a signing key that survives restarts, and the
    // clients the configuration declares. On the next run they already exist, so the same key keeps validating
    // tokens issued before the restart. A real deployment replaces EnsureCreated with EF migrations, and keeps
    // signing keys in an HSM or KMS rather than the database (see the #warning on PersistedSigningKey).
    var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();

    var oidcStoreFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<OidcStoreDbContext>>();
    await using (var oidcStore = await oidcStoreFactory.CreateDbContextAsync())
    {
        await oidcStore.Database.EnsureCreatedAsync();

        if (!await oidcStore.SigningKeys.AnyAsync(key => key.TenantId == tenant.Id
                                                         && key.Generation == tenant.Generation
                                                         && key.IsActive
                                                         && key.Usage == PublicKeyUsages.Signature))
        {
            var keyProtector = scope.ServiceProvider
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector(DatabaseKeysProvider.SigningKeyProtectorPurpose);

            var signingKey = JsonWebKeyFactory.CreateRsa(PublicKeyUsages.Signature);
            oidcStore.SigningKeys.Add(new PersistedSigningKey
            {
                KeyId = signingKey.KeyId!,
                TenantId = tenant.Id,
                Generation = tenant.Generation,
                Usage = PublicKeyUsages.Signature,
                JwkJson = keyProtector.Protect(JsonSerializer.Serialize<JsonWebKey>(signingKey)),
                CreatedAt = clock.GetUtcNow(),
            });
            await oidcStore.SaveChangesAsync();
        }
    }

    // The clients the configuration declares under Oidc:Clients, written through the server's client store as a
    // registration would be. Only appsettings.Development.json declares any, the demo clients test_client and
    // console_client, so a host started in any other environment seeds none. A registration holds the identifier
    // of the registration access token that manages it. No token is issued for a seeded client, so it holds the
    // identifier of one never issued, and no request to the RFC 7592 configuration endpoint can change or remove
    // it. On the next run the client is already held, so the add is refused and the stored row is kept as it is.
#warning DEV convenience only: the demo clients in appsettings.Development.json share the secret "secret", published with this template. A row seeded once, theirs or any configured client's, stays in oidc.db whatever environment later runs on it and whatever the configuration later says (table Clients, ClientKey the id in upper case: TEST_CLIENT and CONSOLE_CLIENT for the demo clients). Before a real deployment delete those rows, or start from a database that was never run in Development.
    var configured = ReadClients(app.Configuration);

    // The client store takes whatever it is given: a client the server could never authenticate (a plain secret
    // where a hash belongs, a hex digest) would start a host that refuses its every token request, and of a
    // repeated id only the first would be kept, without a word. The checks the server runs at startup on the
    // clients a tenant declares judge these instead.
    var oidcOptions = scope.ServiceProvider.GetRequiredService<IOptions<OidcOptions>>().Value;
    var refusals = scope.ServiceProvider.GetServices<ITenantsCheck>()
        .SelectMany(check => check.Check([DefineTenant(configured)], oidcOptions))
        .Select(refusal => refusal.Message)
        .ToList();
    if (refusals.Count > 0)
        throw new InvalidOperationException($"The Oidc:Clients configuration section declares clients the server refuses: {string.Join(" ", refusals)}");

    var clients = scope.ServiceProvider.GetRequiredService<IClientInfoManager>();
    var tokenIds = scope.ServiceProvider.GetRequiredService<ITokenIdGenerator>();
    foreach (var client in configured)
    {
        if (await clients.TryAddClientAsync(tenant, new RegisteredClient(client, tokenIds.GenerateTokenId())))
            continue;

        // Already stored: every restart lands here, and only a stored client that differs from its entry means
        // somebody's edit, a rotated secret say, is not in force.
        var stored = await clients.TryFindRegisteredClientAsync(tenant, client.ClientId);
        if (stored is null || JsonSerializer.Serialize(stored.ClientInfo) != JsonSerializer.Serialize(client))
        {
            app.Logger.LogWarning(
                "Client {ClientId} is already stored, so its entry in Oidc:Clients was not applied to it. To seed it " +
                "again from the configuration, delete its row from oidc.db (table Clients, ClientKey the id in upper case)",
                client.ClientId);
        }
    }
}

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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
#if (UseMvc)
    app.UseExceptionHandler("/Home/Error");
#else
    app.UseExceptionHandler(MinimalApiPages.ErrorPath);
#endif
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors();
app.UseAuthorization();

#if (UseMvc)
// The library's controllers carry no routes of their own, so the default route below never reaches them
app.MapOidcControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
#else
// The Minimal API counterpart of the MVC host's app.MapOidcControllers()
app.MapOidcEndpoints();
app.MapHostPages();
#endif

// The relying party of test_client, so a browser can walk a whole sign-in on this project in Development
app.MapDemoClient(ReadClients(app.Configuration));

app.Run();
