
namespace Boilerplate.Client.Core.Services;

public class NoopLocalHttpServer : ILocalHttpServer
{
    public int GetPort() => -1;

    public Task Start() => Task.CompletedTask;
}
