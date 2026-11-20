using System.Net;

namespace AspNetIdentitySample;

/// <summary>
/// The frame of the pages this host renders as plain HTML: the demo client's pages and the sign-out pages, and,
/// on Minimal API, every page of the host.
/// </summary>
public static class Pages
{
    /// <summary>HTML-encodes a value for an element's text or an attribute in double quotes.</summary>
    public static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    /// <summary>A whole page around the given body, which the caller has already encoded.</summary>
    public static string Layout(string title, string body) => $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <title>{{Encode(title)}} - AspNetIdentitySample</title>
            <link rel="stylesheet" href="/lib/bootstrap-bundle.css" />
            <link rel="stylesheet" href="/css/site.css" />
        </head>
        <body class="h-100">
            <div class="d-flex flex-column min-vh-100">
                <header>
                    <nav class="navbar navbar-expand-sm navbar-light bg-white border-bottom mb-3">
                        <div class="container-fluid">
                            <a class="navbar-brand" href="/">AspNetIdentitySample</a>
                        </div>
                    </nav>
                </header>
                <main role="main" class="m-auto pb-3">
                    {{body}}
                </main>
                <footer class="border-top footer text-muted">
                    <div class="container">
                        &copy; 2026 - Powered by <a href="https://github.com/Abblix/Oidc.Server" target="_blank">Abblix OIDC Server</a>
                    </div>
                </footer>
            </div>
            <script type="module" src="/lib/bootstrap-bundle.js"></script>
        </body>
        </html>
        """;

    /// <summary>A page with one card holding the given body.</summary>
    public static string Card(string title, string body) => Layout(title, $"""
        <div class="card shadow-sm">
            <div class="card-body">
                <div class="container">
                    <h1 class="h4 mb-3">{Encode(title)}</h1>
                    {body}
                </div>
            </div>
        </div>
        """);
}
