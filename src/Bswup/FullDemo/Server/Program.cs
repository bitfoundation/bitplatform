var builder = WebApplication.CreateBuilder(args);

#if DEBUG
if (OperatingSystem.IsWindows())
{
    builder.WebHost.UseUrls("https://localhost:5021", "http://localhost:5020", "https://*:5021", "http://*:5020");
}
else
{
    builder.WebHost.UseUrls("https://localhost:5021", "http://localhost:5020");
}
#endif

Bit.Bswup.Demo.Server.Startup.Services.Add(builder.Services);

var app = builder.Build();

Bit.Bswup.Demo.Server.Startup.Middlewares.Use(app, builder.Environment);

app.Run();
