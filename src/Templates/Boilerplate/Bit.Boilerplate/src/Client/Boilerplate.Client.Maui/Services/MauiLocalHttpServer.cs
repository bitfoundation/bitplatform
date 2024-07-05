#if LocalHttpServerEnabled
using System.Net;
using System.Net.Sockets;
using Microsoft.Maui.Platform;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Extensions;
using Boilerplate.Client.Core;

namespace Boilerplate.Client.Maui.Services;

public partial class MauiLocalHttpServer(IServiceCollection services) : ILocalHttpServer
{
    private int port = -1;
    private Task<int>? startTask;
    private WebApplication? localHttpServer;

    public Task<int> Start()
    {
        return startTask ??= StartImplementation();
    }

    public int Port => port;

    private async Task<int> StartImplementation()
    {
        var builder = WebApplication.CreateEmptyBuilder(options: new()
        {
            ApplicationName = "LocalHttpServer",
            ContentRootPath = Directory.GetCurrentDirectory(),
            EnvironmentName = BuildConfiguration.IsDebug() ? Environments.Development : Environments.Production,
            WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot")
        });

        port = GetAvailableTcpPort();

        builder.WebHost.UseKestrel(options => options.ListenLocalhost(port));

        builder.Services.AddAuthorization();
        builder.Services.AddRouting();
        builder.Services.AddRange(services);

        var app = localHttpServer = builder.Build();

        app.UseStaticFiles(); // Put static files in wwwroot folder of the Client.Maui project.

        app.MapGet("sign-in", async (HttpContext context, IConfiguration configuration) =>
        {
            await Routes.OpenUniversalLink(context.Request.GetEncodedPathAndQuery(), replace: true);

            var url = $"{configuration.GetServerAddress()}/api/Identity/SocialSignedIn?culture={CultureInfo.CurrentUICulture.Name}";
            context.Response.Redirect(url);
        });

        await app.StartAsync();

        return port;
    }

    private int GetAvailableTcpPort()
    {
        using TcpListener l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }
}
#endif
