using System.Text.Json.Nodes;
using Abblix.Oidc.Server.Common.Configuration;
using Abblix.Oidc.Server.Common.Interfaces;
using Abblix.Oidc.Server.Features.RandomGenerators;
using Abblix.Oidc.Server.Features.UserAuthentication;
using Abblix.Oidc.Server.Model;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;
using UriBuilder = Abblix.Utils.UriBuilder;

namespace AspNetIdentitySample.Controllers;

public class AuthController : Controller
{
    // GET: Auth/Login
    public IActionResult Login([FromQuery(Name = "request_uri")] string? requestUri)
    {
        // Return a view with login/password inputs and sign-in button
        return View(new { requestUri });
    }

    // POST: Auth/Login
    [HttpPost]
    public async Task<IActionResult> Login(
        [FromServices] IAntiforgery antiforgery,
        [FromServices] IAuthSessionService authService,
        [FromServices] ISessionIdGenerator sessionIdGenerator,
        [FromServices] IOidcEndpointResolver endpointResolver,
        [FromServices] UserManager<IdentityUser> userManager,
        [FromServices] SignInManager<IdentityUser> signIn,
        [FromServices] TimeProvider clock,
        [FromForm] string? email,
        [FromForm] string? password,
        [FromForm] string? requestUri)
    {
        // A page on another site could submit this form with the attacker's own credentials: the victim's browser
        // would be signed in as the attacker, and whatever the victim then authorizes or enters lands in the
        // attacker's account. The antiforgery token the form carries is what such a page cannot supply. A form
        // opened in another tab before signing in carries a token that no longer matches, so it is shown again.
        if (!await antiforgery.IsRequestValidAsync(HttpContext))
        {
            ModelState.AddModelError("", "The sign-in form has expired, please sign in again");
            return View(new { requestUri });
        }

        var user = string.IsNullOrEmpty(email) ? null : await userManager.FindByEmailAsync(email);

        // verifies the hash and enforces lockout, but issues no cookie: the session stays the library's
        var result = user is not null && !string.IsNullOrEmpty(password)
            ? await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)
            : SignInResult.Failed;

        if (!result.Succeeded)
        {
            // Return an error message to the view to inform the user
            ModelState.AddModelError("", "Invalid username or password");
            return View(new { requestUri });
        }

        // If authentication is successful, create a new authentication session
        var authSession = new AuthSession(
            user!.Id,                                        // the subject the claims adapter resolves later
            sessionIdGenerator.GenerateSessionId(),
            clock.GetUtcNow(),
            CookieAuthenticationDefaults.AuthenticationScheme)
        {
            Email = user.Email,
            EmailVerified = user.EmailConfirmed,
            AuthenticationMethodReferences = ["pwd"],        // lands in the amr claim

            // snapshot the security stamp: the cookie's OnValidatePrincipal compares it on every
            // request, so a password change or UpdateSecurityStampAsync ends this session
            AdditionalClaims = new JsonObject
            {
                ["security_stamp"] = await userManager.GetSecurityStampAsync(user),
            },
        };

        // Sign in the user using the authentication service
        await authService.SignInAsync(authSession);

        // A visit to the login page on its own carries no authorization request to resume
        if (string.IsNullOrEmpty(requestUri))
            return Redirect("/");

        // Redirect the user to the authorization endpoint URL, recovering the OIDC flow. Every endpoint is
        // enabled here, as the defaults leave them, so the resolver always has an address to return.
        var authorizeUrl = new UriBuilder(endpointResolver.Resolve(OidcEndpoints.Authorize)!)
            { Query = { [AuthorizationRequest.Parameters.RequestUri] = requestUri } };
        return Redirect(authorizeUrl);
    }
}
