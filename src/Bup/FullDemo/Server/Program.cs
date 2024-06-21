var builder = WebApplication.CreateBuilder(args);

#if DEBUG
if (OperatingSystem.IsWindows())
{
    builder.WebHost.UseUrls("https://localhost:5011", "http://localhost:5010", "https://*:5011", "http://*:5010");
}
else
{
    builder.WebHost.UseUrls("https://localhost:5011", "http://localhost:5010");
}
#endif

Bit.Bup.Demo.Server.Startup.Services.Add(builder.Services);

var app = builder.Build();

Bit.Bup.Demo.Server.Startup.Middlewares.Use(app, builder.Environment);

app.Run();
