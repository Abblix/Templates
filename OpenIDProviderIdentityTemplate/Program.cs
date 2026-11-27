using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Abblix.DependencyInjection;
using Abblix.Jwt;
using Abblix.Oidc;
using Abblix.Oidc.Server.AspNetCore.Formatters;
using Abblix.Oidc.Server.AspNetCore.MultiTenancy;
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
builder.Services.AddHttpContextAccessor();
#endif

// Grants every client everything it asks for, which suits this provider: its clients are all its own.
// To ask the end user for consent, register your own IUserConsentsProvider instead.
builder.Services.AddConsentGrantedForEveryClient();

// Register Abblix OIDC Server. Its tenants, each an issuer with the clients it declares, come from oidc.db through
// the tenant store registered below rather than from these options. Serving more than one issuer needs a license,
// set here as options.LicenseJwt: https://www.abblix.com/en/docs/multi-tenancy#license
builder.Services.AddOidcServices(_ => { });

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

// Tenants: the database-backed store takes the place of the server's default, which serves the tenants of the
// options. Registered after AddOidcServices, it replaces that default, since the last registration of a service is
// the one resolved. The server reads it again on a timer (OidcOptions.RefreshTenantsEvery), so a tenant or a client
// changed in oidc.db is served as changed without a restart.
builder.Services.AddSingleton<ITenantStore, DurableTenantStore>();

// Signing keys: the database-backed provider takes the place of the server's default, which reads each
// tenant's SigningKeys from its definition. Registered after AddOidcServices, it replaces that default,
// since the last registration of a service is the one resolved, and answers for each tenant's keys, so
// the tenants declare none. The opening gives a tenant its first key before the server serves it.
builder.Services.AddSingleton<IAuthServiceKeysProvider, DatabaseKeysProvider>();
builder.Services.AddSingleton<ITenantOpening, SigningKeyOpening>();

// Client store: the server keeps its own, which serves the clients a tenant declares first, and keeps
// the dynamic registrations in the store the host registers here instead of in memory. A singleton, since the
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

#warning The server keeps authorization codes, token revocations and its other short-lived state in an in-process cache, where a restart empties it and other instances cannot see it; register a persistent, shared IDistributedCache (Redis/SQL) here for production. https://docs.abblix.com/docs/choosing-a-cache-backend

var app = builder.Build();

// Create the Identity schema and seed a first user, so the sample runs out of the box.
// A real deployment replaces this block with EF migrations and its own registration flow.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();

    var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
#if (HasSampleUser)
    // A development account with a login known from the template, so only a Development run creates it. Once
    // created it stays in users.db, so a deployment starts from a users.db that never ran in Development
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

    // Create the OIDC store with its initial rows (InitialData): the tenant and the demo clients. The signing key
    // is created when the server first readies the tenant (SigningKeyOpening). A real deployment replaces
    // EnsureCreated with EF migrations.
    var oidcStoreFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<OidcStoreDbContext>>();
    await using var oidcStore = await oidcStoreFactory.CreateDbContextAsync();
    await oidcStore.Database.EnsureCreatedAsync();
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

// The tenants come from a store of the host's own, so each request is matched to its tenant by its address
// before it is routed
app.UseMultiTenancy();
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
if (app.Environment.IsDevelopment())
    app.MapDemoClient();

app.Run();
