var builder = WebApplication.CreateBuilder(args);

// We need to load the client app configurations to prerender the app on server side.
builder.Configuration.AddClientConfigurations();

Bit.Websites.Platform.Server.Startup.Services.Add(builder.Services, builder.Environment, builder.Configuration);

var app = builder.Build();

Bit.Websites.Platform.Server.Startup.Middlewares.Use(app, builder.Environment, builder.Configuration);

// Start the host first so startup failures propagate immediately, then await the
// SCSS watcher alongside shutdown (the watcher ties its own lifetime to app shutdown).
await app.StartAsync();

await Task.WhenAll(
    app.WaitForShutdownAsync(),
    Bit.Websites.Platform.Server.Services.ScssCompilerService.WatchScssFiles(app) /* Development-only, no-op otherwise */);
