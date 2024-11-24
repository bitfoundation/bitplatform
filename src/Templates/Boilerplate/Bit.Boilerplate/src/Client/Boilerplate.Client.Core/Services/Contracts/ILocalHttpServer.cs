namespace Boilerplate.Client.Core.Services.Contracts;

/// <summary>
/// Social sign-in functions seamlessly on web browsers and on Android and iOS via universal app links. 
/// However, for Windows and macOS, a local HTTP server is needed to ensure a smooth social sign-in experience. 
/// </summary>
public interface ILocalHttpServer
{
    int Start(CancellationToken cancellationToken);
}
