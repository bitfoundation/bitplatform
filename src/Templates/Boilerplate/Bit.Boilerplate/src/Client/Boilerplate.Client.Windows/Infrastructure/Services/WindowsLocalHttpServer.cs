using System.Net;
using System.Text;
using System.Net.Sockets;
using EmbedIO;
using EmbedIO.Actions;

namespace Boilerplate.Client.Windows.Infrastructure.Services;

// Checkout Client.web/wwwroot/web-interop-app.html's comments.
public partial class WindowsLocalHttpServer : ILocalHttpServer
{
    [AutoInject] private PubSubService pubSubService;
    [AutoInject] private IExceptionHandler exceptionHandler;
    [AutoInject] private ClientWindowsSettings clientWindowsSettings;
    [AutoInject] private AbsoluteServerAddressProvider absoluteServerAddressProvider;

    public WindowsWebAuthnService? WebAuthnService { get; set; }

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

        var staticFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.*", SearchOption.AllDirectories)
            .Union(Directory.GetFiles(AppContext.BaseDirectory, "*.*", SearchOption.AllDirectories))
            .Distinct()
            .ToArray();

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
            .WithModule(new ActionModule("/api/ExternalSignInCallback", HttpVerbs.Post, async ctx =>
            {
                try
                {
                    await Application.OpenForms[0]!.InvokeAsync(async (_) =>
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

                await GoBackToApp();
            }))
            .OnAny(async ctx =>
            {
                var ctxImplementation = (IHttpContextImpl)ctx;
                var requestFilePath = ctxImplementation.Request.Url.LocalPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var staticFile = staticFiles.FirstOrDefault(f => f.EndsWith(requestFilePath, StringComparison.OrdinalIgnoreCase));
                if (File.Exists(staticFile) is false)
                {
                    // In development, Blazor employs complex methods to locate files across all installed NuGet packages.
                    // To streamline this, we utilize a web server to serve static files in the development environment.
                    // In production, as all files are deployed to a single folder, we rely on the default file provider.
                    if (AppEnvironment.IsDevelopment())
                    {
                        ctx.Redirect(new Uri(clientWindowsSettings.WebAppUrl ?? absoluteServerAddressProvider.GetAddress(), requestFilePath).ToString());
                    }
                    else
                    {
                        ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    }
                    return;
                }
                ctx.Response.ContentType = ctx.GetMimeType(Path.GetExtension(staticFile!));
                ctx.Response.Headers["Cache-Control"] = "no-cache, max-age=0, must-revalidate, no-store";
                await using var stream = File.OpenRead(staticFile!);
                await stream.CopyToAsync(ctx.Response.OutputStream, ctx.CancellationToken);
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
