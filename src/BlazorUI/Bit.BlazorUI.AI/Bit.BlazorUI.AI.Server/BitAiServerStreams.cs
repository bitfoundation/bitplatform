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
    /// The events of the stream <paramref name="source"/> produces, and a terminal error event in place of any exception
    /// it throws other than its cancellation - producing the stream and starting it included, so a handler that is not an
    /// iterator and throws as it is called (a missing service, a check of the request) ends the same way. The exception is
    /// logged; only a generic message reaches the browser, since an exception's text can hold what a server must not show
    /// (a connection string, a provider's raw response).
    /// </summary>
    internal static async IAsyncEnumerable<BitAiEvent> Guard(
        Func<IAsyncEnumerable<BitAiEvent>> source,
        ILogger? logger,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IAsyncEnumerator<BitAiEvent>? enumerator = null;
        try
        {
            while (true)
            {
                BitAiEvent? current = null;
                BitAiErrorEvent? failure = null;

                try
                {
                    enumerator ??= source().GetAsyncEnumerator(cancellationToken);
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
                    failure = Failure(exception);
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
            if (enumerator is not null) await enumerator.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Writes an event for the wire, or - when the wire format cannot write it (a <see cref="BitAiCustomEvent"/> holding
    /// an app type the settings have no metadata for, an event type the format does not define) - logs why and writes
    /// the terminal error event that ends the reply in its place, as <see cref="Guard"/> does for a failing handler.
    /// </summary>
    /// <returns><see langword="true"/> when the event itself was written; <see langword="false"/> when the error was, and the reply ends.</returns>
    internal static bool TryWrite<T>(BitAiEvent evt, Func<BitAiEvent, T> write, ILogger? logger, out T written)
    {
        try
        {
            written = write(evt);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger?.LogError(exception, "An AI event of type '{Type}' could not be written in the wire format.", evt.Type);
            written = write(Failure(exception));
            return false;
        }
    }

    // Only a generic message reaches the browser; the exception itself is logged where it is caught.
    private static BitAiErrorEvent Failure(Exception exception) => new(new BitAiError("The reply could not be generated.")
    {
        Code = "server_error",
        Kind = exception is TimeoutException ? BitAiErrorKind.Timeout : BitAiErrorKind.Provider,
        IsRetryable = true,
    });
}

/// <summary>
/// The calls one reply asked the browser about, told to the app's <see cref="BitAiApprovalBroker"/> (when one is
/// registered) before each <see cref="BitAiToolApprovalRequestedEvent"/> is sent, and forgotten when the reply ends.
/// </summary>
internal sealed class BitAiAskedCalls(BitAiApprovalBroker? broker)
{
    private readonly List<string> _asked = [];

    /// <summary>Records the call of an approval request, before the event is sent.</summary>
    public void Record(BitAiEvent evt)
    {
        if (evt is not BitAiToolApprovalRequestedEvent approval) return;

        broker?.Expect(approval.ToolCallId);
        lock (_asked) _asked.Add(approval.ToolCallId);
    }

    /// <summary>Lets the broker forget every call of the reply that nothing is waiting on any more.</summary>
    public void Forget()
    {
        if (broker is null) return;

        string[] asked;
        lock (_asked) asked = [.. _asked];
        foreach (var toolCallId in asked) broker.Forget(toolCallId);
    }
}
