
namespace Boilerplate.Client.Core.Services;

public class NoopLocalHttpServer : ILocalHttpServer
{
    public int Port => -1;

    public Task Start() => Task.CompletedTask;
}
