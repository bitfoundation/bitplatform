#if Windows // You can register local http server for other platforms if needed

using Microsoft.AspNetCore.Builder;
using System.Net.Sockets;
using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Extensions;
using Boilerplate.Client.Core;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Bit.BlazorUI;

namespace Boilerplate.Client.Maui.Services;

public partial class MauiLocalHttpServer : ILocalHttpServer
{
    [AutoInject] IPubSubService pubSubService = default!;
    [AutoInject] IExceptionHandler exceptionHandler = default!;
    [AutoInject] IServiceCollection services = default!;

    private WebApplication? _localHttpServer;
    private Task<int?>? _startTask;


    public Task<int?> Start()
    {
        return _startTask ??= StartImplementation();
    }

    private async Task<int?> StartImplementation()
    {
        var builder = WebApplication.CreateEmptyBuilder(options: new()
        {
            ApplicationName = "LocalHttpServer",
            ContentRootPath = Directory.GetCurrentDirectory(),
            EnvironmentName = BuildConfiguration.IsDebug() ? Environments.Development : Environments.Production,
            WebRootPath = AppContext.BaseDirectory
        });

        var availablePort = GetAvailableTcpPort();

        builder.WebHost.UseKestrel(options => options.ListenLocalhost(availablePort));

        builder.Services.AddAuthorization();
        builder.Services.AddRouting();
        builder.Services.TryAddTransient<HtmlRenderer>();

        builder.Services.AddRange(services);

        var app = _localHttpServer = builder.Build();

        // register all other aspnetcore routes here.

        app.MapGet("social-login", async context =>
        {
            // await Routes.OpenUniversalLink(context.Request.GetEncodedPathAndQuery());

            var htmlRenderer = context.RequestServices.GetRequiredService<HtmlRenderer>();

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

        return availablePort;
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
