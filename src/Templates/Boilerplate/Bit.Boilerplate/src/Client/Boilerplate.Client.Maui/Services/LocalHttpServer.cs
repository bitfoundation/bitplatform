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
using Boilerplate.Client.Maui.Components.Pages;

namespace Boilerplate.Client.Maui.Services;

/// <summary>
/// <inheritdoc cref="HeaderName.LocalHttpServerPort"/>
/// </summary>
public partial class LocalHttpServer
{
    [AutoInject] IPubSubService pubSubService = default!;
    [AutoInject] IExceptionHandler exceptionHandler = default!;

    private WebApplication? _localHttpServer;

    public async void Start(IServiceCollection services)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3));

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

            app.Run(async context =>
            {
                await Routes.OpenUniversalLink(context.Request.GetEncodedPathAndQuery());

                var htmlRenderer = context.RequestServices.GetRequiredService<HtmlRenderer>();

                var body = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
                {
                    var renderedComponent = await htmlRenderer.RenderComponentAsync<LocalHttpServerPage>(ParameterView.FromDictionary(new Dictionary<string, object?>
                    {

                    }));

                    return renderedComponent.ToHtmlString();
                });

                context.Response.ContentType = "text/html";

                await context.Response.WriteAsync(body);
            });

            await app.StartAsync();

            pubSubService.Publish(PubSubMessages.LOCAL_HTTP_SERVER_STARTED, availablePort);
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp);
        }
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
