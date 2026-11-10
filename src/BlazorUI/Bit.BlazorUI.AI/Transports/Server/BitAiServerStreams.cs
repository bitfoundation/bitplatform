using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI;

/// <summary>
/// What the server ends of the transports share: a reply that fails on the server ends in a terminal
/// <see cref="BitAiErrorEvent"/> the browser can show and retry, rather than in a broken connection.
/// </summary>
internal static class BitAiServerStreams
{
    /// <summary>
    /// The events of <paramref name="source"/>, and a terminal error event in place of any exception it throws other than
    /// its cancellation. The exception is logged; only a generic message reaches the browser, since an exception's text
    /// can hold what a server must not show (a connection string, a provider's raw response).
    /// </summary>
    internal static async IAsyncEnumerable<BitAiEvent> Guard(
        IAsyncEnumerable<BitAiEvent> source,
        ILogger? logger,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var enumerator = source.GetAsyncEnumerator(cancellationToken);
        try
        {
            while (true)
            {
                BitAiEvent? current = null;
                BitAiErrorEvent? failure = null;

                try
                {
                    if (!await enumerator.MoveNextAsync().ConfigureAwait(false)) break;
                    current = enumerator.Current;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger?.LogError(exception, "The AI reply failed on the server.");
                    failure = new BitAiErrorEvent(new BitAiError("The reply could not be generated.")
                    {
                        Code = "server_error",
                        Kind = exception is TimeoutException ? BitAiErrorKind.Timeout : BitAiErrorKind.Provider,
                        IsRetryable = true,
                    });
                }

                if (failure is not null)
                {
                    yield return failure;
                    break;
                }

                if (current is not null) yield return current;
            }
        }
        finally
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
        }
    }
}
