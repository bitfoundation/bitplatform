//-:cnd:noEmit
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddClientConfigurations();

// The following line (using the * in the URL), allows the emulators and mobile devices to access the app using the host IP address.
if (BuildConfiguration.IsDebug() && OperatingSystem.IsWindows())
{
    builder.WebHost.UseUrls("http://localhost:5030", "http://*:5030");
}

builder.Services.AddServerServices(builder.Environment, builder.Configuration);

var app = builder.Build();

app.ConfiureMiddlewares(builder.Environment, builder.Configuration);

await app.RunAsync();
