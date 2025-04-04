using EmbedIO;
using System.Net;
using EmbedIO.Actions;
using System.Net.Sockets;
using Boilerplate.Client.Core.Components;

namespace Boilerplate.Client.Windows.Services;

/// <summary>
/// <inheritdoc cref="ILocalHttpServer"/>
/// </summary>
public partial class WindowsLocalHttpServer : ILocalHttpServer
{
    [AutoInject] private IExceptionHandler exceptionHandler;
    [AutoInject] private AbsoluteServerAddressProvider absoluteServerAddress;

    private int port = -1;
    private WebServer? localHttpServer;

    public int Port => port;

    public string Origin => $"http://localhost:{port}";

    public int EnsureStarted()
    {
        if (port != -1)
            return port;

        port = GetAvailableTcpPort();

        localHttpServer = new WebServer(o => o
            .WithUrlPrefix($"http://localhost:{port}")
            .WithMode(HttpListenerMode.Microsoft))
            .WithModule(new ActionModule(Urls.SignInPage, HttpVerbs.Get, async ctx =>
            {
                try
                {
                    ctx.Redirect("/close-browser");

                    _ = Task.Delay(1)
                        .ContinueWith(async _ =>
                        {
                            await Routes.OpenUniversalLink(ctx.Request.Url.PathAndQuery, replace: true);
                        });
                }
                catch (Exception exp)
                {
                    exceptionHandler.Handle(exp);
                }
            }))
            .WithModule(new ActionModule("/close-browser", HttpVerbs.Get, async ctx =>
            {
                // Redirect to CloseBrowserPage.razor that will close the browser window.
                var url = new Uri(absoluteServerAddress, $"/api/Identity/CloseBrowserPage?culture={CultureInfo.CurrentUICulture.Name}").ToString();
                ctx.Redirect(url);

                Application.OpenForms[0]!.Invoke(() =>
                {
                    Application.OpenForms[0]!.Activate();
                });
            }))
            .WithModule(new ActionModule("/external-js-runner.html", HttpVerbs.Get, async ctx =>
            {
                try
                {
                    ctx.Response.ContentType = "text/html";
                    await using var fileStream = File.OpenRead("wwwroot/external-js-runner.html");
                    await fileStream.CopyToAsync(ctx.Response.OutputStream, ctx.CancellationToken);
                }
                catch (Exception exp)
                {
                    exceptionHandler.Handle(exp);
                }
            }))
            .WithModule(new ActionModule("/app.js", HttpVerbs.Get, async ctx =>
            {
                try
                {
                    ctx.Response.ContentType = "application/javascript";
                    var filePath = Path.Combine(AppContext.BaseDirectory, @"wwwroot\_content\Boilerplate.Client.Core\scripts\app.js");
                    if (File.Exists(filePath) is false)
                    {
                        filePath = Path.Combine(AppContext.BaseDirectory, @"..\..\..\..", @"Boilerplate.Client.Core\wwwroot\scripts\app.js");
                    }
                    await using var fileStream = File.OpenRead(filePath);
                    await fileStream.CopyToAsync(ctx.Response.OutputStream, ctx.CancellationToken);
                }
                catch (Exception exp)
                {
                    exceptionHandler.Handle(exp);
                }
            }))
            .WithModule(new WindowsExternalJsRunner());

        localHttpServer.HandleHttpException(async (context, exception) =>
        {
            exceptionHandler.Handle(new HttpRequestException(exception.Message), parameters: new Dictionary<string, object?>()
            {
                { "StatusCode" , exception.StatusCode },
                { "RequestUri" , context.Request.Url },
            });
        });

        _ = localHttpServer.RunAsync()
            .ContinueWith(task =>
            {
                if (task.Exception is not null)
                {
                    exceptionHandler.Handle(task.Exception);
                }
            });

        return port;
    }

    public async ValueTask DisposeAsync()
    {
        localHttpServer?.Dispose();
    }

    private int GetAvailableTcpPort()
    {
        using TcpListener l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }
}
