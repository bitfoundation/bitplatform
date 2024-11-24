namespace Boilerplate.Client.Core.Services;

/// <summary>
/// <inheritdoc cref="ILocalHttpServer"/>
/// The <see cref="NoopLocalHttpServer"/> is specifically registered for Android, iOS, and Web, where a local HTTP server is unnecessary.
/// </summary>
public partial class NoopLocalHttpServer : ILocalHttpServer
{
    public int Start(CancellationToken cancellationToken) => -1;
}
