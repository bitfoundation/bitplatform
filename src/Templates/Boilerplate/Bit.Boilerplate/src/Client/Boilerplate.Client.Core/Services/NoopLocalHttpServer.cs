namespace Boilerplate.Client.Core.Services;

public partial class NoopLocalHttpServer : ILocalHttpServer
{
    public int Start(CancellationToken cancellationToken) => throw new NotImplementedException();

    /// <summary>
    /// <inheritdoc cref="ILocalHttpServer.UseLocalHttpServerForSocialSignIn"/>
    /// </summary>
    public bool UseLocalHttpServerForSocialSignIn() => false;
}
