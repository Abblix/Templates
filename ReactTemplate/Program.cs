var builder = WebApplication.CreateBuilder(args);

// The build writes the Vite dev server's address beside the app, in spa.proxy.json, for the SPA proxy; the deep
// link redirect below reads it from there too
if (builder.Environment.IsDevelopment())
    builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "spa.proxy.json"), optional: true);
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

// The app calls this from the Vite dev server too: vite.config.ts proxies /api to this host.
app.MapGet("/api/greeting", () => new { message = "Hello from ASP.NET Core" });

// Any other path under /api answers 404, so a call to an endpoint the host lacks fails visibly instead of
// receiving the SPA's index.html
app.Map("/api/{**path}", () => Results.NotFound());

// In Development the Vite dev server serves the SPA, and the SPA proxy sends only the root there, so a deep link
// opened on the host goes to the same path on the dev server. A published host serves the built index.html for it.
var devServer = app.Configuration["SpaProxyServer:ServerUrl"];
if (app.Environment.IsDevelopment() && !string.IsNullOrEmpty(devServer))
{
    app.MapFallback(context =>
    {
        context.Response.Redirect(devServer + context.Request.Path + context.Request.QueryString);
        return Task.CompletedTask;
    });
}
else
{
    app.MapFallbackToFile("index.html");
}

app.Run();
