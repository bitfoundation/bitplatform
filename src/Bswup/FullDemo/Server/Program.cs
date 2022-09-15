var builder = WebApplication.CreateBuilder(args);

#if DEBUG
if (OperatingSystem.IsWindows())
{
    builder.WebHost.UseUrls("https://localhost:5001", "http://localhost:5000", "https://*:5001", "http://*:5000");
}
#endif

Bit.Bswup.Demo.Server.Startup.Services.Add(builder.Services);

var app = builder.Build();

Bit.Bswup.Demo.Server.Startup.Middlewares.Use(app, builder.Environment);

app.Run();
