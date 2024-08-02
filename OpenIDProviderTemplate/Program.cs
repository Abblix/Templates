using System.Security.Cryptography;
using System.Text;
using Abblix.DependencyInjection;
using Abblix.Jwt;
using Abblix.Oidc.Server.Common.Constants;
using Abblix.Oidc.Server.Features.ClientInformation;
using Abblix.Oidc.Server.Features.UserInfo;
using Abblix.Oidc.Server.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIDProviderApp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add the TestUserStorage as a singleton service in the DI container.
var userInfoStorage = new TestUserStorage(
    // new UserInfo(
    //     Subject: /* Unique ID of the user account */,
    //     Name: /* User name */,
    //     Email: /* Used as login */,
    //     Password: /* Prefer strong password */)
);
builder.Services.AddSingleton(userInfoStorage);

// Use AddAlias to register TestUserStorage also as an implementation of IUserInfoProvider.
builder.Services.AddAlias<IUserInfoProvider, TestUserStorage>();

// Register and configure Abblix OIDC Server
builder.Services.AddOidcServices(options =>
{
	// Configure OIDC Server options here:
	
    // options.Scopes = [new ScopeDefinition(/*...*/)];

    //options.Resources =
    //[
    //    new(new Uri(/*...*/, UriKind.Absolute), new ScopeDefinition(/*...*/)),
	//    /*...*/
    //];
	
    //options.Clients = new[] {
    //    new ClientInfo(/*...*/) {
    //        ClientSecrets = [new ClientSecret { Sha512Hash = ToSha512Hash(/*...*/) }],
    //        TokenEndpointAuthMethod = ClientAuthenticationMethods./*...*/,
    //        AllowedGrantTypes = [GrantTypes./*...*/],
    //        ClientType = ClientType.Confidential,
    //        OfflineAccessAllowed = /*...*/,
    //        PkceRequired = /*...*/,
    //        RedirectUris = [new Uri(/*...*/, UriKind.Absolute)],
    //        PostLogoutRedirectUris = [new Uri(/*...*/, UriKind.Absolute)],
    //   },
	//   /*...*/
    //};
	
	// The following URL leads to Login action of AuthController
    options.LoginUri = new Uri($"/Auth/Login", UriKind.Relative);
	
	// The following line generates a new key for token signing. Replace it if you want to use your own keys.
    options.SigningKeys = new[] { JsonWebKeyFactory.CreateRsa(JsonWebKeyUseNames.Sig) };
});

static byte[] ToSha512Hash(string source) => SHA512.HashData(Encoding.UTF8.GetBytes(source));

// Add authentication services
builder.Services
    .AddAuthentication()
    .AddCookie();

// NOTE! This code is for demonstation purposes only. Do not use MemoryCache as a Distributed cache implementation in production environment.
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
