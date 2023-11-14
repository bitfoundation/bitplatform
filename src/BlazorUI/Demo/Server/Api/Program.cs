var builder = WebApplication.CreateBuilder(args);

#if BlazorWebAssembly
builder.Configuration.AddClientConfigurations();
#endif

#if DEBUG
if (OperatingSystem.IsWindows())
{
// The following line (using the * in the URL), allows the emulators and mobile devices to access the app using the host IP address.
    builder.WebHost.UseUrls("https://localhost:5001", "http://localhost:5000", "https://*:5001", "http://*:5000");
}
else
{
    builder.WebHost.UseUrls("https://localhost:5001", "http://localhost:5000");
}
#endif

Bit.BlazorUI.Demo.Server.Api.Startup.Services.Add(builder.Services, builder.Environment, builder.Configuration);

var app = builder.Build();

Bit.BlazorUI.Demo.Server.Api.Startup.Middlewares.Use(app, builder.Environment, builder.Configuration);

app.Run();
