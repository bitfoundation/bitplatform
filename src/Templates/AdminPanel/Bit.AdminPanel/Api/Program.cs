//-:cnd:noEmit
var builder = WebApplication.CreateBuilder(args);

#if DEBUG
// The following line (using the * in the URL), allows the emulators and mobile devices to access the app using the host IP address.
if (OperatingSystem.IsWindows())
{
    builder.WebHost.UseUrls("https://localhost:5001", "http://localhost:5000", "https://*:5001", "http://*:5000");
}
#endif

AdminPanel.Api.Startup.Services.Add(builder.Services, builder.Environment, builder.Configuration);

var app = builder.Build();

AdminPanel.Api.Startup.Middlewares.Use(app, builder.Environment, builder.Configuration);

app.Run();
