namespace Boilerplate.Client.Core.Services.Contracts;

public interface ILocalHttpServer : IAsyncDisposable
{
    int EnsureStarted();

    int Port { get; }

    string? Origin { get; }
}
