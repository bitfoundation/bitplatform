
namespace Boilerplate.Client.Core.Services;

public class NoopLocalHttpServer : ILocalHttpServer
{
    public int Port { get; } => -1;

    public Task Start() => Task.CompletedTask;
}
