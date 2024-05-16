using System.Net.Sockets;
using System.Net;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Bit.BlazorUI;

namespace Boilerplate.Client.Windows.Services;

public partial class WindowsLocalHttpServer(IServiceCollection services) : ILocalHttpServer
{
    private WebApplication? localHttpServer;
    private Task? startTask;
    private int port;

    public Task Start()
    {
        return startTask ??= StartImplementation();
    }

    public int Port => port;

    private async Task StartImplementation()
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

        app.UseStaticFiles(); // Put static files in wwwroot folder of the Client.Windows project.

        app.MapGet("social-login", async (HttpContext context, HtmlRenderer htmlRenderer) =>
        {
            // await Routes.OpenUniversalLink(context.Request.GetEncodedPathAndQuery());
            var body = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
            {
                var renderedComponent = await htmlRenderer.RenderComponentAsync<BitButton>(ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                }));
                return renderedComponent.ToHtmlString();
            });
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(body);
        });

        await app.StartAsync();
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
