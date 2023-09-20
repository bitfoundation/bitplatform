//-:cnd:noEmit
var builder = WebApplication.CreateBuilder(args);

#if DEBUG
if (OperatingSystem.IsWindows())
{
// The following line (using the * in the URL), allows the emulators and mobile devices to access the app using the host IP address.
    builder.WebHost.UseUrls("https://localhost:5041", "http://localhost:5040", "https://*:5041", "http://*:5040");
}
else
{
    builder.WebHost.UseUrls("https://localhost:5041", "http://localhost:5040");
}
#endif

TodoTemplate.Server.Api.Startup.Services.Add(builder.Services, builder.Environment, builder.Configuration);

var app = builder.Build();

TodoTemplate.Server.Api.Startup.Middlewares.Use(app, builder.Environment, builder.Configuration);

app.Run();
