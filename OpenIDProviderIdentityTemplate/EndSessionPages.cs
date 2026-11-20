using Abblix.Oidc.Server.AspNetCore.Formatters;
using Abblix.Oidc.Server.Common;
using Abblix.Oidc.Server.Common.Configuration;
using Abblix.Oidc.Server.Common.Interfaces;
using Abblix.Oidc.Server.Endpoints.EndSession.Interfaces;
using Abblix.Utils;
#if (UseMvc)
using Microsoft.AspNetCore.Mvc;
#endif
using EndSessionRequest = Abblix.Oidc.Server.Model.EndSessionRequest;
#if (UseMvc)
using TResult = Microsoft.AspNetCore.Mvc.ActionResult;
#else
using TResult = Microsoft.AspNetCore.Http.IResult;
#endif

namespace AspNetIdentitySample;

/// <summary>
/// The two sign-out pages the server leaves to its host: asking the user to confirm a sign-out no client vouched
/// for with an ID token, and telling them they are signed out when no client asked to be sent back. Every other
/// answer of the end-session endpoint is the library's own.
/// </summary>
/// <remarks>
/// The confirmation page sends the request back with the value the server issued for it, which is how the server
/// learns the user agreed; a page on another site cannot read that value, so it cannot sign the user out.
/// </remarks>
public sealed class EndSessionPages(
    IEndSessionResponseFormatter<TResult> inner,
    IOidcEndpointResolver endpointResolver) : IEndSessionResponseFormatter<TResult>
{
    public Task<TResult> FormatResponseAsync(EndSessionRequest request, Result<IEndSessionResponse, OidcError> response)
    {
        if (response.TryGetSuccess(out var success))
        {
            switch (success)
            {
                case ConfirmationRequired confirmation:
                    return Task.FromResult(Html(ConfirmationPage(request, confirmation.Confirmation)));

                case EndSessionSuccess { PostLogoutRedirectUri: null, FrontChannelLogoutRequestUris.Count: 0 }:
                    return Task.FromResult(Html(Pages.Card("Signed out", "<p>You are signed out.</p>")));
            }
        }

        return inner.FormatResponseAsync(request, response);
    }

    private string ConfirmationPage(EndSessionRequest request, string confirmation)
    {
        var fields = new List<(string Name, string? Value)>
        {
            (EndSessionRequest.Parameters.IdTokenHint, request.IdTokenHint),
            (EndSessionRequest.Parameters.LogoutHint, request.LogoutHint),
            (EndSessionRequest.Parameters.ClientId, request.ClientId),
            (EndSessionRequest.Parameters.PostLogoutRedirectUri, request.PostLogoutRedirectUri?.OriginalString),
            (EndSessionRequest.Parameters.State, request.State),
            (EndSessionRequest.Parameters.Confirmation, confirmation),
        };

        var hidden = string.Concat(fields
            .Where(field => field.Value != null)
            .Select(field => $"<input type=\"hidden\" name=\"{field.Name}\" value=\"{Pages.Encode(field.Value)}\" />"));

        var action = Pages.Encode(endpointResolver.Resolve(OidcEndpoints.EndSession)!.OriginalString);
        return Pages.Card("Sign out?", $"""
            <p>Sign out of this provider?</p>
            <form method="post" action="{action}">
                {hidden}
                <button type="submit" class="btn btn-primary" id="sign-out" autofocus>Sign out</button>
            </form>
            """);
    }

#if (UseMvc)
    private static TResult Html(string page) => new ContentResult { Content = page, ContentType = "text/html" };
#else
    private static TResult Html(string page) => Results.Content(page, "text/html");
#endif
}
