namespace Boilerplate.Client.Core.Services;

public partial class NoOpLocalHttpServer : ILocalHttpServer
{
    public int EnsureStarted() => -1;

    public string Origin => $"http://localhost:{Port}";

    public int Port => -1;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
