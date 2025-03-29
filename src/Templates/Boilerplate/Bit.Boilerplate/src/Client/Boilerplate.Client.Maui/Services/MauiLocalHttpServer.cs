using EmbedIO;
using System.Net;
using EmbedIO.Actions;
using System.Reflection;
using System.Net.Sockets;
using Boilerplate.Client.Core.Components;

namespace Boilerplate.Client.Maui.Services;

public partial class MauiLocalHttpServer : ILocalHttpServer
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
            .WithMode(AppPlatform.IsWindows ? HttpListenerMode.Microsoft : HttpListenerMode.EmbedIO))
            .WithModule(new ActionModule(Urls.SignInPage, HttpVerbs.Get, async ctx =>
            {
                try
                {
                    ctx.Redirect("/close-browser");

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
            }))
            .WithModule(new ActionModule("/close-browser", HttpVerbs.Get, async ctx =>
            {
                // Redirect to CloseBrowserPage.razor that will close the browser window.
                var url = new Uri(absoluteServerAddress, $"/api/Identity/CloseBrowserPage?culture={CultureInfo.CurrentUICulture.Name}").ToString();
                ctx.Redirect(url);

                if (AppPlatform.IsIOS)
                {
                    // CloseBrowserPage.razor's `window.close()` does NOT work on iOS's in app browser.
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
                else if (AppPlatform.IsAndroid)
                {
#if Android
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        var intent = new Android.Content.Intent(Platform.AppContext, typeof(Platforms.Android.MainActivity));
                        intent.SetFlags(Android.Content.ActivityFlags.NewTask | Android.Content.ActivityFlags.ClearTop);
                        Platform.AppContext.StartActivity(intent);
                    });
#endif
                }
            }))
            .WithModule(new ActionModule("/external-js-runner.html", HttpVerbs.Get, async ctx =>
            {
                try
                {
                    ctx.Response.ContentType = "text/html";
                    await using var file = Assembly.Load("Boilerplate.Client.Maui").GetManifestResourceStream("Boilerplate.Client.Maui.wwwroot.external-js-runner.html")!;
                    await file.CopyToAsync(ctx.Response.OutputStream, ctx.CancellationToken);
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
                    await using var file = Assembly.Load("Boilerplate.Client.Maui").GetManifestResourceStream("Boilerplate.Client.Maui.wwwroot.scripts.app.js")!;
                    await file.CopyToAsync(ctx.Response.OutputStream, ctx.CancellationToken);
                }
                catch (Exception exp)
                {
                    exceptionHandler.Handle(exp);
                }
            }))
            .WithModule(new MauiExternalJsRunner());

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

    private int GetAvailableTcpPort()
    {
        using TcpListener l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    public async ValueTask DisposeAsync()
    {
        localHttpServer?.Dispose();
    }

}
