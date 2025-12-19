using System.Net;
using System.Text;
using System.Net.Sockets;
using EmbedIO;
using EmbedIO.Actions;
using Boilerplate.Client.Core.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Client.Maui.Services;

// Checkout Client.web/wwwroot/web-interop-app.html's comments.
public partial class MauiLocalHttpServer : ILocalHttpServer
{
    [AutoInject] private PubSubService pubSubService;
    [AutoInject] private IExceptionHandler exceptionHandler;

    public MauiWebAuthnService? WebAuthnService { get; set; }

    private int port = -1;
    private WebServer? localHttpServer;

    public int Port => port;

    public string Origin => $"http://localhost:{port}";

    public int EnsureStarted()
    {
        if (localHttpServer?.State is WebServerState.Listening or WebServerState.Loading)
            return port is -1 ? throw new InvalidOperationException() : port;

        localHttpServer?.Dispose();

        port = GetAvailableTcpPort();

        var staticFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.*", SearchOption.AllDirectories);

        async Task GoBackToApp()
        {
            if (AppPlatform.IsIos)
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
            .WithModule(new ActionModule("/api/ExternalSignInCallback", HttpVerbs.Post, async ctx =>
            {
                try
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        var urlToOpen = ctx.Request.QueryString["urlToOpen"];
                        pubSubService.Publish(ClientAppMessages.EXTERNAL_SIGN_IN_CALLBACK, urlToOpen);
                    });
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
                        WebAuthnService!.GetWebAuthnCredentialTcs!.SetResult(JsonSerializer.Deserialize<JsonElement>(await ctx.GetRequestBodyAsStringAsync())!);
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

                        WebAuthnService!.CreateWebAuthnCredentialTcs!.SetResult(JsonSerializer.Deserialize<JsonElement>(await ctx.GetRequestBodyAsStringAsync())!);
                    }
                }
                finally
                {
                    await GoBackToApp();
                }
            }))
            .WithModule(new ActionModule("/api/LogError", HttpVerbs.Post, async ctx =>
            {
                var exception = new UnknownException(await ctx.GetRequestBodyAsStringAsync());

                var handled = WebAuthnService?.GetWebAuthnCredentialTcs?.TrySetException(exception) ??
                    WebAuthnService?.CreateWebAuthnCredentialTcs?.TrySetException(exception);

                if (handled is not true)
                {
                    exceptionHandler.Handle(exception, displayKind: ExceptionDisplayKind.NonInterrupting);
                }

                await GoBackToApp();
            }))
            .OnAny(async ctx =>
            {
                var ctxImplementation = (IHttpContextImpl)ctx;
                var requestFilePath = ctxImplementation.Request.Url.LocalPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                Stream? staticFileStream = null;
                if (staticFiles.FirstOrDefault(f => f.EndsWith(requestFilePath, StringComparison.OrdinalIgnoreCase)) is string staticFilePath)
                {
                    staticFileStream = File.OpenRead(staticFilePath);
                }
#if Android
                try
                {
                    staticFileStream ??= Platform.AppContext.Assets!.Open(Path.Combine("wwwroot", requestFilePath), Android.Content.Res.Access.Streaming);
                }
                catch { }
#endif
                if (staticFileStream is null)
                {
                    ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }
                ctx.Response.ContentType = ctx.GetMimeType(Path.GetExtension(requestFilePath!));
                ctx.Response.Headers["Cache-Control"] = "no-cache, max-age=0, must-revalidate, no-store";
                await using (staticFileStream)
                    await staticFileStream.CopyToAsync(ctx.Response.OutputStream, ctx.CancellationToken);
            });

        localHttpServer.HandleHttpException(async (context, exception) =>
        {
            exceptionHandler.Handle(new HttpRequestException(exception.Message), parameters: new()
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
