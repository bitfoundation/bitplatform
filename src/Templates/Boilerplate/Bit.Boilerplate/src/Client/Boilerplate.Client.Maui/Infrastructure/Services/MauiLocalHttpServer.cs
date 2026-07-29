using System.Net;
using System.Text;
using System.Net.Sockets;
using EmbedIO;
using EmbedIO.Actions;

namespace Boilerplate.Client.Maui.Infrastructure.Services;

// Checkout Client.web/wwwroot/web-interop-app.html's comments.
public partial class MauiLocalHttpServer : ILocalHttpServer
{
    [AutoInject] private PubSubService pubSubService;
    [AutoInject] private ClientExceptionHandlerBase exceptionHandler;

    public MauiWebAuthnService? WebAuthnService { get; set; }

    private int port = -1;
    private WebServer? localHttpServer;

    public int Port => port;

    public string Origin => $"http://localhost:{port}";

    public string SessionToken { get; } = Guid.NewGuid().ToString("N");

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
            .WithModule(new ActionModule("/api/ExternalSignInCallback", HttpVerbs.Post, async ctx =>
            {
                // This endpoint cannot carry the session token - the value arrives via a redirect the identity
                // server builds, so it would have to survive a round trip through a public API. It is guarded by
                // what it publishes instead: only a RELATIVE url is accepted. A POST with no custom headers is a
                // CORS "simple request", so any page the user has open could reach this once it guessed the port;
                // restricting the payload to a relative path is what stops it injecting an absolute sign-in url
                // carrying an attacker's email/otp, which SignInPanel would then act on.
                if (IsRelativeUrl(ctx.Request.QueryString["urlToOpen"]) is false)
                {
                    ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }

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
                if (IsAuthorized(ctx) is false) return;
                if (WebAuthnService?.GetWebAuthnCredentialOptions is null) { ctx.Response.StatusCode = (int)HttpStatusCode.Conflict; return; }

                await ctx.SendStringAsync(JsonSerializer.Serialize(WebAuthnService.GetWebAuthnCredentialOptions), "application/json", Encoding.UTF8);
            }))
            .WithModule(new ActionModule("/api/WebAuthnCredential", HttpVerbs.Post, async ctx =>
            {
                if (IsAuthorized(ctx) is false) return;
                if (WebAuthnService?.GetWebAuthnCredentialTcs is null) { ctx.Response.StatusCode = (int)HttpStatusCode.Conflict; return; }

                try
                {
                    var error = ctx.Request.QueryString["error"];
                    if (string.IsNullOrEmpty(error) is false)
                    {
                        // TrySetException/TrySetResult: a replayed POST must be a no-op, not an InvalidOperationException.
                        WebAuthnService.GetWebAuthnCredentialTcs.TrySetException(new UnknownException(error));
                    }
                    else
                    {
                        WebAuthnService.GetWebAuthnCredentialTcs.TrySetResult(JsonSerializer.Deserialize<JsonElement>(await ctx.GetRequestBodyAsStringAsync())!);
                    }
                }
                finally
                {
                    await GoBackToApp();
                }
            }))
            .WithModule(new ActionModule("/api/GetCreateWebAuthnCredentialOptions", HttpVerbs.Get, async ctx =>
            {
                if (IsAuthorized(ctx) is false) return;
                if (WebAuthnService?.CreateWebAuthnCredentialOptions is null) { ctx.Response.StatusCode = (int)HttpStatusCode.Conflict; return; }

                await ctx.SendStringAsync(JsonSerializer.Serialize(WebAuthnService.CreateWebAuthnCredentialOptions), "application/json", Encoding.UTF8);
            }))
            .WithModule(new ActionModule("/api/CreateWebAuthnCredential", HttpVerbs.Post, async ctx =>
            {
                if (IsAuthorized(ctx) is false) return;
                if (WebAuthnService?.CreateWebAuthnCredentialTcs is null) { ctx.Response.StatusCode = (int)HttpStatusCode.Conflict; return; }

                try
                {
                    var error = ctx.Request.QueryString["error"];
                    if (string.IsNullOrEmpty(error) is false)
                    {
                        WebAuthnService.CreateWebAuthnCredentialTcs.TrySetException(new UnknownException(error));
                    }
                    else
                    {
                        WebAuthnService.CreateWebAuthnCredentialTcs.TrySetResult(JsonSerializer.Deserialize<JsonElement>(await ctx.GetRequestBodyAsStringAsync())!);
                    }
                }
                finally
                {
                    await GoBackToApp();
                }
            }))
            .WithModule(new ActionModule("/api/LogError", HttpVerbs.Post, async ctx =>
            {
                // No token: this has to be reachable from the external-sign-in page too, which has none. The worst
                // a caller can do is fault a pending ceremony or raise a non-interrupting toast.
                var exception = new UnknownException(await ctx.GetRequestBodyAsStringAsync());

                var getHandled = WebAuthnService?.GetWebAuthnCredentialTcs?.TrySetException(exception) is true;
                var createHandled = WebAuthnService?.CreateWebAuthnCredentialTcs?.TrySetException(exception) is true;

                if (getHandled is false && createHandled is false)
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
                catch { /* Android's AssetManager.Open throws for every miss rather than returning null; the null check below turns that into a 404. */ }
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

    /// <summary>
    /// The WebAuthn endpoints must carry the per-process token that only the app itself puts in the interop URL.
    /// A POST with no custom headers is a CORS "simple request" - no preflight, so CORS never applies - and any
    /// page the user has open could otherwise drive these endpoints once it guessed the loopback port.
    /// </summary>
    private bool IsAuthorized(IHttpContext ctx)
    {
        if (string.Equals(ctx.Request.QueryString["token"], SessionToken, StringComparison.Ordinal))
            return true;

        ctx.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        return false;
    }

    private static bool IsRelativeUrl(string? url)
        => string.IsNullOrEmpty(url) is false && Uri.IsWellFormedUriString(url, UriKind.Relative);

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
