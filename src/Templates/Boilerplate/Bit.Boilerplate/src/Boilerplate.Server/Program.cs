//-:cnd:noEmit
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddClientConfigurations();

if (BuildConfigurationModeDetector.Current.IsDebug())
{
    // The following line (using the * in the URL), allows the emulators and mobile devices to access the app using the host IP address.
    if (OperatingSystem.IsWindows())
    {
        builder.WebHost.UseUrls("https://localhost:5031", "http://localhost:5030", "https://*:5031", "http://*:5030");
    }
    else
    {
        builder.WebHost.UseUrls("https://localhost:5031", "http://localhost:5030");
    }
}

Boilerplate.Server.Startup.Services.Add(builder.Services, builder.Environment, builder.Configuration);

var app = builder.Build();

Boilerplate.Server.Startup.Middlewares.Use(app, builder.Environment, builder.Configuration);

app.Run();
