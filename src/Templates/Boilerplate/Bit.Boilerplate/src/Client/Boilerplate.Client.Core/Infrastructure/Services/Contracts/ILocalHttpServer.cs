namespace Boilerplate.Client.Core.Infrastructure.Services.Contracts;

// Checkout Client.web/wwwroot/web-interop-app.html's comments.
public interface ILocalHttpServer : IAsyncDisposable
{
    int EnsureStarted();

    int Port { get; }

    string? Origin { get; }
}
