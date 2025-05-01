using EmbedIO;
using System.Net;
using System.Text;
using EmbedIO.Actions;
using System.Reflection;
using System.Net.Sockets;
using Microsoft.AspNetCore.Components;
using Boilerplate.Server.Api.Components;
using Boilerplate.Client.Core.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Client.Maui.Services;

public partial class MauiLocalHttpServer : ILocalHttpServer
{
    [AutoInject] private HtmlRenderer htmlRenderer;
    [AutoInject] private IExceptionHandler exceptionHandler;

    public MauiWebAuthnService? WebAuthnService { get; set; }

    private int port = -1;
    private WebServer? localHttpServer;

    public int Port => port;

    public string Origin => $"http://localhost:{port}";

    public int EnsureStarted()
    {
        if (port != -1)
            return port;

        port = GetAvailableTcpPort();

        async Task GoBackToApp()
        {
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
        }

        localHttpServer = new WebServer(o => o
            .WithUrlPrefix($"http://localhost:{port}")
            .WithMode(AppPlatform.IsWindows ? HttpListenerMode.Microsoft : HttpListenerMode.EmbedIO))
            .WithCors()
            .WithModule(new ActionModule("/api/SocialSignInCallback", HttpVerbs.Post, async ctx =>
            {
                try
                {
                    var urlToOpen = ctx.Request.QueryString["urlToOpen"];
                    await Routes.OpenUniversalLink(urlToOpen!, replace: true);
                }
                finally
                {
                    await GoBackToApp();
                }
            }))
            .WithModule(new ActionModule("/api/GetWebAuthnCredentialOptions", HttpVerbs.Get, async ctx =>
            {
                await ctx.SendStringAsync(JsonSerializer.Serialize(WebAuthnService!.GetWebAuthnCredentialOptions), "application/json", Encoding.UTF8);
            }))
            .WithModule(new ActionModule("/api/WebAuthnCredential", HttpVerbs.Post, async ctx =>
            {
                try
                {
                    var error = ctx.Request.QueryString["error"];
                    if (string.IsNullOrEmpty(error) is false)
                    {
                        WebAuthnService!.GetWebAuthnCredentialTcs!.SetException(new UnknownException(error));
                    }
                    else
                    {
                        WebAuthnService!.GetWebAuthnCredentialTcs!.SetResult(JsonSerializer.Deserialize<object>(await ctx.GetRequestBodyAsStringAsync())!);
                    }
                }
                finally
                {
                    await GoBackToApp();
                }
            }))
            .WithModule(new ActionModule("/api/GetCreateWebAuthnCredentialOptions", HttpVerbs.Get, async ctx =>
            {
                await ctx.SendStringAsync(JsonSerializer.Serialize(WebAuthnService!.CreateWebAuthnCredentialOptions), "application/json", Encoding.UTF8);
            }))
            .WithModule(new ActionModule("/api/CreateWebAuthnCredential", HttpVerbs.Post, async ctx =>
            {
                try
                {
                    var error = ctx.Request.QueryString["error"];
                    if (string.IsNullOrEmpty(error) is false)
                    {
                        WebAuthnService!.CreateWebAuthnCredentialTcs!.SetException(new UnknownException(error));
                    }
                    else
                    {

                        WebAuthnService!.CreateWebAuthnCredentialTcs!.SetResult(JsonSerializer.Deserialize<object>(await ctx.GetRequestBodyAsStringAsync())!);
                    }
                }
                finally
                {
                    await GoBackToApp();
                }
            }))
            .WithModule(new ActionModule("/web-interop", HttpVerbs.Get, async ctx =>
            {
                var appJsUrl = "app.js";

                var html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
                    (await htmlRenderer.RenderComponentAsync<HybridAppWebInteropPage>(ParameterView.FromDictionary(new Dictionary<string, object?>
                    {
                        { nameof(HybridAppWebInteropPage.AppJsUrl), appJsUrl }
                    }))).ToHtmlString());

                await ctx.SendStringAsync(html, "text/html", Encoding.UTF8);
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
            }));

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
