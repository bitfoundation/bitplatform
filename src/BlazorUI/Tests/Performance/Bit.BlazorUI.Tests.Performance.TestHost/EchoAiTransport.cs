using System.Runtime.CompilerServices;

namespace Bit.BlazorUI.Tests.Performance.TestHost;

/// <summary>
/// The transport the AI components of the regression pages stream their replies from: "You said: ..." in a few chunks,
/// at once. A page that needs a turn kept in flight - a stop to press, a busy state to read - says so by sending "hang",
/// which streams one sentence and then waits until the turn is stopped.
/// </summary>
public sealed class EchoAiTransport : IBitAiTransport
{
    public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var said = request.Messages.LastOrDefault(m => m.Role.IsUser)?.GetText() ?? string.Empty;
        var reply = $"You said: {said}";

        yield return new BitAiMessageStartedEvent();

        for (var i = 0; i < reply.Length; i += 8)
        {
            yield return new BitAiTextDeltaEvent(reply.Substring(i, Math.Min(8, reply.Length - i)));
        }

        if (said.Trim() is "hang") await Task.Delay(Timeout.Infinite, cancellationToken);

        await Task.Yield();
        yield return new BitAiMessageCompletedEvent();
    }
}
