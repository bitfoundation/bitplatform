using EmbedIO;
using System.Net;
using EmbedIO.Actions;
using System.Net.Sockets;
using Boilerplate.Client.Core.Components;

namespace Boilerplate.Client.Maui.Services;

public partial class MauiLocalHttpServer : ILocalHttpServer
{
    [AutoInject] private IExceptionHandler exceptionHandler;
    [AutoInject] private AbsoluteServerAddressProvider absoluteServerAddress;

    private WebServer? localHttpServer;

    public int Start(CancellationToken cancellationToken)
    {
        var port = GetAvailableTcpPort();

        localHttpServer = new WebServer(o => o
            .WithUrlPrefix($"http://localhost:{port}")
            .WithMode(AppPlatform.IsWindows ? HttpListenerMode.Microsoft : HttpListenerMode.EmbedIO))
            .WithModule(new ActionModule(Urls.SignInPage, HttpVerbs.Get, async ctx =>
            {
                try
                {
                    // Redirect to SocialSignedInPage.razor that will close the browser window.
                    var url = new Uri(absoluteServerAddress, $"/api/Identity/SocialSignedIn?culture={CultureInfo.CurrentUICulture.Name}").ToString();
                    ctx.Redirect(url);

                    if (AppPlatform.IsIOS)
                    {
                        // SocialSignedInPage.razor's `window.close()` does NOT work on iOS's in app browser.
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
#if iOS
                            if (UIKit.UIApplication.SharedApplication.KeyWindow?.RootViewController?.PresentedViewController is SafariServices.SFSafariViewController controller)
                            {
                                controller.DismissViewController(animated: true, completionHandler: null);
                            }
#endif
                        });
                    }

                    _ = Task.Delay(1)
                    .ContinueWith(async _ =>
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await Routes.OpenUniversalLink(ctx.Request.Url.PathAndQuery, replace: true);
                        });
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

    private int GetAvailableTcpPort()
    {
        using TcpListener l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    /// <summary>
    /// <inheritdoc cref="ILocalHttpServer.ShouldUseForSocialSignIn"/>
    /// </summary>
    public bool ShouldUseForSocialSignIn()
    {
        return AppPlatform.IsAndroid is false;
    }
}
