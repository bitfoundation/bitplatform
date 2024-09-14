namespace Boilerplate.Client.Core.Services;

/// <summary>
/// Currently only Windows clients have local http server to have better support for social login.
/// In other platforms, we don't have local http server and we use a noop implementation.
/// </summary>
public partial class NoopLocalHttpServer : ILocalHttpServer
{
    public int Start(CancellationToken cancellationToken) => -1;
}
