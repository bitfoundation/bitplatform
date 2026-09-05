using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Bit.Butil.Demo.Server.Endpoints;

namespace Bit.Butil.Tests.E2E.Infrastructure;

/// <summary>
/// A WebSocket echo endpoint for the duration of the test session, hosted in the test process.
/// </summary>
/// <remarks>
/// The app under test is <c>Bit.Butil.Samples.Web</c> - a standalone WebAssembly host with no server
/// side to add an endpoint to - and pointing the suite at a public echo service would make it need
/// the internet and someone else's uptime. So the suite brings its own: a few lines of ASP.NET Core
/// on a loopback port, whose URL the harness page is handed through the query string.
/// <br/>
/// The protocol itself is <see cref="WebSocketEcho"/>, the very handler the demo server maps at
/// <c>/ws/echo</c>, compiled into this project through a linked source file. The harness asserts on
/// that protocol - text comes back prefixed, binary comes back with every byte incremented, and the
/// literal message <c>close</c> closes with code 4001 - so it has to be the one the demo ships
/// rather than a copy of it that can drift.
/// </remarks>
public static class WebSocketEchoFixture
{
    private static WebApplication? _app;

    /// <summary>The <c>ws://</c> URL of the echo endpoint. Empty until <see cref="Start"/> has run.</summary>
    public static string Url { get; private set; } = string.Empty;

    public static async Task Start()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        // Port 0 rather than a port picked in advance: probing for a free port and then binding it is
        // two steps with a gap in between, and anything else on the machine can take the port during
        // that gap. Kestrel binds first and reports what it got, so there is no gap to lose.
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        var app = builder.Build();
        app.UseWebSockets();
        app.MapGet("/echo", WebSocketEcho.Handle);

        await app.StartAsync();

        _app = app;

        var address = app.Urls.First();
        Url = $"ws://{new Uri(address).Authority}/echo";
    }

    public static async Task Stop()
    {
        if (_app is null) return;
        try
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _app.StopAsync(deadline.Token);
        }
        catch { /* best-effort cleanup */ }
        finally
        {
            await _app.DisposeAsync();
            _app = null;
        }
    }
}
