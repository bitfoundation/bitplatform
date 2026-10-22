using System.Runtime.CompilerServices;
using Bit.BlazorUI;

namespace Bit.BlazorUI.Tests.Consumer;

/// <summary>
/// The transport the app's chat streams its replies from, written the way an app writes one in C# 12: an async iterator
/// of the AI package's events. It echoes the last thing the user said, a few characters at a time.
/// </summary>
public sealed class EchoTransport : IBitAiTransport
{
    public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var said = request.Messages.LastOrDefault(m => m.Role.IsUser)?.GetText() ?? string.Empty;
        var reply = $"You said: {said}";

        yield return new BitAiMessageStartedEvent();

        for (var i = 0; i < reply.Length; i += 4)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            yield return new BitAiTextDeltaEvent(reply.Substring(i, Math.Min(4, reply.Length - i)));
        }

        yield return new BitAiMessageCompletedEvent();
    }
}
