var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddClientConfigurations();

// UseUrls overrides the urls and the http(s)_ports configuration alike, however they were provided
// (--urls / --http_ports / --https_ports on the command line, ASPNETCORE_URLS /
// ASPNETCORE_HTTP(S)_PORTS in the environment, launchSettings), so the debug defaults only apply
// when none of them is set.
if (BuildConfiguration.IsDebug() &&
    builder.Configuration["urls"] is null &&
    builder.Configuration["http_ports"] is null &&
    builder.Configuration["https_ports"] is null)
{
    // The following line (using the * in the URL), allows the emulators and mobile devices to access the app using the host IP address.
    if (OperatingSystem.IsWindows())
    {
        builder.WebHost.UseUrls("https://localhost:5001", "http://localhost:5000", "https://*:5001", "http://*:5000");
    }
    else
    {
        builder.WebHost.UseUrls("https://localhost:5001", "http://localhost:5000");
    }
}

Bit.BlazorUI.Demo.Server.Startup.Services.Add(builder.Services, builder.Environment, builder.Configuration);

var app = builder.Build();

Bit.BlazorUI.Demo.Server.Startup.Middlewares.Use(app, builder.Environment, builder.Configuration);

// Start the host first so startup failures propagate immediately, then await the
// SCSS watcher alongside shutdown (the watcher ties its own lifetime to app shutdown).
await app.StartAsync();

await Task.WhenAll(
    app.WaitForShutdownAsync(),
    Bit.BlazorUI.Demo.Server.Services.ScssCompilerService.WatchScssFiles(app) /* Development-only, no-op otherwise */);
