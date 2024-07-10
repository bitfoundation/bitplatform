using System.Net;
using System.Net.Sockets;
using EmbedIO;
using EmbedIO.Actions;
using Boilerplate.Client.Core;

namespace Boilerplate.Client.Windows.Services;

public partial class WindowsLocalHttpServer : ILocalHttpServer
{
    [AutoInject] private IConfiguration configuration;
    [AutoInject] private IExceptionHandler exceptionHandler;

    private WebServer? localHttpServer;

    public int Start(CancellationToken cancellationToken)
    {
        var port = GetAvailableTcpPort();

        localHttpServer = new WebServer(o => o
            .WithUrlPrefix($"http://localhost:{port}")
            .WithMode(HttpListenerMode.Microsoft))
            .WithModule(new ActionModule("/sign-in", HttpVerbs.Get, async ctx =>
            {
                var url = $"{configuration.GetServerAddress()}/api/Identity/SocialSignedIn?culture={CultureInfo.CurrentUICulture.Name}";

                ctx.Redirect(url);
                ctx.Response.Close();

                await Routes.OpenUniversalLink(ctx.Request.Url.PathAndQuery, replace: true);
            }));

        _ = localHttpServer.RunAsync(cancellationToken)
            .ContinueWith(task =>
            {
                if (task.Exception is not null)
                {
                    exceptionHandler.Handle(task.Exception);
                }
            }, cancellationToken);

        return port;
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
