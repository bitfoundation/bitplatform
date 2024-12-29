using EmbedIO;
using System.Net;
using System.Net.Sockets;
using EmbedIO.Actions;
using Boilerplate.Client.Core.Components;

namespace Boilerplate.Client.Maui.Services;

/// <summary>
/// <inheritdoc cref="ILocalHttpServer"/>
/// </summary>
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
                    bool autoCloseSocialSignedInPage = AppPlatform.IsWindows || AppPlatform.IsMacOS;
                    // The social sign-in process uses an external browser on Windows and macOS, and an in-app browser on Android and iOS (See MauiExternalNavigationService.cs)
                    // Upon completion, the browser is automatically closed via JavaScript (`window.close()`) in SocialSignedInPage.razor. 
                    // However, this method might not always work, although the impact on the user experience is minimal.
                    // For Android and iOS, where the in-app browser is used, a more reliable C# approach is necessary to ensure the in-app browser closes properly.
                    // To achieve this, we set autoClose=false to prevent SocialSignedInPage.razor from attempting to close it and instead use the following C# code to close the browser forcefully.

                    var url = new Uri(absoluteServerAddress, $"/api/Identity/SocialSignedIn?culture={CultureInfo.CurrentUICulture.Name}&autoClose={autoCloseSocialSignedInPage.ToString().ToLowerInvariant()}").ToString();

                    ctx.Redirect(url);

                    if (autoCloseSocialSignedInPage is false)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
#if Android
                            Microsoft.Maui.ApplicationModel.Platform.CurrentActivity.OnBackPressed();
#elif iOS
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
}
