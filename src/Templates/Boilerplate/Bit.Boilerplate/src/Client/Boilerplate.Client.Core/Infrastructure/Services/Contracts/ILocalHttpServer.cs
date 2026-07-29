namespace Boilerplate.Client.Core.Infrastructure.Services.Contracts;

// Checkout Client.web/wwwroot/web-interop-app.html's comments.
public interface ILocalHttpServer : IAsyncDisposable
{
    int EnsureStarted();

    /// <summary>
    /// Random per-process token that every request to the local server must carry. The local http server is bound to
    /// loopback with no authentication of its own in Windows and MAUI implementations, and a POST with no custom headers is a CORS "simple request",
    /// so without this any web page the user visits could drive the app's own endpoints once it guessed the port.
    /// </summary>
    string SessionToken { get; }

    int Port { get; }

    string? Origin { get; }
}
