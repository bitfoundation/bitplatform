namespace Boilerplate.Client.Core.Services;

public partial class NoOpLocalHttpServer : ILocalHttpServer
{
    public int Start(CancellationToken cancellationToken) => -1;

    public string Origin => $"http://localhost:{Port}";

    public int Port => -1;

    /// <summary>
    /// <inheritdoc cref="ILocalHttpServer.ShouldUseForSocialSignIn"/>
    /// </summary>
    public bool ShouldUseForSocialSignIn() => false;
}
