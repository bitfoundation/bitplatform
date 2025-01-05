using EmbedIO;
using System.Net;
using System.Net.Sockets;
using EmbedIO.Actions;
using Boilerplate.Client.Core.Components;

namespace Boilerplate.Client.Windows.Services;

/// <summary>
/// <inheritdoc cref="ILocalHttpServer"/>
/// </summary>
public partial class WindowsLocalHttpServer : ILocalHttpServer
{
    [AutoInject] private IExceptionHandler exceptionHandler;
    [AutoInject] private AbsoluteServerAddressProvider absoluteServerAddress;

    private WebServer? localHttpServer;

    public int Start(CancellationToken cancellationToken)
    {
        var port = GetAvailableTcpPort();

        localHttpServer = new WebServer(o => o
            .WithUrlPrefix($"http://localhost:{port}")
            .WithMode(HttpListenerMode.Microsoft))
            .WithModule(new ActionModule(Urls.SignInPage, HttpVerbs.Get, async ctx =>
            {
                try
                {
                    var url = new Uri(absoluteServerAddress, $"/api/Identity/SocialSignedIn?culture={CultureInfo.CurrentUICulture.Name}").ToString();

                    ctx.Redirect(url);

                    _ = Task.Delay(1)
                        .ContinueWith(async _ =>
                        {
                            Application.OpenForms[0]!.Invoke(() =>
                            {
                                Application.OpenForms[0]!.Activate();
                            });
                            await Routes.OpenUniversalLink(ctx.Request.Url.PathAndQuery, replace: true);
                        });
                }
                catch (Exception exp)
                {
                    exceptionHandler.Handle(exp);
                }
            }));

        localHttpServer.HandleHttpException(async (context, exception) =>
        {
            exceptionHandler.Handle(new HttpRequestException(exception.Message), parameters: new Dictionary<string, object?>()
            {
                { "StatusCode" , exception.StatusCode },
                { "RequestUri" , context.Request.Url },
            });
        });

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

    /// <summary>
    /// <inheritdoc cref="ILocalHttpServer.ShouldUseForSocialSignIn"/>
    /// </summary>

    public bool ShouldUseForSocialSignIn() => true;

    private int GetAvailableTcpPort()
    {
        using TcpListener l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }
}
