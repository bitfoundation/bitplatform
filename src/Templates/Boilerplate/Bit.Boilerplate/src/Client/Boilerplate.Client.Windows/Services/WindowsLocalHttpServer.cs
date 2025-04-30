using EmbedIO;
using System.Net;
using EmbedIO.Actions;
using System.Net.Sockets;
using Boilerplate.Server.Api.Components;
using Boilerplate.Client.Core.Components;
using Fido2NetLib;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Client.Windows.Services;

/// <summary>
/// <inheritdoc cref="ILocalHttpServer"/>
/// </summary>
public partial class WindowsLocalHttpServer : ILocalHttpServer
{
    [AutoInject] private HtmlRenderer htmlRenderer;
    [AutoInject] private IExceptionHandler exceptionHandler;

    public WindowsWebAuthnService? WebAuthnService { get; set; }

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
                    var error = ctx.Request.QueryString["error"];
                    if (string.IsNullOrEmpty(error) is false)
                    {
                        WebAuthnService!.GetWebAuthnCredentialTcs!.SetException(new UnknownException(error));
                    }
                    else
                    {
                        WebAuthnService!.GetWebAuthnCredentialTcs!.SetResult(JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(await ctx.GetRequestBodyAsStringAsync())!);
                    }
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
                    var error = ctx.Request.QueryString["error"];
                    if (string.IsNullOrEmpty(error) is false)
                    {
                        WebAuthnService!.CreateWebAuthnCredentialTcs!.SetException(new UnknownException(error));
                    }
                    else
                    {
                        WebAuthnService!.CreateWebAuthnCredentialTcs!.SetResult(JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(await ctx.GetRequestBodyAsStringAsync())!);
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
                    var filePath = Path.Combine(AppContext.BaseDirectory, @"wwwroot\_content\Boilerplate.Client.Core\scripts\app.js");
                    if (File.Exists(filePath) is false)
                    {
                        filePath = Path.Combine(AppContext.BaseDirectory, @"..\..\..\..", @"Boilerplate.Client.Core\wwwroot\scripts\app.js");
                    }
                    await using var fileStream = File.OpenRead(filePath);
                    await fileStream.CopyToAsync(ctx.Response.OutputStream, ctx.CancellationToken);
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
