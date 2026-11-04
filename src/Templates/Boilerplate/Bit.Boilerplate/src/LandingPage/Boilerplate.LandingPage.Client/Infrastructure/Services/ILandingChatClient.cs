namespace Boilerplate.LandingPage.Client.Infrastructure.Services;

public interface ILandingChatClient
{
    IAsyncEnumerable<string> AskAsync(LandingChatAsk ask, CancellationToken cancellationToken);
}

public sealed class LandingChatException(string message, bool retryable) : Exception(message)
{
    public bool Retryable { get; } = retryable;
}
