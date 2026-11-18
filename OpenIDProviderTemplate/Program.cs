using System.Security.Cryptography;
using System.Text;
using Abblix.DependencyInjection;
using Abblix.Jwt;
using Abblix.Oidc;
using Abblix.Oidc.Server.Common.Constants;
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

    // Configure the issuer's scopes, resources and clients here:

    // Scopes = [new ScopeDefinition(/*...*/)],

    //Resources =
    //[
    //    new(new Uri(/*...*/, UriKind.Absolute), new ScopeDefinition(/*...*/)),
    //    /*...*/
    //],

    // For production, register clients in a durable store rather than hardcoding them; the block below is only a demo. See https://docs.abblix.com/docs/durable-client-store
    //Clients =
    //[
    //    new ClientInfo(/*...*/) {
    //        ClientSecrets = [new ClientSecret { Sha512Hash = ToSha512Hash(/*...*/) }],
    //        TokenEndpointAuthMethod = ClientAuthenticationMethods./*...*/,
    //        AllowedGrantTypes = [GrantTypes./*...*/],
    //        OfflineAccess = OfflineAccess./*...*/,
    //        PkceRequired = /*...*/,
    //        RedirectUris = [new Uri(/*...*/, UriKind.Absolute)],
    //        PostLogoutRedirectUris = [new Uri(/*...*/, UriKind.Absolute)],
    //    },
    //    /*...*/
    //],

    // The following URL leads to Login action of AuthController
    LoginUri = new Uri("/Auth/Login", UriKind.Relative),

    #warning Dev-only: signing key is regenerated on every startup; provide a persistent, shared key for production via a custom IAuthServiceKeysProvider. https://docs.abblix.com/docs/signing-key-persistence
    SigningKeys = [JsonWebKeyFactory.CreateRsa(PublicKeyUsages.Signature)],
}));

static byte[] ToSha512Hash(string source) => SHA512.HashData(Encoding.UTF8.GetBytes(source));

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
