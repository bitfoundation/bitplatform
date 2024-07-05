﻿//+:cnd:noEmit

namespace Boilerplate.Api;

public static partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddSharedConfigurations();

        // The following line (using the * in the URL), allows the emulators and mobile devices to access the app using the host IP address.
        if (BuildConfiguration.IsDebug() && OperatingSystem.IsWindows())
        {
            builder.WebHost.UseUrls("http://localhost:5031", "http://*:5031");
        }

        builder.ConfigureApiServices();
        builder.Services.AddSharedProjectServices();

        var app = builder.Build();

        if (BuildConfiguration.IsDebug())
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        app.ConfiureMiddlewares();

        await app.RunAsync();
    }
}