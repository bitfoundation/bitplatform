using EmbedIO;
using System.Net;
using System.Text;
using System.Reflection;
using EmbedIO.Actions;
using System.Net.Sockets;
using Boilerplate.Client.Core.Components;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;

namespace Boilerplate.Client.Windows.Services;

// Checkout HybridAppWebInterop.razor's comments.
public partial class WindowsLocalHttpServer : ILocalHttpServer
{
    [AutoInject] private HtmlRenderer htmlRenderer;
    [AutoInject] private IExceptionHandler exceptionHandler;

    public WindowsWebAuthnService? WebAuthnService { get; set; }

    private int port = -1;
    private WebServer? localHttpServer;
    private IFileProvider? fileProvider;

    public int Port => port;

    public string Origin => $"http://localhost:{port}";

    public int EnsureStarted()
    {
        if (port != -1)
            return port;

        port = GetAvailableTcpPort();

        fileProvider = GetFileProvider();

        async Task GoBackToApp()
        {
            await Application.OpenForms[0]!.InvokeAsync(async (_) =>
            {
                Application.OpenForms[0]!.Activate();
            });
        }

        localHttpServer = new WebServer(o => o
            .WithUrlPrefix($"http://localhost:{port}")
            .WithMode(HttpListenerMode.Microsoft))
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
                await ctx.SendStringAsync(JsonSerializer.Serialize(WebAuthnService!.GetWebAuthnCredentialOptions!), "application/json", Encoding.UTF8);
            }))
            .WithModule(new ActionModule("/api/WebAuthnCredential", HttpVerbs.Post, async ctx =>
            {
                try
                {
                    WebAuthnService!.GetWebAuthnCredentialTcs!.SetResult(JsonSerializer.Deserialize<JsonElement>(await ctx.GetRequestBodyAsStringAsync())!);
                }
                finally
                {
                    await GoBackToApp();
                }
            }))
            .WithModule(new ActionModule("/api/GetCreateWebAuthnCredentialOptions", HttpVerbs.Get, async ctx =>
            {
                await ctx.SendStringAsync(JsonSerializer.Serialize(WebAuthnService!.CreateWebAuthnCredentialOptions!), "application/json", Encoding.UTF8);
            }))
            .WithModule(new ActionModule("/api/CreateWebAuthnCredential", HttpVerbs.Post, async ctx =>
            {
                try
                {
                    WebAuthnService!.CreateWebAuthnCredentialTcs!.SetResult(JsonSerializer.Deserialize<JsonElement>(await ctx.GetRequestBodyAsStringAsync())!);
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
            }))
            .WithModule(new ActionModule("/web-interop", HttpVerbs.Get, async ctx =>
            {
                var html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
                    (await htmlRenderer.RenderComponentAsync<HybridAppWebInterop>()).ToHtmlString());

                await ctx.SendStringAsync(html, "text/html", Encoding.UTF8);
            }))
            .OnAny(async ctx =>
            {
                try
                {
                    var cntx = (IHttpContextImpl)ctx;
                    var fileInfo = fileProvider.GetFileInfo(ctx.Request.Url.LocalPath);
                    if (File.Exists(fileInfo?.PhysicalPath) is false)
                    {
                        ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return;
                    }
                    ctx.Response.ContentType = ctx.GetMimeType(Path.GetExtension(fileInfo.PhysicalPath));
                    await using var fileStream = File.OpenRead(fileInfo.PhysicalPath);
                    await fileStream.CopyToAsync(ctx.Response.OutputStream, ctx.CancellationToken);
                }
                catch (Exception exp)
                {
                    exceptionHandler.Handle(exp);
                }
            });

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

    private IFileProvider GetFileProvider()
    {
        var blazorWebView = Application.OpenForms[0]!.Controls.OfType<BlazorWebView>().Single();

        var webViewManager = (WebViewManager)blazorWebView.GetType()
            .GetField("_webviewManager", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(blazorWebView)!;

        var staticContentProvider = typeof(WebViewManager).GetField("_staticContentProvider", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(webViewManager)!;

        return (IFileProvider)staticContentProvider
            .GetType().GetField("_fileProvider", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(staticContentProvider)!;
    }
}
