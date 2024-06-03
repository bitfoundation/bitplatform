namespace Boilerplate.Client.Core.Services;

/// <summary>
/// Currently only Windows clients have local http server to have better support for social login.
/// In other platforms, we don't have local http server and we use a noop implementation.
/// </summary>
public class NoopLocalHttpServer : ILocalHttpServer
{
    public int Port => -1;

    public Task<int> Start() => Task.FromResult(Port);
}
